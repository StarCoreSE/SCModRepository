using System;
using ProtoBuf;
using Starcore.FieldGenerator.Networking.Custom;

namespace Starcore.FieldGenerator.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoInclude(1, typeof(BoolSyncPacket))]
    [ProtoInclude(2, typeof(IntSyncPacket))]
    [ProtoInclude(3, typeof(FloatSyncPacket))]
    //[ProtoInclude(4, typeof(SyncRequestPacket))]
    public abstract class PacketBase
    {
        public static readonly Type[] PacketTypes =
        {
            typeof(PacketBase),
            typeof(BoolSyncPacket),
            typeof(IntSyncPacket),
            typeof(FloatSyncPacket),
           // typeof(SyncRequestPacket),
        };

        /// <summary>
        ///     Called whenever your packet is received.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}