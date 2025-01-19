using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

using Starcore.FieldGenerator.API;
using CoreSystems.Api;
using Draygo.API;
using Sandbox.Game.Entities.Cube;

namespace Starcore.FieldGenerator
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FieldGeneratorSession : MySessionComponentBase
    {
        public static FieldGeneratorSession I;
        public static APIProvider APIProvider;
        public static WcApi CoreSysAPI;
        public static HudAPIv2 HudAPI;

        internal APIBackend APIBackend;
        internal static readonly Dictionary<long, HashSet<long>> ActiveGenerators = new Dictionary<long, HashSet<long>>();

        public override void LoadData()
        {
            I = this;

            InitExistingGeneratorCounts();

            MyCubeGrid.OnBlockAddedGlobally += OnBlockAddedGlobally;
            MyCubeGrid.OnBlockRemovedGlobally += OnBlockRemovedGlobally;
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;

            APIBackend = new APIBackend();
            APIProvider = new APIProvider();
            APIProvider.LoadAPI();

            HudAPI = new HudAPIv2();

            CoreSysAPI = new WcApi();
            CoreSysAPI.Load();
        }

        protected override void UnloadData()
        {          
            if (HudAPI.Heartbeat)
            { 
                HudAPI.Unload();
                HudAPI = null;
            }

            if (CoreSysAPI.IsReady)
            {
                CoreSysAPI.Unload();
                CoreSysAPI = null;
            }

            APIProvider.UnloadAPI();
            APIProvider = null;
            APIBackend = null;

            MyCubeGrid.OnBlockAddedGlobally -= OnBlockAddedGlobally;
            MyCubeGrid.OnBlockRemovedGlobally -= OnBlockRemovedGlobally;
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;

            I = null;
        }

        private void OnBlockAddedGlobally<T>(T slimBlock) where T : IMySlimBlock
        {
            if (slimBlock?.FatBlock == null) 
                return;
            if (!string.Equals(slimBlock.FatBlock.BlockDefinition.SubtypeName, "FieldGen_Core",
                              StringComparison.OrdinalIgnoreCase))
                return;

            var gridId = slimBlock.CubeGrid?.EntityId ?? 0;
            if (gridId == 0) 
                return;

            HashSet<long> set;
            if (!ActiveGenerators.TryGetValue(gridId, out set))
            {
                set = new HashSet<long>();
                ActiveGenerators[gridId] = set;
            }

            set.Add(slimBlock.FatBlock.EntityId);
        }

        private void OnBlockRemovedGlobally<T>(T slimBlock) where T : IMySlimBlock
        {
            if (slimBlock?.FatBlock == null) 
                return;
            if (!string.Equals(slimBlock.FatBlock.BlockDefinition.SubtypeName, "FieldGen_Core", StringComparison.OrdinalIgnoreCase))
                return;

            var gridId = slimBlock.CubeGrid?.EntityId ?? 0;
            if (gridId == 0) return;

            HashSet<long> set;
            if (ActiveGenerators.TryGetValue(gridId, out set))
            {
                set.Remove(slimBlock.FatBlock.EntityId);
                if (set.Count == 0)
                    ActiveGenerators.Remove(gridId);
            }
        }

        private void OnEntityAdd(IMyEntity ent)
        {
            var grid = ent as IMyCubeGrid;
            if (grid == null)
                return;

            var slimBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(slimBlocks);

            foreach (var slimBlock in slimBlocks)
            {
                if (slimBlock?.FatBlock == null)
                    continue;

                if (string.Equals(slimBlock.FatBlock.BlockDefinition.SubtypeName, "FieldGen_Core", StringComparison.OrdinalIgnoreCase))
                {
                    var gridId = grid.EntityId;
                    HashSet<long> set;
                    if (!ActiveGenerators.TryGetValue(gridId, out set))
                    {
                        set = new HashSet<long>();
                        ActiveGenerators[gridId] = set;
                    }
                    set.Add(slimBlock.FatBlock.EntityId);
                }
            }
        }

        private void InitExistingGeneratorCounts()
        {
            var allEntities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allEntities);
            foreach (var entity in allEntities)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null)
                    continue;

                var slimBlocks = new List<IMySlimBlock>();
                grid.GetBlocks(slimBlocks);

                foreach (var slimBlock in slimBlocks)
                {
                    if (slimBlock?.FatBlock == null)
                        continue;

                    if (string.Equals(slimBlock.FatBlock.BlockDefinition.SubtypeName, "FieldGen_Core", StringComparison.OrdinalIgnoreCase))
                    {
                        var gridId = grid.EntityId;
                        HashSet<long> set;
                        if (!ActiveGenerators.TryGetValue(gridId, out set))
                        {
                            set = new HashSet<long>();
                            ActiveGenerators[gridId] = set;
                        }
                        set.Add(slimBlock.FatBlock.EntityId);
                    }
                }
            }
        }
    }
}
