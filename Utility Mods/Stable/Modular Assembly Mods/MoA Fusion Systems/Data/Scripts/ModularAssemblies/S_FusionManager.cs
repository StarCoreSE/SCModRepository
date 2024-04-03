using System.Collections.Generic;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.FusionParts;
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
        }

        public void Unload()
        {
            I = null;
        }

        public void UpdateTick()
        {
            foreach (var fusionSystem in FusionSystems.Values)
                fusionSystem.UpdateTick();
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
