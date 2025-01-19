using System;
using ProtoBuf;
using StarCore.RepairModule.Networking.Custom;

namespace StarCore.RepairModule.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoInclude(1, typeof(IgnoreArmorPacket))]
    [ProtoInclude(2, typeof(PriorityOnlyPacket))]
    [ProtoInclude(3, typeof(SubsystemPriorityPacket))]
    [ProtoInclude(4, typeof(SyncRequestPacket))]
    public abstract class PacketBase
    {
        public static readonly Type[] PacketTypes =
        {
            typeof(PacketBase),
            typeof(IgnoreArmorPacket),
            typeof(PriorityOnlyPacket),
            typeof(SubsystemPriorityPacket),
            typeof(SyncRequestPacket),
        };

        /// <summary>
        ///     Called whenever your packet is received.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}