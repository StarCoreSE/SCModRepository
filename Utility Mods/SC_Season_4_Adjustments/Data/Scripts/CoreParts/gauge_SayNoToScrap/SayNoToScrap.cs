using Sandbox.ModAPI;
using VRage.Game;
using Sandbox.Definitions;
using VRage.Game.Components;
using VRage.Utils;

namespace gauge.SayNoToScrap
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class SayNoToScrap : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {

                MyComponentDefinition c = def as MyComponentDefinition;

                if (c == null)
                {
                    continue;
                }

                c.DropProbability = 0;
            }
        }
    }
}