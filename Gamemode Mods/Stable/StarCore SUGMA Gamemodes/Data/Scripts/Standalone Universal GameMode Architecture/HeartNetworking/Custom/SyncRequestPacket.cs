using System;
using ProtoBuf;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    public class SyncRequestPacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Received join-sync request from " + SenderSteamId);
            GameStatePacket.UpdateGamestate();
        }

        public static void RequestSync()
        {
            HeartNetwork.I.SendToServer(new SyncRequestPacket());
        }
    }
}
