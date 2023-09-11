using System;
using System.Collections.Generic;
using MIG.Shared.SE;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public static class Sugar2
    {
        public static List<IMyCubeGrid> GetConnectedGrids(this IMyCubeGrid grid, GridConnectionType with, List<IMyCubeGrid> list = null, bool clear = false) {
            if (with == GridConnectionType.None)
            {
                if (list == null) list = new List<IMyCubeGrid>();
                list.Add(grid);
                return list;
            }
            else
            {
                if (clear)
                {
                    list?.Clear();
                }
                return grid.GetConnectedGrids(with.Get(), list, clear);
            }
        }

        
        
        public static void IncreaseMountLevelToFunctionalState(this IMySlimBlock block, float value, IMyInventory outputInventory, long owner = 0, MyOwnershipShareModeEnum shareModeEnum = MyOwnershipShareModeEnum.Faction, MyCubeBlockDefinition def = null)
        {
            def = def ?? (block.BlockDefinition as MyCubeBlockDefinition);
            block.IncreaseMountLevelToDesiredRatio(def.CriticalIntegrityRatio, value, outputInventory, owner, shareModeEnum, def);
        }
        
        public static void IncreaseMountLevelToDesiredRatio(this IMySlimBlock block, float desiredIntegrityRatio, float value, IMyInventory outputInventory, long owner = 0, MyOwnershipShareModeEnum shareModeEnum = MyOwnershipShareModeEnum.Faction, MyCubeBlockDefinition def = null)
        {
            def = def ?? (block.BlockDefinition as MyCubeBlockDefinition);
            float desiredIntegrity = desiredIntegrityRatio * block.MaxIntegrity + value;
            float welderAmount = desiredIntegrity - block.Integrity;
            
            if (welderAmount <= 0f)
                return;

            IncreaseMountLevelByDesiredRatio(block, 0f, welderAmount, outputInventory, owner, shareModeEnum, def);
        }
        
        public static void IncreaseMountLevelByDesiredRatio(this IMySlimBlock block, float desiredIntegrityRatio, float value, IMyInventory outputInventory, long owner = 0, MyOwnershipShareModeEnum shareModeEnum = MyOwnershipShareModeEnum.Faction, MyCubeBlockDefinition def = null)
        {
            def = def ?? (block.BlockDefinition as MyCubeBlockDefinition);
            float desiredIntegrity = desiredIntegrityRatio * block.MaxIntegrity + value;
           
            if (desiredIntegrity <= 0f)
                return;

            TorchExtensions.IncreaseMountLevel(
                block, 
                desiredIntegrity / def.IntegrityPointsPerSec, 
                owner,
                (MyInventoryBase)outputInventory, 
                0, 
                false, 
                shareModeEnum, false);
            //block.IncreaseMountLevel(desiredIntegrity / def.IntegrityPointsPerSec, owner, outputInventory, 0, false, shareModeEnum);
        }
        
        
        
        public static void DecreaseMountLevelToFunctionalState(this IMySlimBlock block, IMyInventory outputInventory, float desiredIntegrityValue, MyCubeBlockDefinition def = null)
        {
            def = def ?? (block.BlockDefinition as MyCubeBlockDefinition);
            block.DecreaseMountLevelToDesiredRatio(def.CriticalIntegrityRatio, desiredIntegrityValue, false, outputInventory);
        }
        
        public static void DecreaseMountLevelToDesiredRatio(this IMySlimBlock block, float desiredIntegrityRatio, float desiredIntegrityValue, bool canDestroy, IMyInventory outputInventory, MyCubeBlockDefinition def = null)
        {
            def = def ?? (block.BlockDefinition as MyCubeBlockDefinition);
            float desiredIntegrity = desiredIntegrityRatio * block.MaxIntegrity;
            float grinderAmount = block.Integrity - desiredIntegrity - desiredIntegrityValue;
            
            DecreaseMountLevelByDesiredRatio(block, 0, grinderAmount, canDestroy, outputInventory, def);
        }
        
        public static void DecreaseMountLevelByDesiredRatio(this IMySlimBlock block, float desiredIntegrityRatio, float desiredIntegrityValue, bool canDestroy, IMyInventory outputInventory, MyCubeBlockDefinition def = null)
        {
            def = def ?? (block.BlockDefinition as MyCubeBlockDefinition);
        
            float grinderAmount = desiredIntegrityRatio * block.MaxIntegrity + desiredIntegrityValue;
            
            if (!canDestroy)
            {
                grinderAmount = Math.Min(grinderAmount, block.Integrity - 1);
            }
            
            if (grinderAmount <= 0f)
                return;

            if (block.FatBlock != null)
                grinderAmount *= block.FatBlock.DisassembleRatio;
            else
                grinderAmount *= def.DisassembleRatio;
            
            block.DecreaseMountLevel(grinderAmount  / def.IntegrityPointsPerSec, outputInventory, useDefaultDeconstructEfficiency: true);
        }

        public static GridLinkTypeEnum Get(this GridConnectionType type)
        {
            switch (type)
            {
                case GridConnectionType.Electrical: return GridLinkTypeEnum.Electrical;
                case GridConnectionType.Logical: return GridLinkTypeEnum.Logical;
                case GridConnectionType.Mechanical: return GridLinkTypeEnum.Mechanical;
                case GridConnectionType.Physical: return GridLinkTypeEnum.Physical;
                case GridConnectionType.NoContactDamage: return GridLinkTypeEnum.NoContactDamage;
                default: return GridLinkTypeEnum.Electrical;
            }
        }
        
        public static ILimitedBlock GetLimitedBlock(this IMyTerminalBlock t)
        {
            var ship = t.CubeGrid.GetShip();
            if (ship == null) return null;
            ILimitedBlock shipSpecBlock;
            if (ship.LimitedBlocks.TryGetValue(t, out shipSpecBlock))
            {
                return shipSpecBlock;
            }
            return null;
        }

        public static Limits GetLimits(this UsedPoints[] usedPoints, BlockId blockId)
        {
            float mlt = blockId.Mlt * blockId.Mlt2;
            
            Limits result = new Limits();
            if (usedPoints == null) return result;

            foreach (var u in usedPoints)
            {
                float value;

                if (u.UseTierValue) value = OriginalSpecCoreSession.Instance.GetTier(blockId); 
                else if (u.UseCustomValue) value = blockId.CustomValue;
                else value = u.Amount * (u.UseMlts ? mlt : 1);
                
                if (!float.IsNaN(u.MinValue)) value = Math.Max(u.MinValue, value);
                if (u.RoundLimits) value = (float)Math.Round(value);
                result[u.PointId] = value;
            }

            return result;
        }
        
        public static Limits GetLimitsForNoSpecBlocks(this UsedPoints[] usedPoints)
        {
            Limits result = new Limits();
            if (usedPoints == null) return result;

            foreach (var u in usedPoints)
            {
                result[u.PointId] = u.Amount;
            }

            foreach (var kv in OriginalSpecCoreSession.Instance.Points)
            {
                if (!result.ContainsKey(kv.Key))
                {
                    result[kv.Key] = kv.Value.DefaultNoSpecCoreValue;
                }
            }

            return result;
        }

        public static Limits GetLimitsForSpecBlocks(this UsedPoints[] defaultValues, UsedPoints[] main)
        {
            Limits result = new Limits();
            if (defaultValues != null)
            {
                foreach (var u in defaultValues)
                {
                    result[u.PointId] = u.Amount;
                }
            }

            if (main != null)
            {
                foreach (var u in main)
                {
                    result[u.PointId] = u.Amount;
                }
            }

            foreach (var kv in OriginalSpecCoreSession.Instance.Points)
            {
                if (!result.ContainsKey(kv.Key))
                {
                    result[kv.Key] = kv.Value.DefaultSpecCoreValue;
                }
            }

            return result;
        }
        
        public static ISpecBlock GetSpecBlock(this IMyTerminalBlock t)
        {
            var ship = t.CubeGrid.GetShip();
            if (ship == null) return null;
            ISpecBlock shipSpecBlock;
            if (ship.SpecBlocks.TryGetValue(t, out shipSpecBlock))
            {
                return shipSpecBlock;
            }
            return null;
        }

        public static SpecBlock GetUpgradableSpecBlock(this IMyTerminalBlock t)
        {
            return (SpecBlock)GetSpecBlock (t);
        }
    }
}