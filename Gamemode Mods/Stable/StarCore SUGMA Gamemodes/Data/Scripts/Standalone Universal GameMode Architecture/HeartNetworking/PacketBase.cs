using System;
using ProtoBuf;
using SC.SUGMA.HeartNetworking.Custom;

namespace SC.SUGMA.HeartNetworking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoInclude(1, typeof(MatchTimerPacket))]
    [ProtoInclude(2, typeof(GameStatePacket))]
    [ProtoInclude(3, typeof(PointsPacket))]
    [ProtoInclude(4, typeof(SyncRequestPacket))]
    public abstract class PacketBase
    {
        public static readonly Type[] PacketTypes =
        {
            typeof(PacketBase),
            typeof(MatchTimerPacket),
            typeof(GameStatePacket),
            typeof(PointsPacket),
            typeof(SyncRequestPacket),
        };

        /// <summary>
        ///     Called whenever your packet is received.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}