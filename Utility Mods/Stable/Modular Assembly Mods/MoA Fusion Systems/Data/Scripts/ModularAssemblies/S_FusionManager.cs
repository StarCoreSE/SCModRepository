using System.Collections.Generic;
using System.Linq;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.FusionParts;
using VRage.Game.ModAPI;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    internal class S_FusionManager
    {
        public static S_FusionManager I = new S_FusionManager();

        private int _ticks;
        public ModularDefinition Definition;
        public Dictionary<int, S_FusionSystem> FusionSystems = new Dictionary<int, S_FusionSystem>();
        private static ModularDefinitionApi ModularApi => ModularDefinition.ModularApi;


        public void Load()
        {
            I = this;
            ModularApi.OnReady += () => ModularApi.AddOnAssemblyClose(assemblyId => FusionSystems.Remove(assemblyId));
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

        public void OnPartAdd(int PhysicalAssemblyId, IMyCubeBlock NewBlockEntity, bool IsBaseBlock)
        {
            if (!FusionSystems.ContainsKey(PhysicalAssemblyId))
                FusionSystems.Add(PhysicalAssemblyId, new S_FusionSystem(PhysicalAssemblyId));

            FusionSystems[PhysicalAssemblyId].AddPart(NewBlockEntity);
        }

        public void OnPartRemove(int PhysicalAssemblyId, IMyCubeBlock BlockEntity, bool IsBaseBlock)
        {
            if (!FusionSystems.ContainsKey(PhysicalAssemblyId))
                return;

            // Remove if the connection is broken.
            if (!IsBaseBlock)
                FusionSystems[PhysicalAssemblyId].RemovePart(BlockEntity);

            // TODO: OnAssemblyRemoved
        }
    }
}