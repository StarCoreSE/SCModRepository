using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Math0424.Networking
{
    public class MyEasyNetworkManager
    {
        private readonly ushort _commsId;

        //got a packet that made it through inspection
        public Action<PacketIn> OnRecievedPacket;

        //check for sus packets
        public Action<PacketIn> ProcessPacket;

        public MyEasyNetworkManager(ushort commsId)
        {
            this._commsId = commsId;
            TempPlayers = null;
        }

        public List<IMyPlayer> TempPlayers { get; private set; }

        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(_commsId, RecivedPacket);
        }

        public void UnRegister()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(_commsId, RecivedPacket);
        }

        public void TransmitToServer(IPacket data, bool sendToAllPlayers = true, bool sendToSender = false)
        {
            var packet = new PacketBase(data.GetId(), sendToAllPlayers, sendToSender);
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageToServer(_commsId, MyAPIGateway.Utilities.SerializeToBinary(packet));
        }

        //
        //public void ServerTimerSync(ITPacket data, ulong playerId)
        public void ServerTimerSync(ITPacket data, bool sendToAllPlayers = true, bool sendToSender = false)
        {
            var packet = new PacketBase(data.GetTime(), sendToAllPlayers, sendToSender);
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageToServer(_commsId, MyAPIGateway.Utilities.SerializeToBinary(packet));
            //MyAPIGateway.Multiplayer.SendMessageTo(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet), playerId);
        }

        public void TransmitToPlayer(IPacket data, ulong playerId)
        {
            var packet = new PacketBase(data.GetId(), false, false);
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageTo(_commsId, MyAPIGateway.Utilities.SerializeToBinary(packet), playerId);
        }

        private void RecivedPacket(ushort handler, byte[] raw, ulong id, bool isFromServer)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(raw);
                var packetIn = new PacketIn(packet.Id, packet.Data, id, isFromServer);

                ProcessPacket?.Invoke(packetIn);
                if (packetIn.IsCancelled) return;

                if (packet.SendToAllPlayers && MyAPIGateway.Session.IsServer) TransmitPacketToAllPlayers(id, packet);

                if ((!isFromServer && MyAPIGateway.Session.IsServer) ||
                    (isFromServer && (!MyAPIGateway.Session.IsServer || packet.SendToSender)) ||
                    (isFromServer && MyAPIGateway.Session.IsServer))
                    OnRecievedPacket?.Invoke(packetIn);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Exception in SC_Sharetrack!\n" + e);
            }
        }

        private void TransmitPacketToAllPlayers(ulong sender, PacketBase packet)
        {
            if (TempPlayers == null)
                TempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);
            else
                TempPlayers.Clear();

            MyAPIGateway.Players.GetPlayers(TempPlayers);

            foreach (var p in TempPlayers)
            {
                if (p.IsBot || p.SteamUserId == MyAPIGateway.Multiplayer.ServerId ||
                    (!packet.SendToSender && p.SteamUserId == sender))
                    continue;

                MyAPIGateway.Multiplayer.SendMessageTo(_commsId, MyAPIGateway.Utilities.SerializeToBinary(packet),
                    p.SteamUserId);
            }
        }

        [ProtoContract]
        private class PacketBase
        {
            [ProtoMember(1)] public readonly int Id;

            [ProtoMember(2)] public readonly bool SendToAllPlayers;

            [ProtoMember(3)] public readonly bool SendToSender;

            [ProtoMember(4)] public byte[] Data;


            public PacketBase()
            {
            }

            public PacketBase(int id, bool sendToAllPlayers, bool sendToSender)
            {
                this.Id = id;
                this.SendToAllPlayers = sendToAllPlayers;
                this.SendToSender = sendToSender;
            }

            public void Wrap(object data)
            {
                Data = MyAPIGateway.Utilities.SerializeToBinary(data);
            }
        }

        public interface IPacket
        {
            int GetId();
        }

        public interface ITPacket
        {
            int GetTime();
        }

        public class PacketIn
        {
            private readonly byte[] _data;

            public PacketIn(int packetId, byte[] data, ulong senderId, bool isFromServer)
            {
                PacketId = packetId;
                SenderId = senderId;
                IsFromServer = isFromServer;
                _data = data;
            }

            public bool IsCancelled { protected set; get; }
            public int PacketId { protected set; get; }
            public ulong SenderId { protected set; get; }
            public bool IsFromServer { protected set; get; }

            public T UnWrap<T>()
            {
                return MyAPIGateway.Utilities.SerializeFromBinary<T>(_data);
            }

            public void SetCancelled(bool value)
            {
                IsCancelled = value;
            }
        }
    }
}