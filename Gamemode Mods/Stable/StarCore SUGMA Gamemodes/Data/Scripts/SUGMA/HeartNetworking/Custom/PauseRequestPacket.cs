using ProtoBuf;
using Sandbox.ModAPI;
using SC.SUGMA.Utilities;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class PauseRequestPacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            SUtils.Pause();
            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(this);
        }
    }
}
