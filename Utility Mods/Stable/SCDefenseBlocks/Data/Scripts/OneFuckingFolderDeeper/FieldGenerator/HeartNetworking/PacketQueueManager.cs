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

        public PacketBase Peek(out long entityID)
        {
            entityID = 0;
            if (floatPackets.Count > 0)
            {
                var packet = floatPackets.Peek();
                entityID = packet.entityId;
                return packet;
            }

            if (intPackets.Count > 0)
            {
                var packet = intPackets.Peek();
                entityID = packet.entityId;
                return packet;
            }

            if (boolPackets.Count > 0)
            {
                var packet = boolPackets.Peek();
                entityID = packet.entityId;
                return packet;
            }

            return null;
        }

        public void Dequeue()
        {
            if (floatPackets.Count > 0)
            {
                floatPackets.Dequeue(); // Remove the first packet from the queue
                return;
            }

            if (intPackets.Count > 0)
            {
                intPackets.Dequeue();
                return;
            }

            if (boolPackets.Count > 0)
            {
                boolPackets.Dequeue();
                return;
            }
        }


        public bool HasPackets()
        {
            return floatPackets.Count > 0 || intPackets.Count > 0 || boolPackets.Count > 0;
        }
    }

}