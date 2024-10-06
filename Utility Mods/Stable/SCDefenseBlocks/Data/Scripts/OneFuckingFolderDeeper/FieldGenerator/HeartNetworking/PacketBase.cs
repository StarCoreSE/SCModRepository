using System;
using ProtoBuf;
using Starcore.FieldGenerator.Networking.Custom;

namespace Starcore.FieldGenerator.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoInclude(1, typeof(SyncPacket<float>))]
    [ProtoInclude(2, typeof(SyncPacket<int>))]
    [ProtoInclude(3, typeof(SyncPacket<bool>))]
    public abstract class PacketBase
    {
        public static readonly Type[] PacketTypes =
        {
            typeof(PacketBase),
            typeof(SyncPacket<float>),
            typeof(SyncPacket<int>),
            typeof(SyncPacket<bool>),
           // typeof(SyncRequestPacket),
        };

        /// <summary>
        ///     Called whenever your packet is received.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}