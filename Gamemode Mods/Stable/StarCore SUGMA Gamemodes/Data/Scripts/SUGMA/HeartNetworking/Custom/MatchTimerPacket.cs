using System;
using ProtoBuf;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class MatchTimerPacket : PacketBase
    {
        [ProtoMember(13)] private string _senderObjectId;
        [ProtoMember(12)] private readonly long _matchEndTime;
        [ProtoMember(11)] private readonly long _matchStartTime;

        public MatchTimerPacket()
        {
        }

        private MatchTimerPacket(MatchTimer timer)
        {
            _senderObjectId = timer.Id;
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

        public override string ToString()
        {
            return $"TimerId: {_senderObjectId}\nStartTime: {MatchStartTime()}\nEndTime: {MatchEndTime()}";
        }
    }
}