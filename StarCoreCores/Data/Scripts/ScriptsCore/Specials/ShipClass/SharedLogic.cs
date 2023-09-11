using System;
using MIG.Shared.SE;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public static class SharedLogic
    {
        public static Func<int, float, bool> NoZeroLimits = (k, v) => (v != 0);
        
        public static bool IsDrainingPoints(IMyCubeBlock block, IMyFunctionalBlock fblock, Behavior behavior, ConsumeBehaviorType type)
        {
            switch (type)
            {
                case ConsumeBehaviorType.Always:
                    return true;
                
                case ConsumeBehaviorType.IsEnabled:
                    return fblock.Enabled != behavior.Reverse;
                case ConsumeBehaviorType.IsWorking:
                    return (fblock.IsWorking) != behavior.Reverse;
                case ConsumeBehaviorType.IsSinkingResource:
                    return (fblock.ResourceSink.CurrentInputByType(behavior.GetSinkDefinition()) > behavior.Value3) == behavior.Reverse;
                case ConsumeBehaviorType.IsFunctional:
                    return block.IsFunctional != behavior.Reverse;
                case ConsumeBehaviorType.IsArmed:
                    return (block as IMyWarhead).IsArmed == behavior.Reverse;
                case ConsumeBehaviorType.IsProducingResource:
                    return (block.Components.Get<MyResourceSourceComponent>().CurrentOutputByType(behavior.GetSinkDefinition()) > behavior.Value3) == behavior.Reverse;
                case ConsumeBehaviorType.Integrity:
                    return (block.SlimBlock.Integrity > block.SlimBlock.MaxIntegrity * behavior.Value1 + behavior.Value3) == behavior.Reverse;
                
                case ConsumeBehaviorType.DrillHarvestMlt:
                    return ((block as IMyShipDrill).DrillHarvestMultiplier >= behavior.Value1) != behavior.Reverse;
                case ConsumeBehaviorType.InventoryExternalMass:
                    var inv = block.GetInventory() as MyInventory;
                    return ((double)inv.ExternalMass >= behavior.Value1) != behavior.Reverse;
                case ConsumeBehaviorType.ThrustMlt:
                    return ((block as IMyThrust).ThrustMultiplier >= behavior.Value1) != behavior.Reverse;
                case ConsumeBehaviorType.ThrustPowerConsumptionMlt:
                    return ((block as IMyThrust).PowerConsumptionMultiplier >= behavior.Value1) != behavior.Reverse;
                case ConsumeBehaviorType.IsOwnedBy:
                    return behavior.CheckFactionOrUser(block.OwnerId.PlayerFaction(), block.OwnerId);
                case ConsumeBehaviorType.IsBuiltBy:
                    return behavior.CheckFactionOrUser(block.BuiltBy().PlayerFaction(), block.OwnerId);
                case ConsumeBehaviorType.CustomLogic:
                    //TODO SLIME
                    return true;
                default:
                    return true;
            }
        }
    }
}