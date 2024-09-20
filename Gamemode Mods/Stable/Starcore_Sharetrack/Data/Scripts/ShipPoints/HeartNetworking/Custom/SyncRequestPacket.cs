using System;
using ProtoBuf;
using Sandbox.ModAPI;
using StarCore.ShareTrack.ShipTracking;

namespace StarCore.ShareTrack.HeartNetworking.Custom
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