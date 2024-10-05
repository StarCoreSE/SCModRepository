using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Starcore.FieldGenerator.Networking.Custom
{
    [ProtoContract]
    public class SyncPacket<T> : PacketBase
    {
        [ProtoMember(21)] public string propertyName;
        [ProtoMember(22)] public T value;
        [ProtoMember(23)] public long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info($"Received Sync: {propertyName} = {value}");

            var fieldGenerator = FieldGenerator.GetLogic<FieldGenerator>(entityId);

            if (fieldGenerator != null)
            {
                if (typeof(T) == typeof(float))
                {
                    var floatValue = Convert.ToSingle(value);
                    switch (propertyName)
                    {
                        case nameof(FieldGenerator.FieldPower):
                            fieldGenerator.FieldPower = floatValue;
                            break;
                        case nameof(FieldGenerator.MaxFieldPower):
                            fieldGenerator.MaxFieldPower = floatValue;
                            break;
                        case nameof(FieldGenerator.MinFieldPower):
                            fieldGenerator.MinFieldPower = floatValue;
                            break;
                        case nameof(FieldGenerator.SizeModifier):
                            fieldGenerator.SizeModifier = floatValue;
                            break;
                        case nameof(FieldGenerator.Stability):
                            fieldGenerator.Stability = floatValue;
                            break;
                            // Add other float properties as needed
                    }
                }
                else if (typeof(T) == typeof(int))
                {
                    var intValue = Convert.ToInt32(value);
                    switch (propertyName)
                    {
                        case nameof(FieldGenerator.SiegeElapsedTime):
                            fieldGenerator.SiegeElapsedTime = intValue;
                            break;
                        case nameof(FieldGenerator.SiegeCooldownTime):
                            fieldGenerator.SiegeCooldownTime = intValue;
                            break;
                            // Add other int properties as needed
                    }
                }
                else if (typeof(T) == typeof(bool))
                {
                    var boolValue = Convert.ToBoolean(value);
                    switch (propertyName)
                    {
                        case nameof(FieldGenerator.SiegeMode):
                            fieldGenerator.SiegeMode = boolValue;
                            break;
                        case nameof(FieldGenerator.SiegeCooldownActive):
                            fieldGenerator.SiegeCooldownActive = boolValue;
                            break;
                            // Add other bool properties as needed
                    }
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

        public static void SyncProperty(long entityId, string propertyName, T value)
        {
            try
            {
                var packet = new SyncPacket<T>
                {
                    entityId = entityId,
                    propertyName = propertyName,
                    value = value
                };

                Log.Info($"SyncPacket<{typeof(T).Name}> Added to Queue: {propertyName} = {value}");

                PacketQueueManager.I.Enqueue(packet);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}