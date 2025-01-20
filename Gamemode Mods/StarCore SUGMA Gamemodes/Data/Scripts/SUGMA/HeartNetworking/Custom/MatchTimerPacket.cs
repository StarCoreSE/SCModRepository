using System;
using ProtoBuf;
using SC.SUGMA.GameState;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class MatchTimerPacket : PacketBase
    {
        [ProtoMember(12)] private readonly long _matchEndTime;
        [ProtoMember(11)] private readonly long _matchStartTime;
        [ProtoMember(13)] private readonly string _senderObjectId;

        public MatchTimerPacket()
        {
        }

        private MatchTimerPacket(MatchTimer timer)
        {
            _senderObjectId = timer.ComponentId;
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

        public override string ToString()
        {
            return $"TimerId: {_senderObjectId}\nStartTime: {MatchStartTime()}\nEndTime: {MatchEndTime()}";
        }


        #region Static Methods

        public override void Received(ulong sender)
        {
            //Log.Info($"Receive MatchTimerPacket:\n{this}\nTime offset: {NetworkTimeSync.ServerTimeOffset}");
            SUGMA_SessionComponent.I.GetComponent<MatchTimer>(_senderObjectId)
                ?.Update(MatchStartTime(), MatchEndTime());
        }

        public static void SendMatchUpdate(MatchTimer timer)
        {
            var packet = new MatchTimerPacket(timer);
            //Log.Info($"Send MatchTimerPacket:\n{packet}");
            HeartNetwork.I.SendToEveryone(packet);
        }

        #endregion
    }
}