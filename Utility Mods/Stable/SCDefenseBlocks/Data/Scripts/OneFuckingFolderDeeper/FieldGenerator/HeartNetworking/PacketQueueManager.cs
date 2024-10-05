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

        private Dictionary<long, LinkedList<PacketBase>> packetQueues = new Dictionary<long, LinkedList<PacketBase>>();

        private List<long> entityIds = new List<long>();

        public void Init()
        {
            I = this;
        }

        public void Close()
        {
            I = null;
        }

        public void Enqueue(PacketBase packet)
        {
            long entityId = GetEntityId(packet);

            if (!packetQueues.ContainsKey(entityId))
            {
                packetQueues[entityId] = new LinkedList<PacketBase>();
                entityIds.Add(entityId);
            }

            string propertyName = GetPropertyName(packet);
            RemoveStalePackets(packetQueues[entityId], propertyName);

            packetQueues[entityId].AddLast(packet);
        }

        private void RemoveStalePackets(LinkedList<PacketBase> list, string propertyName)
        {
            var currentNode = list.First;

            while (currentNode != null)
            {
                var nextNode = currentNode.Next;

                if (GetPropertyName(currentNode.Value) == propertyName)
                {
                    list.Remove(currentNode);
                }

                currentNode = nextNode;
            }
        }

        public IEnumerable<long> GetEntitiesWithPackets()
        {
            foreach (var entry in packetQueues)
            {
                if (entry.Value.Count > 0)
                    yield return entry.Key;
            }
        }

        public PacketBase PeekNextPacket(long entityID)
        {
            if (packetQueues.ContainsKey(entityID) && packetQueues[entityID].Count > 0)
            {
                return packetQueues[entityID].First.Value;
            }

            return null;
        }

        public void DequeuePacket(long entityID)
        {
            if (packetQueues.ContainsKey(entityID) && packetQueues[entityID].Count > 0)
            {
                packetQueues[entityID].RemoveFirst();
            }

            if (HasNoPackets(entityID))
            {
                entityIds.Remove(entityID);
            }
        }

        private bool HasNoPackets(long entityID)
        {
            return !packetQueues.ContainsKey(entityID) || packetQueues[entityID].Count == 0;
        }

        public bool HasPackets()
        {
            return entityIds.Count > 0;
        }

        private long GetEntityId(PacketBase packet)
        {
            if (packet is SyncPacket<float>)
                return ((SyncPacket<float>)packet).entityId;
            if (packet is SyncPacket<int>)
                return ((SyncPacket<int>)packet).entityId;
            if (packet is SyncPacket<bool>)
                return ((SyncPacket<bool>)packet).entityId;

            return long.MaxValue;
        }

        private string GetPropertyName(PacketBase packet)
        {
            if (packet is SyncPacket<float>)
                return ((SyncPacket<float>)packet).propertyName;
            if (packet is SyncPacket<int>)
                return ((SyncPacket<int>)packet).propertyName;
            if (packet is SyncPacket<bool>)
                return ((SyncPacket<bool>)packet).propertyName;

            return string.Empty;
        }
    }
}