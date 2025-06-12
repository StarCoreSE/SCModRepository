using System.Collections.Generic;
using System.Linq;
using Epstein_Fusion_DS.Communication;
using VRage.Game.ModAPI;

namespace Epstein_Fusion_DS.FusionParts
{
    internal class SFusionManager
    {
        public static SFusionManager I = new SFusionManager();

        private int _ticks;
        public ModularDefinition FusionDefinition;
        public Dictionary<int, SFusionSystem> FusionSystems = new Dictionary<int, SFusionSystem>();
        public ModularDefinition HeatDefinition;
        private static ModularDefinitionApi ModularApi => Epstein_Fusion_DS.ModularDefinition.ModularApi;

        public void Load()
        {
            I = this;
        }

        public void Unload()
        {
            I = null;
        }

        public void UpdateTick()
        {
            foreach (var fusionSystem in FusionSystems.Values)
                fusionSystem.UpdateTick();

            if (_ticks % 100 == 0)
                Update100();

            _ticks++;
        }

        private void Update100()
        {
            var systems = ModularApi.GetAllAssemblies();
            foreach (var fusionSystem in FusionSystems.Values.ToList())
                // Remove invalid systems
                if (!systems.Contains(fusionSystem.PhysicalAssemblyId))
                    FusionSystems.Remove(fusionSystem.PhysicalAssemblyId);
        }

        public void OnPartAdd(int physicalAssemblyId, IMyCubeBlock newBlockEntity, bool isBaseBlock)
        {
            if (!FusionSystems.ContainsKey(physicalAssemblyId))
                FusionSystems.Add(physicalAssemblyId, new SFusionSystem(physicalAssemblyId));

            FusionSystems[physicalAssemblyId].AddPart(newBlockEntity);
        }

        public void OnPartRemove(int physicalAssemblyId, IMyCubeBlock blockEntity, bool isBaseBlock)
        {
            if (!FusionSystems.ContainsKey(physicalAssemblyId))
                return;

            // Remove if the connection is broken.
            if (!isBaseBlock)
                FusionSystems[physicalAssemblyId].RemovePart(blockEntity);

            // TODO: OnAssemblyRemoved
        }
    }
}