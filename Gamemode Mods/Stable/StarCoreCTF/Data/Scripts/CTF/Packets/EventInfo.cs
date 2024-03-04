using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Klime.CTF.CTF;

namespace Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF.Packets
{
    [ProtoContract]
    public class EventInfo
    {
        [ProtoMember(600)]
        public string info;
        [ProtoMember(601)]
        public InfoType infotype;

        public EventInfo()
        {

        }
    }
}
