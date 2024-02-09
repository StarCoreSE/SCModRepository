using System;
using SENetworkAPI;
using VRage.Game;
using VRage.Game.Components;

namespace BlinkDrive
{
	[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
	public class Core : MySessionComponentBase
	{
		public const ushort ModID = 30023;
		public const string ModName = "Blink Drive";

		public static NetSync<Settings> Config;

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			NetworkAPI.Init(ModID, ModName);
			NetworkAPI.LogNetworkTraffic = false;

			Config = new NetSync<Settings>(this, TransferType.ServerToClient, Settings.Load(), true);
		}

		protected override void UnloadData()
		{
			NetworkAPI.Dispose();
		}
	}
}