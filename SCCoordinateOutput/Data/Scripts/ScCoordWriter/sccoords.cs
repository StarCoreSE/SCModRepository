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
using Jnick_SCModRepository.SCCoordinateOutput.Data.Scripts.ScCoordWriter;

namespace YourName.ModName.Data.Scripts.ScCoordWriter
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class coordoutput : MyGameLogicComponent
    {
        private Sandbox.ModAPI.IMyCockpit Cockpit;
        private const string fileExtension = ".scc";
        private int tickCounter = 0;
        CoordWriter writer;
        //private List<string> createdFiles = new List<string>(); // Maintain a list of created filenames

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Cockpit = Entity as Sandbox.ModAPI.IMyCockpit;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // Always register for chat commands, on both server and clients
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;

            CoordsNetworking.I.StartGlobalWriter += StartGlobalWriter;
            CoordsNetworking.I.StopGlobalWriter += StopGlobalWriter;
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

        //public override void UpdateOnceBeforeFrame()
        //{
        //    // Add the filename to the list of created files when it's created
        //    if (writer != null && !createdFiles.Contains(writer.FileName))
        //    {
        //        createdFiles.Add(writer.FileName);
        //    }
        //}

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            // invalid, might I suggest .ToLower()
            if (!messageText.StartsWith("/coordwriter", StringComparison.OrdinalIgnoreCase)) return;
            sendToOthers = false; // Prevent command from being sent to other players' chat

            string[] parts = messageText.Split(' ');
            if (parts.Length < 2) return;

            // little bit silly but It Just Works???
            if (parts[1].ToLower().Contains("start"))
                CoordsNetworking.I?.SendMessage(true);
            else if (parts[1].ToLower().Contains("stop"))
                CoordsNetworking.I?.SendMessage(false);
            else
                ShowCommandList();

            if (CoordsNetworking.I == null)
            {
                MyLog.Default.WriteLineAndConsole("YOU'RE A FUCKING IDIOT, ARISTEAS!");
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
