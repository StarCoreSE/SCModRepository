using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Starcore.FieldGenerator.Networking.Custom;

namespace Starcore.FieldGenerator.Networking
{
    public class PacketQueueManager
    {
        public static PacketQueueManager I;

        private Dictionary<long, LinkedList<FloatSyncPacket>> floatPacketQueues = new Dictionary<long, LinkedList<FloatSyncPacket>>();
        private Dictionary<long, LinkedList<IntSyncPacket>> intPacketQueues = new Dictionary<long, LinkedList<IntSyncPacket>>();
        private Dictionary<long, LinkedList<BoolSyncPacket>> boolPacketQueues = new Dictionary<long, LinkedList<BoolSyncPacket>>();

        private List<long> entityIds = new List<long>();
        private int lastEntityProcessedIndex = -1;

        public void Init()
        {
            I = this;
        }

        public void Close()
        {
            I = null;
        }

        // Seperate Enqueues needed because reasons (TODO: Be Smarter)
        public void Enqueue(FloatSyncPacket packet)
        {
            if (!floatPacketQueues.ContainsKey(packet.entityId))
            {
                floatPacketQueues[packet.entityId] = new LinkedList<FloatSyncPacket>();
                entityIds.Add(packet.entityId);
            }

            RemoveStalePackets(floatPacketQueues[packet.entityId], packet.propertyName);

            floatPacketQueues[packet.entityId].AddLast(packet);
        }

        public void Enqueue(IntSyncPacket packet)
        {
            if (!intPacketQueues.ContainsKey(packet.entityId))
            {
                intPacketQueues[packet.entityId] = new LinkedList<IntSyncPacket>();
                entityIds.Add(packet.entityId);
            }

            RemoveStalePackets(intPacketQueues[packet.entityId], packet.propertyName);

            intPacketQueues[packet.entityId].AddLast(packet);
        }

        public void Enqueue(BoolSyncPacket packet)
        {
            if (!boolPacketQueues.ContainsKey(packet.entityId))
            {
                boolPacketQueues[packet.entityId] = new LinkedList<BoolSyncPacket>();
                entityIds.Add(packet.entityId);
            }

            RemoveStalePackets(boolPacketQueues[packet.entityId], packet.propertyName);

            boolPacketQueues[packet.entityId].AddLast(packet);
        }

        private void RemoveStalePackets<T>(LinkedList<T> list, string propertyName) where T : PacketBase
        {
            var currentNode = list.First;

            while (currentNode != null)
            {
                var nextNode = currentNode.Next;

                if (typeof(T) == typeof(FloatSyncPacket) && (currentNode.Value as FloatSyncPacket).propertyName == propertyName)
                {
                    list.Remove(currentNode);
                }
                else if (typeof(T) == typeof(IntSyncPacket) && (currentNode.Value as IntSyncPacket).propertyName == propertyName)
                {
                    list.Remove(currentNode);
                }
                else if (typeof(T) == typeof(BoolSyncPacket) && (currentNode.Value as BoolSyncPacket).propertyName == propertyName)
                {
                    list.Remove(currentNode);
                }

                currentNode = nextNode;
            }
        }

        public IEnumerable<long> GetEntitiesWithPackets()
        {
            foreach (var entry in floatPacketQueues)
            {
                if (entry.Value.Count > 0)
                    yield return entry.Key;
            }

            foreach (var entry in intPacketQueues)
            {
                if (entry.Value.Count > 0)
                    yield return entry.Key;
            }

            foreach (var entry in boolPacketQueues)
            {
                if (entry.Value.Count > 0)
                    yield return entry.Key;
            }
        }

        public PacketBase PeekNextPacket(long entityID)
        {
            if (floatPacketQueues.ContainsKey(entityID) && floatPacketQueues[entityID].Count > 0)
            {
                return floatPacketQueues[entityID].First.Value;
            }
            else if (intPacketQueues.ContainsKey(entityID) && intPacketQueues[entityID].Count > 0)
            {
                return intPacketQueues[entityID].First.Value;
            }
            else if (boolPacketQueues.ContainsKey(entityID) && boolPacketQueues[entityID].Count > 0)
            {
                return boolPacketQueues[entityID].First.Value;
            }

            return null;
        }

        public void DequeuePacket(long entityID)
        {
            if (floatPacketQueues.ContainsKey(entityID) && floatPacketQueues[entityID].Count > 0)
            {
                floatPacketQueues[entityID].RemoveFirst();
            }
            else if (intPacketQueues.ContainsKey(entityID) && intPacketQueues[entityID].Count > 0)
            {
                intPacketQueues[entityID].RemoveFirst();
            }
            else if (boolPacketQueues.ContainsKey(entityID) && boolPacketQueues[entityID].Count > 0)
            {
                boolPacketQueues[entityID].RemoveFirst();
            }

            if (HasNoPackets(entityID))
            {
                entityIds.Remove(entityID);
            }
        }

        private bool HasNoPackets(long entityID)
        {
            return (!floatPacketQueues.ContainsKey(entityID) || floatPacketQueues[entityID].Count == 0) &&
                   (!intPacketQueues.ContainsKey(entityID) || intPacketQueues[entityID].Count == 0) &&
                   (!boolPacketQueues.ContainsKey(entityID) || boolPacketQueues[entityID].Count == 0);
        }

        public bool HasPackets()
        {
            return entityIds.Count > 0;
        }
    }
}