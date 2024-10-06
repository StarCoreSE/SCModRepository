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

        #region Init
        public void Init()
        {
            I = this;
        }

        public void Close()
        {
            I = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Checks for Entities with Open Queues
        /// </summary>
        /// <returns>Bool</returns>
        public bool QueuesWithPackets()
        {
            return entityIds.Count > 0;
        }

        /// <summary>
        /// Adds Packet to Per-Entity Queue
        /// </summary>
        /// <param name="packet"></param>
        public void EnqueuePacket(PacketBase packet)
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

        /// <summary>
        /// Removes the first packet from an Entitys Queue. If its the last packet in Queue, closes the Queue for that Entity.
        /// </summary>
        /// <param name="entityID"></param>
        public void DequeuePacket(long entityID)
        {
            if (packetQueues.ContainsKey(entityID) && packetQueues[entityID].Count > 0)
            {
                packetQueues[entityID].RemoveFirst();
            }

            if (!EntityHasPackets(entityID))
            {
                entityIds.Remove(entityID);
            }
        }

        /// <summary>
        /// Gets Entities with a Queue
        /// </summary>
        /// <returns>IEnumerable of EntityIDs with Queues</returns>
        public IEnumerable<long> EntitiesWithQueue()
        {
            foreach (var entry in packetQueues)
            {
                if (entry.Value.Count > 0)
                    yield return entry.Key;
            }
        }

        /// <summary>
        /// Retrieves First Packet of an Entities Queue
        /// </summary>
        /// <param name="entityID"></param>
        /// <returns>PacketBase Packet Type</returns>
        public PacketBase FirstInQueue(long entityID)
        {
            if (packetQueues.ContainsKey(entityID) && packetQueues[entityID].Count > 0)
            {
                return packetQueues[entityID].First.Value;
            }

            return null;
        }
        #endregion

        #region Private Methods
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

        private bool EntityHasPackets(long entityID)
        {
            return packetQueues.ContainsKey(entityID) || packetQueues[entityID].Count != 0;
        }

        private long GetEntityId(PacketBase packet)
        {
            if (packet.GetType() == typeof(FloatSyncPacket))
                return ((FloatSyncPacket)packet).entityId;
            if (packet.GetType() == typeof(IntSyncPacket))
                return ((IntSyncPacket)packet).entityId;
            if (packet.GetType() == typeof(BoolSyncPacket))
                return ((BoolSyncPacket)packet).entityId;

            return 0;
        }

        private string GetPropertyName(PacketBase packet)
        {
            if (packet.GetType() == typeof(FloatSyncPacket))
                return ((FloatSyncPacket)packet).propertyName;
            if (packet.GetType() == typeof(IntSyncPacket))
                return ((IntSyncPacket)packet).propertyName;
            if (packet.GetType() == typeof(BoolSyncPacket))
                return ((BoolSyncPacket)packet).propertyName;

            return string.Empty;
        }
        #endregion
    }
}