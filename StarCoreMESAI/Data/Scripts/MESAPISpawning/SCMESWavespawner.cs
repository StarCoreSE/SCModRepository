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

namespace Invalid.StarCoreMESAI.Data.Scripts.MESAPISpawning
{
    [ProtoInclude(1000, typeof(ChatCommandPacket))]
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


        // Add a parameterless constructor required by ProtoBuf
        public ChatCommandPacket()
        {

        }

        public ChatCommandPacket(string CommandString)
        {
            this.CommandString = CommandString;

        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SCMESWaveSpawnerComponent : MySessionComponentBase 
    {

        private ushort netID = 23489;
        private MESApi SpawnerAPI;
        bool registered = false;
        private static int aiShipsDestroyed = 0;
        private bool isEventTriggered = false;

        public override void LoadData()
        {
             if (MyAPIGateway.Multiplayer.IsServer)
            {
                SpawnerAPI = new MESApi();
            }

        }

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered; // Listen for chat messages
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);


        }

        public override void UpdateAfterSimulation()
        {

            if (MyAPIGateway.Multiplayer.IsServer && SpawnerAPI.MESApiReady && !registered)
            {

                SpawnerAPI.RegisterCompromisedRemoteWatcher(true, compromisedevent);
                isEventTriggered = false; //oh god. this is an awful workaround. at least it works. otherwise it seems to trigger like 30 times on compromise otherwise.
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

            // Increment the counter
            aiShipsDestroyed++;

            // Show notification with the updated count
            MyAPIGateway.Utilities.ShowNotification($"Compromised Remote Control Detected. AI Ships Destroyed: {aiShipsDestroyed}", 10000, "Red");

            // Add debug logging
            MyLog.Default.WriteLine($"compromisedevent triggered. Count: {aiShipsDestroyed}");
        }



        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            Packet packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet == null) return;



        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            // Check if the message is a command we are interested in
            string[] parts = messageText.Split(' ');

            if (messageText.StartsWith("/SCStartGauntlet", StringComparison.OrdinalIgnoreCase))
            {

            }
            else { return; }

            sendToOthers = false;
        }


        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered; // Unsubscribe from chat message events
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
            SpawnerAPI.UnregisterListener();
        }
    }
}
