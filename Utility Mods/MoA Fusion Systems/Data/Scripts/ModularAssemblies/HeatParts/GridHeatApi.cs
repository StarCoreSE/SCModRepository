using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Epstein_Fusion_DS.HeatParts
{
    /// <summary>
    /// Copy this class into another mod and call Load() in LoadData() to use the grid heat API.
    /// </summary>
    public sealed class GridHeatApi
    {
        private const long Channel = 11920040L;
        private const string ApiRequest = "GridHeatAPI.Request";

        private Action _onReady;
        private Dictionary<string, Delegate> _api;
        private bool _registered;

        public bool IsReady => _api != null;

        public void Load(Action onReady = null)
        {
            if (_registered)
                return;

            _onReady = onReady;
            _registered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(Channel, ApiRequest);
        }

        public void Unload()
        {
            if (!_registered)
                return;

            MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);
            _registered = false;
            _api = null;
            _onReady = null;
        }

        public int Version()
        {
            var method = Get<Func<int>>("Version");
            return method?.Invoke() ?? 0;
        }

        public bool AddHeat(IMyCubeGrid grid, float heat)
        {
            var method = Get<Func<IMyCubeGrid, float, bool>>("AddHeat");
            return method != null && method(grid, heat);
        }

        public bool SetHeat(IMyCubeGrid grid, float heat)
        {
            var method = Get<Func<IMyCubeGrid, float, bool>>("SetHeat");
            return method != null && method(grid, heat);
        }

        public float GetHeat(IMyCubeGrid grid)
        {
            var method = Get<Func<IMyCubeGrid, float>>("GetHeat");
            return method?.Invoke(grid) ?? -1;
        }

        public float GetHeatRatio(IMyCubeGrid grid)
        {
            var method = Get<Func<IMyCubeGrid, float>>("GetHeatRatio");
            return method?.Invoke(grid) ?? -1;
        }

        public float GetHeatCapacity(IMyCubeGrid grid)
        {
            var method = Get<Func<IMyCubeGrid, float>>("GetHeatCapacity");
            return method?.Invoke(grid) ?? -1;
        }

        public float GetHeatDissipation(IMyCubeGrid grid)
        {
            var method = Get<Func<IMyCubeGrid, float>>("GetHeatDissipation");
            return method?.Invoke(grid) ?? -1;
        }

        public float GetHeatGeneration(IMyCubeGrid grid)
        {
            var method = Get<Func<IMyCubeGrid, float>>("GetHeatGeneration");
            return method?.Invoke(grid) ?? -1;
        }

        public bool SetHeatCapacity(IMyCubeGrid grid, float capacity)
        {
            var method = Get<Func<IMyCubeGrid, float, bool>>("SetHeatCapacity");
            return method != null && method(grid, capacity);
        }

        public bool SetHeatDissipation(IMyCubeGrid grid, float dissipation)
        {
            var method = Get<Func<IMyCubeGrid, float, bool>>("SetHeatDissipation");
            return method != null && method(grid, dissipation);
        }

        public bool SetHeatGeneration(IMyCubeGrid grid, float generation)
        {
            var method = Get<Func<IMyCubeGrid, float, bool>>("SetHeatGeneration");
            return method != null && method(grid, generation);
        }

        public void SetHudVisible(bool visible)
        {
            Get<Action<bool>>("SetHudVisible")?.Invoke(visible);
        }

        public void SetHudOffset(float x, float y, float z)
        {
            Get<Action<float, float, float>>("SetHudOffset")?.Invoke(x, y, z);
        }

        private T Get<T>(string name) where T : class
        {
            if (_api == null || !_api.ContainsKey(name))
                return null;

            return _api[name] as T;
        }

        private void HandleMessage(object obj)
        {
            var api = obj as Dictionary<string, Delegate>;
            if (api == null)
                return;

            _api = api;
            _onReady?.Invoke();
            _onReady = null;
        }
    }
}
