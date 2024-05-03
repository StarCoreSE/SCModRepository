using ProtoBuf;
using static Math0424.Networking.MyEasyNetworkManager;

namespace Math0424.ShipPoints
{
    [ProtoContract]
    public class SyncRequestPacket : IPacket
    {
        public int GetId()
        {
            return 45;
        }
    }
}