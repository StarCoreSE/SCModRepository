using System;
using ProtoBuf;
using SCModRepository_Dev.Gamemode_Mods.Development.Starcore_Sharetrack_Dev.Data.Scripts.ShipPoints.HeartNetworking.
    Custom;
using ShipPoints.HeartNetworking.Custom;

namespace ShipPoints.HeartNetworking
{
    [ProtoInclude(91, typeof(TrackingSyncPacket))]
    [ProtoInclude(92, typeof(SyncRequestPacket))]
    [ProtoInclude(93, typeof(GameStatePacket))]
    [ProtoInclude(94, typeof(ProblemReportPacket))]
    [ProtoInclude(95, typeof(ShieldFillRequestPacket))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract class PacketBase
    {
        public static Type[] Types =
        {
            typeof(PacketBase),
            typeof(TrackingSyncPacket),
            typeof(SyncRequestPacket),
            typeof(GameStatePacket),
            typeof(ProblemReportPacket),
            typeof(ShieldFillRequestPacket)
        };

        /// <summary>
        ///     Called whenever your packet is recieved.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}