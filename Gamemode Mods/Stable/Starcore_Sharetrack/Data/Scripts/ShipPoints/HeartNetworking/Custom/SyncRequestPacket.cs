using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Sandbox.ModAPI;
using ShipPoints.HeartNetworking;
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
