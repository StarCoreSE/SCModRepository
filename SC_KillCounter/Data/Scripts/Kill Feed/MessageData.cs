using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KillFeed
{
    [ProtoContract]
    public class MessageData
    {
        [ProtoMember(1)]
        public long Attacker { get; set; }

        [ProtoMember(2)]
        public long Victim { get; set; }

        public MessageData()
        {
            Attacker = 0;
            Victim = 0;
        }

        public MessageData(long attacker, long victim)
        {
            Attacker = attacker;
            Victim = victim;
        }
    }
}
