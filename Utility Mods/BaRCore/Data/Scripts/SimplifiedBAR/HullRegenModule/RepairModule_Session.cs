using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using StarCore.RepairModule.Networking;
using CoreSystems.Api;

namespace StarCore.RepairModule.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class RepairModuleSession : MySessionComponentBase
    {
        public static HeartNetwork Networking = new HeartNetwork();
        public static WcApi CoreSysAPI;

        public override void LoadData()
        {
            Networking.Init("RepairModuleNetwork");

            CoreSysAPI = new WcApi();
            CoreSysAPI.Load();
        }

        protected override void UnloadData()
        {
            Networking.Close();

            if (CoreSysAPI.IsReady)
            {
                CoreSysAPI.Unload();
                CoreSysAPI = null;
            }
        }
    }
}
