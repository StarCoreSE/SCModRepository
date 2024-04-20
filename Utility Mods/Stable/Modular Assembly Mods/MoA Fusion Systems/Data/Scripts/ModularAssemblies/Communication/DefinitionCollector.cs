using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using static MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication.DefinitionDefs;

// ReSharper disable once CheckNamespace
namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    internal partial class ModularDefinition
    {
        internal static ModularDefinitionApi ModularAPI = new ModularDefinitionApi();
        internal DefinitionContainer Container = new DefinitionContainer();

        internal void LoadDefinitions(params PhysicalDefinition[] defs)
        {
            Container.PhysicalDefs = defs;
        }

        /// <summary>
        ///     Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static DefinitionContainer GetBaseDefinitions()
        {
            return new ModularDefinition().Container;
        }
    }
}