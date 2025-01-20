using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Epstein_Fusion_DS.HeatParts
{
    internal class HeatManager
    {
        public static HeatManager I = new HeatManager();

        private readonly Dictionary<IMyCubeGrid, GridHeatManager> _heatSystems =
            new Dictionary<IMyCubeGrid, GridHeatManager>();

        public void Load()
        {
            I = this;
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
        }

        public void Unload()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            foreach (var system in _heatSystems.Values)
                system.Unload();
            I = null;
        }

        public void UpdateTick()
        {
            foreach (var system in _heatSystems.Values)
                system.UpdateTick();
        }

        public void RemoveAssembly(int assemblyId)
        {
            foreach (var system in _heatSystems.Values)
                system.RemoveAssembly(assemblyId);
        }

        public float GetGridHeatLevel(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.HeatRatio ?? -1;
        }

        public float GetGridHeatCapacity(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.HeatCapacity ?? -1;
        }

        public float GetGridHeatDissipation(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.GrossHeatDissipation ?? -1;
        }

        public float GetGridHeatGeneration(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.HeatGeneration ?? -1;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid) || entity.Physics == null)
                return;
            var grid = (IMyCubeGrid)entity;

            _heatSystems[grid] = new GridHeatManager(grid);
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid) || entity.Physics == null)
                return;
            var grid = (IMyCubeGrid)entity;

            _heatSystems[grid].Unload();
            _heatSystems.Remove(grid);
        }

        public void OnPartAdd(int assemblyId, IMyCubeBlock block, bool isBaseBlock)
        {
            _heatSystems[block.CubeGrid].OnPartAdd(assemblyId, block, isBaseBlock);
        }

        public void OnPartRemove(int assemblyId, IMyCubeBlock block, bool isBaseBlock)
        {
            _heatSystems[block.CubeGrid].OnPartRemove(assemblyId, block, isBaseBlock);
        }
    }
}