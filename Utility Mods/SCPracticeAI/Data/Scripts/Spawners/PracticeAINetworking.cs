using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invalid.SCPracticeAI
{
    internal class PracticeAINetworking
    {
    }

    [ProtoInclude(1000, typeof(PrefabSpawnPacket))]
    [ProtoContract]
    public class Packet
    {
        public Packet()
        {

        }
    }

    [ProtoContract]
    public class PrefabSpawnPacket : Packet
    {
        [ProtoMember(1)]
        public string PrefabName;

        [ProtoMember(2)]
        public int PrefabAmount;

        [ProtoMember(3)]  // New member for faction name
        public string FactionName;

        // Add a parameterless constructor required by ProtoBuf
        public PrefabSpawnPacket()
        {

        }

        public PrefabSpawnPacket(string prefabName, int prefabAmount, string factionName)
        {
            PrefabName = prefabName;
            PrefabAmount = prefabAmount;
            FactionName = factionName; // Set the faction name
        }
    }

}
