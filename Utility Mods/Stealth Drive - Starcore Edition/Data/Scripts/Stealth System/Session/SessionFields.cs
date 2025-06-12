using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using VRage.Utils;
using VRage.Game.Entity;
using System.Collections.Generic;
using VRage.Collections;
using Jakaria.API;
using System.Collections.Concurrent;
using Sandbox.Game.Entities;
using Sandbox.Definitions;

namespace StealthSystem
{
    public partial class StealthSession
    {
        internal const string STATUS_EMISSIVE = "Emissive";
        internal const string RADIANT_EMISSIVE = "Emissive0";

        internal const int FADE_INTERVAL = 5;
        internal const int IsStealthedFlag = 0x20000000;

        internal readonly Dictionary<string, Definitions.DriveDefinition> DriveDefinitions = new Dictionary<string, Definitions.DriveDefinition>();
        internal readonly Dictionary<string, Definitions.SinkDefinition> SinkDefinitions = new Dictionary<string, Definitions.SinkDefinition>();

        internal readonly HashSet<string> ShieldBlocks = new HashSet<string>()
        {
            "EmitterL",
            "EmitterS",
            "EmitterST",
            "EmitterLA",
            "EmitterSA",
            "LargeShipSmallShieldGeneratorBase",
            "LargeShipLargeShieldGeneratorBase",
            "SmallShipSmallShieldGeneratorBase",
            "SmallShipMicroShieldGeneratorBase",
            "LargeGridLargeShield",
            "LargeGridSmallShield",
            "SmallGridLargeShield",
            "SmallGridSmallShield",
        };

        internal string ModPath;
        internal readonly Guid CompDataGuid = new Guid("75BBB4F5-4FB9-4230-AAAA-BB79C9811507");
        internal static readonly MyStringId _square = MyStringId.GetOrCompute("Square");

        internal BoundingBoxD LargeBox;
        internal BoundingBoxD SmallBox;

        internal EntityFlags StealthFlag;

        internal int ShieldDelay;
        internal int JumpPenalty;
        internal int FadeTime;
        internal int FadeSteps;
        internal int DamageThreshold;
        internal float Transparency;
        internal float WaterTransitionDepth;
        internal float WaterOffsetSqr;
        internal bool DisableShields;
        internal bool DisableWeapons;
        internal bool HideThrusterFlames;
        internal bool WorkInWater;
        internal bool WorkOutOfWater;
        internal bool TrackWater;
        internal bool TrackDamage;
        internal bool RevealOnDamage;

        internal readonly Dictionary<long, DriveComp> DriveMap = new Dictionary<long, DriveComp>();
        internal readonly Dictionary<IMyCubeGrid, GridComp> GridMap = new Dictionary<IMyCubeGrid, GridComp>();
        internal readonly Dictionary<IMyGridGroupData, GroupMap> GridGroupMap = new Dictionary<IMyGridGroupData, GroupMap>();
        internal readonly List<GridComp> GridList = new List<GridComp>();
        internal readonly HashSet<IMyCubeGrid> StealthedGrids = new HashSet<IMyCubeGrid>();
        internal readonly Vector3D[] ObbCorners = new Vector3D[8];

        internal Settings ConfigSettings;
        internal APIBackend API;
        internal APIServer APIServer;
        internal readonly WaterModAPI WaterAPI = new WaterModAPI();

        internal object InitObj = new object();
        internal bool Enforced;
        internal bool Inited;
        internal bool PbApiInited;

        internal bool WcActive;
        internal bool WaterMod;
        internal bool RecolourableThrust;

        internal readonly ConcurrentDictionary<long, WaterData> WaterMap = new ConcurrentDictionary<long, WaterData>();
        internal readonly ConcurrentDictionary<long, MyPlanet> PlanetMap = new ConcurrentDictionary<long, MyPlanet>();
        internal readonly ConcurrentDictionary<MyPlanet, long> PlanetTemp = new ConcurrentDictionary<MyPlanet, long>();

        private readonly List<MyEntity> _entities = new List<MyEntity>();
        private readonly ConcurrentCachingList<IMyUpgradeModule> _startBlocks = new ConcurrentCachingList<IMyUpgradeModule>();
        private readonly ConcurrentCachingList<IMyCubeGrid> _startGrids = new ConcurrentCachingList<IMyCubeGrid>();
        private readonly Stack<GroupMap> _groupMapPool = new Stack<GroupMap>(64);
        private readonly Stack<GridComp> _gridCompPool = new Stack<GridComp>(128);

        private readonly Vector3D _large = new Vector3D(1.125, 6.25, 3.5);
        private readonly Vector3D _small = new Vector3D(1.125, 6.25, 1.125);

        public StealthSession()
        {
            API = new APIBackend(this);
            APIServer = new APIServer(this);
        }

        private void Clean()
        {
            DriveDefinitions.Clear();
            SinkDefinitions.Clear();
            ShieldBlocks.Clear();

            DriveMap.Clear();
            GridMap.Clear();
            GridGroupMap.Clear();
            GridList.Clear();
            StealthedGrids.Clear();

            _entities.Clear();
            _startBlocks.ClearImmediate();
            _startGrids.ClearImmediate();
            _groupMapPool.Clear();
            _gridCompPool.Clear();

            _customControls.Clear();
            _customActions.Clear();
        }
    }
}
