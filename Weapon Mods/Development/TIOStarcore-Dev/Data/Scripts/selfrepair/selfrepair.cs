using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace TIOSelfRepair
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "Type18_Artillery", "Type21_Artillery", "Type24_Artillery", "Type19_Driver", "Type22_Driver", "Type25_Driver", "Reaver_Coilgun", "Devastator_Torp", "Priest_Block", "PriestReskin_Block", "APE_Strong", "HeavyFighterBay" )]
    public class TIOSelfRepair : MyGameLogicComponent
    {
        private IMyConveyorSorter artilleryBlock;
        private int triggerTick = 0;
        private const int COUNTDOWN_TICKS = 10 * 60; // (60 ticks per second)

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
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
            if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null) return;

            if (!artilleryBlock.IsFunctional)
            {
                triggerTick += 1;
                if (triggerTick >= COUNTDOWN_TICKS)
                {
                    DoRepair();
                    MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("RepairParticle", artilleryBlock.GetPosition());
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("RepairSound", artilleryBlock.GetPosition());
                    triggerTick = 0; // Restart the timer after repair
                }
            }
            else
            {
                triggerTick = 0; // Reset countdown if the block is functional
            }
        }

        private void DoRepair()
        {
            if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null) return;

            IMySlimBlock slimBlock = artilleryBlock.SlimBlock;
            if (slimBlock == null) return;

            // Fetch the original owner ID of the grid.
            long gridOwnerId = artilleryBlock.CubeGrid.BigOwners.Count > 0 ? artilleryBlock.CubeGrid.BigOwners[0] : 0;

            // If the grid has an owner, proceed with repair and ownership change.

            float repairAmount = 12;
            slimBlock.IncreaseMountLevel(repairAmount, 0L, null, 0f, false, MyOwnershipShareModeEnum.Faction);

            // Try casting to MyCubeBlock and change the owner.
            MyCubeBlock cubeBlock = artilleryBlock as MyCubeBlock;
            if (cubeBlock != null)
            {
                cubeBlock.ChangeBlockOwnerRequest(gridOwnerId, MyOwnershipShareModeEnum.Faction);
            }

        }


        private void ArtilleryBlockEnabledChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != artilleryBlock.EntityId) return;
            if (artilleryBlock.IsFunctional)
            {
                triggerTick = 0; // Reset countdown if functional
            }
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
