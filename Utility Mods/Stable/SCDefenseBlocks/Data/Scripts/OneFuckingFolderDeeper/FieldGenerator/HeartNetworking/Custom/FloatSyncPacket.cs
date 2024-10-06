using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Starcore.FieldGenerator.Networking.Custom
{
    [ProtoContract]
    public class FloatSyncPacket : PacketBase
    {
        [ProtoMember(31)] public string propertyName;
        [ProtoMember(32)] private float value;
        [ProtoMember(33)] public long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info($"Received Float Sync: {propertyName} = {value}");

            var fieldGenerator = FieldGenerator.GetLogic<FieldGenerator>(entityId);

            if (fieldGenerator != null)
            {
                switch (propertyName)
                {
                    case nameof(FieldGenerator.FieldPower):
                        fieldGenerator.FieldPower = value;
                        break;
                    case nameof(FieldGenerator.MaxFieldPower):
                        fieldGenerator.MaxFieldPower = value;
                        break;
                    case nameof(FieldGenerator.MinFieldPower):
                        fieldGenerator.MinFieldPower = value;
                        break;
                    case nameof(FieldGenerator.SizeModifier):
                        fieldGenerator.SizeModifier = value;
                        break;
                    case nameof(FieldGenerator.Stability):
                        fieldGenerator.Stability = value;
                        break;
                        // Add other float properties as needed
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

        public static void SyncFloatProperty(long entityId, string propertyName, float value)
        {
            try
            {
                var packet = new FloatSyncPacket
                {
                    entityId = entityId,
                    propertyName = propertyName,
                    value = value
                };
                   
                Log.Info($"Float-Type Packet Added to Queue: {propertyName} = {value}");

                PacketQueueManager.I.Enqueue(packet);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}