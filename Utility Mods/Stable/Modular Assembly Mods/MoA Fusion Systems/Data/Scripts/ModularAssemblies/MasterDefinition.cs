namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    // turns out whoever wrote the CoreSystems definition handler is REALLY SMART. hats off to you
    internal partial class ModularDefinition
    {
        internal ModularDefinition()
        {
            // it's just like assemblycore, insert definitions here

            LoadDefinitions(Modular_Fusion, Modular_Heat);
        }
    }
}