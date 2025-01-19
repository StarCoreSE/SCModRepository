using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SC.SUGMA.Utilities;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class ShieldFillRequestPacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            SUtils.ShieldCharge();
            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(this);
        }
    }
}