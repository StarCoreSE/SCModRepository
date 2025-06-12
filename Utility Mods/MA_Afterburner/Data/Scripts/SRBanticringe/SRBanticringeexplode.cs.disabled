using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace SRBanticringe
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "SC_SRB")]
    public class SRBDetector : MyGameLogicComponent
    {
        private IMyConveyorSorter SRB;
        private int updateCounter = 0;
        private const int UPDATE_INTERVAL = 6; // Check once per second
        private const double OFFSET_DISTANCE = 1.0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            SRB = (IMyConveyorSorter)Entity;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void UpdateBeforeSimulation10()
        {
            updateCounter++;

            if (updateCounter < UPDATE_INTERVAL)
            {
                return;
            }

            updateCounter = 0;

            if (!SRB.Enabled)
            {
                Vector3D myPos = Entity.GetPosition() + Entity.WorldMatrix.Forward * OFFSET_DISTANCE; // Apply offset
                double radius = 10;
                BoundingSphereD sphere = new BoundingSphereD(myPos, radius);
                MyExplosionInfo explosion = new MyExplosionInfo(0f, 10000f, sphere, MyExplosionTypeEnum.CUSTOM, true);

                MyExplosions.AddExplosion(ref explosion);
            }
        }
    }
}
