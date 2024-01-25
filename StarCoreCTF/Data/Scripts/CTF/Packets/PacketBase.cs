using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Klime.CTF.CTF;

namespace Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF
{
    [ProtoContract]
    public class PacketBase
    {
        [ProtoMember(200)]
        public PacketOp packet_op;

        [ProtoMember(201)]
        public List<Flag> all_flags_packet = new List<Flag>();

        [ProtoMember(202)]
        public GameState gamestate_packet;

        public PacketBase()
        {

        }
    }
}
