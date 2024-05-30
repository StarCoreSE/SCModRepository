using System;
using ProtoBuf;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class MatchTimerPacket : PacketBase
    {
        [ProtoMember(4)] private string _senderObjectId;
        [ProtoMember(3)] private readonly long _matchEndTime;
        [ProtoMember(2)] private readonly long _matchStartTime;
        [ProtoMember(1)] public ulong SenderSteamId;

        public MatchTimerPacket()
        {
        }

        private MatchTimerPacket(MatchTimer timer)
        {
            _senderObjectId = timer.Id;
            SenderSteamId = MyAPIGateway.Multiplayer.MyId;
            _matchStartTime = timer.StartTime.Ticks;
            _matchEndTime = timer.EndTime.Ticks;
        }

        private DateTime MatchStartTime()
        {
            var time = _matchStartTime - (long)(NetworkTimeSync.ServerTimeOffset * TimeSpan.TicksPerMillisecond);
            return new DateTime(time < DateTime.MinValue.Ticks ? DateTime.MinValue.Ticks : time);
        }

        private DateTime MatchEndTime()
        {
            var time = _matchEndTime - (long)(NetworkTimeSync.ServerTimeOffset * TimeSpan.TicksPerMillisecond);
            return new DateTime(time < DateTime.MinValue.Ticks ? DateTime.MinValue.Ticks : time);
        }


        #region Static Methods

        public override void Received(ulong sender)
        {
            SUGMA_SessionComponent.I.GetComponent<MatchTimer>(_senderObjectId)?.Update(MatchStartTime(), MatchEndTime());
        }

        public static void SendMatchUpdate(MatchTimer timer)
        {
            var packet = new MatchTimerPacket(timer);
            HeartNetwork.I.SendToEveryone(packet);
        }

        #endregion
    }
}