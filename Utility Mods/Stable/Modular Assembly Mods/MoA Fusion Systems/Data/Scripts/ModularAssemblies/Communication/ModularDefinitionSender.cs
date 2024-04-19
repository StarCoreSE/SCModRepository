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

        //private void InputHandler(object o)
        //{
        //    var message = o as byte[];
        //
        //    if (o is bool && (bool)o)
        //    {
        //        MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, Storage);
        //        MyLog.Default.WriteLineAndConsole(
        //            $"{ModContext.ModName}.ModularDefinition: Sent definitions & returning to sleep.");
        //    }
        //    else
        //    {
        //        try
        //        {
        //            var call = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);
        //
        //            if (call == null)
        //            {
        //                MyLog.Default.WriteLineAndConsole(
        //                    $"{ModContext.ModName}.ModularDefinition: Invalid FunctionCall!");
        //                return;
        //            }
        //
        //            PhysicalDefinition defToCall = null;
        //            foreach (var definition in StoredDef.PhysicalDefs)
        //                if (call.DefinitionName == definition.Name)
        //                    defToCall = definition;
        //
        //            if (defToCall == null)
        //                //MyLog.Default.WriteLineAndConsole($$"{ModContext.ModName}.ModularDefinition: Function call [{call.DefinitionName}] not addressed to this.");
        //                return;
        //
        //            // TODO: Remove
        //            //object[] Values = call.Values.Values();
        //            try
        //            {
        //                switch (call.ActionId)
        //                {
        //                    case FunctionCall.ActionType.OnPartAdd:
        //                        // TODO: OnPartUpdate? With ConnectedParts?
        //                        defToCall.OnPartAdd?.Invoke(call.PhysicalAssemblyId,
        //                            (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.LongValues[0]),
        //                            call.Values.BoolValues[0]);
        //                        break;
        //                    case FunctionCall.ActionType.OnPartRemove:
        //                        defToCall.OnPartRemove?.Invoke(call.PhysicalAssemblyId,
        //                            (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.LongValues[0]),
        //                            call.Values.BoolValues[0]);
        //                        break;
        //                    case FunctionCall.ActionType.OnPartDestroy:
        //                        defToCall.OnPartDestroy?.Invoke(call.PhysicalAssemblyId,
        //                            (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.LongValues[0]),
        //                            call.Values.BoolValues[0]);
        //                        break;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                MyAPIGateway.Utilities.SendMessage(
        //                    $"ERROR in definition [{call.DefinitionName}]'s {call.ActionId}!\nCheck logs for stack trace.");
        //                MyLog.Default.WriteLineAndConsole(
        //                    $"ERROR in definition [{call.DefinitionName}]'s {call.ActionId}!\nCheck logs for stack trace.");
        //                throw ex;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MyLog.Default.WriteLineAndConsole(
        //                $"{ModContext.ModName}.ModularDefinition: Exception in InputHandler: {ex}\n{ex.StackTrace}");
        //        }
        //    }
        //}
    }
}