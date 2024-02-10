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

namespace YourName.ModName.Data.Scripts.ScCoordWriter
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class coordoutput : MyGameLogicComponent
    {
        private Sandbox.ModAPI.IMyCockpit Cockpit;
        private const string fileExtension = ".scc";
        private int tickCounter = 0;
        CoordWriter writer;
        private List<string> createdFiles = new List<string>(); // Maintain a list of created filenames

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do stuff serverside
            Cockpit = Entity as Sandbox.ModAPI.IMyCockpit;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // Listen for chat commands
            MyAPIGateway.Utilities.MessageEntered += CommandHandler;
        }

        private void CommandHandler(string messageText, ref bool sendToOthers)
        {
            if (!string.IsNullOrEmpty(messageText))
            {
                if (messageText.StartsWith("/coordwriterstart", StringComparison.InvariantCultureIgnoreCase))
                {
                    StartGlobalWriter();
                }
                else if (messageText.StartsWith("/coordwriterstop", StringComparison.InvariantCultureIgnoreCase))
                {
                    StopGlobalWriter();
                }
                else if (messageText.StartsWith("/coordwriterclear", StringComparison.InvariantCultureIgnoreCase))
                {
                    ClearGlobalWriter();
                }
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

                // Debug output
                MyAPIGateway.Utilities.ShowMessage("Coord Writer", "Global writer started.");
            }
        }

        private void StopGlobalWriter()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;

                // Debug output
                MyAPIGateway.Utilities.ShowMessage("Coord Writer", "Global writer stopped.");
            }
        }

        private void ClearGlobalWriter()
        {
            StopGlobalWriter(); // Stop the writer first

            // Delete files associated with the writer
            foreach (string fileName in createdFiles)
            {
                try
                {
                    MyAPIGateway.Utilities.DeleteFileInWorldStorage(fileName, typeof(CoordWriter));
                    MyAPIGateway.Utilities.ShowMessage("Coord Writer", $"File '{fileName}' deleted.");
                }
                catch (Exception ex)
                {
                    MyAPIGateway.Utilities.ShowMessage("Coord Writer", $"Error deleting file '{fileName}': {ex.Message}");
                }
            }

            createdFiles.Clear(); // Clear the list of created filenames

            // Debug output
            MyAPIGateway.Utilities.ShowMessage("Coord Writer", "Files cleared for global writer.");
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

        public override void Close()
        {
            // Clean up event handler
            MyAPIGateway.Utilities.MessageEntered -= CommandHandler;

            // Dispose of writer
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }

            base.Close();
        }
    }
}
