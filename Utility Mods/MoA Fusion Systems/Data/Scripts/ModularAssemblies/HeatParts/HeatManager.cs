using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Epstein_Fusion_DS.HeatParts
{
    internal class HeatManager
    {
        public const long ApiChannel = 11920040L;
        private const string ApiRequest = "GridHeatAPI.Request";

        public static HeatManager I = new HeatManager();

        private readonly Dictionary<IMyCubeGrid, GridHeatManager> _heatSystems =
            new Dictionary<IMyCubeGrid, GridHeatManager>();

        private Dictionary<string, Delegate> _api;

        public bool HeatHudVisible = true;
        public Vector3D HeatHudOffset = new Vector3D(-0.759375, -0.8, 0);

        public void Load()
        {
            I = this;
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
            MyAPIGateway.Utilities.RegisterMessageHandler(ApiChannel, HandleApiRequest);
            RegisterExistingGrids();
        }

        public void Unload()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiChannel, HandleApiRequest);
            foreach (var system in _heatSystems.Values)
                system.Unload();
            _heatSystems.Clear();
            _api = null;
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

        public float GetGridHeat(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.HeatStored ?? -1;
        }

        public float GetGridHeatCapacity(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.TotalHeatCapacity ?? -1;
        }

        public float GetGridHeatDissipation(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.GrossHeatDissipation ?? -1;
        }

        public float GetGridHeatGeneration(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.HeatGeneration ?? -1;
        }

        public bool HasGridHeat(IMyCubeGrid grid)
        {
            return _heatSystems.GetValueOrDefault(grid, null)?.HasApiHeat ?? false;
        }

        public bool AddGridHeat(IMyCubeGrid grid, float heat)
        {
            var system = EnsureGrid(grid);
            if (system == null)
                return false;

            system.AddHeat(heat);
            return true;
        }

        public bool SetGridHeat(IMyCubeGrid grid, float heat)
        {
            var system = EnsureGrid(grid);
            if (system == null)
                return false;

            system.SetHeat(heat);
            return true;
        }

        public bool SetGridHeatCapacity(IMyCubeGrid grid, float capacity)
        {
            var system = EnsureGrid(grid);
            if (system == null)
                return false;

            system.ApiHeatCapacity = Math.Max(0, capacity);
            system.SetHeat(system.HeatStored);
            return true;
        }

        public bool SetGridHeatDissipation(IMyCubeGrid grid, float dissipation)
        {
            var system = EnsureGrid(grid);
            if (system == null)
                return false;

            system.ApiHeatDissipation = Math.Max(0, dissipation);
            return true;
        }

        public bool SetGridHeatGeneration(IMyCubeGrid grid, float generation)
        {
            var system = EnsureGrid(grid);
            if (system == null)
                return false;

            system.ApiHeatGeneration = generation;
            system.HeatGeneration = generation;
            return true;
        }

        public void SetHeatHudVisible(bool visible)
        {
            HeatHudVisible = visible;
        }

        public void SetHeatHudOffset(float x, float y, float z)
        {
            HeatHudOffset = new Vector3D(x, y, z);
        }

        private void HandleApiRequest(object obj)
        {
            if (!(obj is string) || (string)obj != ApiRequest)
                return;

            MyAPIGateway.Utilities.SendModMessage(ApiChannel, GetApi());
        }

        private Dictionary<string, Delegate> GetApi()
        {
            return _api ?? (_api = new Dictionary<string, Delegate>
            {
                ["Version"] = new Func<int>(() => 1),
                ["AddHeat"] = new Func<IMyCubeGrid, float, bool>(AddGridHeat),
                ["SetHeat"] = new Func<IMyCubeGrid, float, bool>(SetGridHeat),
                ["GetHeat"] = new Func<IMyCubeGrid, float>(GetGridHeat),
                ["GetHeatRatio"] = new Func<IMyCubeGrid, float>(GetGridHeatLevel),
                ["GetHeatCapacity"] = new Func<IMyCubeGrid, float>(GetGridHeatCapacity),
                ["GetHeatDissipation"] = new Func<IMyCubeGrid, float>(GetGridHeatDissipation),
                ["GetHeatGeneration"] = new Func<IMyCubeGrid, float>(GetGridHeatGeneration),
                ["SetHeatCapacity"] = new Func<IMyCubeGrid, float, bool>(SetGridHeatCapacity),
                ["SetHeatDissipation"] = new Func<IMyCubeGrid, float, bool>(SetGridHeatDissipation),
                ["SetHeatGeneration"] = new Func<IMyCubeGrid, float, bool>(SetGridHeatGeneration),
                ["SetHudVisible"] = new Action<bool>(SetHeatHudVisible),
                ["SetHudOffset"] = new Action<float, float, float>(SetHeatHudOffset),
            });
        }

        private void RegisterExistingGrids()
        {
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid && entity.Physics != null);

            foreach (var entity in entities)
                EnsureGrid((IMyCubeGrid)entity);
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid) || entity.Physics == null)
                return;
            var grid = (IMyCubeGrid)entity;

            EnsureGrid(grid);
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid) || entity.Physics == null)
                return;
            var grid = (IMyCubeGrid)entity;

            if (!_heatSystems.ContainsKey(grid))
                return;

            _heatSystems[grid].Unload();
            _heatSystems.Remove(grid);
        }

        public void OnPartAdd(int assemblyId, IMyCubeBlock block, bool isBaseBlock)
        {
            EnsureGrid(block.CubeGrid)?.OnPartAdd(assemblyId, block, isBaseBlock);
        }

        public void OnPartRemove(int assemblyId, IMyCubeBlock block, bool isBaseBlock)
        {
            EnsureGrid(block.CubeGrid)?.OnPartRemove(assemblyId, block, isBaseBlock);
        }

        private GridHeatManager EnsureGrid(IMyCubeGrid grid)
        {
            if (grid == null)
                return null;

            if (!_heatSystems.ContainsKey(grid))
                _heatSystems[grid] = new GridHeatManager(grid);

            return _heatSystems[grid];
        }
    }
}
