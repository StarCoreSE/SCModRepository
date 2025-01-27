using Epstein_Fusion_DS.Networking;
using VRage.Game.Components;
using VRage.Utils;
using static Epstein_Fusion_DS.Communication.DefinitionDefs;

namespace Epstein_Fusion_DS.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, int.MinValue)]
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

            new HeartNetwork().LoadData();
        }

        public override void UpdateAfterSimulation()
        {
            HeartNetwork.I.Update();
        }

        protected override void UnloadData()
        {
            HeartNetwork.I.UnloadData();
            Epstein_Fusion_DS.ModularDefinition.ModularApi.UnloadData();
        }

        private void SendDefinitions()
        {
            Epstein_Fusion_DS.ModularDefinition.ModularApi.RegisterDefinitions(StoredDef);
        }
    }
}