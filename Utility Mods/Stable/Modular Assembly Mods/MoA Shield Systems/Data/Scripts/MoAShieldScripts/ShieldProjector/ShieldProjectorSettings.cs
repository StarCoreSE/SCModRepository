using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Network;
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
           // Owner.IsFoamingSync.ValueChanged += (obj) => isFoaming = obj.Value;
           // Owner.FoamRadiusSync.ValueChanged += (obj) => foamRadius = obj.Value;
        }

        [ProtoMember(1)] private bool isFoaming;
        [ProtoMember(2)] private float foamRadius;
        ShieldProjector Owner;

        public bool IsFoaming
        {
            get
            {
                return isFoaming;
            }
            set
            {
                isFoaming = value;
              //  Owner.IsFoamingSync.Value = value;
            }
        }

        public float FoamRadius
        {
            get
            {
                return foamRadius;
            }
            set
            {
                foamRadius = value;
               // Owner.FoamRadiusSync.Value = value;
            }
        }
    }
}
