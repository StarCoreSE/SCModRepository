using System;
using ProtoBuf;
using ShipPoints.HeartNetworking.Custom;

namespace ShipPoints.HeartNetworking
{
    [ProtoInclude(1, typeof(TrackingSyncPacket))]

    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract partial class PacketBase
    {
        /// <summary>
        /// Called whenever your packet is recieved.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);

        public static Type[] Types = {
            typeof(PacketBase),
            typeof(TrackingSyncPacket),
        };
    }
}