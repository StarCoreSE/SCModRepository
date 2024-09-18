/*using System;
using ProtoBuf;

namespace Starcore.FieldGenerator.Networking.Custom
{
    [ProtoContract]
    public class SyncRequestPacket : PacketBase
    {
        [ProtoMember(12)] private long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recived Sync Request From: " + SenderSteamId);
            SiegeModePacket.UpdateSiegeMode(entityId);
            FieldPowerPacket.UpdateFieldPower(entityId);
            MaxFieldPowerPacket.UpdateMaxFieldPower(entityId);
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
}*/