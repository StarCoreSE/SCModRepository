using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.ModAPI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Utils;

namespace ReactorUpgrade
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "LargeBlockLargeGenerator", "LargeBlockSmallGenerator")]
    public class MyReactorUpgradeLogic : MyGameLogicComponent
    {
        private IMyReactor m_reactor;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            m_reactor = Entity as IMyReactor;

            if (m_reactor.BlockDefinition.SubtypeName == "LargeBlockLargeGenerator")
            {
                m_reactor.AddUpgradeValue("ReactorOutputLarge", 1f);
                m_reactor.OnUpgradeValuesChanged += OnUpgradeValuesChangedLarge;
                OnUpgradeValuesChangedLarge();
            }
            else
            {
                m_reactor.AddUpgradeValue("ReactorOutputSmall", 1f);
                m_reactor.OnUpgradeValuesChanged += OnUpgradeValuesChangedSmall;
                OnUpgradeValuesChangedSmall();
            }

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private void OnUpgradeValuesChangedLarge()
        {
            m_reactor.PowerOutputMultiplier = m_reactor.UpgradeValues["ReactorOutputLarge"];
        }

        private void OnUpgradeValuesChangedSmall()
        {
            m_reactor.PowerOutputMultiplier = m_reactor.UpgradeValues["ReactorOutputSmall"];
        }

        public override void UpdateAfterSimulation100()
        {
            if (m_reactor.BlockDefinition.SubtypeName == "LargeBlockLargeGenerator")
            {
                m_reactor.PowerOutputMultiplier = m_reactor.UpgradeValues["ReactorOutputLarge"];
            }
            else
            {
                m_reactor.PowerOutputMultiplier = m_reactor.UpgradeValues["ReactorOutputSmall"];
            }
        }
    }
}
