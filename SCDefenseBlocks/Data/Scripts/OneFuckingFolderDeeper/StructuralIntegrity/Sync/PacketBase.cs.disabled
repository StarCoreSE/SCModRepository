using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.StructuralIntegrity.Sync
{
    [ProtoInclude(2, typeof(PacketBlockSettings))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract class PacketBase
    {
        [ProtoMember(1)]
        public readonly ulong SenderId;

        protected Networking Networking => SI_Utility.Instance.Networking;

        public PacketBase()
        {
            SenderId = MyAPIGateway.Multiplayer.MyId;
        }

        /// <summary>
        /// Called when this packet is received on this machine.
        /// </summary>
        /// <param name="relay">Set to true to relay this packet to clients, only works server side.</param>
        public abstract void Received(ref bool relay);
    }
}
