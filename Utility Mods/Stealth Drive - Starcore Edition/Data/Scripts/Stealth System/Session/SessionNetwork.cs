using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;

namespace StealthSystem
{
    public partial class StealthSession
    {
        internal const ushort ServerPacketId = 65347;
        internal const ushort ClientPacketId = 65348;

        public static void SendPacketToServer(Packet packet)
        {
            var rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyModAPIHelper.MyMultiplayer.Static.SendMessageToServer(ServerPacketId, rawData, true);
        }

        public static void SendPacketToClient(Packet packet, ulong client)
        {
            var rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(ClientPacketId, rawData, client, true);
        }

        public static void SendPacketToClients(Packet packet, List<ulong> clients)
        {
            var rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

            foreach (var client in clients)
                MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(ClientPacketId, rawData, client, true);
        }

        internal void ProcessPacket(ushort id, byte[] rawData, ulong sender, bool reliable)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(rawData);
                if (packet == null || packet.EntityId != 0 && !DriveMap.ContainsKey(packet.EntityId))
                {
                    Logs.WriteLine($"Invalid packet - null:{packet == null}");
                    return;
                }

                var comp = packet.EntityId == 0 ? null : DriveMap[packet.EntityId];
                switch (packet.Type)
                {
                    case PacketType.UpdateState:
                        var uPacket = packet as UpdateStatePacket;
                        comp.EnterStealth = uPacket.EnterStealth && !comp.StealthActive;
                        comp.ExitStealth = uPacket.ExitStealth && comp.StealthActive;
                        break;
                    case PacketType.UpdateDuration:
                        var dPacket = packet as UpdateDurationPacket;
                        //comp.RemainingDuration += dPacket.DurationChange;
                        comp.TotalTime += dPacket.DurationChange;
                        break;
                    case PacketType.Replicate:
                        var rPacket = packet as ReplicationPacket;
                        if (rPacket.Fresh)
                            comp.ReplicatedClients.Add(sender);
                        else
                            comp.ReplicatedClients.Remove(sender);
                        break;
                    case PacketType.Settings:
                        var sPacket = packet as SettingsPacket;
                        UpdateEnforcement(sPacket.Settings);
                        break;
                    default:
                        Logs.WriteLine($"Invalid packet type - {packet.GetType()}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logs.WriteLine($"Exception in ProcessPacket: {ex}");
            }

        }

    }

    [ProtoContract]
    [ProtoInclude(4, typeof(UpdateStatePacket))]
    [ProtoInclude(5, typeof(UpdateDurationPacket))]
    [ProtoInclude(6, typeof(ReplicationPacket))]
    [ProtoInclude(7, typeof(SettingsPacket))]
    public class Packet
    {
        [ProtoMember(1)] internal long EntityId;
        [ProtoMember(2)] internal PacketType Type;
    }

    [ProtoContract]
    public class UpdateStatePacket : Packet
    {
        [ProtoMember(1)] internal bool EnterStealth;
        [ProtoMember(2)] internal bool ExitStealth;
    }

    [ProtoContract]
    public class UpdateDurationPacket : Packet
    {
        [ProtoMember(1)] internal int DurationChange;
    }

    [ProtoContract]
    public class ReplicationPacket : Packet
    {
        [ProtoMember(1)] internal bool Fresh;
    }

    [ProtoContract]
    public class SettingsPacket : Packet
    {
        [ProtoMember(1)] internal StealthSettings Settings;
    }

    public enum PacketType
    {
        UpdateState,
        UpdateDuration,
        Replicate,
        Settings
    }

}
