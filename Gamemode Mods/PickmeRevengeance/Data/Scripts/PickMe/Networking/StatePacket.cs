using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Utils;

namespace PickMe.Networking
{
    [ProtoContract]
    public class StatePacket: PacketBase
    {
        // tag numbers in this class won't collide with tag numbers from the base class
        [ProtoMember(1)]
        public string Text;

        public StatePacket() { } // Empty constructor required for deserialization

        public StatePacket(string text)
        {
            Text = text;
        }

        public override bool Received()
        {
            MyAPIGateway.Utilities.ShowNotification(Text, 5000);
            if (MyAPIGateway.Session.IsServer) Text = Session.Instance.stateControl.Check(this);
            else Text = Session.Instance.stateControl.CheckClient(this);
            return true; // relay packet to other clients (only works if server receives it)
        }
    }
}
