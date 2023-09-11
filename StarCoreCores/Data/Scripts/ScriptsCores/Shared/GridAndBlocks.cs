using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using Sandbox.Definitions;

namespace MIG.Shared.SE {
    static class GridAndBlocks {
        public static void GetRefinerySpeedAndYield(this IMyRefinery rf, out double speed, out double yield, out double power, out double ingotsPerSec) {
            var def = (rf.SlimBlock.BlockDefinition as MyRefineryDefinition);
            speed = (1+rf.UpgradeValues["Productivity"]) * def.RefineSpeed;
            yield = rf.UpgradeValues["Effectiveness"] * def.MaterialEfficiency;
            power = rf.UpgradeValues["PowerEfficiency"];
            ingotsPerSec = speed * yield;
        }

        public static void GetAssemblerSpeedAndPower(this IMyAssembler rf, out double speed, out double power) {
            var def = (rf.SlimBlock.BlockDefinition as MyAssemblerDefinition);
            speed = (1+rf.UpgradeValues["Productivity"]) * def.AssemblySpeed;
            power = rf.UpgradeValues["PowerEfficiency"];
        }

        

        public static bool checkSubtypeName(this IMySlimBlock x, string[] startsWith = null, string[] exactNames = null, bool onlyFunctional = false) {
            var bd = x.BlockDefinition;
            if (bd == null || bd.Id == null || bd.Id.SubtypeName == null) return false;

            var t = bd.Id.SubtypeName;

            if (onlyFunctional) {
                if (!x.FatBlock.IsFunctional) {
                    return false;
                }
            }

            if (startsWith != null) {
                foreach (var y in startsWith) {
                    if (t.StartsWith(y)) return true;
                }
            }
            if (exactNames != null) {
                foreach (var z in exactNames) {
                    if (z.Equals(t)) return true;
                }
            }
            return false;
        }

        public static List<IMySlimBlock> GetBlocksBySubtypeName(this IMyCubeGrid g, List<IMySlimBlock> blocks, string[] startsWith = null, string[] exactNames = null, bool onlyFunctional = false, bool inAllGrids = false) {
            if (blocks == null) blocks = new List<IMySlimBlock>();

            g.GetBlocks(blocks, x => checkSubtypeName(x, startsWith, exactNames, onlyFunctional));

            if (inAllGrids) {
                var connectedGrids = MyAPIGateway.GridGroups.GetGroup(g, GridLinkTypeEnum.Physical);
                foreach (var y in connectedGrids) {
                    y.GetBlocks(blocks, x => checkSubtypeName(x, startsWith, exactNames, onlyFunctional));
                }
            }

            return blocks;
        }


        public static T FindBlock<T>(this IMyCubeGrid y) {
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);

            foreach (var x in blocks) {
                if (x.FatBlock == null) {
                    //Log.Info("Find Block failed:" + x.GetType().Name + " " + x);
                    continue;
                }
                if (x.FatBlock is T) {
                    return (T)(x.FatBlock);
                } else {
                    //Log.Info("Find Block failed:" + x.GetType().Name + " " + x.FatBlock.GetType().Name);
                }
            }

            return default(T);
        }


        public static ICollection<T> FindBlocks<T>(this IMyCubeGrid y, ICollection<T> ret, Func<IMySlimBlock, bool> filter) {
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);
            foreach (var x in blocks) {
                if (filter(x)) ret.Add((T)x.FatBlock);
            }

            return ret;
        }

        public static void FindBlocks (this IMyCubeGrid y, Action<IMySlimBlock> filter) {
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);
            foreach (var x in blocks) {
                filter.Invoke(x);
            }
        }

        public static ICollection<T> FindBlocks<T>(this IMyCubeGrid y, ICollection<T> ret) {
            return y.FindBlocks<T>(ret, x => {
                var fat = x.FatBlock;
                if (fat == null) return false;
                return fat is T;
            });
        }

        public static List<IMySlimBlock> FindBlocks(this IMyCubeGrid y, Func<IMySlimBlock, bool> filter) {
            var blocks = new List<IMySlimBlock>();
            var ret = new List<IMySlimBlock>();
            y.GetBlocks(blocks);
            foreach (var x in blocks) {
                if (filter(x)) ret.Add(x);
            }

            return ret;
        }

        public static List<T> FindBlocksOfType<T>(this IMyCubeGrid y) where T : IMyCubeBlock
        {
            var ret = new List<T>();
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);

            foreach (var x in blocks)
            {
                var fat = x.FatBlock;
                if (fat == null) continue;
                if (!(fat is T)) continue;

                ret.Add((T)fat);
            }

            return ret;
        }


        public static T FindBlock<T>(this IMyCubeGrid y, string name) where T : IMyTerminalBlock {
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);

            foreach (var x in blocks) {
                var fat = x.FatBlock;
                if (fat == null) continue;
                if (!(fat is T)) continue;
                var f = (T)fat;
                if (f.CustomName.Equals(name)) return (T)(x.FatBlock);
            }

            return default(T);
        }

        public static IMySlimBlock FindBlock(this IMyCubeGrid y, Func<IMySlimBlock, bool> filter) {
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);
            foreach (var x in blocks) {
                if (filter(x)) return x;
            }

            return null;
        }
    }
}
