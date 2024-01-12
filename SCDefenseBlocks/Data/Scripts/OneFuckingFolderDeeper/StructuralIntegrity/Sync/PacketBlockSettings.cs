using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.StructuralIntegrity.Sync
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class PacketBlockSettings : PacketBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public SI_Settings Settings;

        public PacketBlockSettings() { } // Empty constructor required for deserialization

        public void Send(long entityId, SI_Settings settings)
        {
            EntityId = entityId;
            Settings = settings;

            if (MyAPIGateway.Multiplayer.IsServer)
                Networking.RelayToClients(this);
            else
                Networking.SendToServer(this);
        }

        public override void Received(ref bool relay)
        {
            var block = MyAPIGateway.Entities.GetEntityById(EntityId) as IMyCollector;

            if (block == null)
                return;

            var logic = block.GameLogic?.GetAs<SI_Core>();

            if (logic == null)
                return;

            logic.Settings.FieldPower = Settings.FieldPower;
            logic.Settings.GridModifier = Settings.GridModifier;

            relay = true;
        }
    }
}
