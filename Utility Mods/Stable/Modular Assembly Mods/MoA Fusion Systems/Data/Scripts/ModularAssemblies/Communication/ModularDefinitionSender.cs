using VRage.Game.Components;
using VRage.Utils;
using static StarCore.FusionSystems.Communication.DefinitionDefs;

namespace StarCore.FusionSystems.
    Communication
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
            StoredDef = ModularDefinition.GetBaseDefinitions();

            // Send definitions over as soon as the API loads, and create the API before anything else can init.
            ModularDefinition.ModularApi.Init(ModContext, SendDefinitions);
        }

        protected override void UnloadData()
        {
            ModularDefinition.ModularApi.UnloadData();
        }

        private void SendDefinitions()
        {
            ModularDefinition.ModularApi.RegisterDefinitions(StoredDef);
        }
    }
}