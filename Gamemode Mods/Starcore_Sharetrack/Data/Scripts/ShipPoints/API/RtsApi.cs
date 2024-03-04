using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace RelativeTopSpeed
{
	public class RtsApi
	{
		private const long ChannelId = 2772681332;
		public bool IsReady { get; private set; }

		private Action ReadyCallback;

		private bool isRegistered = false;

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

			IsReady = true;
			ReadyCallback?.Invoke();
		}

		private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
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
		/// returns the crusing speed of the grid.
		/// </summary>
		public float GetCruiseSpeed(IMyCubeGrid grid) => _GetCruiseSpeed.Invoke(grid);
		private Func<IMyCubeGrid, float> _GetCruiseSpeed;

		/// <summary>
		/// gets the maximum possible speed (cruise speed + max boost)
		/// </summary>
		public float GetMaxSpeed(IMyCubeGrid grid) => _GetMaxSpeed.Invoke(grid);
		private Func<IMyCubeGrid, float> _GetMaxSpeed;

		/// <summary>
		/// returns 4 values: forward boost, min, average, max
		/// </summary>
		public float[] GetBoost(IMyCubeGrid grid) => _GetBoost.Invoke(grid);
		private Func<IMyCubeGrid, float[]> _GetBoost;



		/// <summary>
		/// Returns 4 values: forward accel, min, average, max
		/// </summary>
		public float[] GetAcceleration(IMyCubeGrid grid) => _GetAcceleration.Invoke(grid);
		private Func<IMyCubeGrid, float[]> _GetAcceleration;

		/// <summary>
		/// Uses Base6Directions.Direction
		/// forward = reverse accel
		/// backword = forward accel
		/// left = right accel
		/// ...
		/// </summary>
		public float[] GetAccelerationByDirection(IMyCubeGrid grid) => _GetAccelerationByDirection.Invoke(grid);
		private Func<IMyCubeGrid, float[]> _GetAccelerationByDirection;

	}
}
