using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using CoreSystems.Api;
using Starcore.FieldGenerator.Networking;

namespace Starcore.FieldGenerator
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FieldGeneratorSession : MySessionComponentBase
    {
        public static WcApi CoreSysAPI;
        public static HeartNetwork Networking = new HeartNetwork();

        public override void LoadData()
        {
            base.LoadData();

            Networking.Init("FieldGeneratorNetwork");

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
