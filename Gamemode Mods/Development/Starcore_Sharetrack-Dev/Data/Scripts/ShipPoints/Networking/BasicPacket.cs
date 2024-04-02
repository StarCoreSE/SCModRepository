using ProtoBuf;
using static Math0424.Networking.MyEasyNetworkManager;

namespace ShipPoints.Data.Scripts.ShipPoints.Networking
{
    [ProtoContract]
    class BasicPacket : IPacket
    {
        [ProtoMember(1)]
        int id;

        public BasicPacket(int id)
        {
            this.id = id;
        }
        public int GetId()
        {
            return id;
        }
    }
}
