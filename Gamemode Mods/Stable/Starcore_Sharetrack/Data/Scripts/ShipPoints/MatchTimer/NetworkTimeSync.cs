using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;

namespace SCModRepository.Gamemode_Mods.Stable.Starcore_Sharetrack.Data.Scripts.ShipPoints.MatchTimer
{
    /// <summary>
    /// Packet used for syncing time betweeen client and server.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class NetworkTimeSync : MySessionComponentBase
    {
        public static double ThisPlayerPing = 0;
        public static double ServerTimeOffset = 0;

        public const ushort NetworkId = 23762;

        public override void LoadData()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkId, ReceiveMessage);
            MyLog.Default.WriteLineAndConsole("[NetworkTimeSync] Registered network message handler.");
            UpdateTimeOffset();
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, ReceiveMessage);
            MyLog.Default.WriteLineAndConsole("[NetworkTimeSync] De-registered network message handler.");
        }

        int tickCounter = 0;
        public override void UpdateAfterSimulation()
        {
            if (tickCounter % 307 == 0)
                UpdateTimeOffset();
            tickCounter++;
        }

        private void UpdateTimeOffset()
        {
            ThisPlayerPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            if (!MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageToServer(
                    NetworkId,
                    MyAPIGateway.Utilities.SerializeToBinary(new TimeSyncPacket() { OutgoingTimestamp = ThisPlayerPing }));
            }
        }

        void ReceiveMessage(ushort networkId, byte[] serialized, ulong sender, bool isFromServer)
        {
            if (serialized == null || serialized.Length == 0)
                return;
            TimeSyncPacket packet = MyAPIGateway.Utilities.SerializeFromBinary<TimeSyncPacket>(serialized);
            if (packet == null)
                return;

            packet.Received(sender);
        }
    }

    [ProtoContract]
    internal class TimeSyncPacket
    {
        [ProtoMember(21)] public double OutgoingTimestamp;
        [ProtoMember(22)] public double IncomingTimestamp;

        public void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(
                    NetworkTimeSync.NetworkId,
                    MyAPIGateway.Utilities.SerializeToBinary(new TimeSyncPacket()
                    {
                        IncomingTimestamp = this.OutgoingTimestamp,
                        OutgoingTimestamp = DateTime.UtcNow.TimeOfDay.TotalMilliseconds
                    }),
                    SenderSteamId);
            }
            else
            {
                NetworkTimeSync.ThisPlayerPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds - NetworkTimeSync.ThisPlayerPing;
                NetworkTimeSync.ServerTimeOffset = OutgoingTimestamp - IncomingTimestamp - NetworkTimeSync.ThisPlayerPing;
                //HeartLog.Log("Outgoing Timestamp: " + OutgoingTimestamp + "\nIncoming Timestamp: " + IncomingTimestamp);
                //HeartLog.Log("Total ping time (ms): " + HeartData.I.Net.estimatedPing);
            }
        }
    }
}
