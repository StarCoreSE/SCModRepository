using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.DynamicResistence.Sync
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class PacketBlockSettings : PacketBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public DynaResistBlockSettings Settings;

        public PacketBlockSettings() { } // Empty constructor required for deserialization

        public void Send(long entityId, DynaResistBlockSettings settings)
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

            var logic = block.GameLogic?.GetAs<DynamicResistLogic>();

            if(logic == null)
                return;

            logic.Settings.FieldPower = this.Settings.FieldPower;
            logic.Settings.Modifier = this.Settings.Modifier;

            relay = true;
        }
    }
}
