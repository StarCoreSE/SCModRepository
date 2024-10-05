using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Starcore.FieldGenerator.Networking.Custom
{
    [ProtoContract]
    public class IntSyncPacket : PacketBase
    {
        [ProtoMember(41)] public string propertyName;
        [ProtoMember(42)] private int value;
        [ProtoMember(43)] public long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info($"Received Int Sync: {propertyName} = {value}");

            var fieldGenerator = FieldGenerator.GetLogic<FieldGenerator>(entityId);

            if (fieldGenerator != null)
            {
                switch (propertyName)
                {
                    case nameof(FieldGenerator.SiegeElapsedTime):
                        fieldGenerator.SiegeElapsedTime = value;
                        break;
                    case nameof(FieldGenerator.SiegeCooldownTime):
                        fieldGenerator.SiegeCooldownTime = value;
                        break;
                        // Add other int properties as needed
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

        public static void SyncIntProperty(long entityId, string propertyName, int value)
        {
            try
            {
                var packet = new IntSyncPacket
                {
                    entityId = entityId,
                    propertyName = propertyName,
                    value = value
                };

                Log.Info($"Int-Type Packet Added to Queue: {propertyName} = {value}");

                PacketQueueManager.I.Enqueue(packet);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}