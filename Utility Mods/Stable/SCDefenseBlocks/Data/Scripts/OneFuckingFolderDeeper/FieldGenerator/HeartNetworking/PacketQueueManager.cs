using System;
using ProtoBuf;
using Starcore.FieldGenerator.Networking.Custom;

namespace Starcore.FieldGenerator.Networking
{
    public class PacketQueueManager
    {
        public static PacketQueueManager I;

        private Queue<FloatSyncPacket> floatPackets = new Queue<FloatSyncPacket>();
        private Queue<IntSyncPacket> intPackets = new Queue<IntSyncPacket>();
        private Queue<BoolSyncPacket> boolPackets = new Queue<BoolSyncPacket>();

        public void Init()
        {
            I = this;
        }

        public void Close()
        {
            I = null;
        }
        
        public void Enqueue(FloatSyncPacket packet)
        {
            floatPackets.Enqueue(packet);
        }

        public void Enqueue(IntSyncPacket packet)
        {
            intPackets.Enqueue(packet);
        }

        public void Enqueue(BoolSyncPacket packet)
        {
            boolPackets.Enqueue(packet);
        }


        public PacketBase Dequeue(out long entityID)
        {
            entityID = 0;
            if (floatPackets.Count > 0)
            {
                var packet = floatPackets.Dequeue();
                entityID = packet.entityId;
                return packet;
            }

            if (intPackets.Count > 0)
            {
                var packet = intPackets.Dequeue();
                entityID = packet.entityId;
                return packet;
            }

            if (boolPackets.Count > 0)
            {
                var packet = boolPackets.Dequeue();
                entityID = packet.entityId;
                return packet;
            }

            return null;
        }

        public bool HasPackets()
        {
            return floatPackets.Count > 0 || intPackets.Count > 0 || boolPackets.Count > 0;
        }
    }

}