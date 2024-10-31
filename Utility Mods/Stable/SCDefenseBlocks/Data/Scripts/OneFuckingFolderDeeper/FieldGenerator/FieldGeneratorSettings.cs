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
        private FieldGenerator Owner;

        public FieldGeneratorSettings() { }

        public FieldGeneratorSettings(FieldGenerator owner) {
            Owner = owner;
        }

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

        public bool SiegeMode {
            get { return siegeMode; }
            set { siegeMode = value; }
        }

        public float FieldPower {
            get { return fieldPower; }
            set { fieldPower = MathHelper.Clamp(value, MinFieldPower, MaxFieldPower); }
        }

        public float MaxFieldPower {
            get { return maxFieldPower; }
            set { maxFieldPower = value; }
        }

        public float MinFieldPower {
            get { return minFieldPower; }
            set { minFieldPower = value; }
        }

        public bool SiegeCooldownActive {
            get { return siegeCooldownActive; }
            set { siegeCooldownActive = value; }
        }

        public int SiegeElapsedTime {
            get { return siegeElapsedTime; }
            set { siegeElapsedTime = value; }
        }

        public int SiegeCooldownTime {
            get { return siegeCooldownTime; }
            set { siegeCooldownTime = value; }
        }

        public void CopyFrom(FieldGeneratorSettings other) {
            siegeMode = other.siegeMode;
            fieldPower = other.fieldPower;
            maxFieldPower = other.maxFieldPower;
            minFieldPower = other.minFieldPower;
            siegeCooldownActive = other.siegeCooldownActive;
            siegeElapsedTime = other.siegeElapsedTime;
            siegeCooldownTime = other.siegeCooldownTime;
        }
    }
}
