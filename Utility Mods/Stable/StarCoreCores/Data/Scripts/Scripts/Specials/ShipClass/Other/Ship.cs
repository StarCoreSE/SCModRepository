using System;
using System.Collections.Generic;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public class Ship
    {
        public IMyCubeGrid grid;
        public HashSet<MyShipController> Cockpits = new HashSet<MyShipController>();
        public Dictionary<IMyCubeBlock, ISpecBlock> SpecBlocks = new Dictionary<IMyCubeBlock, ISpecBlock>();
        public Dictionary<IMyCubeBlock, ILimitedBlock> LimitedBlocks = new Dictionary<IMyCubeBlock, ILimitedBlock>();
        public ISpecBlock CachedCore = null;

        public Dictionary<Type, HashSet<IMyCubeBlock>> BlocksCache;
        public Dictionary<MyDefinitionId, HashSet<IMyCubeBlock>> BlocksCacheByType;
        
        public AutoTimer limitsLastChecked = new AutoTimer(OriginalSpecCoreSession.Instance.Settings.Timers.CheckLimitsInterval, new Random().Next(OriginalSpecCoreSession.Instance.Settings.Timers.CheckLimitsInterval));

        public Ship(IMyCubeGrid grid)
        {
            if (OriginalSpecCoreSession.Instance.Settings.Cache?.EnableBlockCache ?? false)
            {
                BlocksCache = new Dictionary<Type, HashSet<IMyCubeBlock>>();
                BlocksCacheByType = new Dictionary<MyDefinitionId, HashSet<IMyCubeBlock>>();
            }
            
            this.grid = grid;
            grid.FindBlocks((x)=>Grid_OnBlockAdded(x));
            grid.OnBlockAdded += Grid_OnBlockAdded;
            grid.OnBlockRemoved += Grid_OnBlockRemoved;
            grid.OnMarkForClose += Grid_OnMarkForClose;
            grid.OnGridSplit += Grid_OnGridSplit;
        }
        
        private void Grid_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
            grid.OnBlockAdded -= Grid_OnBlockAdded;
            grid.OnBlockRemoved -= Grid_OnBlockRemoved;
            grid.OnMarkForClose -= Grid_OnMarkForClose;
            grid.OnGridSplit -= Grid_OnGridSplit;
        }
        
        private void Grid_OnBlockRemoved(IMySlimBlock obj) {
            onAddedRemoved (obj, false);
        }
        private void Grid_OnBlockAdded(IMySlimBlock obj) {
            onAddedRemoved (obj, true);
        }

        private void Grid_OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2) {
            //LimitsChecker.OnGridSplit(arg1, arg2);
        }

        public void BeforeSimulation()
        {
            
            //RefreshConnections(this);
            if (limitsLastChecked.tick()) {
                LimitsChecker.CheckLimitsInGrid(grid);
            }
        }

        internal void onAddedRemoved(IMySlimBlock obj, bool added) {
            
            var fat = obj.FatBlock;
            if (fat != null) {
                RegisterUnregisterType(fat, added, Cockpits);

                if (added)
                {
                    ISpecBlock specBlock;
                    ILimitedBlock limitedBlock;
                    if (OriginalSpecCoreSession.GetLimitedBlock(fat, out limitedBlock, out specBlock))
                    {
                        if (limitedBlock != null)
                        {
                            LimitedBlocks[fat] = limitedBlock;
                        }
                        if (specBlock != null)
                        {
                            SpecBlocks[fat] = specBlock;
                        }
                    }

                    if (OriginalSpecCoreSession.Instance.Settings.Cache?.EnableBlockCache ?? false)
                    {
                        BlocksCache.GetOrNew(fat.GetType()).Add(fat);
                    }
                    
                }
                else
                {
                    ILimitedBlock block;
                    ISpecBlock spec;


                    if (LimitedBlocks.TryGetValue(fat, out block))
                    {
                        block.Destroy();
                        LimitedBlocks.Remove(fat);
                        Hooks.TriggerOnLimitedBlockDestroyed(block);
                    }

                    if (SpecBlocks.TryGetValue(fat, out spec))
                    {
                        spec.Destroy();
                        SpecBlocks.Remove(fat);
                        Hooks.TriggerOnSpecBlockDestroyed(spec);
                    }

                    if (OriginalSpecCoreSession.Instance.Settings.Cache?.EnableBlockCache ?? false)
                    {
                        BlocksCache.GetOrNew(fat.GetType()).Remove(fat);
                    }
                }
            }
        }
        
        private bool RegisterUnregisterType<T>(IMyCubeBlock fat, bool added, ICollection<T> collection) where T : IMyCubeBlock
        {
            if (fat is T)
            {
                if (added) collection.Add((T)fat);
                else collection.Remove((T)fat);
                return true;
            }

            return false;
        }

        public void ResetLimitsTimer()
        {
            limitsLastChecked.reset();
        }
    }
}