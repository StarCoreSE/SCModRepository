using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using static MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication.DefinitionDefs;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation, int.MaxValue - 1)]
    internal class ModularDefinitionSender : MySessionComponentBase
    {
        internal DefinitionContainer StoredDef;

        public override void LoadData()
        {
            MyLog.Default.WriteLineAndConsole(
                $"{ModContext.ModName}.ModularDefinition: Init new ModularAssembliesDefinition");

            // Send definitions over as soon as the API loads, and create the API before anything else can init.
            ModularDefinition.ModularApi = new ModularDefinitionApi(ModContext, SendDefinitions);

            // Init
            StoredDef = ModularDefinition.GetBaseDefinitions();
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