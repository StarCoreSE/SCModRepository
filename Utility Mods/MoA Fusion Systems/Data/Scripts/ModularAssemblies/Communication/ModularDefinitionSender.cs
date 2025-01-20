using VRage.Game.Components;
using VRage.Utils;
using static Epstein_Fusion_DS.Communication.DefinitionDefs;

namespace Epstein_Fusion_DS.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation, int.MinValue)]
    internal class ModularDefinitionSender : MySessionComponentBase
    {
        internal ModularDefinitionContainer StoredDef;

        public override void LoadData()
        {
            MyLog.Default.WriteLineAndConsole(
                $"{ModContext.ModName}.ModularDefinition: Init new ModularAssembliesDefinition");

            // Init
            StoredDef = Epstein_Fusion_DS.ModularDefinition.GetBaseDefinitions();

            // Send definitions over as soon as the API loads, and create the API before anything else can init.
            Epstein_Fusion_DS.ModularDefinition.ModularApi.Init(ModContext, SendDefinitions);
        }

        protected override void UnloadData()
        {
            Epstein_Fusion_DS.ModularDefinition.ModularApi.UnloadData();
        }

        private void SendDefinitions()
        {
            Epstein_Fusion_DS.ModularDefinition.ModularApi.RegisterDefinitions(StoredDef);
        }
    }
}