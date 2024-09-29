using System;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace SC.SUGMA.HeartNetworking
{
    /// <summary>
    ///     Packet used for syncing time betweeen client and server.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class NetworkTimeSync : MySessionComponentBase
    {
        public const ushort NetworkId = 23764;
        public static double ThisPlayerPing;
        public static double ServerTimeOffset;

        private int _tickCounter;
        public static double MessageSendTimestamp { get; private set; }

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
                Log.Exception(ex, typeof(NetworkTimeSync));
                throw ex;
            }
        }

        protected override void UnloadData()
        {
            try
            {
                if (MyAPIGateway.Multiplayer != null)
                {
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, ReceiveMessage);
                    MyLog.Default.WriteLineAndConsole("[NetworkTimeSync] De-registered network message handler.");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(NetworkTimeSync), "Error unloading NetworkTimeSync");
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
                Log.Exception(ex, typeof(NetworkTimeSync));
            }
        }

        private void UpdateTimeOffset()
        {
            MessageSendTimestamp = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            if (!MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToServer(
                    NetworkId,
                    MyAPIGateway.Utilities.SerializeToBinary(new TimeSyncPacket
                        { OutgoingTimestamp = MessageSendTimestamp }));
        }

        private void ReceiveMessage(ushort networkId, byte[] serialized, ulong sender, bool isFromServer)
        {
            if (serialized == null || serialized.Length == 0)
            {
                Log.Info("Received empty or null time sync packet.");
                return;
            }

            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<TimeSyncPacket>(serialized);
                if (packet == null)
                {
                    Log.Info("Failed to deserialize time sync packet.");
                    return;
                }

                packet.Received(sender);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(NetworkTimeSync), "Error processing time sync packet");
            }
        }

    }

    [ProtoContract]
    internal class TimeSyncPacket
    {
        [ProtoMember(22)] public double IncomingTimestamp;
        [ProtoMember(21)] public double OutgoingTimestamp;

        public void Received(ulong senderSteamId)
        {
            try
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    var response = new TimeSyncPacket
                    {
                        IncomingTimestamp = OutgoingTimestamp,
                        OutgoingTimestamp = DateTime.UtcNow.TimeOfDay.TotalMilliseconds
                    };
                    byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(response);
                    if (serialized != null)
                    {
                        MyAPIGateway.Multiplayer.SendMessageTo(NetworkTimeSync.NetworkId, serialized, senderSteamId);
                    }
                    else
                    {
                        Log.Info("Failed to serialize TimeSyncPacket response.");
                    }
                }
                else
                {
                    NetworkTimeSync.ThisPlayerPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds - NetworkTimeSync.MessageSendTimestamp;
                    NetworkTimeSync.ServerTimeOffset = OutgoingTimestamp - IncomingTimestamp - NetworkTimeSync.ThisPlayerPing;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(TimeSyncPacket), "Error processing TimeSyncPacket");
            }
        }
    }
}