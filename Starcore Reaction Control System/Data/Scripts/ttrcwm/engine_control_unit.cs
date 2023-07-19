using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
//using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ttrcwm
{
    class engine_control_unit
    {
        #region fields

        const int NUM_ROTATION_SAMPLES = 6, PHYSICS_ENABLE_DELAY = 6;
        const float MAX_THRUST_LEVEL = 0.2f;

        enum thrust_dir { fore = 0, aft = 3, starboard = 1, port = 4, dorsal = 2, ventral = 5 };
        class thruster_info     // Technically a struct
        {
            public float max_force, actual_max_force;
            public Vector3 max_torque, grid_centre_pos, CoM_offset, reference_vector, static_moment;
            public thrust_dir nozzle_direction;
            public float current_setting, prev_setting;
            public bool is_RCS, group_no_RCS, override_cleared;
        };

        private static float[] __control_vector = new float[6];
        private static float[] __actual_force = new float[6];
        private static float[] __linear_component = new float[6];
        //private static float[] __braking_vector    = new float[6];
        private static float[] __thrust_vector = new float[6];
        private static float[] __counter_gravity = new float[6];
        private static float[] __cur_firing_vector = new float[6];
        private static float[] __settings = new float[6];
        private static float[] __linear_velocity = new float[6];
        private static bool[] __rotation_enable = new bool[6];

        private MyCubeGrid _grid;
        private List<IMyBlockGroup> _all_groups = new List<IMyBlockGroup>();
        private List<IMyTerminalBlock> _blocks_in_group = new List<IMyTerminalBlock>();

        private Dictionary<MyThrust, thruster_info>[] _thrusters =
        {
            new Dictionary<MyThrust, thruster_info>(),   // fore
            new Dictionary<MyThrust, thruster_info>(),   // starboard
            new Dictionary<MyThrust, thruster_info>(),   // dorsal
            new Dictionary<MyThrust, thruster_info>(),   // aft
            new Dictionary<MyThrust, thruster_info>(),   // port
            new Dictionary<MyThrust, thruster_info>()    // ventral
        };
        private HashSet<thruster_info>[] _control_sets =
        {
            new HashSet<thruster_info>(),   // roll clockwise
            new HashSet<thruster_info>(),   // pitch down
            new HashSet<thruster_info>(),   // yaw right
            new HashSet<thruster_info>(),   // roll counter-clockwise
            new HashSet<thruster_info>(),   // pitch up
            new HashSet<thruster_info>()    // yaw left
        };
        private float[] _max_force = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        private float[] _lin_force = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

        private HashSet<MyGyro> _gyroscopes = new HashSet<MyGyro>();

        private Vector3D _grid_CoM_location = Vector3D.Zero;
        private MatrixD _inverse_world_transform;
        private float _max_gyro_torque = 0.0f, _spherical_moment_of_inertia;

        private Vector3 _manual_thrust, _manual_rotation, _prev_rotation, _target_rotation, _gyro_override = Vector3.Zero, _local_angular_velocity;
        private bool _is_gyro_override_active = false, _all_engines_off = false, _under_player_control = false, _rotation_active = false, _thruster_added_or_removed = false;
        private bool _force_override_refresh = false;

        private Vector3[] _rotation_samples = new Vector3[NUM_ROTATION_SAMPLES];
        private Vector3 _sample_sum = Vector3.Zero;
        private int _current_index = 0, _physics_enable_delay = PHYSICS_ENABLE_DELAY;

        #endregion

        #region Properties

        public bool autopilot_on { get; set; }
        public bool linear_dampers_on { get; set; }

        #endregion

        #region DEBUG

        private void screen_info(string message, int display_time_ms, MyFontEnum font, bool controlled_only)
        {
            try
            {
                bool display = !controlled_only;

                if (!display)
                {
                    var controller = MyAPIGateway.Session.ControlledObject as MyShipController;
                    if (controller != null)
                        display = controller.CubeGrid == _grid;
                }
                if (display)
                    MyAPIGateway.Utilities.ShowNotification(message, display_time_ms, font);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"An error occurred in screen_info: {e.Message}");
                MyLog.Default.WriteLine(e);
            }
        }


        private void log_ECU_action(string method_name, string message)
        {
            MyLog.Default.WriteLine(string.Format("TTDTWM\tengine_control_unit<{0} [{1}]>.{2}(): {3}", _grid.DisplayName, _grid.EntityId, method_name, message));
            int num_controlled_thrusters = 0;
            foreach (var cur_direction in _thrusters)
                num_controlled_thrusters += cur_direction.Count;
            MyLog.Default.WriteLine(string.Format("TTDTWM\ttotal thrusters: {0} ({1}/{2}/{3}/{4}/{5}/{6} controlled)",
                num_controlled_thrusters,
                _thrusters[(int)thrust_dir.fore].Count,
                _thrusters[(int)thrust_dir.aft].Count,
                _thrusters[(int)thrust_dir.starboard].Count,
                _thrusters[(int)thrust_dir.port].Count,
                _thrusters[(int)thrust_dir.dorsal].Count,
                _thrusters[(int)thrust_dir.ventral].Count));
        }

        private void screen_text(string method_name, string message, int display_time_ms, bool controlled_only)
        {
            if (method_name == "")
                screen_info(string.Format("\"{0}\" {1}", _grid.DisplayName, message), display_time_ms, MyFontEnum.White, controlled_only);
            else
                screen_info(string.Format("engine_control_unit.{0}(): \"{1}\" {2}", method_name, _grid.DisplayName, message), display_time_ms, MyFontEnum.White, controlled_only);
        }

        private void screen_vector<type>(string method_name, string vector_name, type[] vector, int display_time_ms, bool controlled_only)
        {
            screen_text(method_name, string.Format("{0} = {1:F3}/{2:F3}/{3:F3}/{4:F3}/{5:F3}/{6:F3}",
                vector_name,
                vector[(int)thrust_dir.fore],
                vector[(int)thrust_dir.aft],
                vector[(int)thrust_dir.starboard],
                vector[(int)thrust_dir.port],
                vector[(int)thrust_dir.dorsal],
                vector[(int)thrust_dir.ventral]), display_time_ms, controlled_only);
        }

        #endregion

        #region torque calculation

        private void refresh_thruster_info_for_single_direction(Dictionary<MyThrust, thruster_info> thrusters)
        {
            try
            {
                thruster_info cur_thruster_info;

                foreach (var cur_thruster in thrusters)
                {
                    cur_thruster_info = cur_thruster.Value;
                    cur_thruster_info.CoM_offset = cur_thruster_info.grid_centre_pos - _grid_CoM_location;
                    cur_thruster_info.max_torque = Vector3.Cross(cur_thruster_info.CoM_offset, -cur_thruster.Key.ThrustForwardVector * cur_thruster.Key.BlockDefinition.ForceMagnitude);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error refreshing thruster info for single direction: {ex}");
            }
        }

        private void refresh_thruster_info()
        {
            try
            {
                for (int dir_index = 0; dir_index < 6; ++dir_index)
                    refresh_thruster_info_for_single_direction(_thrusters[dir_index]);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error refreshing thruster info: {ex}");
            }
        }


        private void calculate_and_apply_torque()
        {
            try
            {
                const float MIN_ANGULAR_ACCELERATION = (float)(0.1 * Math.PI / 180.0);

                Vector3 torque = Vector3.Zero, useful_torque, parasitic_torque;
                float current_strength;

                foreach (var cur_direction in _thrusters)
                {
                    foreach (var cur_thruster in cur_direction)
                    {
                        current_strength = cur_thruster.Key.CurrentStrength;
                        if (current_strength > 1.0f)
                            current_strength = 1.0f;
                        else if (current_strength < 0.0f)
                            current_strength = 0.0f;
                        torque += cur_thruster.Value.max_torque * current_strength;
                    }
                }

                if (!_rotation_active || autopilot_on)
                {
                    useful_torque = Vector3.Zero;
                    parasitic_torque = torque;
                }
                else
                {
                    float manual_rotation_length2 = _manual_rotation.LengthSquared(), angular_velocity_length2 = _local_angular_velocity.LengthSquared();

                    if (manual_rotation_length2 <= 0.0001f)
                        useful_torque = Vector3.Zero;
                    else
                    {
                        float projection_dot_product = Vector3.Dot(torque, _manual_rotation);

                        useful_torque = (projection_dot_product > 0.0f) ? ((projection_dot_product / manual_rotation_length2) * _manual_rotation) : Vector3.Zero;
                    }
                    Vector3 leftover_torque = torque - useful_torque;
                    if (angular_velocity_length2 > 0.0001f)
                    {
                        float projection_dot_product = Vector3.Dot(leftover_torque, _local_angular_velocity);

                        if (projection_dot_product < 0.0f)
                            useful_torque += (projection_dot_product / angular_velocity_length2) * _local_angular_velocity;
                    }
                    parasitic_torque = torque - useful_torque;
                }

                float gyro_limit = _max_gyro_torque;
                if (gyro_limit < 1.0f)
                    gyro_limit = 1.0f;
                if (parasitic_torque.LengthSquared() <= gyro_limit * gyro_limit)
                    parasitic_torque = Vector3.Zero;
                else
                    parasitic_torque -= Vector3.Normalize(parasitic_torque) * gyro_limit;

                torque = useful_torque + parasitic_torque;
                if (_physics_enable_delay > 0)
                    --_physics_enable_delay;
                else if (torque.LengthSquared() > MIN_ANGULAR_ACCELERATION * MIN_ANGULAR_ACCELERATION * _spherical_moment_of_inertia * _spherical_moment_of_inertia)
                {
                    MyAPIGateway.Parallel.For(0, 1, i =>
                    {
                        torque = Vector3.Transform(torque, _grid.WorldMatrix.GetOrientation());
                        _grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, Vector3.Zero, null, torque);
                    });
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Error in calculate_and_apply_torque()", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine($"Error in calculate_and_apply_torque(): {e}");
            }
        }

        #endregion

        #region thrust control

        private static void decompose_vector(Vector3 source_vector, float[] decomposed_vector)
        {
            try
            {
                decomposed_vector[(int)thrust_dir.fore] = (source_vector.Z > 0.0f) ? (source_vector.Z) : 0.0f;
                decomposed_vector[(int)thrust_dir.aft] = (source_vector.Z < 0.0f) ? (-source_vector.Z) : 0.0f;
                decomposed_vector[(int)thrust_dir.port] = (source_vector.X > 0.0f) ? (source_vector.X) : 0.0f;
                decomposed_vector[(int)thrust_dir.starboard] = (source_vector.X < 0.0f) ? (-source_vector.X) : 0.0f;
                decomposed_vector[(int)thrust_dir.ventral] = (source_vector.Y > 0.0f) ? (source_vector.Y) : 0.0f;
                decomposed_vector[(int)thrust_dir.dorsal] = (source_vector.Y < 0.0f) ? (-source_vector.Y) : 0.0f;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error in decompose_vector: {e.Message}");
                for (int i = 0; i < decomposed_vector.Length; i++)
                    decomposed_vector[i] = 0.0f;
            }
        }

        private static void recompose_vector(float[] decomposed_vector, out Vector3 result_vector)
        {
            try
            {
                result_vector.Z = decomposed_vector[(int)thrust_dir.fore] - decomposed_vector[(int)thrust_dir.aft];
                result_vector.X = decomposed_vector[(int)thrust_dir.port] - decomposed_vector[(int)thrust_dir.starboard];
                result_vector.Y = decomposed_vector[(int)thrust_dir.ventral] - decomposed_vector[(int)thrust_dir.dorsal];
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error in recompose_vector: {e.Message}");
                result_vector = Vector3.Zero;
            }
        }


        private void fill_control_sets(thruster_info cur_thruster_info)
        {
            try
            {
                if (cur_thruster_info.reference_vector.LengthSquared() < _grid.GridSize * _grid.GridSize)
                    return;

                Vector3 sample_vector, reference_norm = Vector3.Normalize(cur_thruster_info.reference_vector);
                for (int dir_index = 0; dir_index < 6; ++dir_index)
                {
                    __cur_firing_vector[dir_index] = 1.0f;
                    recompose_vector(__cur_firing_vector, out sample_vector);
                    decompose_vector(Vector3.Cross(sample_vector, reference_norm), __linear_component);
                    if (__linear_component[(int)cur_thruster_info.nozzle_direction] > 0.0f)
                        _control_sets[dir_index].Add(cur_thruster_info);
                    __cur_firing_vector[dir_index] = 0.0f;
                }
            }
            catch (Exception ex)
            {
                string message = $"An error occurred in fill_control_sets(): {ex.Message}";
                MyAPIGateway.Utilities.ShowNotification(message, 5000, "Red");
                MyAPIGateway.Utilities.InvokeOnGameThread(() => MyLog.Default.WriteLineAndConsole(message));
            }
        }


        private void refresh_control_sets()
        {
            try
            {
                for (int dir_index = 0; dir_index < 6; ++dir_index)
                    _control_sets[dir_index].Clear();
                foreach (var cur_direction in _thrusters)
                {
                    foreach (var cur_thruster_info in cur_direction.Values)
                        fill_control_sets(cur_thruster_info);
                }
            }
            catch (Exception e)
            {
                // Log the error
                MyLog.Default.WriteLine($"An error occurred in the refresh_control_sets method: {e.Message}");
            }
        }


        private void apply_thrust_settings(bool reset_all_thrusters)
        {
            const float MIN_OVERRIDE = 1.001f;
            float setting, setting_ratio;
            int thruster_dir, opposite_dir;
            bool dry_run, reset_thrusters;
            thruster_info cur_thruster_info;

            if (reset_all_thrusters && _all_engines_off && !_force_override_refresh)
                return;

            if (MyAPIGateway.Multiplayer == null || MyAPIGateway.Multiplayer.IsServer)
                dry_run = false;
            else
            {
                bool is_rotation_small = (_manual_rotation - _prev_rotation).LengthSquared() < 0.0001f;

                dry_run = !_force_override_refresh || is_rotation_small;
                if (!is_rotation_small)
                    _prev_rotation = _manual_rotation;
            }

            for (int dir_index = 0; dir_index < 6; ++dir_index)
            {
                reset_thrusters = reset_all_thrusters || !__rotation_enable[dir_index];
                foreach (var cur_thruster in _thrusters[dir_index])
                {
                    cur_thruster_info = cur_thruster.Value;
                    if (cur_thruster_info.group_no_RCS)
                        continue;

                    if (_force_override_refresh)
                        cur_thruster_info.prev_setting = (int)Math.Ceiling(cur_thruster.Key.CurrentStrength * /*100.0f*/ cur_thruster_info.max_force);
                    if (reset_thrusters || !cur_thruster_info.is_RCS || cur_thruster_info.actual_max_force < 1.0f || !cur_thruster.Key.IsWorking)
                    {
                        if (cur_thruster_info.prev_setting != 0)
                        {
                            if (!dry_run)
                                cur_thruster.Key.SetValueFloat("Override", 0.0f);
                            cur_thruster_info.current_setting = cur_thruster_info.prev_setting = 0;
                        }
                        continue;
                    }

                    setting = cur_thruster_info.current_setting * /*100.0f*/ cur_thruster_info.max_force;
                    thruster_dir = (int)cur_thruster_info.nozzle_direction;
                    opposite_dir = (thruster_dir < 3) ? (thruster_dir + 3) : (thruster_dir - 3);
                    if (_rotation_active && setting < MIN_OVERRIDE && __thrust_vector[thruster_dir] < MAX_THRUST_LEVEL && __thrust_vector[opposite_dir] < MAX_THRUST_LEVEL)
                        setting = MIN_OVERRIDE;
                    setting_ratio = cur_thruster_info.prev_setting / setting;
                    if (setting_ratio <= 0.99f || setting_ratio >= 1.01f)
                    {
                        if (!dry_run)
                            cur_thruster.Key.SetValueFloat("Override", setting);
                        cur_thruster_info.prev_setting = setting;
                    }
                }
            }

            _all_engines_off = reset_all_thrusters;
            _force_override_refresh = false;
        }

        // Ensures that resulting linear force is zero (to prevent undesired drift when turning)
        void normalise_thrust()
        {
            int opposite_dir = 3;
            float new_force_ratio, current_force, opposite_force;

            for (int dir_index = 0; dir_index < 3; ++dir_index)
            {
                current_force = opposite_force = 0.0f;
                foreach (var cur_thruster_info in _thrusters[dir_index].Values)
                {
                    if (cur_thruster_info.is_RCS)
                        current_force += cur_thruster_info.current_setting * cur_thruster_info.actual_max_force;
                }
                foreach (var cur_thruster_info in _thrusters[opposite_dir].Values)
                {
                    if (cur_thruster_info.is_RCS)
                        opposite_force += cur_thruster_info.current_setting * cur_thruster_info.actual_max_force;
                }

                if (current_force >= 1.0f && current_force - __counter_gravity[dir_index] > opposite_force)
                {
                    new_force_ratio = (opposite_force + __counter_gravity[dir_index]) / current_force;
                    foreach (var cur_thruster_info in _thrusters[dir_index].Values)
                    {
                        if (cur_thruster_info.is_RCS)
                            cur_thruster_info.current_setting *= new_force_ratio;
                    }
                }
                if (opposite_force >= 1.0f && opposite_force - __counter_gravity[opposite_dir] > current_force)
                {
                    new_force_ratio = (current_force + __counter_gravity[opposite_dir]) / opposite_force;
                    foreach (var cur_thruster_info in _thrusters[opposite_dir].Values)
                    {
                        if (cur_thruster_info.is_RCS)
                            cur_thruster_info.current_setting *= new_force_ratio;
                    }
                }

                ++opposite_dir;
            }
        }

        private void handle_thrust_control(float[] __linear_velocity)
        {
            const float DAMPING_CONSTANT = 10.0f;

            foreach (var cur_direction in _thrusters)
            {
                foreach (var cur_thruster_info in cur_direction.Values)
                {
                    if (cur_thruster_info.is_RCS)
                        cur_thruster_info.override_cleared = false;
                }
            }

            Matrix inverse_world_rotation = _inverse_world_transform.GetOrientation();
            _local_angular_velocity = Vector3.Transform(_grid.Physics.AngularVelocity, inverse_world_rotation);
            float manual_rotation_length2 = _manual_rotation.LengthSquared();
            Vector3 desirted_angular_velocity, local_linear_velocity = Vector3.Transform(_grid.Physics.LinearVelocity, inverse_world_rotation);
            if (manual_rotation_length2 <= 0.0001f)
                desirted_angular_velocity = -_local_angular_velocity;
            else
            {
                float projection_dot_prduct = Vector3.Dot(_local_angular_velocity, _manual_rotation);
                Vector3 local_velocity_projection = (projection_dot_prduct / manual_rotation_length2) * _manual_rotation,
                        local_velocity_rejection = _local_angular_velocity - local_velocity_projection;

                if (projection_dot_prduct < 0.0f)
                    local_velocity_projection = Vector3.Zero;
                desirted_angular_velocity = _manual_rotation * 2.0f + local_velocity_projection - local_velocity_rejection;
            }

            _rotation_active = desirted_angular_velocity.LengthSquared() >= 0.0005f;
            if (!_rotation_active || _is_gyro_override_active || autopilot_on)
            {
                apply_thrust_settings(reset_all_thrusters: true);
                return;
            }
            decompose_vector(desirted_angular_velocity, __control_vector);
            decompose_vector(local_linear_velocity, __linear_velocity);
            float min_control = (!linear_dampers_on || local_linear_velocity.LengthSquared() <= 1.0f) ? 0.02f : 0.3f,
                longitudinal_speed = __linear_velocity[(int)thrust_dir.fore] + __linear_velocity[(int)thrust_dir.aft],
                     lateral_speed = __linear_velocity[(int)thrust_dir.port] + __linear_velocity[(int)thrust_dir.starboard],
                    vertical_speed = __linear_velocity[(int)thrust_dir.dorsal] + __linear_velocity[(int)thrust_dir.ventral];
            bool pitch_control_on = __control_vector[(int)thrust_dir.port] + __control_vector[(int)thrust_dir.starboard] >= min_control,
                   yaw_control_on = __control_vector[(int)thrust_dir.dorsal] + __control_vector[(int)thrust_dir.ventral] >= min_control,
                  roll_control_on = __control_vector[(int)thrust_dir.fore] + __control_vector[(int)thrust_dir.aft] >= min_control;
            __rotation_enable[(int)thrust_dir.fore] = __rotation_enable[(int)thrust_dir.aft] = pitch_control_on || yaw_control_on || longitudinal_speed <= 1.0f;
            __rotation_enable[(int)thrust_dir.port] = __rotation_enable[(int)thrust_dir.starboard] = yaw_control_on || roll_control_on || lateral_speed <= 1.0f;
            __rotation_enable[(int)thrust_dir.dorsal] = __rotation_enable[(int)thrust_dir.ventral] = pitch_control_on || roll_control_on || vertical_speed <= 1.0f;

            Vector3 local_gravity_force/*, linear_damping*/;
            if (!linear_dampers_on)
                local_gravity_force = /*linear_damping =*/ Vector3.Zero;
            else
            {
                local_gravity_force = Vector3.Transform(_grid.Physics.Gravity * _grid.Physics.Mass, inverse_world_rotation);
                //linear_damping      = (-2.0f * _grid.Physics.Mass) * Vector3.Transform(_grid.Physics.LinearVelocity, inverse_world_rotation);
            }
            //decompose_vector(      linear_damping, __braking_vector );
            decompose_vector(_manual_thrust, __thrust_vector);
            decompose_vector(-local_gravity_force, __counter_gravity);

            /*
            for (int dir_index = 0; dir_index < 6; ++dir_index)
            {
                if (_lin_force[dir_index] >= 1.0f)
                    __thrust_vector[dir_index] += __braking_vector[dir_index] / _lin_force[dir_index];
            }
            */
            for (int dir_index = 0; dir_index < 6; ++dir_index)
            {
                int opposite_dir = (dir_index < 3) ? (dir_index + 3) : (dir_index - 3);
                float initial_setting;

                if (__thrust_vector[dir_index] < MAX_THRUST_LEVEL && __thrust_vector[opposite_dir] < MAX_THRUST_LEVEL)
                    initial_setting = (_lin_force[dir_index] >= 1.0f) ? (__counter_gravity[dir_index] / _lin_force[dir_index]) : 0.0f;
                else
                    initial_setting = 0.0f;
                foreach (var cur_thruster_info in _thrusters[dir_index].Values)
                    cur_thruster_info.current_setting = initial_setting;
            }
            for (int dir_index = 0; dir_index < 6; ++dir_index)
            {
                int thruster_dir, opposite_dir;
                float control = DAMPING_CONSTANT * __control_vector[dir_index] * _grid.Physics.Mass;

                for (thruster_dir = 0; thruster_dir < 6; ++thruster_dir)
                    __settings[thruster_dir] = (_max_force[thruster_dir] >= 1.0f) ? (control / _max_force[thruster_dir]) : 0.0f;
                foreach (var cur_thruster_info in _control_sets[dir_index])
                {
                    if (!cur_thruster_info.is_RCS)
                        continue;

                    thruster_dir = (int)cur_thruster_info.nozzle_direction;
                    opposite_dir = (thruster_dir < 3) ? (thruster_dir + 3) : (thruster_dir - 3);
                    if (__thrust_vector[thruster_dir] < MAX_THRUST_LEVEL && __thrust_vector[opposite_dir] < MAX_THRUST_LEVEL)
                    {
                        cur_thruster_info.current_setting += __settings[thruster_dir];
                        if (cur_thruster_info.current_setting > 1.0f)
                            cur_thruster_info.current_setting = 1.0f;
                    }
                }
            }

            normalise_thrust();
            apply_thrust_settings(reset_all_thrusters: false);
        }
        #endregion

        #region thruster manager

        private static thrust_dir get_nozzle_orientation(MyThrust thruster)
        {
            Vector3I dir_vector = thruster.ThrustForwardVector;
            if (dir_vector == Vector3I.Forward)
                return thrust_dir.fore;
            if (dir_vector == Vector3I.Backward)
                return thrust_dir.aft;
            if (dir_vector == Vector3I.Left)
                return thrust_dir.port;
            if (dir_vector == Vector3I.Right)
                return thrust_dir.starboard;
            if (dir_vector == Vector3I.Up)
                return thrust_dir.dorsal;
            if (dir_vector == Vector3I.Down)
                return thrust_dir.ventral;
            throw new ArgumentException("Thruster " + ((IMyTerminalBlock) thruster).CustomName  + " is not grid-aligned");
        }

        private void update_reference_vectors()
        {
            try
            {
                for (int dir_index = 0; dir_index < 3; ++dir_index)
                {
                    if (_max_force[dir_index] < 1.0f || _max_force[dir_index + 3] < 1.0f)
                    {
                        foreach (var cur_thruster_info in _thrusters[dir_index].Values)
                        {
                            cur_thruster_info.reference_vector = cur_thruster_info.CoM_offset;
                        }
                    }
                    else
                    {
                        var rcs_thrusters = _thrusters[dir_index].Values.Concat(_thrusters[dir_index + 3].Values)
                            .Where(t => t.is_RCS).ToList();

                        var total_static_moment = rcs_thrusters.Select(t => t.static_moment).Aggregate(Vector3.Zero, (acc, cur) => acc + cur);
                        var CoT_location = total_static_moment / (_max_force[dir_index] + _max_force[dir_index + 3]);

                        MyAPIGateway.Parallel.For(0, rcs_thrusters.Count, i =>
                        {
                            var t = rcs_thrusters[i];
                            t.reference_vector = t.grid_centre_pos - CoT_location;
                        });
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Exception in update_reference_vectors: {e}");
            }
        }
        private void check_thruster_control_changed()
        {
            try
            {
                bool changesMade = false;

                foreach (var curDirection in _thrusters)
                {
                    foreach (var curThruster in curDirection)
                        curThruster.Value.group_no_RCS = false;
                }

                IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper?.GetTerminalSystemForGrid(_grid);
                if (gridTerminal == null)
                    return;

                _all_groups.Clear();
                gridTerminal.GetBlockGroups(_all_groups);

                foreach (var curGroup in _all_groups)
                {
                    MyThrust curThruster;

                    if (curGroup.Name.ToUpper().Contains("[NO RCS]"))
                    {
                        _blocks_in_group.Clear();
                        curGroup.GetBlocks(_blocks_in_group);
                        foreach (var curBlock in _blocks_in_group)
                        {
                            curThruster = curBlock as MyThrust;
                            if (curThruster == null)
                                continue;
                            foreach (var curDirection in _thrusters)
                            {
                                if (curDirection.ContainsKey(curThruster))
                                {
                                    curDirection[curThruster].group_no_RCS = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                foreach (var curDirection in _thrusters)
                {
                    thruster_info curThrusterInfo;

                    foreach (var curThruster in curDirection)
                    {
                        curThrusterInfo = curThruster.Value;
                        if (curThrusterInfo.actual_max_force < 0.01f * curThrusterInfo.max_force || !curThruster.Key.IsWorking)
                            curThrusterInfo.is_RCS = false;
                        else if (!(curThrusterInfo.is_RCS ^ curThrusterInfo.group_no_RCS))
                        {
                            curThrusterInfo.is_RCS = !curThrusterInfo.group_no_RCS;
                            changesMade = true;
                        }
                        if (!curThrusterInfo.is_RCS && !curThrusterInfo.override_cleared)
                        {
                            curThruster.Key.SetValueFloat("Override", 0.0f);
                            curThrusterInfo.override_cleared = changesMade = true;
                        }
                    }
                }
                if (changesMade)
                {
                    for (int dirIndex = 0; dirIndex < 6; ++dirIndex)
                    {
                        _max_force[dirIndex] = 0.0f;
                        foreach (var curThrusterInfo in _thrusters[dirIndex].Values)
                        {
                            if (curThrusterInfo.is_RCS)
                                _max_force[dirIndex] += curThrusterInfo.max_force;
                        }
                    }
                }
                if (changesMade || _thruster_added_or_removed)
                {
                    refresh_thruster_info();
                    update_reference_vectors();
                    refresh_control_sets();
                    _thruster_added_or_removed = false;
                }
            }
            catch (Exception e)
            {
                // Log the error
                MyLog.Default.WriteLine($"Error occurred in check_thruster_control_changed: {e.Message}");
            }
        }

        private void refresh_real_max_forces_for_single_direction(int dir_index, bool atmosphere_present, float air_density)
        {
            thruster_info      cur_thruster_info;
            float              thrust_multiplier, planetoid_influence;
            MyThrustDefinition thruster_definition;

            _lin_force[dir_index] = 0.0f;
            foreach (var cur_thruster in _thrusters[dir_index])
            {
                cur_thruster_info   = cur_thruster.Value;
                thruster_definition = cur_thruster.Key.BlockDefinition;

                if (!atmosphere_present && thruster_definition.NeedsAtmosphereForInfluence)
                    planetoid_influence = 0.0f;
                else if (thruster_definition.MaxPlanetaryInfluence <= thruster_definition.MinPlanetaryInfluence)
                    planetoid_influence = 1.0f;
                else
                {
                    planetoid_influence = (air_density - thruster_definition.MinPlanetaryInfluence) / (thruster_definition.MaxPlanetaryInfluence - thruster_definition.MinPlanetaryInfluence);
                    if (planetoid_influence < 0.0f)
                        planetoid_influence = 0.0f;
                    else if (planetoid_influence > 1.0f)
                        planetoid_influence = 1.0f;
                }
                thrust_multiplier = (1.0f - planetoid_influence) * thruster_definition.EffectivenessAtMinInfluence + planetoid_influence * thruster_definition.EffectivenessAtMaxInfluence;

                cur_thruster_info.actual_max_force = cur_thruster_info.max_force  * thrust_multiplier;
                _lin_force[dir_index]             += cur_thruster_info.actual_max_force;
            }
        }

        private void refresh_real_max_forces()
        {
            BoundingBoxD grid_bounding_box = _grid.PositionComp.WorldAABB;
            MyPlanet     closest_planetoid = MyGamePruningStructure.GetClosestPlanet(ref grid_bounding_box);
            bool         atmosphere_present;
            float        air_density;

            if (closest_planetoid == null)
            {
                atmosphere_present = false;
                air_density        = 0.0f;
            }
            else
            {
                atmosphere_present = closest_planetoid.HasAtmosphere;
                air_density        = closest_planetoid.GetAirDensity(grid_bounding_box.Center);
            }

            for (int dir_index = 0; dir_index < 6; ++dir_index)
                refresh_real_max_forces_for_single_direction(dir_index, atmosphere_present, air_density);
        }

        public void assign_thruster(IMyThrust thruster_ref)
        {
            var thruster = thruster_ref as MyThrust;

            try
            {
                if (MyAPIGateway.Multiplayer == null || MyAPIGateway.Multiplayer.IsServer)
                    thruster.SetValueFloat("Override", 0.0f);

                var new_thruster = new thruster_info();
                new_thruster.grid_centre_pos = (thruster.Min + thruster.Max) * (_grid.GridSize / 2.0f);
                new_thruster.max_force = new_thruster.actual_max_force = thruster.BlockDefinition.ForceMagnitude;
                new_thruster.CoM_offset = new_thruster.reference_vector = new_thruster.grid_centre_pos - _grid_CoM_location;
                new_thruster.static_moment = new_thruster.grid_centre_pos * new_thruster.max_force;
                new_thruster.nozzle_direction = get_nozzle_orientation(thruster);
                new_thruster.override_cleared = false;
                new_thruster.is_RCS = new_thruster.group_no_RCS = _thruster_added_or_removed = true;

                MyAPIGateway.Parallel.For(0, 6, dir_index =>
                {
                    if ((int)new_thruster.nozzle_direction == dir_index)
                    {
                        _max_force[dir_index] += new_thruster.max_force;
                        _lin_force[dir_index] += new_thruster.max_force;
                        _thrusters[dir_index].Add(thruster, new_thruster);
                    }
                });
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error in assign_thruster: {ex}");
            }
        }



        public void dispose_thruster(IMyThrust thruster_ref)
        {
            try
            {
                var thruster = thruster_ref as MyThrust;
                if (thruster == null)
                {
                    MyLog.Default.WriteLine($"Error: Thruster reference is null or invalid in dispose_thruster method");
                    return;
                }

                for (int dir_index = 0; dir_index < 6; ++dir_index)
                {
                    Dictionary<MyThrust, thruster_info> cur_direction = _thrusters[dir_index];

                    if (cur_direction.ContainsKey(thruster))
                    {
                        thruster_info cur_thruster_info = cur_direction[thruster];
                        if (cur_thruster_info.is_RCS)
                            _max_force[(int)cur_thruster_info.nozzle_direction] -= cur_thruster_info.max_force;
                        _lin_force[(int)cur_thruster_info.nozzle_direction] -= cur_thruster_info.actual_max_force;
                        cur_direction.Remove(thruster);
                        _thruster_added_or_removed = true;
                        //log_ECU_action("dispose_thruster", string.Format("{0} ({1}) [{2}]", ((PB.IMyTerminalBlock) thruster).CustomName, get_nozzle_orientation(thruster).ToString(), thruster.EntityId));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"Error in dispose_thruster method: {ex.ToString()}");
            }
        }


        public engine_control_unit(IMyCubeGrid grid_ref)
        {
            try
            {
                _grid = (MyCubeGrid)grid_ref;
                _inverse_world_transform = _grid.PositionComp.WorldMatrixNormalizedInv;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error in engine_control_unit constructor: {ex}");
            }
        }

        #endregion

        #region Gyroscope handling

        private void calc_spherical_moment_of_inertia()
        {
            try
            {
                Vector3I grid_dim = _grid.Max - _grid.Min + Vector3I.One;
                int low_dim = grid_dim.X, med_dim = grid_dim.Y, high_dim = grid_dim.Z, temp;

                if (low_dim < 0)
                    low_dim = -low_dim;
                if (med_dim < 0)
                    med_dim = -med_dim;
                if (high_dim < 0)
                    high_dim = -high_dim;

                do
                {
                    temp = -1;
                    if (low_dim > med_dim)
                    {
                        temp = low_dim;
                        low_dim = med_dim;
                        med_dim = temp;
                    }
                    if (med_dim > high_dim)
                    {
                        temp = med_dim;
                        med_dim = high_dim;
                        high_dim = temp;
                    }
                } while (temp >= 0);

                float smallest_area = low_dim * med_dim * _grid.GridSize * _grid.GridSize;
                float reference_radius = (float)Math.Sqrt(smallest_area / Math.PI);
                _spherical_moment_of_inertia = 0.4f * ((_grid.Physics.Mass >= 1.0f) ? _grid.Physics.Mass : 1.0f) * reference_radius * reference_radius;

                //log_ECU_action("calc_spherical_moment_of_inertia", string.Format("smallest area = {0} m2, radius = {1} m, SMoI = {2} t*m2", smallest_area, reference_radius, _spherical_moment_of_inertia / 1000.0f));
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowNotification($"Error in calc_spherical_moment_of_inertia(): {ex.Message}", 5000, MyFontEnum.Red);
                MyLog.Default.WriteLine($"Error in calc_spherical_moment_of_inertia(): {ex}");
            }
        }
        private void refresh_gyro_info()
        {
            Vector3 gyro_override = Vector3.Zero;
            float max_gyro_torque = 0.0f;
            int num_overriden_gyroscopes = 0;

            try
            {
                MyAPIGateway.Parallel.For(0, _gyroscopes.Count, i =>
                {
                    var cur_gyroscope = _gyroscopes.ElementAt(i);

                    if (cur_gyroscope.IsWorking)
                    {
                        System.Threading.Interlocked.Exchange(ref max_gyro_torque, max_gyro_torque + cur_gyroscope.MaxGyroForce);

                        if (cur_gyroscope.GyroOverride)
                        {
                            System.Threading.Interlocked.Exchange(ref gyro_override.X, gyro_override.X + cur_gyroscope.GyroOverrideVelocityGrid.X);
                            System.Threading.Interlocked.Exchange(ref gyro_override.Y, gyro_override.Y + cur_gyroscope.GyroOverrideVelocityGrid.Y);
                            System.Threading.Interlocked.Exchange(ref gyro_override.Z, gyro_override.Z + cur_gyroscope.GyroOverrideVelocityGrid.Z);

                            System.Threading.Interlocked.Increment(ref num_overriden_gyroscopes);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Error in refresh_gyro_info()", 5000, MyFontEnum.Red);
                MyLog.Default.WriteLine($"Error in refresh_gyro_info(): {e}");
            }

            if (autopilot_on)
            {
                gyro_override = Vector3.Zero;
                _is_gyro_override_active = true;
            }
            else if (num_overriden_gyroscopes > 0)
            {
                gyro_override /= num_overriden_gyroscopes;
                _is_gyro_override_active = true;
            }
            else if (_is_gyro_override_active)
            {
                reset_user_input(reset_gyros_only: true);
                _is_gyro_override_active = false;
            }

            _gyro_override = gyro_override;
            _max_gyro_torque = max_gyro_torque;
        }


        public void assign_gyroscope(IMyGyro new_gyroscope)
        {
            try
            {
                _gyroscopes.Add((MyGyro)new_gyroscope);
            }
            catch (ArgumentNullException ex)
            {
                // Handle ArgumentNullException here
                MyLog.Default.WriteLine($"ArgumentNullException occurred in assign_gyroscope method: {ex.Message}");
            }
            catch (InvalidCastException ex)
            {
                // Handle InvalidCastException here
                MyLog.Default.WriteLine($"InvalidCastException occurred in assign_gyroscope method: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle any other exception here
                MyLog.Default.WriteLine($"Exception occurred in assign_gyroscope method: {ex.Message}");
            }
        }

        public void dispose_gyroscope(IMyGyro gyroscope_to_remove)
        {
            try
            {
                _gyroscopes.Remove((MyGyro)gyroscope_to_remove);
            }
            catch (ArgumentNullException ex)
            {
                // Handle the case where gyroscope_to_remove is null
                MyLog.Default.WriteLine($"ArgumentNullException caught in dispose_gyroscope: {ex.Message}");
            }
            catch (InvalidCastException ex)
            {
                // Handle the case where gyroscope_to_remove is not of type MyGyro
                MyLog.Default.WriteLine($"InvalidCastException caught in dispose_gyroscope: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                MyLog.Default.WriteLine($"Exception caught in dispose_gyroscope: {ex.Message}");
            }
        }


        #endregion

        #region Flight controls handling

        public bool is_under_control_of(VRage.Game.ModAPI.Interfaces.IMyControllableEntity current_controller)
        {
            var    controller = current_controller as MyShipController;
            return controller != null && controller.CubeGrid == _grid;
        }

        public void check_autopilot(IMyRemoteControl RC_block)
        {
            //var RC_block_proper = (MyRemoteControl) RC_block;
            //autopilot_on       |= ((MyObjectBuilder_RemoteControl) RC_block_proper.GetObjectBuilderCubeBlock()).AutoPilotEnabled;
            autopilot_on |= RC_block.IsAutoPilotEnabled;
        }

        public void reset_user_input(bool reset_gyros_only)
        {
            _manual_rotation       = _target_rotation = Vector3.Zero;
            _under_player_control &= reset_gyros_only;
        }

        public void translate_linear_input(Vector3 input_thrust, VRage.Game.ModAPI.Interfaces.IMyControllableEntity current_controller)
        {
            try
            {
                var controller = current_controller as MyShipController;
                if (controller != null && controller.CubeGrid == _grid)
                {
                    Matrix cockpit_matrix;
                    controller.Orientation.GetMatrix(out cockpit_matrix);
                    _manual_thrust = Vector3.Clamp(Vector3.Transform(input_thrust, cockpit_matrix), -Vector3.One, Vector3.One);
                    _under_player_control = true;
                }
                else
                {
                    reset_user_input(reset_gyros_only: false);
                }
            }
            catch (Exception e)
            {
                // Handle the exception here. For example, log the error message or display a message to the user.
                MyAPIGateway.Utilities.ShowNotification("An error occurred while translating linear input: " + e.Message, 5000, "Red");
            }
        }



        public void translate_rotation_input(Vector3 input_rotation, VRage.Game.ModAPI.Interfaces.IMyControllableEntity current_controller)
        {
            try
            {
                var controller = current_controller as MyShipController;
                if (controller == null || controller.CubeGrid != _grid)
                {
                    reset_user_input(reset_gyros_only: false);
                    return;
                }

                Matrix cockpit_matrix;
                controller.Orientation.GetMatrix(out cockpit_matrix);
                _target_rotation.X = input_rotation.X * (-0.05f);
                _target_rotation.Y = input_rotation.Y * (-0.05f);
                _target_rotation.Z = input_rotation.Z * (-0.2f);
                _target_rotation = Vector3.Transform(_target_rotation, cockpit_matrix);
                _under_player_control = true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Error in translate_rotation_input: {e.ToString()}");
            }
        }


        #endregion

        public void handle_60Hz()
        {
            /*
            bool disabler = false;
            IMyGridTerminalSystem grid_terminal = MyAPIGateway.TerminalActionsHelper?.GetTerminalSystemForGrid(_grid);
            if (grid_terminal == null)
                return;
            _all_groups.Clear();
            grid_terminal.GetBlockGroups(_all_groups);
            foreach (var cur_group in _all_groups)
            {
                if (cur_group.Name.ToUpper().Contains("[FUCKOFF]"))
                {
                    disabler = true;
                }
                else disabler = false;
            }*/

            /*
            bool disabler = true;
            IMyGridTerminalSystem grid_terminal = MyAPIGateway.TerminalActionsHelper?.GetTerminalSystemForGrid(_grid);
            if (grid_terminal == null)
                return;

            List<IMyConveyorSorter> block_list = new List<IMyConveyorSorter>();
            grid_terminal.GetBlocksOfType(block_list);

            foreach (IMyConveyorSorter block in block_list)
            {
                if (block != null && block.Enabled && block.BlockDefinition.SubtypeId == "ARYXTempestCannon")
                {
                    disabler = false;
                    check_thruster_control_changed();
                    _force_override_refresh = true;
                    
                    break;

                }
                else
                {
                    bool changesMade = false;
                    
                    foreach (var curDirection in _thrusters)
                    {
                        thruster_info curThrusterInfo;

                        foreach (var curThruster in curDirection)
                        {
                            curThrusterInfo = curThruster.Value;
                            if (!curThrusterInfo.override_cleared)
                            {
                                curThruster.Key.SetValueFloat("Override", 0.0f);
                                curThrusterInfo.override_cleared = changesMade = true;
                            }
                        }
                    }
                    if (changesMade)
                    {
                        for (int dirIndex = 0; dirIndex < 6; ++dirIndex)
                        {
                            _max_force[dirIndex] = 0.0f;
                            foreach (var curThrusterInfo in _thrusters[dirIndex].Values)
                            {
                                if (curThrusterInfo.is_RCS)
                                    _max_force[dirIndex] += curThrusterInfo.max_force;
                            }
                        }
                    }
                    if (changesMade || _thruster_added_or_removed)
                    {
                        refresh_thruster_info();
                        update_reference_vectors();
                        refresh_control_sets();
                        _thruster_added_or_removed = false;
                    }
                    disabler = true;
                    break;

                }
            }*/


            bool disabler = true;

            try
            {
                IMyGridTerminalSystem grid_terminal = MyAPIGateway.TerminalActionsHelper?.GetTerminalSystemForGrid(_grid);
                if (grid_terminal == null) return;

                List<IMyConveyorSorter> block_list = new List<IMyConveyorSorter>();
                grid_terminal.GetBlocksOfType(block_list, b => b.Enabled && b.BlockDefinition.SubtypeId == "SC_RCS_Computer");

                if (block_list.Count > 0)
                {
                    disabler = false;
                    check_thruster_control_changed();
                    _force_override_refresh = true;
                }
                else
                {
                    bool changesMade = false;
                    var allThrusters = _thrusters.SelectMany(thrusterList => thrusterList).ToList();

                    foreach (var curThruster in allThrusters)
                    {
                        try
                        {
                            thruster_info curThrusterInfo = curThruster.Value;
                            if (!curThrusterInfo.override_cleared)
                            {
                                curThruster.Key.SetValueFloat("Override", 0.0f);
                                curThrusterInfo.override_cleared = changesMade = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the error or notify the user about the exception
                            // For example: 
                            MyAPIGateway.Utilities.ShowNotification($"Error: {ex.Message}", 5000, MyFontEnum.Red);
                        }
                    }

                    if (changesMade)
                    {
                        for (int dirIndex = 0; dirIndex < 6; ++dirIndex)
                        {
                            _max_force[dirIndex] = 0.0f;
                            foreach (var curThrusterInfo in _thrusters[dirIndex].Values)
                            {
                                if (curThrusterInfo.is_RCS) _max_force[dirIndex] += curThrusterInfo.max_force;
                            }
                        }
                    }

                    if (changesMade || _thruster_added_or_removed)
                    {
                        refresh_thruster_info();
                        update_reference_vectors();
                        refresh_control_sets();
                        _thruster_added_or_removed = false;
                    }
                    disabler = true;
                }
            }
            catch (Exception ex)
            {
                // Log the error or notify the user about the exception
                // For example: 
                MyAPIGateway.Utilities.ShowNotification($"Error: {ex.Message}", 5000, MyFontEnum.Red);
            }



            try
            {
                if (_grid.Physics == null || _grid.Physics.IsStatic || disabler == true)
                {
                    _physics_enable_delay = PHYSICS_ENABLE_DELAY;
                    return;
                }

                // Suppress input noise caused by analog controls
                _sample_sum += _target_rotation - _rotation_samples[_current_index];
                _rotation_samples[_current_index] = _target_rotation;
                if (++_current_index >= NUM_ROTATION_SAMPLES)
                    _current_index = 0;
                _manual_rotation = _sample_sum / NUM_ROTATION_SAMPLES;

                _inverse_world_transform = _grid.PositionComp.WorldMatrixNormalizedInv;
                handle_thrust_control(__linear_velocity);

                MyAPIGateway.Parallel.For(0, 3, dir_index =>
                {
                    if (_max_force[dir_index] < 1.0f || _max_force[dir_index + 3] < 1.0f)
                    {
                        foreach (var cur_thruster_info in _thrusters[dir_index].Values)
                            cur_thruster_info.reference_vector = cur_thruster_info.CoM_offset;
                    }
                    else
                    {
                        var rcs_thrusters = _thrusters[dir_index].Values.Concat(_thrusters[dir_index + 3].Values)
                            .Where(t => t.is_RCS).ToList();

                        var total_static_moment = rcs_thrusters.Select(t => t.static_moment).Aggregate(Vector3.Zero, (acc, cur) => acc + cur);
                        var CoT_location = total_static_moment / (_max_force[dir_index] + _max_force[dir_index + 3]);

                        MyAPIGateway.Parallel.For(0, rcs_thrusters.Count, i =>
                        {
                            var t = rcs_thrusters[i];
                            t.reference_vector = t.grid_centre_pos - CoT_location;
                        });
                    }
                });

                calculate_and_apply_torque();
            }
            catch (Exception e)
            {
                // handle exception gracefully, log error message
                MyLog.Default.WriteLineAndConsole($"Error occurred in handle_60Hz(): {e.Message}\n{e.StackTrace}");
            }
        }

        public void handle_4Hz()
        {
            try
            {
                if (_grid.Physics == null || _grid.Physics.IsStatic)
                    return;

                calc_spherical_moment_of_inertia();
                refresh_gyro_info();
                var current_grid_CoM = Vector3D.Transform(_grid.Physics.CenterOfMassWorld, _inverse_world_transform);
                bool CoM_shifted = (current_grid_CoM - _grid_CoM_location).LengthSquared() > 0.01f;
                if (CoM_shifted)
                {
                    _grid_CoM_location = current_grid_CoM;
                    refresh_thruster_info();
                    update_reference_vectors();
                    refresh_control_sets();
                    //log_ECU_action("handle_4Hz", "CoM refreshed");
                }
                refresh_real_max_forces();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Exception in handle_4Hz: {e}");
            }
        }

        public void handle_2s_period()
        {
            try
            {
                if (_grid?.Physics == null || _grid.Physics.IsStatic)
                    return;
                check_thruster_control_changed();
                _force_override_refresh = true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error in handle_2s_period: {e}");
            }
        }

    }
}
