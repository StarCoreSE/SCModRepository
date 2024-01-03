using ProtoBuf;

namespace YourName.ModName.Data.Scripts.OneFuckingFolderDeeper.StructuralIntegrity
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
