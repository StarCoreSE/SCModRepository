using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using CoreSystems.Api;
using Draygo.API;

namespace Starcore.FieldGenerator
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FieldGeneratorSession : MySessionComponentBase
    {
        public static WcApi CoreSysAPI;
        public static HudAPIv2 HudAPI;

        public override void LoadData()
        {
            HudAPI = new HudAPIv2();

            CoreSysAPI = new WcApi();
            CoreSysAPI.Load();
        }

        protected override void UnloadData()
        {
            if (HudAPI.Heartbeat)
            { 
                HudAPI.Unload();
                HudAPI = null;
            }

            if (CoreSysAPI.IsReady)
            {
                CoreSysAPI.Unload();
                CoreSysAPI = null;
            }
        }
    }
}
