using System;
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
using VRage.Game.GUI.TextPanel; // Ensure this is added for TextHUD API
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

        public override void LoadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                SpawnerAPI = new MESApi();
            }

            // Initialize HudAPIv2 with a callback
            new HudAPIv2(OnHudApiReady);
        }

        private void OnHudApiReady()
        {
            // Initialize your HUD elements here
            aiShipsDestroyedHUD = new HudAPIv2.HUDMessage(
                new StringBuilder("AI Ships Destroyed: 0"),
                new Vector2D(0.5, 0.5), // Position on the screen
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
            if (MyAPIGateway.Multiplayer.IsServer)
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

                // Reset the isEventTriggered flag after the interval has passed
                if ((DateTime.UtcNow - lastEventTriggerTime).TotalSeconds >= EventResetIntervalSeconds)
                {
                    isEventTriggered = false;
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

            // Show notification with the updated count
            //MyAPIGateway.Utilities.ShowNotification($"Compromised Remote Control Detected. AI Ships Destroyed: {aiShipsDestroyed}", 10000, "Red");

            // Update the HUD element
            if (hudInitialized)
            {
                aiShipsDestroyedHUD.Message.Clear().Append($"AI Ships Destroyed: {aiShipsDestroyed}");
            }

            // Add debug logging
            MyLog.Default.WriteLine($"compromisedevent triggered. Count: {aiShipsDestroyed}");
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
            // Use a type check followed by an explicit cast
            if (packet is CounterUpdatePacket)
            {
                var counterPacket = (CounterUpdatePacket)packet;
                // Update the local counter with the value from the server
                aiShipsDestroyed = counterPacket.CounterValue;
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            string[] parts = messageText.Split(' ');

            if (messageText.StartsWith("/SCStartGauntlet", StringComparison.OrdinalIgnoreCase))
            {
                // Handle command logic here
            }
            else
            {
                return;
            }

            sendToOthers = false;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered; // Unsubscribe from chat message events
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
            SpawnerAPI.UnregisterListener();
            HudAPI.Unload();

            // Clean-up the HUD element
            if (hudInitialized)
            {
                aiShipsDestroyedHUD.DeleteMessage();
            }
        }
    }
}
