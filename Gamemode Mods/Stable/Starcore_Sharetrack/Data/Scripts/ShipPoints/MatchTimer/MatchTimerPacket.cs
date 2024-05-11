using System;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Utils;

namespace ShipPoints.MatchTiming
{
    [ProtoContract]
    internal class MatchTimerPacket
    {
        [ProtoMember(3)] private readonly long _matchEndTime;
        [ProtoMember(2)] private readonly long _matchStartTime;
        [ProtoMember(1)] public ulong SenderSteamId;

        public MatchTimerPacket()
        {
        }

        public MatchTimerPacket(MatchTimer timer)
        {
            SenderSteamId = MyAPIGateway.Multiplayer.MyId;
            _matchStartTime = timer.StartTime.Ticks;
            _matchEndTime = timer.EndTime.Ticks;
        }

        public DateTime MatchStartTime()
        {
            var time = _matchStartTime - (long)(NetworkTimeSync.ServerTimeOffset * TimeSpan.TicksPerMillisecond);
            return new DateTime(time < DateTime.MinValue.Ticks ? DateTime.MinValue.Ticks : time);
        }

        public DateTime MatchEndTime()
        {
            var time = _matchEndTime - (long)(NetworkTimeSync.ServerTimeOffset * TimeSpan.TicksPerMillisecond);
            return new DateTime(time < DateTime.MinValue.Ticks ? DateTime.MinValue.Ticks : time);
        }


        #region Static Methods

        public static void RegisterToRecieve()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MatchTimer.NetworkId, RecieveMessage);
            MyLog.Default.WriteLineAndConsole("[MatchTimer] Registered network message handler.");
        }

        public static void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MatchTimer.NetworkId, RecieveMessage);
            MyLog.Default.WriteLineAndConsole("[MatchTimer] De-registered network message handler.");
        }

        private static void RecieveMessage(ushort networkId, byte[] serialized, ulong sender, bool isFromServer)
        {
            if (serialized == null || serialized.Length == 0 || !isFromServer)
                return;
            var packet = MyAPIGateway.Utilities.SerializeFromBinary<MatchTimerPacket>(serialized);
            if (packet == null)
                return;
            MatchTimer.I.UpdateFromPacket(packet);
        }

        private static void SendToEveryone(MatchTimerPacket packet)
        {
            var serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToOthers(MatchTimer.NetworkId, serialized);
        }

        public static void SendMatchUpdate(MatchTimer timer)
        {
            var packet = new MatchTimerPacket(timer);
            SendToEveryone(packet);
        }

        #endregion
    }
}