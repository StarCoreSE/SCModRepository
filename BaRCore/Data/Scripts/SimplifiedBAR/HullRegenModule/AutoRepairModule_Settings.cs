using ProtoBuf;

namespace StarCore.AutoRepairModule
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class AutoRepairModuleSettings
    {
        [ProtoMember(1)]
        public int SubsystemPriority;

        [ProtoMember(2)]
        public bool ExclusiveMode;
        
        [ProtoMember(3)]
        public bool IgnoreArmor;

    }
}
