using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.Components;
using VRage.Profiler;
using VRage.Utils;
using static Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions.DefinitionDefs;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    /// <summary>
    /// Handles all communication about definitions.
    /// </summary>
    internal class DefinitionHandler
    {
        public static DefinitionHandler I;
        const int DefinitionMessageId = 8772;
        const int InboundMessageId = 8773;
        const int OutboundMessageId = 8771;

        public List<ModularDefinition> ModularDefinitions = new List<ModularDefinition>();

        public void Init()
        {
            I = this;

            if (!MyAPIGateway.Session.IsServer)
                return;

            MyLog.Default.WriteLineAndConsole("Modular Assemblies: DefinitionHandler loading...");

            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, ActionMessageHandler);
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, true);

            MyAPIGateway.Session.OnSessionReady += CheckValidDefinitions;
            MyLog.Default.WriteLineAndConsole("Modular Assemblies: Init DefinitionHandler.cs");
        }

        public void Unload()
        {
            I = null;
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyLog.Default.WriteLineAndConsole("Modular Assemblies: DefinitionHandler closing...");

            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.UnregisterMessageHandler(InboundMessageId, ActionMessageHandler);
        }

        public void DefMessageHandler(object o)
        {
            try
            {
                var message = o as byte[];
                if (message == null) return;

                DefinitionContainer baseDefArray = null;
                try
                {
                    baseDefArray = MyAPIGateway.Utilities.SerializeFromBinary<DefinitionContainer>(message);
                }
                catch {}

                if (baseDefArray != null)
                {
                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Recieved {baseDefArray.PhysicalDefs.Length} definitions.");
                    foreach (var def in baseDefArray.PhysicalDefs)
                    {
                        ModularDefinition modDef = ModularDefinition.Load(def);
                        if (modDef != null)
                        {
                            bool isDefinitionValid = true;
                            foreach (var definiton in ModularDefinitions)
                            {
                                if (definiton.Name == modDef.Name)
                                {
                                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Duplicate DefinitionName in definition {modDef.Name}! Skipping load...");
                                    MyAPIGateway.Utilities.ShowMessage("ModularAssemblies", $"Duplicate DefinitionName in definition {modDef.Name}! Skipping load...");
                                    isDefinitionValid = false;
                                }
                            }
                            if (isDefinitionValid)
                                ModularDefinitions.Add(modDef);
                        }
                    }
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Invalid definition container!");
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Exception in DefinitionMessageHandler: {ex}"); }
        }

        public void ActionMessageHandler(object o)
        {
            try
            {
                var message = o as byte[];
                if (message == null) return;

                FunctionCall functionCall = null;
                try
                {
                    functionCall = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);
                }
                catch { }

                if (functionCall != null)
                {
                    //MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Recieved action of type {functionCall.ActionId}.");

                    PhysicalAssembly wep = AssemblyPartManager.I.AllPhysicalAssemblies[functionCall.PhysicalAssemblyId];
                    if (wep == null)
                    {
                        MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Invalid PhysicalAssembly!");
                        return;
                    }

                    // TODO: Remove
                    //object[] Values = functionCall.Values.Values();

                    switch (functionCall.ActionId)
                    {
                        default:
                            // Fill in here if necessary.
                            break;
                    }
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: functionCall null!");
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Exception in ActionMessageHandler: {ex}"); }
        }

        public void SendOnPartAdd(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartAdd,
                DefinitionName = DefinitionName,
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        public void SendOnPartRemove(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartRemove,
                DefinitionName = DefinitionName,
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        public void SendOnPartDestroy(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartDestroy,
                DefinitionName = DefinitionName,
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        private void SendFunc(FunctionCall call)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            //MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Sending function call [id {call.ActionId}] to [{call.DefinitionName}].");
        }

        private void CheckValidDefinitions()
        {
            // Get all block definition subtypes
            var defs = MyDefinitionManager.Static.GetAllDefinitions();
            List<string> validSubtypes = new List<string>();
            foreach (var def in defs)
            {
                var blockDef = def as MyCubeBlockDefinition;

                if (blockDef != null)
                {
                    validSubtypes.Add(def.Id.SubtypeName);
                }
            }
            foreach (var def in ModularDefinitions.ToList())
                CheckDefinitionValid(def, validSubtypes);
        }

        private void CheckDefinitionValid(ModularDefinition modDef, List<string> validSubtypes)
        {
            foreach (var subtypeId in modDef.AllowedBlocks)
            {
                if (!validSubtypes.Contains(subtypeId))
                {
                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Invalid SubtypeId [{subtypeId}] in definition {modDef.Name}! Unexpected behavior may occur.");
                    MyAPIGateway.Utilities.ShowMessage("ModularAssemblies", $"Invalid SubtypeId [{subtypeId}] in definition {modDef.Name}! Unexpected behavior may occur.");
                }
            }
        }
    }
}
