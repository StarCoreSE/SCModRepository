using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using StarCore.RepairModule.Networking;

namespace StarCore.RepairModule.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class RepairModuleSession : MySessionComponentBase
    {
        public static HeartNetwork Networking = new HeartNetwork();

        public override void LoadData()
        {
            Networking.Init("RepairModuleNetwork");
        }

        protected override void UnloadData()
        {
            Networking.Close();
        }
    }
}
