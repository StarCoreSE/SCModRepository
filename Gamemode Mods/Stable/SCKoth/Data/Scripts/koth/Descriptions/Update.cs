using ProtoBuf;
using System.Collections.Generic;

namespace KingOfTheHill.Descriptions
{
    [ProtoContract]
    public class Update
    {
        [ProtoMember(1)]
        public List<ZoneDescription> Zones { get; set; } = new List<ZoneDescription>();
    }
}
