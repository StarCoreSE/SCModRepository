using System;
using ProtoBuf;

namespace StarCore.RepairModule.Networking.Custom
{
    [ProtoContract]
    public class SyncRequestPacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recived Sync Request From: " + SenderSteamId);
            IgnoreArmorPacket.UpdateIgnoreArmor();
            PriorityOnlyPacket.UpdatePriorityOnly();
            SubsystemPriorityPacket.UpdateSubsystemPriority();
        }

        public static void RequestSync()
        {
            HeartNetwork.I.SendToServer(new SyncRequestPacket());
        }
    }
}