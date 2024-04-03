using System.Collections.Generic;
using System.Linq;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.FusionParts;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    internal class S_FusionManager
    {
        public static S_FusionManager I = new S_FusionManager();
        public ModularDefinition Definition;
        public Dictionary<int, S_FusionSystem> FusionSystems = new Dictionary<int, S_FusionSystem>();
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;


        public void Load()
        {
            I = this;
            ModularAPI.OnReady += () => ModularAPI.AddOnAssemblyClose(assemblyId => FusionSystems.Remove(assemblyId));
        }

        public void Unload()
        {
            I = null;
        }

        private int _ticks = 0;
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
            int[] systems = ModularAPI.GetAllAssemblies();
            foreach (var fusionSystem in FusionSystems.Values.ToList())
            {
                // Remove invalid systems
                if (!systems.Contains(fusionSystem.PhysicalAssemblyId))
                {
                    FusionSystems.Remove(fusionSystem.PhysicalAssemblyId);
                }
            }
        }

        public void OnPartAdd(int PhysicalAssemblyId, MyEntity NewBlockEntity, bool IsBaseBlock)
        {
            if (!FusionSystems.ContainsKey(PhysicalAssemblyId))
                FusionSystems.Add(PhysicalAssemblyId, new S_FusionSystem(PhysicalAssemblyId));

            FusionSystems[PhysicalAssemblyId].AddPart((IMyCubeBlock)NewBlockEntity);
        }

        public void OnPartRemove(int PhysicalAssemblyId, MyEntity BlockEntity, bool IsBaseBlock)
        {
            if (!FusionSystems.ContainsKey(PhysicalAssemblyId))
                return;
            
            // Remove if the connection is broken.
            if (!IsBaseBlock)
                FusionSystems[PhysicalAssemblyId].RemovePart((IMyCubeBlock)BlockEntity);

            // TODO: OnAssemblyRemoved
        }
    }
}
