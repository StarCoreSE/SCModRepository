using System;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace ShipPoints.MatchTiming
{
    /// <summary>
    ///     Packet used for syncing time betweeen client and server.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class NetworkTimeSync : MySessionComponentBase
    {
        public const ushort NetworkId = 23762;
        public static double ThisPlayerPing;
        public static double ServerTimeOffset;

        private int _tickCounter;

        public override void LoadData()
        {
            try
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkId, ReceiveMessage);
                MyLog.Default.WriteLineAndConsole("[NetworkTimeSync] Registered network message handler.");
                UpdateTimeOffset();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw ex;
            }
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, ReceiveMessage);
                MyLog.Default.WriteLineAndConsole("[NetworkTimeSync] De-registered network message handler.");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw ex;
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (_tickCounter % 307 == 0)
                    UpdateTimeOffset();
                _tickCounter++;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void UpdateTimeOffset()
        {
            ThisPlayerPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            if (!MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToServer(
                    NetworkId,
                    MyAPIGateway.Utilities.SerializeToBinary(new TimeSyncPacket
                        { OutgoingTimestamp = ThisPlayerPing }));
        }

        private void ReceiveMessage(ushort networkId, byte[] serialized, ulong sender, bool isFromServer)
        {
            if (serialized == null || serialized.Length == 0)
                return;
            var packet = MyAPIGateway.Utilities.SerializeFromBinary<TimeSyncPacket>(serialized);
            if (packet == null)
                return;

            packet.Received(sender);
        }
    }

    [ProtoContract]
    internal class TimeSyncPacket
    {
        [ProtoMember(22)] public double IncomingTimestamp;
        [ProtoMember(21)] public double OutgoingTimestamp;

        public void Received(ulong senderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(
                    NetworkTimeSync.NetworkId,
                    MyAPIGateway.Utilities.SerializeToBinary(new TimeSyncPacket
                    {
                        IncomingTimestamp = OutgoingTimestamp,
                        OutgoingTimestamp = DateTime.UtcNow.TimeOfDay.TotalMilliseconds
                    }),
                    senderSteamId);
            }
            else
            {
                NetworkTimeSync.ThisPlayerPing =
                    DateTime.UtcNow.TimeOfDay.TotalMilliseconds - NetworkTimeSync.ThisPlayerPing;
                NetworkTimeSync.ServerTimeOffset =
                    OutgoingTimestamp - IncomingTimestamp - NetworkTimeSync.ThisPlayerPing;
                //HeartLog.Log("Outgoing Timestamp: " + OutgoingTimestamp + "\nIncoming Timestamp: " + IncomingTimestamp);
                //HeartLog.Log("Total ping time (ms): " + HeartData.I.Net.estimatedPing);
            }
        }
    }
}