using static Scripts.ModularAssemblies.Communication.DefinitionDefs;

namespace Scripts.ModularAssemblies.Communication
{
    partial class ModularDefinition
    {
        internal DefinitionContainer Container = new DefinitionContainer();
        internal static ModularDefinitionAPI ModularAPI = null;

        internal void LoadDefinitions(params PhysicalDefinition[] defs)
        {
            Container.PhysicalDefs = defs;
        }

        /// <summary>
        /// Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static DefinitionContainer GetBaseDefinitions()
        {
            return new ModularDefinition().Container;
        }
    }
}
