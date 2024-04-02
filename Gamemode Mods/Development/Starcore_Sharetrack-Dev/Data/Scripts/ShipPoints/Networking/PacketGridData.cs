using klime.PointCheck;
using ProtoBuf;
using static Math0424.Networking.MyEasyNetworkManager;

namespace Math0424.ShipPoints
{
    [ProtoContract]
    public class PacketGridData : IPacket
    {
        [ProtoMember(1)] public byte value;
        [ProtoMember(2)] public long id;
        [ProtoMember(3)] public ShipTracker tracked;
        public PacketGridData()
        {
            tracked = new ShipTracker();
        }
        public int GetId() { return 1; }
    }
}
