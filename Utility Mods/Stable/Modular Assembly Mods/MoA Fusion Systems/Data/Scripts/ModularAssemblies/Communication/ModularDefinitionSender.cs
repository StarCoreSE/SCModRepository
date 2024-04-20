using System;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using static MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication.DefinitionDefs;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    internal class ModularDefinitionSender : MySessionComponentBase
    {
        internal DefinitionContainer StoredDef;

        public override void LoadData()
        {
            MyLog.Default.WriteLineAndConsole(
                $"{ModContext.ModName}.ModularDefinition: Init new ModularAssembliesDefinition");

            // Init
            StoredDef = ModularDefinition.GetBaseDefinitions();

            // Send definitions over as soon as the API loads
            ModularDefinition.ModularAPI.LoadData(ModContext, SendDefinitions);
        }

        protected override void UnloadData()
        {
            ModularDefinition.ModularAPI.UnloadData();
        }

        private void SendDefinitions()
        {
            ModularDefinition.ModularAPI.RegisterDefinitions(StoredDef);
        }
    }
}