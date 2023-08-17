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

namespace ArtilleryRepair
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "Type18_Artillery_Block")]
    public class ArtilleryRepairLogic : MyGameLogicComponent
    {
        private IMyConveyorSorter artilleryBlock;
        private int repairTick = 0;
        private const int REPAIR_INTERVAL = 600; // 10 seconds (60 ticks per second in Space Engineers)

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do repairs serverside
            artilleryBlock = Entity as IMyConveyorSorter;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null) return;
            artilleryBlock.EnabledChanged += ArtilleryBlockEnabledChanged;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (repairTick >= REPAIR_INTERVAL)
            {
                DoRepair();
                repairTick = 0;
            }

            repairTick++;
        }

        private void DoRepair()
        {
            if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null || artilleryBlock.Integrity >= 1f) return;

            float repairAmount = artilleryBlock.MaxIntegrity * 0.1f; // 10% repair
            artilleryBlock.IncreaseBuildIntegrity(repairAmount);
        }

        private void ArtilleryBlockEnabledChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != artilleryBlock.EntityId) return;
            repairTick = 0; // Reset the repair timer when the block is enabled/disabled
        }

        public override void Close()
        {
            if (artilleryBlock != null)
            {
                artilleryBlock.EnabledChanged -= ArtilleryBlockEnabledChanged;
            }
        }
    }
}
