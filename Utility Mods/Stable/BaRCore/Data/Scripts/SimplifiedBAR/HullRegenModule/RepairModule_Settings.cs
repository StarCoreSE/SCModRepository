using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Network;
using VRage.Sync;
using VRage.Utils;

namespace StarCore.RepairModule
{
    [ProtoContract]
     public class RepairModuleSettings
     {
        RepairModuleSettings() { }
        public RepairModuleSettings(RepairModule owner)
        {
            Owner = owner;
            Owner.ignoreArmorSync.ValueChanged += (obj) => ignoreArmor = obj.Value;
            Owner.priorityOnlySync.ValueChanged += (obj) => priorityOnly = obj.Value;
            Owner.subsystemPrioritySync.ValueChanged += (obj) => subsystemPriority = obj.Value;
        }

        [ProtoMember(1)] private bool ignoreArmor;
        [ProtoMember(2)] private bool priorityOnly;
        [ProtoMember(3)] private long subsystemPriority;
        RepairModule Owner;

        public bool IgnoreArmor
        {
            get
            {
                return ignoreArmor;
            }
            set
            {
                ignoreArmor = value;
                Owner.ignoreArmorSync.Value = value;
            }
        }

        public bool PriorityOnly
        {
            get
            {
                return priorityOnly;
            }
            set
            {
                priorityOnly = value;
                Owner.priorityOnlySync.Value = value;
            }
        }

        public long SubsystemPriority
        {
            get
            {
                return subsystemPriority;
            }
            set
            {
                subsystemPriority = value;
                Owner.subsystemPrioritySync.Value = value;
            }
        }
     }
}
