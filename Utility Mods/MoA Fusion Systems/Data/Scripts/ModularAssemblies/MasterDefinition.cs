namespace Epstein_Fusion_DS
{
    // turns out whoever wrote the CoreSystems definition handler is REALLY SMART. hats off to you
    internal partial class ModularDefinition
    {
        internal ModularDefinition()
        {
            // it's just like assemblycore, insert definitions here

            LoadDefinitions(ModularFusion, ModularHeat);
        }
    }
}