using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace SENetworkAPI
{
	[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
	public class SessionTools : MySessionComponentBase
	{
		protected override void UnloadData()
		{
			NetworkAPI.Dispose();
		}
	}
}
