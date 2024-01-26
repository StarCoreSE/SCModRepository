using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using ProtoBuf;
using Invalid.ModularEncountersSystems.API;
using System.Text;
using VRage.Game.GUI.TextPanel;
using InvalidWave.Draygo.API;
using static VRageRender.MyBillboard;
using System.Xml;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Linq;
using System.IO;
using static StarCoreMESAI.Data.Scripts.MESAPISpawning.SCMESWave_Packets;

namespace Invalid.StarCoreMESAI.Data.Scripts.MESAPISpawning
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SCMESWaveSpawnerComponent : MySessionComponentBase
    {
        private ushort netID = 23489;
        private MESApi SpawnerAPI;
        private bool registered = false;
        private static int aiShipsDestroyed = 0;
        private bool isEventTriggered = false;
        private DateTime lastEventTriggerTime;
        private const double EventResetIntervalSeconds = 1; // Time in seconds to reset the event trigger
        private DateTime lastBroadcastTime;

        // TextHUD API fields
        private HudAPIv2.HUDMessage aiShipsDestroyedHUD;
        private HudAPIv2 HudAPI;
        private bool hudInitialized = false;
        private HudAPIv2.HUDMessage waveTimerHUD;

        // Dictionary for spawn groups and their spawn times
        private Dictionary<string, SpawnGroupInfo> spawnGroupTimings = new Dictionary<string, SpawnGroupInfo>();
        private DateTime lastWaveCheckTime;
        private bool wavesStarted = false; // Flag to control wave spawning
        private int additionalShipsPerWave = 0;

        private DateTime lastWaveNotificationTime;

        public override void LoadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                SpawnerAPI = new MESApi();
            }

            LoadWaveData();
            // Add other spawn groups and info as needed
        }
        #region config
        private void LoadWaveData()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                try
                {
                    string fileName = "WaveData.cfg"; // Configuration file name

                    if (MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, GetType()))
                    {
                        using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, GetType()))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Split the line by semicolons and trim whitespace
                                string[] sections = line.Trim().Split(';');

                                if (sections.Length >= 3)
                                {
                                    string name = sections[0].Trim();
                                    int startTime;

                                    if (int.TryParse(sections[1].Trim(), out startTime))
                                    {
                                        // Parse the prefab section
                                        string[] prefabParts = sections[2].Trim().Split(',');
                                        Dictionary<string, int> prefabs = new Dictionary<string, int>();

                                        foreach (var part in prefabParts)
                                        {
                                            var prefabInfo = part.Trim().Split(':');
                                            if (prefabInfo.Length == 2)
                                            {
                                                int quantity;
                                                if (int.TryParse(prefabInfo[1], out quantity))
                                                {
                                                    prefabs.Add(prefabInfo[0].Trim(), quantity);
                                                }
                                            }
                                        }

                                        // Add the wave data to your dictionary
                                        spawnGroupTimings.Add(name, new SpawnGroupInfo(startTime, prefabs));

                                        // Print the loaded data to chat for debugging
                                        MyLog.Default.WriteLineAndConsole($"Name: {name}, StartTime: {startTime}, Prefabs: {string.Join(", ", prefabs.Keys)}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Handle the case where the configuration file doesn't exist
                        MyLog.Default.WriteLineAndConsole("Configuration file not found. Generating a new one.");
                        CreateBlankConfig();
                    }
                }
                catch (Exception e)
                {
                    // Handle any exceptions that may occur during file reading or parsing
                    MyLog.Default.WriteLineAndConsole("Error loading configuration file: " + e.Message);
                }
            }
        }

        private void CreateBlankConfig()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                try
                {
                    string fileName = "WaveData.cfg"; // Configuration file name

                    if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, GetType()))
                    {
                        // Create a blank configuration file with default values in the new format
                        string defaultConfig =
                            "Wave1;10;Prefab1, Prefab2, Prefab3\n" +
                            "Wave2;20;Prefab4, Prefab5\n" +
                            "Wave3;30;Prefab6, Prefab7, Prefab8, Prefab9";

                        // Write the default configuration to the file
                        var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, GetType());
                        writer.Write(defaultConfig);
                        writer.Flush();
                        writer.Close();
                    }
                }
                catch (Exception e)
                {
                    // Handle any exceptions that may occur during file creation
                    MyLog.Default.WriteLineAndConsole("Error creating configuration file: " + e.Message);
                }
            }
        }
        #endregion
        private void OnHudApiReady()
        {
            // Initialize your HUD elements here
            aiShipsDestroyedHUD = new HudAPIv2.HUDMessage(
                new StringBuilder("AI Ships Destroyed: 0"),
                new Vector2D(0.5, 0.5), // Position on the screen
                Scale: 1.0,
                Blend: BlendTypeEnum.PostPP);

            waveTimerHUD = new HudAPIv2.HUDMessage(
                new StringBuilder("Next Wave: --:--"),
                new Vector2D(0.5, 0.55), // Adjust the position as needed
                Scale: 1.0,
                Blend: BlendTypeEnum.PostPP);

            hudInitialized = true;
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered; // Listen for chat messages
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Multiplayer.IsServer && wavesStarted)
            {
                HandleEventWatcherRegistration();
                BroadcastCounterUpdate();
                HandleWaveNotifications();
                ResetEventTrigger();
                ProcessSpawnGroups();
            }
        }

        private void HandleEventWatcherRegistration()
        {
            if (SpawnerAPI.MESApiReady && !registered)
            {
                SpawnerAPI.RegisterCompromisedRemoteWatcher(true, compromisedevent);
                registered = true;
            }
        }

        private void BroadcastCounterUpdate()
        {
            if ((DateTime.UtcNow - lastBroadcastTime).TotalSeconds >= 5)
            {
                BroadcastCounter();
                lastBroadcastTime = DateTime.UtcNow;
            }
        }

        private void HandleWaveNotifications()
        {
            if (spawnGroupTimings.Count > 0)
            {
                var firstGroupInfo = spawnGroupTimings.First().Value;
                var nextWaveSpawnTime = firstGroupInfo.SpawnTime;
                var timeSinceStart = (int)(DateTime.UtcNow - lastWaveCheckTime).TotalSeconds;
                var timeUntilNextWave = nextWaveSpawnTime - timeSinceStart;

                const int messageInterval = 60; // 60 seconds

                if ((DateTime.UtcNow - lastWaveNotificationTime).TotalSeconds >= messageInterval ||
                    (timeUntilNextWave <= 10 && timeUntilNextWave >= 0 && (DateTime.UtcNow - lastWaveNotificationTime).TotalSeconds >= 10))
                {
                    string message;

                    if (timeUntilNextWave > 10)
                        message = $"Next Wave: {(timeUntilNextWave / 60).ToString("D2")}:{(timeUntilNextWave % 60).ToString("D2")}";
                    else
                        message = $"Next Wave in: {timeUntilNextWave}s";

                    MyAPIGateway.Utilities.SendMessage(message);
                    lastWaveNotificationTime = DateTime.UtcNow;
                }
            }
        }

        private void ResetEventTrigger()
        {
            if ((DateTime.UtcNow - lastEventTriggerTime).TotalSeconds >= EventResetIntervalSeconds)
            {
                if (isEventTriggered)
                {
                    // Optional: Log or message statement here
                    // Example: MyAPIGateway.Utilities.SendMessage("Event trigger reset.");
                }
                isEventTriggered = false;
            }
        }

        private void ProcessSpawnGroups()
        {
            var keysToRemove = new List<string>();
            foreach (var spawnGroup in spawnGroupTimings)
            {
                var groupKey = spawnGroup.Key;
                var groupValue = spawnGroup.Value;
                var spawnTime = groupValue.SpawnTime;

                if ((DateTime.UtcNow - lastWaveCheckTime).TotalSeconds >= spawnTime)
                {
                    Vector3D spawnCoords = new Vector3D(-20000, 0, 0);

                    foreach (var prefabEntry in groupValue.Prefabs)
                    {
                        string prefabName = prefabEntry.Key;
                        int quantity = prefabEntry.Value + additionalShipsPerWave; // Add additional ships per wave, capped at 10

                        for (int i = 0; i < quantity; i++)
                        {
                            bool spawnResult = SpawnerAPI.SpawnSpaceCargoShip(spawnCoords, new List<string> { prefabName });

                            if (spawnResult)
                            {
                                MyAPIGateway.Utilities.SendMessage($"Spawned prefab: {prefabName}");
                            }
                            else
                            {
                                MyAPIGateway.Utilities.SendMessage($"Failed to spawn prefab: {prefabName}");
                            }
                        }
                    }

                    keysToRemove.Add(groupKey);
                }
            }

            foreach (var keyToRemove in keysToRemove)
            {
                spawnGroupTimings.Remove(keyToRemove);
            }

            if (spawnGroupTimings.Count == 0)
            {
                wavesStarted = false;
            }
        }

        private void compromisedevent(IMyRemoteControl arg1, IMyCubeGrid arg2)
        {
            // Incrementing the counter each time an AI ship is destroyed
            aiShipsDestroyed++;
            MyAPIGateway.Utilities.SendMessage($"SCMESWaveSpawner: Compromised event triggered. AI Ships Destroyed: {aiShipsDestroyed}");
            // Updating the HUD element
            if (hudInitialized)
            {
                aiShipsDestroyedHUD.Message.Clear().Append($"AI Ships Destroyed: {aiShipsDestroyed}");
            }
        }

        private void BroadcastCounter()
        {
            var packet = new CounterUpdatePacket(aiShipsDestroyed);
            var serializedPacket = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToOthers(netID, serializedPacket);
        }

        // Inside the NetworkHandler method
        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {

            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet != null)
            {
                if (packet is ChatCommandPacket)
                {
                    var commandPacket = (ChatCommandPacket)packet;
                    if (MyAPIGateway.Session.IsServer)
                    {
                        // Process the command on the server
                        ProcessCommand(commandPacket.CommandString);
                    }
                }
                else if (packet is CounterUpdatePacket)
                {
                    var counterPacket = (CounterUpdatePacket)packet;
                    aiShipsDestroyed = counterPacket.CounterValue;
                }
            }
        }

        // Inside the OnMessageEntered method
        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/SCStartGauntlet", StringComparison.OrdinalIgnoreCase))
            {
                // Prevent the message from being broadcasted to other players
                sendToOthers = false;

                if (!MyAPIGateway.Multiplayer.IsServer)
                {
                    // Send a network message to the server with the command
                    var packet = new ChatCommandPacket(messageText);
                    var serializedPacket = MyAPIGateway.Utilities.SerializeToBinary(packet);
                    MyAPIGateway.Multiplayer.SendMessageToServer(netID, serializedPacket);
                }
                else
                {
                    // Process the command on the server
                    ProcessCommand(messageText);
                }
            }
        }

        // Inside the ProcessCommand method
        private void ProcessCommand(string messageText)
        {

            string[] commandParts = messageText.Split(' ');
            int parsedAdditionalShips;
            if (commandParts.Length > 1 && int.TryParse(commandParts[1], out parsedAdditionalShips))
            {
                additionalShipsPerWave = Math.Min(parsedAdditionalShips, 10);
                wavesStarted = true;
                lastWaveCheckTime = DateTime.UtcNow;

                MyAPIGateway.Utilities.SendMessage($"Started spawning waves with additional ships per wave: {additionalShipsPerWave} (Server)");
            }
            else
            {
                MyAPIGateway.Utilities.SendMessage("Invalid command format. Use /SCStartGauntlet X to specify additional ships per wave (max 10). (Server)");
            }
        }


        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered; // Unsubscribe from chat message events
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
            }
            catch (Exception e)
            {
                // Log the exception and any relevant information
                MyLog.Default.WriteLineAndConsole($"Error in UnloadData: {e.Message}");
                if (e.InnerException != null)
                {
                    MyLog.Default.WriteLineAndConsole($"Inner Exception: {e.InnerException.Message}");
                }

                // Handle the exception or rethrow it if necessary
                throw;
            }
        }

    }
}
