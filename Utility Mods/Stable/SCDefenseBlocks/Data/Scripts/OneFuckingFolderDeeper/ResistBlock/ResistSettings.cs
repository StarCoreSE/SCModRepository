using ProtoBuf;
using Starcore.ResistBlock;

namespace Starcore.ResistBlock
{
    [ProtoContract]
    internal class ResistSettings
    {
        public ResistSettings() { }

        public ResistSettings(ResistBlock owner)
        {
            Owner = owner;
            Owner.IsActiveSync.ValueChanged += (obj) => { IsActive = Owner.IsActiveSync.Value; };
            Owner.ResistanceStrengthSync.ValueChanged += (obj) => { ResistanceStrength = Owner.ResistanceStrengthSync.Value; };
        }

        [ProtoMember(1)]
        private bool isActive;

        [ProtoMember(2)]
        private float resistanceStrength;

        ResistBlock Owner;

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;
                if (Owner != null && Owner.IsActiveSync != null)
                    Owner.IsActiveSync.Value = value;
            }
        }

        public float ResistanceStrength
        {
            get { return resistanceStrength; }
            set
            {
                resistanceStrength = value;
                if (Owner != null && Owner.ResistanceStrengthSync != null)
                    Owner.ResistanceStrengthSync.Value = value;
            }
        }
    }
}