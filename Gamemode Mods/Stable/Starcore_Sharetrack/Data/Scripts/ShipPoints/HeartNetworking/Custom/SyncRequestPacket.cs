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

            Log.Info("Received join-sync request from " + SenderSteamId);

            HeartNetwork.I.SendToPlayer(new GameStatePacket(PointCheck.I), SenderSteamId);
            TrackingManager.I.ServerDoSync();
        }
    }
}