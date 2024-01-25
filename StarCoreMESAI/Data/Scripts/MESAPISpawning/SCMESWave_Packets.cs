using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarCoreMESAI.Data.Scripts.MESAPISpawning
{
    public class SCMESWave_Packets
    {

        [ProtoInclude(1000, typeof(ChatCommandPacket))]
        [ProtoInclude(1001, typeof(CounterUpdatePacket))]
        [ProtoContract]
        public class Packet
        {
            public Packet()
            {
            }
        }

        [ProtoContract]
        public class ChatCommandPacket : Packet
        {
            [ProtoMember(1)]
            public string CommandString;

            public ChatCommandPacket()
            {
            }

            public ChatCommandPacket(string CommandString)
            {
                this.CommandString = CommandString;
            }
        }

        [ProtoContract]
        public class CounterUpdatePacket : Packet
        {
            [ProtoMember(1)]
            public int CounterValue;

            public CounterUpdatePacket()
            {
            }

            public CounterUpdatePacket(int counterValue)
            {
                this.CounterValue = counterValue;
            }
        }

        public class SpawnGroupInfo
        {
            public int SpawnTime { get; set; }
            public Dictionary<string, int> Prefabs { get; private set; }

            public SpawnGroupInfo(int spawnTime, Dictionary<string, int> prefabs)
            {
                SpawnTime = spawnTime;
                Prefabs = prefabs;
            }
        }


    }
}
