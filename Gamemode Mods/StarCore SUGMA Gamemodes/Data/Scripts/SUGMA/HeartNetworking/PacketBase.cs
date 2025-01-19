using System;
using ProtoBuf;
using SC.SUGMA.HeartNetworking.Custom;

namespace SC.SUGMA.HeartNetworking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoInclude(91, typeof(MatchTimerPacket))]
    [ProtoInclude(92, typeof(GameStatePacket))]
    [ProtoInclude(93, typeof(PointsPacket))]
    [ProtoInclude(94, typeof(SyncRequestPacket))]
    [ProtoInclude(95, typeof(ShieldFillRequestPacket))]
    [ProtoInclude(96, typeof(ProblemReportPacket))]
    [ProtoInclude(97, typeof(MissingPlayerOverridePacket))]
    [ProtoInclude(98, typeof(AutoBalancePacket))]
    [ProtoInclude(99, typeof(PauseRequestPacket))]
    [ProtoInclude(100, typeof(ClearBoardRequestPacket))]
    public abstract class PacketBase
    {
        public static readonly Type[] PacketTypes =
        {
            typeof(PacketBase),
            typeof(MatchTimerPacket),
            typeof(GameStatePacket),
            typeof(PointsPacket),
            typeof(SyncRequestPacket),
            typeof(ShieldFillRequestPacket),
            typeof(ProblemReportPacket),
            typeof(MissingPlayerOverridePacket),
            typeof(AutoBalancePacket),
            typeof(PauseRequestPacket),
            typeof(ClearBoardRequestPacket),
        };

        /// <summary>
        ///     Called whenever your packet is received.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}