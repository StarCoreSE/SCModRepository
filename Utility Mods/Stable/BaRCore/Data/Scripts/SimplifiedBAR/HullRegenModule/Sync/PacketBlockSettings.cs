using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.AutoRepairModule.Sync
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class PacketBlockSettings : PacketBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public AutoRepairModuleSettings Settings;

        public PacketBlockSettings() { } // Empty constructor required for deserialization

        public void Send(long entityId, AutoRepairModuleSettings settings)
        {
            EntityId = entityId;
            Settings = settings;

            if(MyAPIGateway.Multiplayer.IsServer)
                Networking.RelayToClients(this);
            else
                Networking.SendToServer(this);
        }

        public override void Received(ref bool relay)
        {
            var block = MyAPIGateway.Entities.GetEntityById(this.EntityId) as IMyCollector;

            if(block == null)
                return;

            var logic = block.GameLogic?.GetAs<AutoRepairModule>();

            if(logic == null)
                return;

            logic.Settings.SubsystemPriority = this.Settings.SubsystemPriority;
            logic.Settings.RepairPositionalList = this.Settings.RepairPositionalList;
            logic.Settings.PriorityPositionalList = this.Settings.PriorityPositionalList;

            relay = true;
        }
    }
}
