using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Starcore.FieldGenerator.Networking.Custom
{
    [ProtoContract]
    public class BoolSyncPacket : PacketBase
    {
        [ProtoMember(21)] public string propertyName;
        [ProtoMember(22)] private bool value;
        [ProtoMember(23)] public long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info($"Received Bool Sync: {propertyName} = {value}");

            var fieldGenerator = FieldGenerator.GetLogic<FieldGenerator>(entityId);

            if (fieldGenerator != null)
            {
                switch (propertyName)
                {
                    case nameof(FieldGenerator.SiegeMode):
                        fieldGenerator.SiegeMode = value;
                        break;
                    case nameof(FieldGenerator.SiegeCooldownActive):
                        fieldGenerator.SiegeCooldownActive = value;
                        break;
                        // Add other bool properties as needed
                }

                if (MyAPIGateway.Session.IsServer)
                {
                    HeartNetwork.I.SendToEveryone(this);
                }
            }
            else
            {
                Log.Info($"Received method failed: FieldGenerator is null. Entity ID: {entityId}");
            }
        }

        public static void SyncBoolProperty(long entityId, string propertyName, bool value)
        {
            try
            {
                var packet = new BoolSyncPacket
                {
                    entityId = entityId,
                    propertyName = propertyName,
                    value = value
                };

                Log.Info($"Bool-Type Packet Added to Queue: {propertyName} = {value}");

                PacketQueueManager.I.EnqueuePacket(packet);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}