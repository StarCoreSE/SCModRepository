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
        private const int DefinitionMessageId = 8772;
        private const int InboundMessageId = 8771;
        private const int OutboundMessageId = 8773;
        internal byte[] Storage;

        internal DefinitionContainer StoredDef;

        public override void LoadData()
        {
            MyLog.Default.WriteLineAndConsole(
                $"{ModContext.ModName}.ModularDefinition: Init new ModularAssembliesDefinition");
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, InputHandler);

            // Init
            StoredDef = ModularDefinition.GetBaseDefinitions();
            Storage = MyAPIGateway.Utilities.SerializeToBinary(StoredDef);

            ModularDefinition.ModularAPI.LoadData(ModContext);

            // Send message in case this loads after the main mod
            MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, Storage);
            MyLog.Default.WriteLineAndConsole(
                $"{ModContext.ModName}.ModularDefinition: Packaged and sent definitions, now going to sleep.");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(InboundMessageId, InputHandler);
            Array.Clear(Storage, 0, Storage.Length);
            Storage = null;
            ModularDefinition.ModularAPI.UnloadData();
        }

        private void InputHandler(object o)
        {
            var message = o as byte[];

            if (o is bool && (bool)o)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, Storage);
                MyLog.Default.WriteLineAndConsole(
                    $"{ModContext.ModName}.ModularDefinition: Sent definitions & returning to sleep.");
            }
            else
            {
                try
                {
                    var call = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);

                    if (call == null)
                    {
                        MyLog.Default.WriteLineAndConsole(
                            $"{ModContext.ModName}.ModularDefinition: Invalid FunctionCall!");
                        return;
                    }

                    PhysicalDefinition defToCall = null;
                    foreach (var definition in StoredDef.PhysicalDefs)
                        if (call.DefinitionName == definition.Name)
                            defToCall = definition;

                    if (defToCall == null)
                        //MyLog.Default.WriteLineAndConsole($$"{ModContext.ModName}.ModularDefinition: Function call [{call.DefinitionName}] not addressed to this.");
                        return;

                    // TODO: Remove
                    //object[] Values = call.Values.Values();
                    try
                    {
                        switch (call.ActionId)
                        {
                            case FunctionCall.ActionType.OnPartAdd:
                                // TODO: OnPartUpdate? With ConnectedParts?
                                defToCall.OnPartAdd?.Invoke(call.PhysicalAssemblyId,
                                    (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.LongValues[0]),
                                    call.Values.BoolValues[0]);
                                break;
                            case FunctionCall.ActionType.OnPartRemove:
                                defToCall.OnPartRemove?.Invoke(call.PhysicalAssemblyId,
                                    (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.LongValues[0]),
                                    call.Values.BoolValues[0]);
                                break;
                            case FunctionCall.ActionType.OnPartDestroy:
                                defToCall.OnPartDestroy?.Invoke(call.PhysicalAssemblyId,
                                    (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.LongValues[0]),
                                    call.Values.BoolValues[0]);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyAPIGateway.Utilities.SendMessage(
                            $"ERROR in definition [{call.DefinitionName}]'s {call.ActionId}!\nCheck logs for stack trace.");
                        MyLog.Default.WriteLineAndConsole(
                            $"ERROR in definition [{call.DefinitionName}]'s {call.ActionId}!\nCheck logs for stack trace.");
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole(
                        $"{ModContext.ModName}.ModularDefinition: Exception in InputHandler: {ex}\n{ex.StackTrace}");
                }
            }
        }

        private void SendFunc(FunctionCall call)
        {
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            //MyLog.Default.WriteLineAndConsole($$"{ModContext.ModName}.ModularDefinition: Sending function call [id {call.ActionId}].");
        }
    }
}