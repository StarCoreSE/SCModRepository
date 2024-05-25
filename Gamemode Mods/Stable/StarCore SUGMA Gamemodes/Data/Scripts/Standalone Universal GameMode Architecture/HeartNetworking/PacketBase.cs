using System;
using ProtoBuf;

namespace SC.SUGMA.HeartNetworking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract class PacketBase
    {
        public static readonly Type[] PacketTypes =
        {
            typeof(PacketBase),
        };

        /// <summary>
        ///     Called whenever your packet is received.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);
    }
}