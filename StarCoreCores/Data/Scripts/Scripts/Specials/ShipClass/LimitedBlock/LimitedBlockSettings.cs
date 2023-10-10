using System.Xml.Serialization;
using ProtoBuf;
using VRage.Game;

namespace MIG.SpecCores
{
    [ProtoContract]
    public class LimitedBlockSettings
    {
        [XmlAttribute("AutoEnable")]
        [ProtoMember(1)] 
        public bool AutoEnable = true;

        [XmlAttribute("SmartTurnOn")]
        [ProtoMember(2)] 
        public bool SmartTurnOn = true;
        
        [XmlIgnore()] //State
        [ProtoMember(3)]
        public bool WasDisabledBySpecCore = true;

        [XmlIgnore()] //State
        [ProtoMember(4)] 
        public long BeforeDamageOwnerId;
        
        [XmlIgnore()] //State
        [ProtoMember(5)] 
        public MyOwnershipShareModeEnum BeforeDamageShareMode;

        [XmlAttribute("AutoEnableShowGUI")]
        [ProtoMember(6)] 
        public bool AutoEnableShowGUI = true;
        
        [XmlAttribute("SmartTurnOnShowGUI")]
        [ProtoMember(7)] 
        public bool SmartTurnOnShowGUI = true;

        public LimitedBlockSettings()
        {
            
        }

        public LimitedBlockSettings(LimitedBlockSettings other)
        {
            AutoEnable = other.AutoEnable;
            SmartTurnOn = other.SmartTurnOn;
            WasDisabledBySpecCore = other.WasDisabledBySpecCore;
            BeforeDamageOwnerId = other.BeforeDamageOwnerId;
            BeforeDamageShareMode = other.BeforeDamageShareMode;
            AutoEnableShowGUI = other.AutoEnableShowGUI;
            SmartTurnOnShowGUI = other.SmartTurnOnShowGUI;
        }
    }
}