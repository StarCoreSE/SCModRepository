using System;
using ProtoBuf;
using Starcore.FieldGenerator.Networking.Custom;

namespace Starcore.FieldGenerator.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoInclude(1, typeof(SyncPacket<>))]
    public abstract class PacketBase
    {
        public static readonly Type[] PacketTypes =
        {
            typeof(PacketBase),
            typeof(SyncPacket<>),
           // typeof(SyncRequestPacket),
        };

        /// <summary>
        ///     Called whenever your packet is received.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}