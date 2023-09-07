using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingOfTheHill.Descriptions
{
    [ProtoContract]
    public class PlanetDescription
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public List<ScoreDescription> Scores { get; set; } = new List<ScoreDescription>();
    }
}
