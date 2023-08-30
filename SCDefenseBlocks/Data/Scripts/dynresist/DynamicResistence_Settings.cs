using ProtoBuf;

namespace StarCore.DynamicResistence
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class DynaResistBlockSettings
    {
        [ProtoMember(1)]
        public float Polarization;

        [ProtoMember(2)]
        public float Modifier;
    }
}
