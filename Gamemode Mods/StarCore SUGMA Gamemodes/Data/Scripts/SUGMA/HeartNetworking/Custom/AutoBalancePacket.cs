using ProtoBuf;
using SC.SUGMA.Utilities;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class AutoBalancePacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            TeamBalancer.PerformBalancing();
        }
    }
}
