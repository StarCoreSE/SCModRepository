using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace StarCore.ShareTrack.API
{
    public class RtsApi
    {
        private const long ChannelId = 2772681332;
        private Func<IMyCubeGrid, float[]> _GetAcceleration;
        private Func<IMyCubeGrid, float[]> _GetAccelerationByDirection;
        private Func<IMyCubeGrid, float[]> _GetBoost;
        private Func<IMyCubeGrid, float> _GetCruiseSpeed;
        private Func<IMyCubeGrid, float> _GetMaxSpeed;
        private Func<IMyCubeGrid, float> _GetNegativeInfluence;
        private Func<IMyCubeGrid, float> _GetReducedAngularSpeed;

        private bool isRegistered;

        private Action ReadyCallback;
        public bool IsReady { get; private set; }

        public void Load(Action readyCallback = null)
        {
            if (isRegistered)
                throw new Exception($"{GetType().Name}.Load() should not be called multiple times!");

            isRegistered = true;
            ReadyCallback = readyCallback;
            MyAPIGateway.Utilities.RegisterMessageHandler(ChannelId, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(ChannelId, "ApiEndpointRequest");
        }

        public void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ChannelId, HandleMessage);
            IsReady = false;
            isRegistered = false;
        }

        private void HandleMessage(object obj)
        {
            if (obj is string) // the sent "ApiEndpointRequest" will also be received here, explicitly ignoring that
                return;

            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
                return;

            AssignMethod(dict, "GetCruiseSpeed", ref _GetCruiseSpeed);
            AssignMethod(dict, "GetMaxSpeed", ref _GetMaxSpeed);
            AssignMethod(dict, "GetBoost", ref _GetBoost);
            AssignMethod(dict, "GetAcceleration", ref _GetAcceleration);
            AssignMethod(dict, "GetAccelerationByDirection", ref _GetAccelerationByDirection);
            AssignMethod(dict, "GetNegativeInfluence", ref _GetNegativeInfluence);
            AssignMethod(dict, "GetReducedAngularSpeed", ref _GetReducedAngularSpeed);

            IsReady = true;
            ReadyCallback?.Invoke();
        }

        private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field)
            where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;

            if (field == null)
                throw new Exception(
                    $"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }

        /// <summary>
        ///     Returns the cruising speed of the grid.
        /// </summary>
        public float GetCruiseSpeed(IMyCubeGrid grid)
        {
            return _GetCruiseSpeed.Invoke(grid);
        }

        /// <summary>
        ///     Gets the maximum possible speed (cruise speed + max boost)
        /// </summary>
        public float GetMaxSpeed(IMyCubeGrid grid)
        {
            return _GetMaxSpeed.Invoke(grid);
        }

        /// <summary>
        ///     Returns 4 values: forward boost, min, average, max
        /// </summary>
        public float[] GetBoost(IMyCubeGrid grid)
        {
            return _GetBoost.Invoke(grid);
        }

        /// <summary>
        ///     Returns 4 values: forward accel, min, average, max
        /// </summary>
        public float[] GetAcceleration(IMyCubeGrid grid)
        {
            return _GetAcceleration.Invoke(grid);
        }

        /// <summary>
        ///     Uses Base6Directions.Direction
        ///     forward = reverse accel
        ///     backward = forward accel
        ///     left = right accel
        ///     ...
        /// </summary>
        public float[] GetAccelerationByDirection(IMyCubeGrid grid)
        {
            return _GetAccelerationByDirection.Invoke(grid);
        }

        /// <summary>
        ///     Returns the negative influence for the specified grid.
        /// </summary>
        public float GetNegativeInfluence(IMyCubeGrid grid)
        {
            return _GetNegativeInfluence.Invoke(grid);
        }

        /// <summary>
        ///     Returns the reduced angular speed for the specified grid.
        /// </summary>
        public float GetReducedAngularSpeed(IMyCubeGrid grid)
        {
            return _GetReducedAngularSpeed.Invoke(grid);
        }
    }
}