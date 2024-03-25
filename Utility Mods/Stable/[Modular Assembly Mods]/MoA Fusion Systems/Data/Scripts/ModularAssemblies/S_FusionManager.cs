using Modular_Definitions.Data.Scripts.ModularAssemblies;
using Sandbox.ModAPI;
using SCModRepository.Utility_Mods.Stable._Modular_Assembly_Mods_.MoA_Fusion_Systems.Data.Scripts.ModularAssemblies;
using Scripts.ModularAssemblies.Communication;
using Scripts.ModularAssemblies.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRageMath;

namespace Scripts.ModularAssemblies
{

    internal class S_FusionManager
    {
        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public static S_FusionManager I = new S_FusionManager();
        public ModularDefinition Definition;
        public Dictionary<int, S_FusionSystem> FusionSystems = new Dictionary<int, S_FusionSystem>();


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
            foreach (S_FusionSystem fusionSystem in FusionSystems.Values)
                fusionSystem.UpdateTick();
        }

        public void OnPartAdd(int PhysicalAssemblyId, MyEntity NewBlockEntity, bool IsBaseBlock)
        {
            if (!FusionSystems.ContainsKey(PhysicalAssemblyId))
                FusionSystems.Add(PhysicalAssemblyId, new S_FusionSystem(PhysicalAssemblyId));

            FusionSystems[PhysicalAssemblyId].AddPart((IMyCubeBlock) NewBlockEntity);
        }

        public void OnPartRemove(int PhysicalAssemblyId, MyEntity BlockEntity, bool IsBaseBlock)
        {
            // Remove if the connection is broken.
            if (!IsBaseBlock)
            {
                FusionSystems[PhysicalAssemblyId].RemovePart((IMyCubeBlock) BlockEntity);
            }
            else
            {
                FusionSystems.Remove(PhysicalAssemblyId);
            }
        }
    }
}
