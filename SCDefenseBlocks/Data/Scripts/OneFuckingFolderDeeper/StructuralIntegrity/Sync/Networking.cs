using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace StarCore.StructuralIntegrity.Sync
{
    public class Networking
    {
        public readonly ushort PacketId;
        private ConcurrentBag<IMyPlayer> tempPlayers = new ConcurrentBag<IMyPlayer>();

        public Networking(ushort packetId)
        {
            PacketId = packetId;
        }

        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(PacketId, ReceivedPacket);
        }

        public void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(PacketId, ReceivedPacket);
        }

        private void ReceivedPacket(byte[] rawData)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(rawData);
                bool relay = false;
                packet.Received(ref relay);

                if (relay)
                    RelayToClients(packet, rawData);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void SendToServer(PacketBase packet)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToServer(PacketId, bytes);
        }

        public void SendToPlayer(PacketBase packet, ulong steamId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(PacketId, bytes, steamId);
        }

        public void RelayToClients(PacketBase packet, byte[] rawData = null)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            tempPlayers = new ConcurrentBag<IMyPlayer>();

            List<IMyPlayer> playerList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(playerList);

            foreach (var p in playerList)
            {
                if (p.SteamUserId != MyAPIGateway.Multiplayer.ServerId && p.SteamUserId != packet.SenderId)
                {
                    tempPlayers.Add(p);
                }
            }

            if (rawData == null)
                rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

            foreach (var p in tempPlayers)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(PacketId, rawData, p.SteamUserId);
            }
        }
    }
}
