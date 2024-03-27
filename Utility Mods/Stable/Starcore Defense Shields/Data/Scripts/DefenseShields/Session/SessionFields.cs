using ParallelTasks;
using VRage.Input;

namespace DefenseShields
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Support;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using VRage.Collections;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.Utils;
    using VRageMath;
    using VRage.ModAPI;

    public partial class Session
    {
        internal const ushort PACKET_ID = 62520;
        internal const double TickTimeDiv = 0.0625;
        internal const double TwoStep = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 2;
        internal const float ShieldShuntBonus = 0.16f;
        internal const float ShieldBypassBonus = 0.2f;
        internal const int RingOverFlowLimit = 15;

        internal static readonly MyConcurrentPool<MyProtectors> ProtSets = new MyConcurrentPool<MyProtectors>(150, null, 1000);

        internal const int ClientCfgVersion = 15;
        internal const string ClientCfgName = "DefenseShieldsClient.cfg";

        internal readonly int[] SlotCnt = new int[9];
        internal readonly Vector3D[] LosPointSphere = new Vector3D[2000];

        internal readonly MyStringHash MPExplosion = MyStringHash.GetOrCompute("MPExplosion");
        internal readonly MyStringHash MPEnergy = MyStringHash.GetOrCompute("MPEnergy");
        internal readonly MyStringHash MPKinetic = MyStringHash.GetOrCompute("MPKinetic");
        internal readonly MyStringHash MPEMP = MyStringHash.GetOrCompute("MPEMP");
        internal readonly MyStringHash MpIgnoreDamage = MyStringHash.GetOrCompute("MpIgnoreDamage");
        internal readonly MyStringHash Bypass = MyStringHash.GetOrCompute("bypass");
        internal MyStringId Password;
        internal MyStringId PasswordTooltip;
        internal MyStringId ShieldFreq;
        internal MyStringId ShieldFreqTooltip;

        internal readonly Guid O2GeneratorSettingsGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811502");
        internal readonly Guid EnhancerStateGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811503");
        internal readonly Guid O2GeneratorStateGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811504");
        internal readonly Guid ControllerStateGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811505");
        internal readonly Guid EmitterStateGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811506");
        internal readonly Guid ControllerSettingsGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811508");
        internal readonly Guid ModulatorSettingsGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811509");
        internal readonly Guid ModulatorStateGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811510");
        internal readonly Guid ControllerEnforceGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811511");
        internal readonly Guid PlanetShieldSettingsGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811512");
        internal readonly Guid PlanetShieldStateGuid = new Guid("85BBB4F5-4FB9-4230-BEEF-BB79C9811513");

        internal readonly Icosphere Icosphere = new Icosphere(5);

        internal readonly ConcurrentDictionary<long, IMyPlayer> Players = new ConcurrentDictionary<long, IMyPlayer>();

        internal readonly List<DefenseShields> WebWrapper = new List<DefenseShields>();

        internal readonly ConcurrentQueue<IThreadEvent> ThreadEvents = new ConcurrentQueue<IThreadEvent>();

        internal readonly ConcurrentDictionary<MyEntity, MyProtectors> GlobalProtect = new ConcurrentDictionary<MyEntity, MyProtectors>();
        internal readonly ConcurrentDictionary<long, ShieldGridComponent> IdToBus = new ConcurrentDictionary<long, ShieldGridComponent>();
        internal readonly ConcurrentDictionary<DefenseShields, bool> FunctionalShields = new ConcurrentDictionary<DefenseShields, bool>();
        internal readonly ConcurrentDictionary<DefenseShields, byte> ActiveShields = new ConcurrentDictionary<DefenseShields, byte>();
        internal readonly Dictionary<MyCubeGrid, uint> CheckForSplits = new Dictionary<MyCubeGrid, uint>();
        internal readonly ConcurrentDictionary<MyCubeGrid, ParentGrid> GetParentGrid = new ConcurrentDictionary<MyCubeGrid, ParentGrid>();
        internal readonly ConcurrentDictionary<long, byte> ManagedAttackers = new ConcurrentDictionary<long, byte>();
        internal readonly ConcurrentDictionary<MyCubeGrid, ConcurrentCachingList<MyBatteryBlock>> GridBatteryMap = new ConcurrentDictionary<MyCubeGrid, ConcurrentCachingList<MyBatteryBlock>>();

        internal readonly HashSet<MyCubeGrid> WatchForSplits = new HashSet<MyCubeGrid>();

        internal readonly List<Emitters> Emitters = new List<Emitters>();
        internal readonly List<Enhancers> Enhancers = new List<Enhancers>();
        internal readonly List<O2Generators> O2Generators = new List<O2Generators>();
        internal readonly List<Modulators> Modulators = new List<Modulators>();
        internal readonly List<DefenseShields> Controllers = new List<DefenseShields>();
        internal readonly MyConcurrentPool<List<MyEntity>> ListMyEntityPool = new MyConcurrentPool<List<MyEntity>>(100);

        internal readonly MyConcurrentPool<HashSet<CubeAccel>> SetCubeAccelPool = new MyConcurrentPool<HashSet<CubeAccel>>(100);
        internal readonly MyConcurrentPool<EntIntersectInfo> EntIntersectInfoPool = new MyConcurrentPool<EntIntersectInfo>(100, info => info.Clean());

        internal readonly MyConcurrentPool<ImpactRingEffectData> RingPool = new MyConcurrentPool<ImpactRingEffectData>(100);
        internal readonly MyConcurrentPool<ProtectCache> ProtectCachePool = new MyConcurrentPool<ProtectCache>(100, info => info.Clean());
        internal readonly MyConcurrentPool<ShieldVsShieldThreadEvent> ShieldEventPool = new MyConcurrentPool<ShieldVsShieldThreadEvent>(25, info => info.Clean());
        internal readonly MyConcurrentPool<FloaterThreadEvent> FloaterPool = new MyConcurrentPool<FloaterThreadEvent>(100, info => info.Clean());
        internal readonly MyConcurrentPool<MeteorDmgThreadEvent> MeteorPool = new MyConcurrentPool<MeteorDmgThreadEvent>(25, info => info.Clean());
        internal readonly MyConcurrentPool<CollisionDataThreadEvent> CollisionPool = new MyConcurrentPool<CollisionDataThreadEvent>(100, info => info.Clean());
        internal readonly MyConcurrentPool<StationCollisionDataThreadEvent> StaticCollisionPool = new MyConcurrentPool<StationCollisionDataThreadEvent>(100, info => info.Clean());
        internal readonly MyConcurrentPool<PlayerCollisionThreadEvent> PlayerCollisionPool = new MyConcurrentPool<PlayerCollisionThreadEvent>(10, info => info.Clean());
        internal readonly MyConcurrentPool<CharacterEffectThreadEvent> PlayerEffectPool = new MyConcurrentPool<CharacterEffectThreadEvent>(10, info => info.Clean());
        internal readonly MyConcurrentPool<ManyBlocksThreadEvent> ManyBlocksPool = new MyConcurrentPool<ManyBlocksThreadEvent>(100, info => info.Clean());
        internal readonly MyConcurrentPool<VoxelCollisionDmgThreadEvent> VoxelCollisionDmgPool = new MyConcurrentPool<VoxelCollisionDmgThreadEvent>(25, info => info.Clean());
        internal readonly MyConcurrentPool<VoxelCollisionPhysicsThreadEvent> VoxelCollisionPhysicsPool = new MyConcurrentPool<VoxelCollisionPhysicsThreadEvent>(25, info => info.Clean());
        internal readonly MyConcurrentPool<ForceDataThreadEvent> ForceDataPool = new MyConcurrentPool<ForceDataThreadEvent>(100, info => info.Clean());
        internal readonly MyConcurrentPool<ConcurrentCachingList<MyBatteryBlock>> BatteryListPool = new MyConcurrentPool<ConcurrentCachingList<MyBatteryBlock>>(100);

        internal ControlQuery ControlRequest;
        internal enum ControlQuery
        {
            None,
            Keyboard,
            Mouse,
        }

        internal readonly HashSet<string> DsActions = new HashSet<string>()
        {
            "DS-C_ToggleShield_Toggle",
            "DS-C_ToggleShield_On",
            "DS-C_ToggleShield_Off",
            "DS-C_ChargeRate_Reset",
            "DS-C_ChargeRate_Increase",
            "DS-C_ChargeRate_Decrease",
            "DS-C_SideRedirect_Toggle",
            "DS-C_SphereFit_Toggle",
            "DS-C_ShieldFortify_Toggle",
            "DS-C_HideActive_Toggle",
            "DS-C_RefreshAnimation_Toggle",
            "DS-C_HitWaveAnimation_Toggle",
            "DS-C_HideIcon_Toggle",
            "DS-C_UseBatteries_Toggle"
        };

        internal readonly HashSet<string> ModActions = new HashSet<string>()
        {
            "DS-M_DamageModulation_Reset",
            "DS-M_DamageModulation_Increase",
            "DS-M_DamageModulation_Decrease",
            "DS-M_ModulateVoxels_Toggle",
            "DS-M_ModulateGrids_Toggle",
            "DS-M_ModulateEmpProt_Toggle"
        };


        internal readonly Dictionary<ShieldSides, string> ShieldHealthSides = new Dictionary<ShieldSides, string>
        {
            {ShieldSides.Left, "ShieldLeft" },
            {ShieldSides.Right, "ShieldRight" },
            {ShieldSides.Up, "ShieldTop" },
            {ShieldSides.Down, "ShieldBottom" },
            {ShieldSides.Forward, "ShieldFront" },
            {ShieldSides.Backward, "ShieldBack" }
        };

        internal readonly Dictionary<ShieldSides, string> ShieldShuntedSides = new Dictionary<ShieldSides, string>
        {
            {ShieldSides.Left, "RedirectLeft" },
            {ShieldSides.Right, "RedirectRight" },
            {ShieldSides.Up, "RedirectTop" },
            {ShieldSides.Down, "RedirectBottom" },
            {ShieldSides.Forward, "RedirectFront" },
            {ShieldSides.Backward, "RedirectBack" }
        };

        internal readonly Dictionary<ShieldSides, MyStringId> ShieldDirectedSidesDraw = new Dictionary<ShieldSides, MyStringId>()
        {
            {ShieldSides.Left, MyStringId.GetOrCompute("DS_ShieldRedirectLeft") },
            {ShieldSides.Right,MyStringId.GetOrCompute("DS_ShieldRedirectRight") },
            {ShieldSides.Up, MyStringId.GetOrCompute("DS_ShieldRedirectUp") },
            {ShieldSides.Down, MyStringId.GetOrCompute("DS_ShieldRedirectDown") },
            {ShieldSides.Forward, MyStringId.GetOrCompute("DS_ShieldRedirectFront") },
            {ShieldSides.Backward, MyStringId.GetOrCompute("DS_ShieldRedirectBack") }
        };


        public enum ShieldSides
        {
            Forward,
            Backward,
            Left,
            Right,
            Up,
            Down
        }

        public struct ShieldInfo
        {
            public ShieldSides Side;
            public bool Redirected;
        }

        internal readonly Dictionary<ShieldSides, int> SideControlMap = new Dictionary<ShieldSides, int>
        {
            {ShieldSides.Left, -1 },
            {ShieldSides.Right, 1 },
            {ShieldSides.Up, 1 },
            {ShieldSides.Down, -1 },
            {ShieldSides.Forward, -1 },
            {ShieldSides.Backward, 1 }
        };

        internal readonly Color Color90 = new Color(255, 255, 255, 255 );
        internal readonly Color Color80 = new Color(255, 255, 255, 0);
        internal readonly Color Color70 = new Color(127, 255, 255, 255);
        internal readonly Color Color60 = new Color(64, 255, 255, 255);
        internal readonly Color Color50 = new Color(0, 255, 255, 255);
        internal readonly Color Color40 = new Color(0, 255, 0, 255);
        internal readonly Color Color30 = new Color(255, 255, 0, 255);
        internal readonly Color Color20 = new Color(255, 18, 0, 255);
        internal readonly Color Color10 = new Color(255, 0, 0, 255);
        internal readonly Color Color00 = new Color(0.05f, 0, 0, 255);

        internal readonly MyStringId HudIconBlackShield = MyStringId.GetOrCompute("DS_ShieldBlack");
        internal readonly MyStringId HudIconWhiteShield = MyStringId.GetOrCompute("DS_ShieldWhite");

        internal readonly MyStringId HudIconOffline = MyStringId.GetOrCompute("DS_ShieldOffline");
        internal readonly MyStringId HudIconHealth10 = MyStringId.GetOrCompute("DS_ShieldHealth10");
        internal readonly MyStringId HudIconHealth20 = MyStringId.GetOrCompute("DS_ShieldHealth20");
        internal readonly MyStringId HudIconHealth30 = MyStringId.GetOrCompute("DS_ShieldHealth30");
        internal readonly MyStringId HudIconHealth40 = MyStringId.GetOrCompute("DS_ShieldHealth40");
        internal readonly MyStringId HudIconHealth50 = MyStringId.GetOrCompute("DS_ShieldHealth50");
        internal readonly MyStringId HudIconHealth60 = MyStringId.GetOrCompute("DS_ShieldHealth60");
        internal readonly MyStringId HudIconHealth70 = MyStringId.GetOrCompute("DS_ShieldHealth70");
        internal readonly MyStringId HudIconHealth80 = MyStringId.GetOrCompute("DS_ShieldHealth80");
        internal readonly MyStringId HudIconHealth90 = MyStringId.GetOrCompute("DS_ShieldHealth90");
        internal readonly MyStringId HudIconHealth100 = MyStringId.GetOrCompute("DS_ShieldHealth100");
        internal readonly MyStringId HudIconVenting = MyStringId.GetOrCompute("DS_ShieldVenting");

        internal readonly MyStringId[] HudHealthHpIcons = 
        {
            MyStringId.NullOrEmpty,
            MyStringId.GetOrCompute("DS_ShieldHeal10"),
            MyStringId.GetOrCompute("DS_ShieldHeal20"),
            MyStringId.GetOrCompute("DS_ShieldHeal30"),
            MyStringId.GetOrCompute("DS_ShieldHeal40"),
            MyStringId.GetOrCompute("DS_ShieldHeal50"),
            MyStringId.GetOrCompute("DS_ShieldHeal60"),
            MyStringId.GetOrCompute("DS_ShieldHeal70"),
            MyStringId.GetOrCompute("DS_ShieldHeal80"),
            MyStringId.GetOrCompute("DS_ShieldHeal90"),
            MyStringId.GetOrCompute("DS_ShieldHeal100"),
            MyStringId.GetOrCompute("DS_ShieldDps100"),
            MyStringId.GetOrCompute("DS_ShieldDps90"),
            MyStringId.GetOrCompute("DS_ShieldDps80"),
            MyStringId.GetOrCompute("DS_ShieldDps70"),
            MyStringId.GetOrCompute("DS_ShieldDps60"),
            MyStringId.GetOrCompute("DS_ShieldDps50"),
            MyStringId.GetOrCompute("DS_ShieldDps40"),
            MyStringId.GetOrCompute("DS_ShieldDps30"),
            MyStringId.GetOrCompute("DS_ShieldDps20"),
            MyStringId.GetOrCompute("DS_ShieldDps10"),
        };

        internal readonly MyStringId[] HudHpLossIcons =
        {
            MyStringId.NullOrEmpty,
            MyStringId.GetOrCompute("DS_ShieldHPLoss90"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss80"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss70"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss60"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss50"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss40"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss30"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss20"),
            MyStringId.GetOrCompute("DS_ShieldHPLoss10"),
        };

        internal readonly MyStringId[] HudPenChanceIcons =
        {
            MyStringId.GetOrCompute("DS_ShieldPenLess1Percent"),
            MyStringId.GetOrCompute("DS_ShieldPen1Percent"),
            MyStringId.GetOrCompute("DS_ShieldPen2Percent"),
            MyStringId.GetOrCompute("DS_ShieldPen5Percent"),
            MyStringId.GetOrCompute("DS_ShieldPen10Percent"),
            MyStringId.GetOrCompute("DS_ShieldPen20Percent"),
            MyStringId.GetOrCompute("DS_ShieldPen30Percent"),
            MyStringId.GetOrCompute("DS_ShieldPen40Percent"),
            MyStringId.GetOrCompute("DS_ShieldPenGreater50Percent"),
        };

        internal readonly MyStringId[] HudModulationIcons =
        {
            MyStringId.GetOrCompute("DS_KineticEnergyNeutral"),
            MyStringId.GetOrCompute("DS_ShieldEnergy1"),
            MyStringId.GetOrCompute("DS_ShieldEnergy2"),
            MyStringId.GetOrCompute("DS_ShieldEnergy3"),
            MyStringId.GetOrCompute("DS_ShieldEnergy4"),
            MyStringId.GetOrCompute("DS_ShieldEnergy5"),
            MyStringId.GetOrCompute("DS_ShieldEnergy6"),
            MyStringId.GetOrCompute("DS_ShieldEnergy7"),
            MyStringId.GetOrCompute("DS_ShieldEnergy8"),
            MyStringId.GetOrCompute("DS_ShieldEnergy9"),
            MyStringId.GetOrCompute("DS_ShieldEnergy10"),
            MyStringId.GetOrCompute("DS_ShieldKinetic1"),
            MyStringId.GetOrCompute("DS_ShieldKinetic2"),
            MyStringId.GetOrCompute("DS_ShieldKinetic3"),
            MyStringId.GetOrCompute("DS_ShieldKinetic4"),
            MyStringId.GetOrCompute("DS_ShieldKinetic5"),
            MyStringId.GetOrCompute("DS_ShieldKinetic6"),
            MyStringId.GetOrCompute("DS_ShieldKinetic7"),
            MyStringId.GetOrCompute("DS_ShieldKinetic8"),
            MyStringId.GetOrCompute("DS_ShieldKinetic9"),
            MyStringId.GetOrCompute("DS_ShieldKinetic10"),
        };

        internal readonly MyStringId HudIconHeat10 = MyStringId.GetOrCompute("DS_ShieldHeat10");
        internal readonly MyStringId HudIconHeat20 = MyStringId.GetOrCompute("DS_ShieldHeat20");
        internal readonly MyStringId HudIconHeat30 = MyStringId.GetOrCompute("DS_ShieldHeat30");
        internal readonly MyStringId HudIconHeat40 = MyStringId.GetOrCompute("DS_ShieldHeat40");
        internal readonly MyStringId HudIconHeat50 = MyStringId.GetOrCompute("DS_ShieldHeat50");
        internal readonly MyStringId HudIconHeat60 = MyStringId.GetOrCompute("DS_ShieldHeat60");
        internal readonly MyStringId HudIconHeat70 = MyStringId.GetOrCompute("DS_ShieldHeat70");
        internal readonly MyStringId HudIconHeat80 = MyStringId.GetOrCompute("DS_ShieldHeat80");
        internal readonly MyStringId HudIconHeat90 = MyStringId.GetOrCompute("DS_ShieldHeat90");
        internal readonly MyStringId HudIconHeat100 = MyStringId.GetOrCompute("DS_ShieldHeat100");

        internal bool[] SphereOnCamera = Array.Empty<bool>();
        internal bool CustomDataReset = true;

        internal volatile bool EntSlotTick;
        internal volatile bool Dispatched;

        private const int EntCleanCycle = 3600;
        private const int EntMaxTickAge = 36000;
        private static int _entSlotAssigner;
        internal bool InMenu;

        internal ulong MultiplayerId;
        internal long PlayerId;
        internal bool PlayersLoaded;
        internal bool CanChangeHud;
        internal bool ShutDown;

        internal readonly ApiBackend Api = new ApiBackend();
        internal ShieldSettings Settings;
        internal UiInput UiInput;
        internal string PlayerMessage;
        internal readonly Dictionary<string, MyKeys> KeyMap = new Dictionary<string, MyKeys>();
        internal readonly Dictionary<string, MyMouseButtonsEnum> MouseMap = new Dictionary<string, MyMouseButtonsEnum>();
        private readonly List<MyCubeGrid> _tmpWatchGridsToRemove = new List<MyCubeGrid>();
        internal readonly ConcurrentQueue<MyEntity> EntRefreshQueue = new ConcurrentQueue<MyEntity>();
        private readonly ConcurrentDictionary<MyEntity, uint> _globalEntTmp = new ConcurrentDictionary<MyEntity, uint>();
        private readonly List<MyKeys> _pressedKeys = new List<MyKeys>();
        
        internal IMyCamera Camera;
        internal Task MonitorTask = new Task();
        internal int ActiveShieldRings;
        internal int RingOverFlows;

        internal double ScaleFov;
        internal float AspectRatio;
        internal float AspectRatioInv;
        internal float CurrentFovWithZoom;

        internal MatrixD CameraMatrix;
        internal Vector3D CameraPos;
        internal readonly BoundingFrustumD CameraFrustrum = new BoundingFrustumD();
        private int _count = -1;
        private int _lCount;
        private int _eCount;
        private string _lastKeyAction;

        public Session()
        {
            LoadTextMaps("EN", out CharacterMap); // possible translations in future

            BuildMap(MyStringId.GetOrCompute("WeaponStatWindow"), 0, 0, 0, 128, 768, 128, 768, 384, ref InfoBackground);
            BuildMap(MyStringId.GetOrCompute("HeatAtlasBar"), 0, 0, 0, 64, 1024, 64, 1024, 1024, ref HeatBarTexture);
            BuildMap(MyStringId.GetOrCompute("ReloadingIcons"), 0, 0, 0, 64, 64, 64, 64, 512, ref ReloadingTexture);
            BuildMap(MyStringId.GetOrCompute("ReloadingIcons"), 0, 384, 0, 64, 64, 64, 64, 512, ref OutofAmmoTexture);
            BuildMap(MyStringId.GetOrCompute("RechargingIcons"), 0, 0, 0, 64, 64, 64, 64, 640, ref ChargingTexture);
            BuildMap(MyStringId.GetOrCompute("BlockTargetAtlas"), 0, 0, 0, 256, 256, 256, 256, 2560, ref PaintedTexture); // InitOffset X,Y offset X,Y uv X,Y textureSize X,Y

            UiInput = new UiInput(this);
            UtilsStatic.UnitSphereRandomOnly(ref LosPointSphere);
        }

        internal static DefenseShieldsEnforcement Enforced { get; set; } = new DefenseShieldsEnforcement();
        internal static Session Instance { get; private set; }
        internal static bool EnforceInit { get; set; }

        internal uint Tick { get; set; }

        internal int OnCount { get; set; }
        internal int RefreshCycle { get; set; }
        internal int EntSlotScaler { get; set; } = 9;
        internal int MinScaler { get; set; } = 1;
        internal int PlayerEventId { get; set; }
        internal long LastTerminalId { get; set; }
        internal int ClientLoadCount { get; set; }
        internal float MaxEntitySpeed { get; set; } = 210;

        internal double HudShieldDist { get; set; } = double.MaxValue;
        internal double SyncDistSqr { get; private set; }
        internal double SyncBufferedDistSqr { get; private set; }
        internal double SyncDist { get; private set; }

        internal bool HudIconReset { get; set; } = true;
        internal bool OnCountThrottle { get; set; }
        internal bool GameLoaded { get; set; }
        internal bool MiscLoaded { get; set; }
        internal bool Tick10 { get; set; }
        internal bool Tick20 { get; set; }
        internal bool Tick30 { get; set; }
        internal bool Tick60 { get; set; }
        internal bool Tick180 { get; set; }
        internal bool Tick120 { get; set; }

        internal bool Tick300 { get; set; }
        internal bool Tick600 { get; set; }
        internal bool Tick1800 { get; set; }
        internal bool WebWrapperOn { get; set; }
        internal bool ScalerChanged { get; set; }
        internal bool DsControl { get; set; }
        internal bool ModControl { get; set; }
        internal bool O2Control { get; set; }
        internal bool MpActive { get; set; }
        internal bool IsServer { get; set; }
        internal bool HandlesInput { get; set; }
        internal bool DedicatedServer { get; set; }
        internal bool DsAction { get; set; }
        internal bool ModAction { get; set; }
        internal bool ThyaImages { get; set; }
        internal bool SessionReady { get; set; }
        internal bool FastRefresh { get; set; }
        internal DefenseShields HudComp { get; set; }
        internal DSUtils Dsutil1 { get; set; } = new DSUtils();

        internal IMyHudNotification HudNotify;
        internal IMyTerminalControlSlider WidthSlider { get; set; }
        internal IMyTerminalControlSlider HeightSlider { get; set; }
        internal IMyTerminalControlSlider DepthSlider { get; set; }
        internal IMyTerminalControlSlider OffsetWidthSlider { get; set; }
        internal IMyTerminalControlSlider OffsetHeightSlider { get; set; }
        internal IMyTerminalControlSlider OffsetDepthSlider { get; set; }
        internal IMyTerminalControlSlider Fit { get; set; }
        internal IMyTerminalControlCheckbox SphereFit { get; set; }
        internal IMyTerminalControlCheckbox SideShunting { get; set; }
        internal IMyTerminalControlCheckbox ShowShunting { get; set; }

        internal IMyTerminalControlCheckbox FortifyShield { get; set; }
        internal IMyTerminalControlCheckbox BatteryBoostCheckBox { get; set; }
        internal IMyTerminalControlCheckbox HideActiveCheckBox { get; set; }
        internal IMyTerminalControlCheckbox NoWarningSoundsCheckBox { get; set; }
        internal IMyTerminalControlCheckbox DimShieldHitsCheckBox { get; set; }

        internal IMyTerminalControlCheckbox SendToHudCheckBox { get; set; }
        internal IMyTerminalControlOnOffSwitch ToggleShield { get; set; }

        internal IMyTerminalControlOnOffSwitch TopShield { get; set; }
        internal IMyTerminalControlOnOffSwitch BottomShield { get; set; }
        internal IMyTerminalControlOnOffSwitch LeftShield { get; set; }
        internal IMyTerminalControlOnOffSwitch RightShield { get; set; }
        internal IMyTerminalControlOnOffSwitch FrontShield { get; set; }
        internal IMyTerminalControlOnOffSwitch BackShield { get; set; }

        internal IMyTerminalControlButton HeatSink { get; set; }
        internal IMyTerminalControlCombobox ShellSelect { get; set; }
        internal IMyTerminalControlCombobox ShellVisibility { get; set; }
        internal IMyTerminalControlCombobox PowerScaleSelect { get; set; }
        internal IMyTerminalControlSlider PowerWatts { get; set; }

        internal IMyTerminalControlSlider ModDamage { get; set; }
        internal IMyTerminalControlCheckbox ModVoxels { get; set; }
        internal IMyTerminalControlCheckbox ModGrids { get; set; }
        internal IMyTerminalControlCheckbox ModAllies { get; set; }
        internal IMyTerminalControlCheckbox PassiveModulation { get; set; }
        internal IMyTerminalControlCheckbox ModEmp { get; set; }
        internal IMyTerminalControlCheckbox ModReInforce { get; set; }
        internal IMyTerminalControlSeparator ModSep1 { get; set; }
        internal IMyTerminalControlSeparator ModSep2 { get; set; }

        internal IMyTerminalControlCheckbox O2DoorFix { get; set; }

        internal IMyTerminalControlCheckbox PsBatteryBoostCheckBox { get; set; }
        internal IMyTerminalControlCheckbox PsHideActiveCheckBox { get; set; }
        internal IMyTerminalControlCheckbox PsRefreshAnimationCheckBox { get; set; }
        internal IMyTerminalControlCheckbox PsHitWaveAnimationCheckBox { get; set; }

        internal IMyTerminalControlCheckbox PsSendToHudCheckBox { get; set; }
        internal IMyTerminalControlOnOffSwitch PsToggleShield { get; set; }

        internal GetFitSeq[] FitSeq = new GetFitSeq[]
        {
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 1f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.1f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 1f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.2f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.3f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.4f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.5f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.6f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.7f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.8f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.9f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.1f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.2f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.3f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.4f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.5f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.6f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.7f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.8f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.9f)
        };

        internal GetFitSeq[] Fits = new GetFitSeq[]
        {
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 0.3f),
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 0.4f),
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 0.5f),
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 0.6f),
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 0.7f),
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 0.8f),
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 0.9f),
            new GetFitSeq(Math.Sqrt(1), Math.Sqrt(2), 1f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.1f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.2f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.3f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.4f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.5f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.6f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.7f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.8f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 0.9f),
            new GetFitSeq(Math.Sqrt(2), Math.Sqrt(3), 1f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.1f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.3f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.5f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.7f),
            new GetFitSeq(Math.Sqrt(4), Math.Sqrt(5), 0.9f),
        };

        internal readonly string[] Thya = { "THYA-ShieldC", "THYA-ShieldH", "THYA-ShieldV" };
    }
}
