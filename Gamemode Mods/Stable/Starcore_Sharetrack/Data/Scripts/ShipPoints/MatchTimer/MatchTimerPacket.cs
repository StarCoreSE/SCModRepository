using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace SCModRepository.Gamemode_Mods.Stable.Starcore_Sharetrack.Data.Scripts.ShipPoints.MatchTimer
{
    [ProtoContract]
    internal class MatchTimerPacket
    {
        [ProtoMember(1)] public ulong SenderSteamId;
        [ProtoMember(2)] private long matchStartTime;
        [ProtoMember(3)] private long matchEndTime;

        public DateTime MatchStartTime()
        {
            long time = matchStartTime - (long)(NetworkTimeSync.ServerTimeOffset * TimeSpan.TicksPerMillisecond);
            return new DateTime(time < DateTime.MinValue.Ticks ? DateTime.MinValue.Ticks : time);
        }
        public DateTime MatchEndTime()
        {
            long time = matchEndTime - (long)(NetworkTimeSync.ServerTimeOffset * TimeSpan.TicksPerMillisecond);
            return new DateTime(time < DateTime.MinValue.Ticks ? DateTime.MinValue.Ticks : time);
        }
        public MatchTimerPacket()
        {
        }

        public MatchTimerPacket(MatchTimer timer)
        {
            SenderSteamId = MyAPIGateway.Multiplayer.MyId;
            matchStartTime = timer.StartTime.Ticks;
            matchEndTime = timer.EndTime.Ticks;
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
            MatchTimerPacket packet = MyAPIGateway.Utilities.SerializeFromBinary<MatchTimerPacket>(serialized);
            if (packet == null)
                return;
            MatchTimer.I.UpdateFromPacket(packet);
        }
        private static void SendToEveryone(MatchTimerPacket packet)
        {
            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToOthers(MatchTimer.NetworkId, serialized);
        }
        public static void SendMatchUpdate(MatchTimer timer)
        {
            MatchTimerPacket packet = new MatchTimerPacket(timer);
            SendToEveryone(packet);
        }
        #endregion
    }
}
