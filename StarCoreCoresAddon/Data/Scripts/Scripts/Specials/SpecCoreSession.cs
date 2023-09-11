using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using ProtoBuf;
using Sandbox.ModAPI;
using Scripts.Specials.ShipClass;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace MIG.SpecCores
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SpecCoreSession : MySessionComponentBase
    {
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            SpecBlockHooks.Init();
        }

        private bool inited = false;
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            FrameExecutor.Update();
            
        }

        protected override void UnloadData()
        {
            SpecBlockHooks.Close();
        }
    }
}