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

        private void compromisedevent(IMyRemoteControl arg1, IMyCubeGrid arg2)
        {

            MyAPIGateway.Utilities.ShowNotification("Compromised Remote Control Detected", 10000, "Red");
        }

        public override void UpdateAfterSimulation()
        {

            if (MyAPIGateway.Multiplayer.IsServer && SpawnerAPI.MESApiReady && !registered)
            {

                SpawnerAPI.RegisterCompromisedRemoteWatcher(true, compromisedevent);

            }

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

            if (messageText.StartsWith("/mesmesspawnredteam", StringComparison.OrdinalIgnoreCase))
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
