using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions.SessionComponents;
using VRage.Utils;
using VRageMath;
using VRageRender.Messages;

namespace GV_Settings
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class NoMinPrice : MySessionComponentBase
    {
        public override void LoadData()
        {
            base.LoadData();
            var allDefs = MyDefinitionManager.Static.GetAllDefinitions();

            foreach(var component in allDefs.OfType<MyPhysicalItemDefinition>()){
                component.MinimalPricePerUnit = 0;
            }
        }
    }
}
