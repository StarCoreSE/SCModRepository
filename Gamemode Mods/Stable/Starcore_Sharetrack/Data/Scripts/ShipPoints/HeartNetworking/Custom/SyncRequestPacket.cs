using System;
using ProtoBuf;
using Sandbox.ModAPI;
using ShipPoints.ShipTracking;

namespace ShipPoints.HeartNetworking.Custom
{
    [ProtoContract]
    internal class SyncRequestPacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            if (AllGridsList.I == null)
                throw new Exception("Null PointCheck instance!");
            if (TrackingManager.I == null)
                throw new Exception("Null TrackingManager instance!");

            Log.Info("Received join-sync request from " + SenderSteamId);

            TrackingManager.I.ServerDoSync();
        }
    }
}