using System.Collections.Generic;
using FusionSystems.Communication;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace FusionSystems.HeatParts
{
    internal class GridHeatManager
    {
        private static ModularDefinitionApi ModularApi => ModularDefinition.ModularApi;

        private const float BaseHeatCapacityModifier = 0.25f;
        private const float BaseHeatDissipationModifier = 1/1800f;

        public MyCubeGrid Grid { get; private set; }
        private Dictionary<int, HeatSystem> _heatSystems = new Dictionary<int, HeatSystem>();

        public float HeatRatio = float.PositiveInfinity;
        public float HeatCapacity = 0;
        public float BaseHeatCapacity = 0;

        public float HeatStored = 0;

        public float HeatDissipation = 0;
        public float BaseHeatDissipation = 0;

        public float HeatGeneration = 0;

        public void UpdateTick()
        {
            if (_ticks % 15 == 0)
                Update15Tick();
            _ticks++;

            if ((HeatCapacity + BaseHeatCapacity) == 0)
            {
                HeatRatio = 1;
                HeatStored = 0;
                return;
            }

            HeatStored = HeatStored - (HeatDissipation + BaseHeatDissipation)*HeatRatio/60 + HeatGeneration/60;
            if (HeatStored < 0)
                HeatStored = 0;
            else if (HeatStored > (HeatCapacity + BaseHeatCapacity))
                HeatStored = (HeatCapacity + BaseHeatCapacity);

            HeatRatio = HeatStored / (HeatCapacity + BaseHeatCapacity);
            MyAPIGateway.Utilities.ShowNotification($"Heat: {HeatRatio*100:N0}% |  {HeatStored:F}/{(HeatCapacity + BaseHeatCapacity):F} | +{HeatGeneration:F} -{(HeatDissipation + BaseHeatDissipation)*HeatRatio:F}/s", 1000/60);
        }

        private int _ticks = 0;
        private void Update15Tick()
        {
            Vector3 gridSize = (Grid.Max - Grid.Min) * Grid.GridSize;

            BaseHeatCapacity = Grid.BlocksCount * BaseHeatCapacityModifier;
            BaseHeatDissipation = 2 * (gridSize.X * gridSize.Y + gridSize.Y * gridSize.Z + gridSize.Z * gridSize.X) * BaseHeatDissipationModifier;

            HeatGeneration = 0;
            foreach (int assemblyId in ModularApi.GetGridAssemblies(Grid))
            {
                HeatGeneration += ModularApi.GetAssemblyProperty<float>(assemblyId, "HeatGeneration"); // Can pull from all heat sources
            }
        }

        public GridHeatManager(IMyCubeGrid grid)
        {
            Grid = (MyCubeGrid) grid;
        }

        public void Unload()
        {
            foreach (var system in _heatSystems.Values)
                system.OnClose();
        }

        public void OnPartAdd(int assemblyId, IMyCubeBlock block, bool isBaseBlock)
        {
            if (!_heatSystems.ContainsKey(assemblyId))
            {
                _heatSystems[assemblyId] = new HeatSystem(assemblyId, this);
            }

            _heatSystems[assemblyId].OnBlockAdd(block);
        }

        public void OnPartRemove(int assemblyId, IMyCubeBlock block, bool isBaseBlock)
        {
            HeatSystem system = _heatSystems[assemblyId];
            system.OnBlockRemove(block);

            if (system.BlockCount <= 0)
            {
                system.OnClose();
                _heatSystems.Remove(assemblyId);
            }
        }
    }
}
