using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace SRBanticringe
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "SC_SRB")]
    public class SRBexplodealternate : MyGameLogicComponent
    {
        private IMyConveyorSorter SRB;
        private int triggerTick = 0;
        private const double OFFSET_DISTANCE = 3;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            SRB = Entity as IMyConveyorSorter;
            (SRB.CubeGrid as MyCubeGrid).ForceDisablePrediction = true;  // Corrected Line

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            MyVisualScriptLogicProvider.ShowNotification("client prediction disabled???", 10000, "Red");
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (SRB == null || SRB.CubeGrid.Physics == null) return;
            SRB.EnabledChanged += SRBEnabledChanged;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            
        }

        public override void UpdateAfterSimulation()
        {
            if (triggerTick == 1)
            {
                DoExplosion();
            }

            triggerTick = 0;
        }

        private void DoExplosion()
        {
            if (SRB == null || SRB.CubeGrid.Physics == null || SRB.Enabled) return;
            double radius = 30;
           // BoundingSphereD sphere = new BoundingSphereD(SRB.WorldMatrix.Translation + (SRB.WorldMatrix.Forward * OFFSET_DISTANCE), radius);
            //MyExplosionInfo explosion = new MyExplosionInfo(0f, 10000f, sphere, MyExplosionTypeEnum.CUSTOM, true);

            //MyExplosions.AddExplosion(ref explosion);
        }

        private void SRBEnabledChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != SRB.EntityId) return;
            triggerTick += 1;

            // Check if the grid is client-predicted and show a notification
            bool isPredicted = (SRB.CubeGrid as MyCubeGrid).IsClientPredicted;
            MyVisualScriptLogicProvider.ShowNotification($"Grid is client-predicted: {isPredicted}", 2000, "Red");
        }

        public override void Close()
        {
            if (SRB != null)
            {
                SRB.EnabledChanged -= SRBEnabledChanged;
                (SRB.CubeGrid as MyCubeGrid).ForceDisablePrediction = false;  // Corrected Line
            }
        }
    }
}
