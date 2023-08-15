using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor),false)]
    public class MyReactorUpgradeLogic : MyGameLogicComponent
    {
        private IMyReactor m_reactor;
        private IMyCubeBlock m_parent;
        private MyObjectBuilder_EntityBase m_objectBuilder = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            m_reactor = Entity as IMyReactor;
            m_parent = Entity as IMyCubeBlock;

            m_parent.AddUpgradeValue("ReactorOutput", 1f);

            m_objectBuilder = objectBuilder;

            m_parent.OnUpgradeValuesChanged += OnUpgradeValuesChanged;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return m_objectBuilder;
        }

        private void OnUpgradeValuesChanged()
        {
            m_reactor.PowerOutputMultiplier = m_parent.UpgradeValues["ReactorOutput"];
        }
    }
}
