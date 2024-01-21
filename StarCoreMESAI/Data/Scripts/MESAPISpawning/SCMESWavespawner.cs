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
using Draygo.API;
using static VRageRender.MyBillboard;

namespace Invalid.StarCoreMESAI.Data.Scripts.MESAPISpawning
{
    [ProtoInclude(1000, typeof(ChatCommandPacket))]
    [ProtoInclude(1001, typeof(CounterUpdatePacket))]
    [ProtoContract]
    public class Packet
    {
        public Packet()
        {
        }
    }

    [ProtoContract]
    public class ChatCommandPacket : Packet
    {
        [ProtoMember(1)]
        public string CommandString;

        public ChatCommandPacket()
        {
        }

        public ChatCommandPacket(string CommandString)
        {
            this.CommandString = CommandString;
        }
    }

    [ProtoContract]
    public class CounterUpdatePacket : Packet
    {
        [ProtoMember(1)]
        public int CounterValue;

        public CounterUpdatePacket()
        {
        }

        public CounterUpdatePacket(int counterValue)
        {
            this.CounterValue = counterValue;
        }
    }

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
        private Dictionary<string, int> spawnGroupTimings = new Dictionary<string, int>();
        private DateTime lastWaveCheckTime;
        private bool wavesStarted = false; // Flag to control wave spawning

        public override void LoadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                SpawnerAPI = new MESApi();
            }

            // Initialize HudAPIv2 with a callback
            new HudAPIv2(OnHudApiReady);

            // Populate the dictionary with spawn groups and timings
            spawnGroupTimings.Add("SpawnSCDM", 6);      // Spawn after 60 seconds
            spawnGroupTimings.Add("SpawnRIAN", 12);     // Spawn after 120 seconds
            spawnGroupTimings.Add("SpawnTidewater", 18); // Spawn after 180 seconds
            // Add other spawn groups and timings as needed
        }

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
                if (SpawnerAPI.MESApiReady && !registered)
                {
                    SpawnerAPI.RegisterCompromisedRemoteWatcher(true, compromisedevent);
                    isEventTriggered = false;
                    registered = true;
                }

                // Check if 5 seconds have passed since the last broadcast
                if ((DateTime.UtcNow - lastBroadcastTime).TotalSeconds >= 5)
                {
                    BroadcastCounter();
                    lastBroadcastTime = DateTime.UtcNow;
                }

                // Update the wave timer HUD
                if (hudInitialized && spawnGroupTimings.Count > 0)
                {
                    var nextWaveSpawnTime = new List<int>(spawnGroupTimings.Values)[0]; // Get the first spawn time
                    var timeSinceStart = (int)(DateTime.UtcNow - lastWaveCheckTime).TotalSeconds;
                    var timeUntilNextWave = nextWaveSpawnTime - timeSinceStart;
                    if (timeUntilNextWave < 0) timeUntilNextWave = 0;
                    waveTimerHUD.Message.Clear().Append("Next Wave: " + (timeUntilNextWave / 60).ToString("D2") + ":" + (timeUntilNextWave % 60).ToString("D2"));
                }

                // Reset the isEventTriggered flag after the interval has passed
                if ((DateTime.UtcNow - lastEventTriggerTime).TotalSeconds >= EventResetIntervalSeconds)
                {
                    isEventTriggered = false;
                }

                // Check and spawn waves based on spawnGroupTimings
                foreach (var spawnGroup in spawnGroupTimings)
                {
                    if ((DateTime.UtcNow - lastWaveCheckTime).TotalSeconds >= spawnGroup.Value)
                    {
                        // Spawn the group at -10,000X
                        Vector3D spawnCoords = new Vector3D(-10000, 0, 0);
                        List<string> spawnGroups = new List<string> { spawnGroup.Key };
                        SpawnerAPI.SpawnSpaceCargoShip(spawnCoords, spawnGroups);

                        // Remove the spawned group from the dictionary to avoid respawning
                        spawnGroupTimings.Remove(spawnGroup.Key);

                        // Break out of the loop after spawning a group
                        break;
                    }
                }

                // If all groups have been spawned, stop the wave spawning
                if (spawnGroupTimings.Count == 0)
                {
                    wavesStarted = false;
                }
            }
        }

        private void compromisedevent(IMyRemoteControl arg1, IMyCubeGrid arg2)
        {
            if (isEventTriggered)
            {
                // Skip if the event has already been processed
                return;
            }

            isEventTriggered = true; // Set the flag to true to indicate processing
            lastEventTriggerTime = DateTime.UtcNow; // Update the time when event was last triggered

            // Increment the counter
            aiShipsDestroyed++;

            // Update the HUD element
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

        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (MyAPIGateway.Session.IsServer) return;

            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet is CounterUpdatePacket)
            {
                var counterPacket = (CounterUpdatePacket)packet;
                // Update the local counter with the value from the server
                aiShipsDestroyed = counterPacket.CounterValue;
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/SCStartGauntlet", StringComparison.OrdinalIgnoreCase))
            {
                wavesStarted = true; // Start spawning waves
                lastWaveCheckTime = DateTime.UtcNow; // Initialize the wave check time
                sendToOthers = false;
                MyAPIGateway.Utilities.ShowMessage("SCMESWaveSpawner", "Started spawning waves");
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered; // Unsubscribe from chat message events
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
            SpawnerAPI.UnregisterListener();
            if (hudInitialized)
            {
                aiShipsDestroyedHUD.DeleteMessage();
            }
        }
    }
}
