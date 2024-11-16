using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Starcore.FieldGenerator {
    [ProtoContract]
    public class FieldGeneratorSettings {
        public FieldGeneratorSettings() { }

        public FieldGeneratorSettings(FieldGenerator owner) {
            Owner = owner;
            // Set up all sync handlers
            Owner.SiegeModeSync.ValueChanged += (obj) => siegeMode = obj.Value;
            Owner.FieldPowerSync.ValueChanged += (obj) => fieldPower = obj.Value;
            Owner.MaxFieldPowerSync.ValueChanged += (obj) => maxFieldPower = obj.Value;
            Owner.MinFieldPowerSync.ValueChanged += (obj) => minFieldPower = obj.Value;
            Owner.SiegeCooldownActiveSync.ValueChanged += (obj) => siegeCooldownActive = obj.Value;
            Owner.SiegeElapsedTimeSync.ValueChanged += (obj) => siegeElapsedTime = obj.Value;
            Owner.SiegeCooldownTimeSync.ValueChanged += (obj) => siegeCooldownTime = obj.Value;
            Owner.StabilitySync.ValueChanged += (obj) => stability = obj.Value;
        }

        [ProtoIgnore]
        private FieldGenerator Owner;

        [ProtoMember(1)]
        private bool siegeMode;
        [ProtoMember(2)]
        private float fieldPower;
        [ProtoMember(3)]
        private float maxFieldPower;
        [ProtoMember(4)]
        private float minFieldPower;
        [ProtoMember(5)]
        private bool siegeCooldownActive;
        [ProtoMember(6)]
        private int siegeElapsedTime;
        [ProtoMember(7)]
        private int siegeCooldownTime;
        [ProtoMember(8)]
        private float stability;

        public bool SiegeMode {
            get { return siegeMode; }
            set
            {
                siegeMode = value;
                if (Owner != null) Owner.SiegeModeSync.Value = value;
            }
        }

        public float FieldPower {
            get { return fieldPower; }
            set {
                fieldPower = MathHelper.Clamp(value, MinFieldPower, MaxFieldPower);
                if (Owner != null)
                    Owner.FieldPowerSync.Value = fieldPower;
            }
        }

        public float MaxFieldPower {
            get { return maxFieldPower; }
            set {
                // Ensure MaxFieldPower doesn't exceed what's possible with max modules
                maxFieldPower = MathHelper.Clamp(value, 0, Config.PerModuleAmount * Config.MaxModuleCount);
                if (Owner != null)
                    Owner.MaxFieldPowerSync.Value = maxFieldPower;
            }
        }

        public float MinFieldPower {
            get { return minFieldPower; }
            set {
                minFieldPower = MathHelper.Clamp(value, 0, MaxFieldPower);
                if (Owner != null)
                    Owner.MinFieldPowerSync.Value = minFieldPower;
            }
        }

        public bool SiegeCooldownActive {
            get { return siegeCooldownActive; }
            set
            {
                siegeCooldownActive = value;
                if (Owner != null) Owner.SiegeCooldownActiveSync.Value = value;
            }
        }

        public int SiegeElapsedTime {
            get { return siegeElapsedTime; }
            set
            {
                siegeElapsedTime = value;
                if (Owner != null) Owner.SiegeElapsedTimeSync.Value = value;
            }
        }

        public int SiegeCooldownTime {
            get { return siegeCooldownTime; }
            set
            {
                siegeCooldownTime = value;
                if (Owner != null) Owner.SiegeCooldownTimeSync.Value = value;
            }
        }

        public float Stability {
            get { return stability; }
            set
            {
                stability = MathHelper.Clamp(value, 0, 100);
                if (Owner != null) Owner.StabilitySync.Value = stability;
            }
        }

        public void CopyFrom(FieldGeneratorSettings other) {
            SiegeMode = other.siegeMode;
            FieldPower = other.fieldPower;
            MaxFieldPower = other.maxFieldPower;
            MinFieldPower = other.minFieldPower;
            SiegeCooldownActive = other.siegeCooldownActive;
            SiegeElapsedTime = other.siegeElapsedTime;
            SiegeCooldownTime = other.siegeCooldownTime;
            Stability = other.stability;
        }
    }
}
