using System;
using ProtoBuf;

namespace StarCore.RepairModule.Networking.Custom
{
    [ProtoContract]
    public class SyncRequestPacket : PacketBase
    {
        [ProtoMember(12)] private long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recived Sync Request From: " + SenderSteamId);
            IgnoreArmorPacket.UpdateIgnoreArmor(entityId);
            PriorityOnlyPacket.UpdatePriorityOnly(entityId);
            SubsystemPriorityPacket.UpdateSubsystemPriority(entityId);
        }

        public static void RequestSync(long entityID)
        {
            SyncRequestPacket packet = new SyncRequestPacket
            {
                entityId = entityID,
            };

            HeartNetwork.I.SendToServer(packet);
        }
    }
}