using System.Collections.Generic;
using Digi;
using MIG.Shared.SE;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public class GridGroupInfo
    {
        public int TotalPCU = 0;
        public int TotalBlocks = 0;
        public int LargeGrids = 0;
        public int SmallGrids = 0;
        public void Calculate(List<IMyCubeGrid> grids)
        {
            LargeGrids = 0;
            SmallGrids = 0;
            TotalBlocks = 0;
            TotalPCU = 0;
            foreach (var x in grids)
            {
                LargeGrids += x.GridSizeEnum == MyCubeSize.Large ? 1 : 0;
                SmallGrids += x.GridSizeEnum == MyCubeSize.Small ? 1 : 0;
                TotalPCU += (x as MyCubeGrid).BlocksPCU;
                TotalBlocks += (x as MyCubeGrid).BlocksCount;
            }
        }

        public override string ToString()
        {
            return $"LargeGrids={LargeGrids} SmallGrids={SmallGrids} TotalPCU={TotalPCU} TotalBlocks={TotalBlocks}";
        }

        public TypeOfGridGroup GetTypeOfGridGroup(List<IMyCubeGrid> grids)
        {
            TypeOfGridGroup result = TypeOfGridGroup.None;
            foreach (var g in grids)
            {
                if (g.IsStatic)
                {
                    result |= TypeOfGridGroup.Static;
                    break;
                }
            }
            result |= LargeGrids > 0 ? TypeOfGridGroup.Large : TypeOfGridGroup.None;
            result |= SmallGrids > 0 ? TypeOfGridGroup.Small : TypeOfGridGroup.None;
            return result;
        }

        public bool CanBeApplied(Limits limits, out string error)
        {
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MAX_LARGEGRIDS, LargeGrids, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MAX_SMALLGRIDS, SmallGrids, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MAX_GRIDS, LargeGrids + SmallGrids, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MAX_PCU, TotalPCU, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MAX_BLOCKS, TotalBlocks, out error)) return false;
            
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MIN_LARGEGRIDS, LargeGrids, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MIN_SMALLGRIDS, SmallGrids, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MIN_GRIDS, LargeGrids + SmallGrids, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MIN_PCU, TotalPCU, out error)) return false;
            if (!CheckSpecCoreLimit(limits, LimitsChecker.TYPE_MIN_BLOCKS, TotalBlocks, out error)) return false;

            return true;
        }


        private bool CheckSpecCoreLimit(Limits limits, int id, float valueToCheck, out string error)
        {
            error = null;
            float value;
            
            var lp = OriginalSpecCoreSession.Instance.Points.GetOr(id, null);
            if (lp == null)
            {
                return true;
            }
            
            if (limits.TryGetValue(id, out value))
            {
                if (lp.Behavior == PointBehavior.LessOrEqual)
                {
                    if (valueToCheck > value)
                    {
                        error = lp.ActivationError;
                        return false;
                    }
                }
                else if (lp.Behavior == PointBehavior.MoreOrEqual)
                {
                    if (valueToCheck < value)
                    {
                        error = lp.ActivationError;
                        return false;
                    }
                }
                else
                {
                    if (OriginalSpecCoreSession.IsDebug)
                    {
                        Log.ChatError($"Wrong behaviour: {lp.Behavior} for PointId={lp.Id}");
                    }
                    return true;
                }
            }
            return true;
        }
    }
}