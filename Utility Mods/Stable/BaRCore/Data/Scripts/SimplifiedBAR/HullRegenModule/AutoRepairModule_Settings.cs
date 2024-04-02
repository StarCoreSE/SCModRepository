using ProtoBuf;
using VRageMath;

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

        [ProtoMember(4)]
        public Vector3I[] RepairPositionalList; 
        
        [ProtoMember(5)]
        public Vector3I[] PriorityPositionalList;

    }
}
