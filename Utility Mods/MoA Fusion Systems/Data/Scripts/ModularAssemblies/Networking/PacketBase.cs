using ProtoBuf;

namespace Epstein_Fusion_DS.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoInclude(101, typeof(BlockPacket))]
    public abstract class PacketBase
    {
        /// <summary>
        /// Called whenever your packet is recieved.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}
