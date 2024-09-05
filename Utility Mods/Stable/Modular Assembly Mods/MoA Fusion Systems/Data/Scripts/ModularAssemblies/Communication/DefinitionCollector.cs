using StarCore.FusionSystems.Communication;
using static StarCore.FusionSystems.Communication.DefinitionDefs;

// ReSharper disable once CheckNamespace
namespace StarCore.FusionSystems
{
    internal partial class ModularDefinition
    {
        internal static ModularDefinitionApi ModularApi = new ModularDefinitionApi();
        internal ModularDefinitionContainer Container = new ModularDefinitionContainer();

        internal void LoadDefinitions(params ModularPhysicalDefinition[] defs)
        {
            Container.PhysicalDefs = defs;
        }

        /// <summary>
        ///     Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static ModularDefinitionContainer GetBaseDefinitions()
        {
            return new StarCore.FusionSystems.ModularDefinition().Container;
        }
    }
}