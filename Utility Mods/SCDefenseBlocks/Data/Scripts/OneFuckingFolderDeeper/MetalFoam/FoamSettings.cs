using Invalid.MetalFoam;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Network;
using VRage.Sync;
using VRage.Utils;

namespace Jnick_SCModRepository.SCDefenseBlocks.Data.Scripts.OneFuckingFolderDeeper.MetalFoam
{
    [ProtoContract]
    internal class FoamSettings
    {
        FoamSettings() { }
        public FoamSettings(MetalFoamGenerator owner)
        {
            Owner = owner;
            Owner.IsFoamingSync.ValueChanged += (obj) => isFoaming = obj.Value;
            Owner.FoamRadiusSync.ValueChanged += (obj) => foamRadius = obj.Value;
        }

        [ProtoMember(1)] private bool isFoaming;
        [ProtoMember(2)] private float foamRadius;
        MetalFoamGenerator Owner;

        public bool IsFoaming
        {
            get
            {
                return isFoaming;
            }
            set
            {
                isFoaming = value;
                Owner.IsFoamingSync.Value = value;
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
                Owner.FoamRadiusSync.Value = value;
            }
        }
    }
}
