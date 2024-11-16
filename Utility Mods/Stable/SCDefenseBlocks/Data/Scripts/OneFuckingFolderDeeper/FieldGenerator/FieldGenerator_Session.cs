using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using CoreSystems.Api;
using Sandbox.ModAPI;

namespace Starcore.FieldGenerator
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FieldGeneratorSession : MySessionComponentBase
    {
        public static WcApi CoreSysAPI;

        public override void LoadData()
        {
            base.LoadData();

            CoreSysAPI = new WcApi();
            CoreSysAPI.Load();
        }

        protected override void UnloadData()
        {
            if (CoreSysAPI.IsReady)
            {
                CoreSysAPI.Unload();
                CoreSysAPI = null;
            }
        }
    }
}
