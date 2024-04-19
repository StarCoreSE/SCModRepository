using ProtoBuf;
using System;
using VRage.Sync;
using VRage.Utils;

namespace Invalid.ShieldProjector
{
    [ProtoContract]
    internal class ShieldProjectorSettings
    {
        ShieldProjectorSettings() { }
        public ShieldProjectorSettings(ShieldProjector owner)
        {
            Owner = owner;
        }

        [ProtoMember(1)] // Ensure this property is serialized by assigning it a unique ProtoMember tag.
        private float squareSize = 5.0f; // Default value of 5 meters

        ShieldProjector Owner;


        public float SquareSize
        {
            get { return squareSize; }
            set { squareSize = value; }
        }
    }
}
