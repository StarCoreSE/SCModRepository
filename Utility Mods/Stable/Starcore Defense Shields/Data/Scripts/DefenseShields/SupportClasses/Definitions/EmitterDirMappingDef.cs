using System.Collections.Generic;
using VRageMath;

namespace DefenseShields.Support
{
    public class EmitterDefinition
    {
        public readonly Dictionary<Vector3, MappingDefinition> Def = new Dictionary<Vector3, MappingDefinition>
        {
            [Vector3.Forward] = new MappingDefinition { Primary = new Emitters(), Secondary = new Emitters(), Assigned = new Emitters(), Max = float.MinValue},
            [Vector3.Backward] = new MappingDefinition { Primary = new Emitters(), Secondary = new Emitters(), Assigned = new Emitters(), Max = float.MinValue },
            [Vector3.Left] = new MappingDefinition { Primary = new Emitters(), Secondary = new Emitters(), Assigned = new Emitters(), Max = float.MinValue },
            [Vector3.Right] = new MappingDefinition { Primary = new Emitters(), Secondary = new Emitters(), Assigned = new Emitters(), Max = float.MinValue },
            [Vector3.Up] = new MappingDefinition { Primary = new Emitters(), Secondary = new Emitters(), Assigned = new Emitters(), Max = float.MinValue },
            [Vector3.Down] = new MappingDefinition { Primary = new Emitters(), Secondary = new Emitters(), Assigned = new Emitters(), Max = float.MinValue }
        };


        public MappingDefinition Get(Vector3 subtype)
        {
            return Def.GetValueOrDefault(subtype);
        }
    }

    public class MappingDefinition
    {
        public Emitters Primary;
        public Emitters Secondary;
        public Emitters Assigned;
        public float Max;
    }
}
