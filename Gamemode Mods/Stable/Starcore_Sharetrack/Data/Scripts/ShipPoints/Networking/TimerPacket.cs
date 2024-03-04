using ProtoBuf;
using static Math0424.Networking.MyEasyNetworkManager;

namespace ShipPoints.Data.Scripts.ShipPoints.Networking
{
    [ProtoContract]
    class TimerPacket : ITPacket
    {
        [ProtoMember(1)]
        public int ServerTime;
        public TimerPacket() { } // Empty constructor required for deserialization

        public TimerPacket(int ServerTime)
        {
            this.ServerTime = ServerTime;
        }
        public int GetTime()
        {
            return ServerTime;
        }
    }
}
