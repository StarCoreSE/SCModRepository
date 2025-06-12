using Epstein_Fusion_DS.Communication;
using static Epstein_Fusion_DS.Communication.DefinitionDefs;

// ReSharper disable once CheckNamespace
namespace Epstein_Fusion_DS
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
            return new Epstein_Fusion_DS.ModularDefinition().Container;
        }
    }
}