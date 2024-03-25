using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using System;
using VRageMath;
using VRage.Game.Entity;
using static Scripts.ModularAssemblies.Communication.DefinitionDefs;

namespace Scripts.ModularAssemblies.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    internal class ModularDefinitionSender : MySessionComponentBase
    {
        const int DefinitionMessageId = 8772;
        const int InboundMessageId = 8771;
        const int OutboundMessageId = 8773;

        internal DefinitionContainer storedDef;
        internal byte[] Storage;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyLog.Default.WriteLineAndConsole("ModularAssembliesDefinition: Init new ModularAssembliesDefinition");
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, InputHandler);

            // Init
            storedDef = ModularDefinition.GetBaseDefinitions();
            Storage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);

            ModularDefinition.ModularAPI = new ModularDefinitionAPI();
            ModularDefinition.ModularAPI.LoadData();

            MyLog.Default.WriteLineAndConsole($"ModularAssembliesDefinition: Packaged definitions & going to sleep.");
        }

        protected override void UnloadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

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
                MyLog.Default.WriteLineAndConsole("ModularAssembliesDefinition: Sent definitions & returning to sleep.");
            }
            else
            {
                try
                {
                    FunctionCall call;
                    call = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);

                    if (call == null)
                    {
                        MyLog.Default.WriteLineAndConsole($"ModularAssembliesDefinition: Invalid FunctionCall!");
                        return;
                    }

                    PhysicalDefinition defToCall = null;
                    foreach (var definition in storedDef.PhysicalDefs)
                        if (call.DefinitionName == definition.Name)
                            defToCall = definition;

                    if (defToCall == null)
                    {
                        //MyLog.Default.WriteLineAndConsole($"ModularAssembliesDefinition: Function call [{call.DefinitionName}] not addressed to this.");
                        return;
                    }

                    // TODO: Remove
                    //object[] Values = call.Values.Values();
                    try
                    {
                        switch (call.ActionId)
                        {
                            case FunctionCall.ActionType.OnPartAdd:
                                // TODO: OnPartUpdate? With ConnectedParts?
                                defToCall.OnPartAdd?.Invoke(call.PhysicalAssemblyId, (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.longValues[0]), call.Values.boolValues[0]);
                                break;
                            case FunctionCall.ActionType.OnPartRemove:
                                defToCall.OnPartRemove?.Invoke(call.PhysicalAssemblyId, (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.longValues[0]), call.Values.boolValues[0]);
                                break;
                            case FunctionCall.ActionType.OnPartDestroy:
                                defToCall.OnPartDestroy?.Invoke(call.PhysicalAssemblyId, (MyEntity)MyAPIGateway.Entities.GetEntityById(call.Values.longValues[0]), call.Values.boolValues[0]);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyAPIGateway.Utilities.SendMessage($"ERROR in definition [{call.DefinitionName}]'s {call.ActionId}!\nCheck logs for stack trace.");
                        MyLog.Default.WriteLineAndConsole($"ERROR in definition [{call.DefinitionName}]'s {call.ActionId}!\nCheck logs for stack trace.");
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole($"ModularAssembliesDefinition: Exception in InputHandler: {ex}\n{ex.StackTrace}");
                }
            }
        }

        private void SendFunc(FunctionCall call)
        {
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            //MyLog.Default.WriteLineAndConsole($"ModularAssembliesDefinition: Sending function call [id {call.ActionId}].");
        }
    }
}
