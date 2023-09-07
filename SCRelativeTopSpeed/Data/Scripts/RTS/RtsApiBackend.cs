using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace RelativeTopSpeedGV
{
	public class RtsApiBackend
	{
		private const long ChannelId = 2772681332;
		private static Dictionary<string, Delegate> APIMethods;

		private static RelativeTopSpeed RTS;
		public static bool IsInitialized => RTS != null;

		public static void Init(RelativeTopSpeed rts)
		{
			RTS = rts;

			APIMethods = new Dictionary<string, Delegate>() {
				["GetCruiseSpeed"] = new Func<IMyCubeGrid, float>(RTS.GetCruiseSpeed),
				["GetMaxSpeed"] = new Func<IMyCubeGrid, float>(RTS.GetMaxSpeed),
				["GetBoost"] = new Func<IMyCubeGrid, float[]>(RTS.GetBoost),
				["GetAcceleration"] = new Func<IMyCubeGrid, float[]>(RTS.GetAcceleration),
				["GetAccelerationByDirection"] = new Func<IMyCubeGrid, float[]>(RTS.GetAccelerationsByDirection),
			};

			MyAPIGateway.Utilities.RegisterMessageHandler(ChannelId, OnMessageRecieved);
			MyAPIGateway.Utilities.SendModMessage(ChannelId, APIMethods);
		}

		public static void Close() 
		{
			MyAPIGateway.Utilities.UnregisterMessageHandler(ChannelId, OnMessageRecieved);
			RTS = null;
		}

		private static void OnMessageRecieved(object o)
		{
			if ((o as string) == "ApiEndpointRequest")
				MyAPIGateway.Utilities.SendModMessage(ChannelId, APIMethods);
		}
	}
}
