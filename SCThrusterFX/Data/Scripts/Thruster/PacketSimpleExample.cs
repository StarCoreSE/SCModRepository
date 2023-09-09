using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Utils;

namespace MWI.Thruster
{
    // An example packet extending another packet.
    // Note that it must be ProtoIncluded in PacketBase for it to work.
    [ProtoContract]
    public class PacketSimpleExample : PacketBase
    {
        // tag numbers in this class won't collide with tag numbers from the base class
        [ProtoMember(1)]
        public long blockID;

        [ProtoMember(2)]
        public float thrustValue;

        public PacketSimpleExample() { } // Empty constructor required for deserialization

        public PacketSimpleExample(long serverEntity,float serverThrust)
        {
            blockID = serverEntity;
            thrustValue = serverThrust;
        }

        public override bool Received()
        {
            var msg = $"EntitiesRecieved = '{blockID}';";
            ThrusterSession.Instance.SyncedValue(blockID,thrustValue);
            //DeathFxSession.Instance.floatyEntityID.Add(partEntity);
            //DeathFxSession.Instance.particleID.Add(particles);
            //MyLog.Default.WriteLineAndConsole(msg);
            //MyAPIGateway.Utilities.ShowNotification(msg, Number);

            return true; // relay packet to other clients (only works if server receives it)
        }
    }
}
