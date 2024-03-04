using ProtoBuf;

namespace StarCore.StructuralIntegrity
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class SI_Settings
    {
        [ProtoMember(1)]
        public float FieldPower;

        [ProtoMember(2)]
        public float GridModifier;
    }
}
