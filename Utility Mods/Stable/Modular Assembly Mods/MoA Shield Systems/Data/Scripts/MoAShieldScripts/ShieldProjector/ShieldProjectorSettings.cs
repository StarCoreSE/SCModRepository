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

        [ProtoMember(1)]
        private bool isFoaming;
        [ProtoMember(2)]
        private float foamRadius;
        [ProtoMember(3)] // Ensure this property is serialized by assigning it a unique ProtoMember tag.
        private float squareSize = 5.0f; // Default value of 5 meters

        ShieldProjector Owner;

        public bool IsFoaming
        {
            get { return isFoaming; }
            set
            {
                isFoaming = value;
                // This can be uncommented if you handle sync values.
                // Owner.IsFoamingSync.Value = value;
            }
        }

        public float FoamRadius
        {
            get { return foamRadius; }
            set
            {
                foamRadius = value;
                // This can be uncommented if you handle sync values.
                // Owner.FoamRadiusSync.Value = value;
            }
        }

        public float SquareSize
        {
            get { return squareSize; }
            set { squareSize = value; }
        }
    }
}
