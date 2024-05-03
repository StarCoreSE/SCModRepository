using ProtoBuf;
using static Math0424.Networking.MyEasyNetworkManager;

namespace ShipPoints.Data.Scripts.ShipPoints.Networking
{
    [ProtoContract]
    internal class BasicPacket : IPacket
    {
        [ProtoMember(1)] private readonly int _id;

        public BasicPacket(int id)
        {
            this._id = id;
        }

        public int GetId()
        {
            return _id;
        }
    }
}