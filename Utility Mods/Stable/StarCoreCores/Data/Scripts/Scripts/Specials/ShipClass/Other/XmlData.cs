using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using Digi;
using MIG.Shared.SE;
using ProtoBuf;
using VRage.Game;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    #region Enums

    [Flags]
    public enum TypeOfGridGroup
    {
        None = 0, 
        Large = 1, 
        Small = 2, 
        Static = 4
    }
    
    public enum GridConnectionType
    {
        None,
        Logical,
        Physical,
        NoContactDamage,
        Mechanical,
        Electrical
    }
    
    [Flags]
    public enum GUIClasses
    {
        Basic = 1,
        EnergyAndProduction = 2,
        RotorsAndPistons = 4,
        ShipControl = 8,
        Tools = 16,
        Weapons = 32,
        Other = 64,
        Strange = 128,
        
        All = Basic | EnergyAndProduction | RotorsAndPistons | ShipControl | Tools | Weapons | Other | Strange
    }
    
    public enum EnableDisableBehaviorType
    {
        None,
        SetEnabled,
        WeldToFunctional,
        WeldBy,
        WeldTo,
        SetArmed,
        SetInventoryMass,
        SetThrustMlt,
        SetThrustPowerConsumptionMlt,
        SetReactorPowerOutputMlt,
        SetGasGeneratorMlt,
        Destroy,
        SetDrillHarvestMlt,
        GrindToFunctional,
        GrindBy,
        GrindTo,
        CustomLogic
    }
    
    public enum ConsumeBehaviorType
    {
        None,
        Always,
        IsFunctional,
        Integrity,
        IsEnabled,
        IsWorking,
        IsSinkingResource,
        IsProducingResource,
        IsArmed,
        DrillHarvestMlt,
        InventoryExternalMass,
        ThrustMlt,
        ThrustPowerConsumptionMlt,
        ReactorPowerOutputMlt,
        IsBuiltBy,
        IsOwnedBy,
        CustomLogic
    }
    
    public enum PointBehavior
    {
        Property,
        SumLessOrEqual,
        LessOrEqual,
        MoreOrEqual,
        UpgradePoint
    }

    #endregion

    #region Minor

    
    public class Cache
    {
        [XmlAttribute("EnableBlockCache")]
        public bool EnableBlockCache;
    }

    public class Timers
    {
        [XmlAttribute("CheckLimitsInterval")]
        public int CheckLimitsInterval = 30;
        
        [XmlAttribute("CheckBlocksOnSubGridsInterval")]
        public int CheckBlocksOnSubGridsInterval = 60;
    }

    public class Tier
    {
        [XmlAttribute("Contains")]
        public string Contains = null;
        
        [XmlAttribute("EndsWith")]
        public string EndsWith = null;
        
        [XmlAttribute("Value")]
        public int Value = 0;
    }
    
    public class LimitPointFormat
    {
        private static Dictionary<string, LimitPointFormat> Data = new Dictionary<string, LimitPointFormat>()
        {
            {"DefaultLessOrEqual", new LimitPointFormat()
            {
                Format = "[Color=#FF00A000]{PointName}:{Current}<={Max} ({Total}){UnitName}[/Color]", 
                FormatPossibleOverlimiting ="[Color=#FFA00000]{PointName}:{Current}<={Max} ({Total}){UnitName}{SpecCores_Gui_PossibleOverlimit}[/Color]",
                FormatOverlimiting = "[Color=#FFA00000]{PointName}:{Current}&lt;={Max} ({Total}){UnitName}{SpecCores_Gui_Overlimit}[/Color]"
            }},
            {"DefaultSumLessOrEqual", new LimitPointFormat()
            {
                Format = "[Color=#FF00A000]{PointName}:{Current}<={Max} ({Total}){UnitName}[/Color]", 
                FormatPossibleOverlimiting ="[Color=#FFA00000]{PointName}:{Current}<={Max} ({Total}){UnitName}{SpecCores_Gui_PossibleOverlimit}[/Color]",
                FormatOverlimiting = "[Color=#FFA00000]{PointName}:{Current}<={Max} ({Total}){UnitName}{SpecCores_Gui_Overlimit}[/Color]"
            }},
            {"DefaultMoreOrEqual", new LimitPointFormat()
            {
                Format = "[Color=#FF00A000]{PointName}:{Current}>={Max} ({Total}){UnitName}[/Color]", 
                FormatPossibleOverlimiting ="[Color=#FFA00000]{PointName}:{Current}>={Max} ({Total}){UnitName}{SpecCores_Gui_PossibleOverlimit}[/Color]",
                FormatOverlimiting = "[Color=#FFA00000]{PointName}:{Current}>={Max} ({Total}){UnitName}{SpecCores_Gui_Overlimit}[/Color]"
            }},
            {"DefaultProperty", new LimitPointFormat()
            {
                Format = "[Color=#FFA000A0]{PointName}:{Max} {UnitName}[/Color]", 
                FormatPossibleOverlimiting ="",
                FormatOverlimiting = ""
            }},
            {"DefaultUpgradePoint", new LimitPointFormat()
            {
                Format = "[Color=#FF00A0A0]{PointName}:{Max} {UnitName}[/Color]", 
                FormatPossibleOverlimiting ="",
                FormatOverlimiting = ""
            }}
        };

        public static void Init(Dictionary<string, LimitPointFormat> data)
        {
            foreach (var kv in data)
            {
                Data[kv.Key] = kv.Value;
            }
        }
        
        public static LimitPointFormat GetDefault(LimitPoint u)
        {
            switch (u.Behavior)
            {
                case PointBehavior.SumLessOrEqual: return Data["DefaultSumLessOrEqual"];
                case PointBehavior.LessOrEqual: return Data["DefaultLessOrEqual"];
                case PointBehavior.MoreOrEqual: return Data["DefaultMoreOrEqual"];
                case PointBehavior.Property: return Data["DefaultProperty"];
                case PointBehavior.UpgradePoint: return Data["DefaultUpgradePoint"];
                default:
                    throw new Exception($"Unknown default LimitedPointFormat {u.Behavior}");
            }
        }

        [XmlAttribute("Id")]
        public string Id = "";

        [XmlAttribute("Format")]
        public string Format = "";
        
        [XmlAttribute("FormatPossibleOverlimiting")]
        public string FormatPossibleOverlimiting = "";
        
        [XmlAttribute("FormatOverlimiting")]
        public string FormatOverlimiting = "";
        
        [XmlAttribute("NumberFormat")]
        public string NumberFormat = "";

        [XmlAttribute("Visible")]
        public bool Visible = true;
        
        [XmlAttribute("ShowOnTopIfOverlimit")]
        public bool ShowOnTopIfOverlimit = true;
        
        [XmlAttribute("ShowOnTopIfPossibleOverlimit")]
        public bool ShowOnTopIfPossibleOverlimit = true;

        [XmlAttribute("VisibleIfAllZero")]
        public bool VisibleIfAllZero = false;
        
        [XmlAttribute("VisibleIfInfinity")]
        public bool VisibleIfInfinity = true;
    }
    
    #endregion
    
    #region MainParts

    [ProtoContract]
    public class LimitPoint
    {
        [XmlAttribute("PointId")]
        public string IDD = "UnknownId";
        
        [XmlAttribute("NId")]
        public int Id = 999;
        
        [XmlAttribute("DefaultSpecCoreValue")]
        public float DefaultSpecCoreValue;
        
        [XmlAttribute("DefaultNoSpecCoreValue")]
        public float DefaultNoSpecCoreValue;

        [XmlAttribute("FormatId")]
        public string FormatId = null;
        
        [XmlAttribute("Behavior")]
        public PointBehavior Behavior = PointBehavior.SumLessOrEqual;
        
        [XmlAttribute("IsCustom")]
        public bool IsCustom = false;
        
        [XmlAttribute("Name")]
        public string Name = "Unknown";
        
        [XmlAttribute("DisplayOrder")]
        public float DisplayOrder = 1;
        
        [XmlAttribute("ActivationError")]
        public string ActivationError = "UnknownActivationError";
        
        [XmlAttribute("UnitName")]
        public string UnitName = "";

        [XmlIgnore]
        public LimitPointFormat Format;
    }
    
    [ProtoContract]
    public class UsedPoints
    {
        [XmlAttribute("PointId")]
        public string PointName = "UnknownPointName";
        
        [XmlIgnore()]
        public int PointId;

        public void AfterDeserialize()
        {
            PointId = OriginalSpecCoreSession.GetPointId(PointName);
        }
        
        [XmlAttribute("Amount")]
        public float Amount;
        
        [XmlAttribute("UseMlts")]
        public bool UseMlts = true;
        
        [XmlAttribute("UseCustomValue")]
        public bool UseCustomValue = false;
        
        [XmlAttribute("UseTierValue")]
        public bool UseTierValue = false;
        
        [XmlAttribute("MinValue")]
        public float MinValue = float.NaN;
        
        [XmlAttribute("RoundLimits")]
        public bool RoundLimits = true;
    }

    
    public class BlockId
    {
        [XmlAttribute("Mlt")] 
        public float Mlt = 1;
        
        [XmlAttribute("Mlt2")] 
        public float Mlt2 = 1;
        
        [XmlAttribute("CustomValue")] 
        public float CustomValue = 1;

        [XmlAttribute("DisableOrder")]
        public float DisableOrder = 1;
        
        [XmlAttribute("Matcher")]
        public string Matcher = null;

        [XmlText]
        public string Value;

        public BlockId()
        {
        }

        public BlockId(BlockId other, string value)
        {
            Mlt = other.Mlt;
            Mlt2 = other.Mlt2;
            CustomValue = other.CustomValue;
            DisableOrder = other.DisableOrder;
            Value = value;
        }
    }

    public class Behavior
    {
        [XmlAttribute("DisableBehavior")]
        public EnableDisableBehaviorType DisableBehavior;
        
        [XmlAttribute("PunishBehavior")]
        public EnableDisableBehaviorType PunishBehavior;
        
        [XmlAttribute("ContinuousViolation")]
        public EnableDisableBehaviorType ContinuousViolation;
        
        [XmlAttribute("EnableBehavior")]
        public EnableDisableBehaviorType EnableBehavior;

        [XmlAttribute("ConsumeBehavior")]
        public ConsumeBehaviorType ConsumeBehavior;
        
        [XmlAttribute("ProvideBehavior")]
        public ConsumeBehaviorType ProvideBehavior;
        
        [XmlAttribute("Value1")]
        public float Value1 = 0;
        
        [XmlAttribute("Value2")]
        public float Value2 = 0;
        
        [XmlAttribute("Value3")]
        public float Value3 = 0;
        
        [XmlAttribute("Value4")]
        public float Value4 = 0;
        
        [XmlAttribute("Reverse")]
        public bool Reverse = false;
        
        [XmlAttribute("BoolValue")]
        public bool BoolValue = false;
        
        [XmlAttribute("TextValue")]
        public string TextValue;
        
        [XmlAttribute("TextValue2")]
        public string TextValue2;
        
        [XmlAttribute("CustomLogic")]
        public string CustomLogic;
        
        [XmlAttribute("ProvideLimitsError")]
        public string ProvideLimitsError = "UnknownError";
        
        [XmlAttribute("PunishedBy")]
        public string PunishedByPointIds = null;

        [XmlIgnore]
        public string[] TextValues;
        
        [XmlIgnore]
        public string[] TextValues2;
        
        [XmlIgnore]
        public long[] TextValues2Longs;
        
        [XmlIgnore] 
        private int[] PunishedByCache = null;

        public void AfterDeserialize()
        {
            TextValues = (TextValue ?? "").ToStrings(",", " ");
            TextValues2 = (TextValue2 ?? "").ToStrings(",", " ");
            TextValues2Longs = (TextValue2 ?? "").ToLongs();

            if (PunishedByPointIds == null)
            {
                PunishedByCache = new int[0];
            }
            else if (PunishedByPointIds.ToLower() == "All" || PunishedByPointIds == "*")
            {
                PunishedByCache = OriginalSpecCoreSession.Instance.Points.Keys.ToArray();
            }
            else
            {
                var list = new List<int>();
                foreach (var s in PunishedByPointIds.ToStrings())
                {
                    LimitPoint point;
                    if (!OriginalSpecCoreSession.Instance.PointsByName.TryGetValue(s, out point))
                    {
                        OriginalSpecCoreSession.AddLoadingError($"PunishedBy has wrong PointName {s}");
                    }
                    else
                    {
                        list.Add(point.Id);
                    }
                }
                
                PunishedByCache = list.ToArray();
            }
        }

        public int[] GetPunishedBy()
        {
            return PunishedByCache;
        }

        [XmlIgnore] 
        private MyDefinitionId ResourceDefinitionId;
        
        [XmlIgnore] 
        private bool ResourceDefinitionIdInited;
        
        public MyDefinitionId GetSinkDefinition()
        {
            if (!ResourceDefinitionIdInited)
            {   
                if (TextValue != null)
                {
                    MyDefinitionId.TryParse("MyObjectBuilder_GasProperties/" + TextValue, out ResourceDefinitionId);
                }

                ResourceDefinitionIdInited = true;
            }

            return ResourceDefinitionId;
        }

        public bool CheckFactionOrUser(IMyFaction faction, long identity)
        {
            if (faction != null && TextValues != null && TextValues.Length > 0)
            {
                if (TextValues.Contains(faction.Tag) != Reverse)
                {
                    return true;
                }
            }
            return TextValues2Longs.Contains(identity) != Reverse;
        }
    }
    


    #endregion

    #region Main

       
    public class SpecCoreSettings
    {
        public Timers Timers = new Timers();

        public GUIClasses LimitedBlocksCanBe = GUIClasses.Basic | GUIClasses.EnergyAndProduction | GUIClasses.RotorsAndPistons | GUIClasses.Tools | GUIClasses.Weapons;
        public GUIClasses SpecBlocksCanBe = GUIClasses.Basic | GUIClasses.ShipControl;
        
        [XmlArrayItem("Point")]
        public LimitPoint[] Points;

        [XmlArrayItem("PointFormat")] 
        public LimitPointFormat[] PointsFormats;
        
        public GridConnectionType ConnectionType = GridConnectionType.Physical;
        
        public SpecBlockInfo[] SpecBlocks;
        
        
        public LimitedBlockInfo[] LimitedBlocks;

        public NoSpecCoreSettings NoSpecCoreSettings;

        
        public float DefaultTier = 0;
        [XmlArrayItem("Tier")]
        public Tier[] Tiers;

        [XmlArrayItem("Upgrade")]
        public Upgrade[] Upgrades;

        public bool DebugMode = false;
        public bool EnableLogs = true;
        public bool RandomPunishment = true;

        public String HudNoSpecCoreText;
        public String HudSpecCoreOverlimitText;
        public String HudSpecCoreActiveText;

        [XmlElement("Cache")]
        public Cache Cache;

        
    }
    public class NoSpecCoreSettings
    {
        [XmlArrayItem("Point")]
        public UsedPoints[] LargeStatic;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] LargeDynamic;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] SmallStatic;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] SmallDynamic;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] LargeAndSmallStatic;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] LargeAndSmallDynamic;

        [XmlIgnore]
        private Dictionary<TypeOfGridGroup, Limits> m_limits = new Dictionary<TypeOfGridGroup, Limits>();
        
        public Limits GetLimits(TypeOfGridGroup mlt)
        {
            return m_limits[mlt];
        }
        
        public void AfterDeserialize()
        {
            foreach (var up in LargeStatic) up.AfterDeserialize();
            foreach (var up in LargeDynamic) up.AfterDeserialize();
            foreach (var up in SmallStatic) up.AfterDeserialize();
            foreach (var up in SmallDynamic) up.AfterDeserialize();
            foreach (var up in LargeAndSmallStatic) up.AfterDeserialize();
            foreach (var up in LargeAndSmallDynamic) up.AfterDeserialize();

            InitLimits(TypeOfGridGroup.Large);
            InitLimits(TypeOfGridGroup.Small);
            InitLimits(TypeOfGridGroup.Large | TypeOfGridGroup.Static);
            InitLimits(TypeOfGridGroup.Small | TypeOfGridGroup.Static);
            InitLimits(TypeOfGridGroup.Large | TypeOfGridGroup.Small);
            InitLimits(TypeOfGridGroup.Large | TypeOfGridGroup.Small | TypeOfGridGroup.Static);
        }

        private void InitLimits(TypeOfGridGroup mlt)
        {
            UsedPoints[] pointsArray = null;
            switch (mlt)
            {
                case TypeOfGridGroup.Large: 
                    pointsArray = LargeDynamic;
                    break;
                case TypeOfGridGroup.Static | TypeOfGridGroup.Large: 
                    pointsArray = LargeStatic;
                    break;
                case TypeOfGridGroup.Small: 
                    pointsArray = SmallDynamic;
                    break;
                case TypeOfGridGroup.Static | TypeOfGridGroup.Small: 
                    pointsArray = SmallStatic;
                    break;
                case TypeOfGridGroup.Small | TypeOfGridGroup.Large: 
                    pointsArray = LargeAndSmallDynamic;
                    break;
                case TypeOfGridGroup.Static | TypeOfGridGroup.Small | TypeOfGridGroup.Large: 
                    pointsArray = LargeAndSmallStatic;
                    break;
            }
            
            m_limits[mlt] = pointsArray.GetLimitsForNoSpecBlocks();
        }
        
       
    }

    public class LimitedBlockInfo
    {
        [XmlArrayItem("Id")]
        public BlockId[] BlockIds;
        
        [XmlAttribute("CustomLogicId")]
        public string CustomLogicId = null;

        [XmlArrayItem("Point")]
        public UsedPoints[] UsedPoints;

        [XmlAttribute("CanWorkOnSubGrids")]
        public bool CanWorkOnSubGrids = true;
        
        [XmlAttribute("CanWorkWithoutSpecCore")]
        public bool CanWorkWithoutSpecCore = true;

        public LimitedBlockSettings DefaultBlockSettings = new LimitedBlockSettings(); 

        [XmlIgnore]
        private Dictionary<BlockId, Limits> m_limits = new Dictionary<BlockId, Limits>();
        
        
        [XmlArrayItem("Behavior")] 
        public Behavior[] Behaviors = null;

        [XmlIgnore]
        public bool CanBePunished = false;

        public void AfterDeserialize()
        {
            if (UsedPoints == null || UsedPoints.Length == 0)
            {
                OriginalSpecCoreSession.AddLoadingError("UsedPoints are null for some LimitedBlockInfo");
            }
            else
            {
                foreach (var usedPoint in UsedPoints)
                {
                    usedPoint.AfterDeserialize();
                }
            }
            

            Behaviors = Behaviors ?? new Behavior[0];
            
            foreach (var behavior in Behaviors)
            {
                behavior.AfterDeserialize();
                if (behavior.GetPunishedBy().Length != 0)
                {
                    CanBePunished = true;
                }
            }
        }
        
        public Limits GetLimits(BlockId blockId)
        {
            Limits result;
            if (!m_limits.TryGetValue(blockId, out result))
            {
                result = UsedPoints.GetLimits(blockId);
                m_limits[blockId] = result;
            }

            return result;
        }
    }
    
    public class SpecBlockInfo
    {
        [XmlAttribute("BlockIds")]
        public string BlockIds;
        
        [XmlArrayItem("Behavior")] 
        public Behavior[] Behaviors = null;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] DefaultStaticAndDynamic;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] DefaultStatic;
        
        [XmlArrayItem("Point")]
        public UsedPoints[] DefaultDynamic;

        [XmlElement("PossibleUpgrades")]
        public string PossibleUpgradesString = null;
        
        
        
        [XmlIgnore]
        public int[] PossibleUpgrades;

        [XmlIgnore]
        public Limits DefaultStaticValues;
        
        [XmlIgnore]
        public Limits DefaultDynamicValues;
        
        public void AfterDeserialize()
        {
            //Parse PossibleUpgrades
            LoadPossibleUpgrades();
            

            LoadPoints();

            //Parse Behaviors
            Behaviors = Behaviors ?? new Behavior[0];
            
            foreach (var behavior in Behaviors)
            {
                behavior.AfterDeserialize();
            }
        }
        
        private void LoadPoints()
        {
            //Parse Default and Static
            if (DefaultStaticAndDynamic != null) foreach (var up in DefaultStaticAndDynamic) up.AfterDeserialize();
            if (DefaultStatic != null) foreach (var up in DefaultStatic) up.AfterDeserialize();
            if (DefaultDynamic != null) foreach (var up in DefaultDynamic) up.AfterDeserialize();

            DefaultStaticValues = DefaultStaticAndDynamic.GetLimitsForSpecBlocks(DefaultStatic);
            DefaultDynamicValues = DefaultStaticAndDynamic.GetLimitsForSpecBlocks(DefaultDynamic);
        }
        
        private void LoadPossibleUpgrades()
        {
            if (String.IsNullOrEmpty(PossibleUpgradesString))
            {
                PossibleUpgrades = new int[0];
            }
            else
            {
                var temp = PossibleUpgradesString.ToStrings(","," ", "\r\n", "\n");
                var lower = PossibleUpgradesString.ToLower();
                var list = new List<int>();
                
                foreach (var possibleUpgrade in temp)
                {
                    if (!OriginalSpecCoreSession.Instance.UpgradesByName.ContainsKey(possibleUpgrade) && possibleUpgrade.ToLower() != "except")
                    {
                        OriginalSpecCoreSession.AddLoadingError($"Wasn't able to find upgrade with id {possibleUpgrade}");
                    }
                }
                
                if (lower.Contains("except"))
                {
                    foreach (var upgrade in OriginalSpecCoreSession.Instance.Upgrades)
                    {
                        if (!temp.Contains(upgrade.Value.Name))
                        {
                            list.Add(upgrade.Key);    
                        }
                    }
                }
                else
                {
                    foreach (var s in temp)
                    {
                        list.Add(OriginalSpecCoreSession.Instance.UpgradesByName[s].NId);
                    }
                }
                PossibleUpgrades = list.ToArray();
            }
        }
    }

    #endregion

    #region Upgrades

    [ProtoContract]
    public class Upgrade
    {
        [XmlAttribute("Name")]
        public string Name;
        
        [XmlAttribute("NId")]
        public int NId;
        
        [XmlAttribute("DisplayName")]
        public string DisplayName;

        [XmlArrayItem("Level")] 
        public UpgradeLevel[] Levels;
    
        public void AfterDeserialize()
        {
           
            foreach (var ul in Levels)
            {
                ul.AfterDeserialize();
            }
        }
    }

    public class Locks
    {
        [XmlAttribute("LockGroups")] 
        public string LockGroupsUnparsed  = "";
        
        [XmlAttribute("AddedHardLocks")] 
        public string AddHardLocksUnparsed = "";
        
        [XmlAttribute("AddedSoftLocks")] 
        public string AddSoftLocksUnparsed = "";
        
        [XmlAttribute("RemovedHardLocks")] 
        public string RemoveHardLocksUnparsed = "";
        
        [XmlAttribute("RemovedSoftLocks")] 
        public string RemoveSoftLocksUnparsed = "";

        public override string ToString()
        {
            return $"{AddHardLocks.Length} {AddSoftLocks.Length} {RemoveHardLocks.Length} {RemoveSoftLocks.Length} {LockGroupsUnparsed} {AddHardLocksUnparsed} {AddSoftLocksUnparsed} {RemoveHardLocksUnparsed} {RemoveSoftLocksUnparsed}";
        }

        [XmlIgnore()] 
        public string[] LockGroups;
        
        [XmlIgnore()] 
        public string[] AddHardLocks;
        
        [XmlIgnore()] 
        public string[] AddSoftLocks;
        
        [XmlIgnore()] 
        public string[] RemoveHardLocks;
        
        [XmlIgnore()] 
        public string[] RemoveSoftLocks;

        public void AfterDeserialize()
        {
            LockGroups = LockGroupsUnparsed.ToStrings(",", " ");
            AddHardLocks = AddHardLocksUnparsed.ToStrings(",", " ");
            AddSoftLocks = AddSoftLocksUnparsed.ToStrings(",", " ");
            RemoveHardLocks = RemoveHardLocksUnparsed.ToStrings(",", " ");
            RemoveSoftLocks = RemoveSoftLocksUnparsed.ToStrings(",", " ");
        }
    }
    
    public class UpgradeLevel
    {
        [XmlArrayItem("Cost")]
        public UpgradeCostPart[] Costs;
        
        [XmlArrayItem("Modificator")]
        public AttributeModificator[] Modificators;

        [XmlIgnore()] 
        public Dictionary<int, float> TotalUpgradeCost;

        [XmlElement("Locks")]
        public Locks Locks = new Locks();

        public void AfterDeserialize()
        {
            Locks.AfterDeserialize();
            
            if (Modificators != null)
            {
                foreach (var modificator in Modificators)
                {
                    modificator.AfterDeserialize();
                }
            }

            Costs = Costs ?? new UpgradeCostPart[0];
            
            var total = new Dictionary<int, float>();
            foreach (var cost in Costs)
            {
                cost.AfterDeserialize();
                total.Sum(cost.PointId, cost.Value);
            }
            TotalUpgradeCost = total;
        }
    }
    
    public class AttributeModificator
    {
        [XmlAttribute("PointId")]
        public string PointName;
        
        [XmlIgnore]
        public int PointId;

        [XmlAttribute]
        public float SumBefore = 0;
        [XmlAttribute]
        public float SumStaticBefore = 0;
        [XmlAttribute]
        public float SumDynamicBefore = 0;
        
        [XmlAttribute]
        public float SumAfter = 0;
        [XmlAttribute]
        public float SumStaticAfter = 0;
        [XmlAttribute]
        public float SumDynamicAfter = 0;
        
        [XmlAttribute]
        public float Mlt = 1;
        [XmlAttribute]
        public float MltStatic = 1;
        [XmlAttribute]
        public float MltDynamic = 1;

        public void AfterDeserialize()
        {
            PointId = OriginalSpecCoreSession.GetPointId(PointName);
        }
    }
    
    public class UpgradeCostPart
    {
        [XmlAttribute("PointId")] 
        public string PointName = "UnknownUpgradeCostId";
        
        [XmlIgnore()] 
        public int PointId;
        
        [XmlAttribute("Value")] 
        public float Value;

        public void AfterDeserialize()
        {
            PointId = OriginalSpecCoreSession.GetPointId(PointName);
        }
    }

    #endregion
    
}