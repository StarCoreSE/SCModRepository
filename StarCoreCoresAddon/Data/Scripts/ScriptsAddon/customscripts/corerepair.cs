using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
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

namespace StarCoreCoreRepair
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockBeacon_HeavyCore")]
    public class StarCoreCoreRepair : MyGameLogicComponent
    {
        private IMyBeacon shipCore;
        private int triggerTick = 0;
        private int repairCountdown = 0;
        private const int COUNTDOWN_TICKS = 10 * 60;
        private const int REPAIR_DELAY = 30 * 60;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            shipCore = Entity as IMyBeacon;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            MyAPIGateway.Utilities.ShowNotification("StarCoreCoreRepair Initialized", 2000);
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;
            shipCore.EnabledChanged += ShipCoreEnabledChanged;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;

            string gridName = shipCore.CubeGrid.DisplayName;  // Obtain grid name

            if (!shipCore.IsFunctional)
            {
                triggerTick += 1;
                if (triggerTick >= COUNTDOWN_TICKS)
                {
                    MyAPIGateway.Utilities.ShowNotification("Resistance set to 100%", 2000);
                    MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(gridName, 0f); // 0f for 100% resistance
                    repairCountdown = REPAIR_DELAY;
                    triggerTick = 0;
                }
            }
            else
            {
                triggerTick = 0;
                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(gridName, 1f); // Reset to normal resistance
            }

            if (repairCountdown > 0)
            {
                repairCountdown -= 1;
                if (repairCountdown <= 0)
                {
                    DoRepair();
                    MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("RepairParticle", shipCore.GetPosition());
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("RepairSound", shipCore.GetPosition());
                    MyAPIGateway.Utilities.ShowNotification("Block Repaired", 2000);
                }
            }
        }

        private void DoRepair()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;

            IMySlimBlock slimBlock = shipCore.SlimBlock;
            if (slimBlock == null) return;

            float repairAmount = 20;
            slimBlock.IncreaseMountLevel(repairAmount, 0L, null, 0f, false, MyOwnershipShareModeEnum.Faction);
        }

        private void ShipCoreEnabledChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != shipCore.EntityId) return;
            if (shipCore.IsFunctional)
            {
                MyAPIGateway.Utilities.ShowNotification("Block is functional. Resetting countdown.", 2000);
                triggerTick = 0;
            }
        }

        public override void Close()
        {
            if (shipCore != null)
            {
                shipCore.EnabledChanged -= ShipCoreEnabledChanged;
            }
        }
    }
}
