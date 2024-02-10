using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using System.IO;
using ProtoBuf;

namespace YourName.ModName.Data.Scripts.ScCoordWriter
{
    // Define packet structure for command handling
    [ProtoContract]
    public class CommandPacket
    {
        [ProtoMember(1)]
        public bool StartCommand;

        [ProtoMember(2)]
        public bool StopCommand;
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class coordoutput : MyGameLogicComponent
    {
        private Sandbox.ModAPI.IMyCockpit Cockpit;
        private const string fileExtension = ".scc";
        private int tickCounter = 0;
        CoordWriter writer;
        private List<string> createdFiles = new List<string>(); // Maintain a list of created filenames

        private ushort netID = 29400; // Define a unique network ID for message communication

        private bool isCommandHandlerRegistered = false; // Flag to track if command handler is registered

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do stuff serverside
            Cockpit = Entity as Sandbox.ModAPI.IMyCockpit;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // Register message handler for command handling on the main thread
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                // Register the command handler and set the flag accordingly
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, CommandHandler);
                isCommandHandlerRegistered = true;
            });

            // Register chat command handler
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        }

        // Define message handler for command handling
        private void CommandHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only handle commands on the server

            CommandPacket packet = MyAPIGateway.Utilities.SerializeFromBinary<CommandPacket>(arg2);
            if (packet == null) return;

            if (packet.StartCommand)
            {
                StartGlobalWriter();
            }
            else if (packet.StopCommand)
            {
                StopGlobalWriter();
            }
        }

        private void StartGlobalWriter()
        {
            if (writer == null && Cockpit != null && Cockpit.CubeGrid.Physics != null)
            {
                // Determine if the grid is static or not
                bool isStatic = Cockpit.CubeGrid.IsStatic;

                string factionName = GetFactionName(Cockpit.OwnerId);
                writer = new CoordWriter(Cockpit.CubeGrid, fileExtension, factionName, isStatic);

                // Call WriteStartingData only once
                if (!writer.HasStartedData)
                {
                    writer.WriteStartingData(factionName);
                    writer.HasStartedData = true; // Set the flag to indicate it has been called
                }

                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

                // Send chat message to all players
                MyVisualScriptLogicProvider.SendChatMessage("Coord Writer", "Global writer started");
            }
        }

        private void StopGlobalWriter()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;

                // Send chat message to all players
                MyVisualScriptLogicProvider.SendChatMessage("Coord Writer", "Global writer stopped");
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return; // Only execute on the server

            if (writer == null || Cockpit == null || Cockpit.CubeGrid.Physics == null || !Cockpit.IsFunctional)
                return;

            tickCounter++;
            if (tickCounter % 30 == 0) // Output once per quarter second
            {
                // Get the forward direction of the cockpit
                Matrix cockpitWorldMatrix = Cockpit.WorldMatrix;
                Vector3D forwardDirection = cockpitWorldMatrix.Forward;

                writer.WriteNextTick(MyAPIGateway.Session.GameplayFrameCounter, true, 1.0f, forwardDirection);
            }
        }

        private string GetFactionName(long playerId)
        {
            IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            return playerFaction != null ? playerFaction.Name : "Unowned";
        }

        public override void UpdateOnceBeforeFrame()
        {
            // Add the filename to the list of created files when it's created
            if (writer != null && !createdFiles.Contains(writer.FileName))
            {
                createdFiles.Add(writer.FileName);
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/coordwriter", StringComparison.OrdinalIgnoreCase)) return;
            string[] parts = messageText.Split(' ');

            if (parts.Length == 1)
            {
                // Show list of available commands and usage instructions
                ShowCommandList();
            }
            else if (parts.Length >= 2)
            {
                if (string.Equals(parts[1], "start", StringComparison.OrdinalIgnoreCase))
                {
                    // Start the global writer
                    StartGlobalWriter();
                }
                else if (string.Equals(parts[1], "stop", StringComparison.OrdinalIgnoreCase))
                {
                    // Stop the global writer
                    StopGlobalWriter();
                }
                else
                {
                    // Invalid command
                    MyVisualScriptLogicProvider.SendChatMessage("Coord Writer", "Invalid command. Use '/coordwriter start' or '/coordwriter stop'.");
                }
            }
        }

        private void ShowCommandList()
        {
            MyVisualScriptLogicProvider.SendChatMessage("Coord Writer", "Available commands:");
            MyVisualScriptLogicProvider.SendChatMessage("Coord Writer", "/coordwriter start - Start the global writer.");
            MyVisualScriptLogicProvider.SendChatMessage("Coord Writer", "/coordwriter stop - Stop the global writer.");
        }

        public override void Close()
        {
            // Safely unregister message handler on the main thread
            if (isCommandHandlerRegistered && netID != 0)
            {
                if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null)
                {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        // Further check to ensure that MyAPIGateway.Multiplayer is still not null when this lambda executes
                        if (MyAPIGateway.Multiplayer != null)
                        {
                            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, CommandHandler);
                            isCommandHandlerRegistered = false; // Ensure to set the flag to false after unregistering
                        }
                    });
                }
            }

            // Dispose of writer safely
            DisposeWriterSafely();

            base.Close();
        }

        private void DisposeWriterSafely()
        {
            if (writer != null)
            {
                try
                {
                    writer.Close();
                }
                catch (Exception ex)
                {
                    // Log or handle the exception as necessary
                    MyLog.Default.WriteLine($"Error closing writer: {ex.Message}");
                }
                finally
                {
                    writer = null;
                }
            }
        }

    }
}
