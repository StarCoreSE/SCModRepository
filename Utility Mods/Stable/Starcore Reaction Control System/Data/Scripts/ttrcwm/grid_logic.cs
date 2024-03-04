using System;
using System.Collections.Generic;

using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace ttrcwm
{
    class grid_logic: IDisposable
    {
        #region fields

        const float MESSAGE_MULTIPLIER = 10.0f, MESSAGE_SHIFT = 128.0f;
        const int   CONTROLS_TIMEOUT = 2;

        private static byte[] long_message  = new byte[8 + 3];

        private IMyCubeGrid                    _grid;
        private HashSet<IMyControllableEntity> _ship_controllers     = new HashSet<IMyControllableEntity>();
        private HashSet<IMyRemoteControl>      _RC_blocks            = new HashSet<IMyRemoteControl>();
        private engine_control_unit            _ECU                  = null;
        private Vector3UByte                   _prev_manual_rotation = new Vector3UByte(128, 128, 128);

        private int  _num_thrusters = 0, _zero_controls_counter = 0;
        private bool _disposed = false, _ID_on;

        #endregion

        #region auxiliaries

        private void log_grid_action(string method_name, string message)
        {
            MyLog.Default.WriteLine(string.Format("TTDTWM\tgrid_logic.{0}(): \"{1}\" {2}", method_name, _grid.DisplayName, message));
        }

        private void check_disposed()
        {
            if (_disposed)
                throw new Exception(string.Format("grid_logic for \"{0}\" has been disposed", _grid.DisplayName));
        }

        private IMyPlayer get_controlling_player()
        {
            if (MyAPIGateway.Multiplayer != null)
                return MyAPIGateway.Multiplayer.Players.GetPlayerControllingEntity(_grid);
            if (_ECU == null || !_ECU.is_under_control_of(sync_helper.local_controller))
                return null;
            return sync_helper.local_player;
        }

        #endregion

        #region event handlers

        private void on_block_added(IMySlimBlock block)
        {
            check_disposed();
            IMyCubeBlock entity = block.FatBlock;
            if (entity != null)
            {
                var controller = entity as IMyControllableEntity;
                if (controller != null)
                    _ship_controllers.Add(controller);
                var RC_block = entity as IMyRemoteControl;
                if (RC_block != null)
                    _RC_blocks.Add(RC_block);

                var thruster = entity as IMyThrust;
                if (thruster != null)
                {
                    if (_ECU == null)
                        _ECU = new engine_control_unit(_grid);
                    _ECU.assign_thruster(thruster);
                    ++_num_thrusters;
                }
                var gyro = entity as IMyGyro;
                if (gyro != null)
                {
                    if (_ECU == null)
                        _ECU = new engine_control_unit(_grid);
                    _ECU.assign_gyroscope(gyro);
                }
            }
        }

        private void on_block_removed(IMySlimBlock block)
        {
            check_disposed();
            IMyCubeBlock entity = block.FatBlock;
            if (entity != null)
            {
                var controller = entity as IMyControllableEntity;
                if (controller != null)
                    _ship_controllers.Remove(controller);
                var RC_block = entity as IMyRemoteControl;
                if (RC_block != null)
                    _RC_blocks.Remove(RC_block);

                if (_ECU != null)
                {
                    var thruster = entity as IMyThrust;
                    if (thruster != null)
                    {
                        _ECU.dispose_thruster(thruster);
                        --_num_thrusters;
                    }
                    var gyro = entity as IMyGyro;
                    if (gyro != null)
                        _ECU.dispose_gyroscope(gyro);
                }
            }
        }

        /*
        internal static void rotation_message_handler(byte[] argument)
        {
            grid_logic instance = sync_helper.decode_entity_id(argument);
            if (instance == null || instance._disposed || instance._ECU == null)
                return;
            Vector3 manual_rotation;
            IMyPlayer controlling_player = instance.get_controlling_player();
            if (controlling_player == null || MyAPIGateway.Multiplayer != null && !MyAPIGateway.Multiplayer.IsServer)
                return;
            manual_rotation.X = (argument[ 8] - MESSAGE_SHIFT) / MESSAGE_MULTIPLIER;
            manual_rotation.Y = (argument[ 9] - MESSAGE_SHIFT) / MESSAGE_MULTIPLIER;
            manual_rotation.Z = (argument[10] - MESSAGE_SHIFT) / MESSAGE_MULTIPLIER;
            instance._ECU.translate_rotation_input(manual_rotation, controlling_player.Controller.ControlledEntity);
            instance._zero_controls_counter = 0;
        }
        */

        #endregion

        #region event triggers

        /*
        private void send_rotation_message(Vector3 manual_rotation)
        {
            if (MyAPIGateway.Multiplayer == null || MyAPIGateway.Multiplayer.IsServer)
                return;

            Vector3UByte packed_vector = Vector3UByte.Round(manual_rotation * MESSAGE_MULTIPLIER + Vector3.One * MESSAGE_SHIFT);
            if (packed_vector == _prev_manual_rotation)
                return;
            sync_helper.encode_entity_id(_grid, long_message);
            long_message[ 8] = packed_vector.X;
            long_message[ 9] = packed_vector.Y;
            long_message[10] = packed_vector.Z;
            _prev_manual_rotation = packed_vector;
            MyAPIGateway.Multiplayer.SendMessageToServer(sync_helper.ROTATION_MESSAGE_ID, long_message);
        }
        */

        #endregion

        private void handle_user_input(IMyControllableEntity controller)
        {
            try
            {
                Vector3 manual_rotation, manual_thrust;

                if (_ECU == null)
                    return;

                var ship_controller = controller as Sandbox.ModAPI.Ingame.IMyShipController;

                if (ship_controller == null)
                    manual_rotation = manual_thrust = Vector3.Zero;
                else
                {
                    manual_thrust = ship_controller.MoveIndicator;
                    manual_rotation.X = ship_controller.RotationIndicator.X;
                    manual_rotation.Y = ship_controller.RotationIndicator.Y;
                    manual_rotation.Z = ship_controller.RollIndicator;
                }

                _ECU.translate_rotation_input(manual_rotation, controller);
                _ECU.translate_linear_input(manual_thrust, controller);
                _zero_controls_counter = 0;
            }
            catch (Exception e)
            {
                // Log the error
                MyLog.Default.WriteLine($"Error in handle_user_input: {e.Message}");
            }
        }


        public void handle_60Hz()
        {
            try
            {
                check_disposed();
                if (!_grid.IsStatic && _ECU != null && _num_thrusters > 0)
                {
                    IMyPlayer controlling_player = get_controlling_player();
                    if (controlling_player == null)
                    {
                        _ECU.reset_user_input(reset_gyros_only: false);
                        _prev_manual_rotation = Vector3UByte.Zero;
                    }
                    else //if (!sync_helper.network_handlers_registered || MyAPIGateway.Multiplayer == null || !MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Multiplayer.IsServerPlayer(controlling_player.Client))
                        handle_user_input(controlling_player.Controller.ControlledEntity);

                    _ID_on = false;
                    foreach (var cur_controller in _ship_controllers)
                    {
                        _ID_on |= cur_controller.EnabledDamping;
                        //break;
                    }
                    _ECU.linear_dampers_on = _ID_on;

                    _ECU.handle_60Hz();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"An error occurred in handle_60Hz(): {ex.ToString()}");
            }
        }

        public void handle_4Hz()
        {
            try
            {
                check_disposed();
                if (_ECU == null)
                    return;
                if (!_grid.IsStatic && _num_thrusters > 0)
                {
                    _ECU.handle_4Hz();
                    _ECU.autopilot_on = false;
                    foreach (var cur_RC_block in _RC_blocks)
                        _ECU.check_autopilot(cur_RC_block);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"An error occurred in handle_4Hz: {ex.ToString()}");
            }
        }


        public void handle_2s_period()
        {
            try
            {
                check_disposed();

                if (!_grid.IsStatic && _ECU != null && _num_thrusters > 0)
                {
                    _ECU.handle_2s_period();

                    if (MyAPIGateway.Multiplayer != null && !MyAPIGateway.Multiplayer.IsServer)
                    {
                        _prev_manual_rotation = new Vector3UByte(128, 128, 128);
                    }
                    else if (_zero_controls_counter++ >= CONTROLS_TIMEOUT)
                    {
                        _ECU.reset_user_input(reset_gyros_only: false);
                        _prev_manual_rotation = new Vector3UByte(128, 128, 128);
                        _zero_controls_counter = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace to the game's log file
                MyLog.Default.WriteLine($"Exception occurred in handle_2s_period(): {ex.Message}");
                MyLog.Default.WriteLine(ex.StackTrace);
            }
        }


        public grid_logic(IMyCubeGrid new_grid)
        {
            try
            {
                _grid = new_grid;
                _grid.OnBlockAdded += on_block_added;
                _grid.OnBlockRemoved += on_block_removed;
                sync_helper.register_logic_object(this, _grid.EntityId);
                _ID_on = ((MyObjectBuilder_CubeGrid)_grid.GetObjectBuilder()).DampenersEnabled;

                var block_list = new List<IMySlimBlock>();
                _grid.GetBlocks(block_list, delegate (IMySlimBlock block)
                {
                    try
                    {
                        return block.FatBlock is IMyThrust || block.FatBlock is IMyGyro || block.FatBlock is IMyCockpit || block.FatBlock is IMyRemoteControl;
                    }
                    catch (Exception e)
                    {
                        // Log the exception
                        MyLog.Default.WriteLine($"Error occurred while checking if block is valid: {e.Message}");
                        return false;
                    }
                });
                foreach (var cur_block in block_list)
                    on_block_added(cur_block);
            }
            catch (Exception e)
            {
                // Log the exception
                MyLog.Default.WriteLine($"Error occurred while initializing grid logic: {e.Message}");
            }
        }


        public void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    _grid.OnBlockAdded -= on_block_added;
                    _grid.OnBlockRemoved -= on_block_removed;
                    sync_helper.deregister_logic_object(_grid.EntityId);

                    var block_list = new List<IMySlimBlock>();
                    _grid.GetBlocks(block_list, delegate (IMySlimBlock block)
                    {
                        return block.FatBlock is IMyThrust || block.FatBlock is IMyGyro || block.FatBlock is IMyCockpit || block.FatBlock is IMyRemoteControl;
                    });
                    foreach (var cur_block in block_list)
                        on_block_removed(cur_block);

                    _disposed = true;
                }
            }
            catch (Exception ex)
            {
                // Log the error
                MyLog.Default.WriteLineAndConsole($"Error occurred in Dispose method: {ex}");
            }
        }

    }
}
