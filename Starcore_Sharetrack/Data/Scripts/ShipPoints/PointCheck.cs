using DefenseShields;
using Draygo.API;
using Math0424.Networking;
using Math0424.ShipPoints;
using RelativeTopSpeed;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SENetworkAPI;
using ShipPoints.Data.Scripts.ShipPoints.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Library.Utils;
using VRageMath;
using CoreSystems.Api;
using static Math0424.Networking.MyNetworkHandler;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.Noise.Patterns;
using VRage;
using System.Linq;

namespace klime.PointCheck
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class PointCheck : MySessionComponentBase
    {

        private NetworkAPI Network => NetworkAPI.Instance;
        public const ushort ComId = 42511; public const string Keyword = "/debug"; public const string DisplayName = "Debug";
        //Old cap
        public NetSync<int> ServerSyncTimer;
        public NetSync<int> CaptainCapTimer;
        public NetSync<string> team1;
        public NetSync<string> team2;
        public NetSync<string> team3;
        public static string t_tempteam1 = "RED";
        public static string t_tempteam2 = "BLU";
        public static string t_tempteam3 = "GRE";
        public static NetSync<int> ServerMatchState;
        public static int LocalMatchState = 0;
        static bool IAmTheCaptainNow;
        public NetSync<int> ThreeTeams;
        public NetSync<int> GameModeSwitch;
        public static int Local_GameModeSwitch = 3;
        public static int Local_ProblemSwitch = 0;
        public NetSync<int> ProblemSwitch;
        public int newGameModeSwitch = 3;
        public int oldGameModeSwitch = 3;
        //
        public NetSync<Vector3D> CaptainRandVector3D;
        public Vector3D ClientRandVector3D;
        //crazy cap
        string capProgress1;
        string capProgress2;
        string capProgress3;
        T CastProhibit<T>(T ptr, object val) => (T)val;
        public static Dictionary<string, int> PointValues = new Dictionary<string, int>();
        public static WcApi WC_api { get; private set; }
        public static ShieldApi SH_api { get; private set; }
        public RtsApi RTS_api { get; private set; }

        public Vector3 ctrpoint = new Vector3(0, 0, 0);
        public Vector3 ctrpoint2 = new Vector3(4500, 1500, 9000);
        public Vector3 ctrpoint3 = new Vector3(-4500, -1500, -9000);
        public double capdist = 1000;
        public double capdistCenter = 1000;
        public Vector3 capdistScale;
        public static Dictionary<long, List<ulong>> Sending = new Dictionary<long, List<ulong>>();
        public static Dictionary<long, ShipTracker> Data = new Dictionary<long, ShipTracker>();
        public static List<long> Tracking = new List<long>();
        public string capstat = "";
        public string ZoneControl1 = ""; public string ZoneControl2 = ""; public string ZoneControl3 = ""; public string ZoneControl = "";
        private static Dictionary<long, IMyPlayer> all_players = new Dictionary<long, IMyPlayer>();
        private static List<IMyPlayer> listPlayers = new List<IMyPlayer>();
        public static int timer = 0;
        private int _fastStart;
        public NetSync<int> Team1Tickets; public NetSync<int> Team2Tickets; public NetSync<int> Team3Tickets;
        static int captimerZ1T1; static int captimerZ1T2; static int captimerZ1T3;
        static int captimerZ2T1; static int captimerZ2T2; static int captimerZ2T3;
        static int captimerZ3T1; static int captimerZ3T2; static int captimerZ3T3;
        //public NetSync<int> CaptainCapTimerZ1; public NetSync<int> CaptainCapTimerZ2; public NetSync<int> CaptainCapTimerZ3;
        public NetSync<int> CaptainCapTimerZ1T1; public NetSync<int> CaptainCapTimerZ1T2; public NetSync<int> CaptainCapTimerZ1T3;
        public NetSync<int> CaptainCapTimerZ2T1; public NetSync<int> CaptainCapTimerZ2T2; public NetSync<int> CaptainCapTimerZ2T3;
        public NetSync<int> CaptainCapTimerZ3T1; public NetSync<int> CaptainCapTimerZ3T2; public NetSync<int> CaptainCapTimerZ3T3;
        static int Capcolor1 = 0; static int Capcolor2 = 0; static int Capcolor3 = 0;
        int? NewCountT1 = 0; int? OldCountT1 = 0; int? NewCountT2 = 0; int? OldCountT2 = 0; int? NewCountT3 = 0; int? OldCountT3 = 0; int CapOut = 20;

        private readonly List<MyEntity> _managedBntities = new List<MyEntity>(1000);



        private readonly List<MyEntity> _managedEntities = new List<MyEntity>(1000);
        private int _count;
        private const double CombatRadius = 12500;
        private BoundingSphereD _combatMinSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius);
        private BoundingSphereD _combatMaxSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius + 22500);

        public enum ViewState { None, InView, InView2, GridSwitch, ExitView };
        ViewState vState = ViewState.None;

        public enum ViewStateP { ThisIsFine, ItsOver}
        ViewStateP vStateP = ViewStateP.ThisIsFine;

        HudAPIv2 text_api;
        public static HudAPIv2.HUDMessage statMessage, integretyMessage, timerMessage, ticketmessage, statMessage_Battle, statMessage_Battle_Gunlist, problemmessage;
        public static bool NameplateVisible = true; public static bool broadcaststat = false;
        public static String[] viewmode = new string[] { "Player", "Grid", "Grid & Player", "False" };
        public static int viewstat = 0;
        public static int wintime = 120;
        public static int decaytime = 180;
        public static int delaytime = 60; //debug
        public static int matchtime = 72000;
        public static int MatchTickets = 1500;
        public static int temp_ServerTimer = 0;
        public int temp_LocalTimer = 0;
        //bubble visual
        private string Capsphere1 = "\\Models\\Cubes\\InnerShield.mwm";
        private string Capsphere2 = "\\Models\\Cubes\\InnerShield.mwm";
        private string Capsphere3 = "\\Models\\Cubes\\InnerShield.mwm";
        private const string SphereModel = "\\Models\\Cubes\\InnerShield.mwm";
        private const string SphereModelOrange = "\\Models\\Cubes\\ShieldPassive07.mwm";
        private const string SphereModelBlue = "\\Models\\Cubes\\ShieldPassive08.mwm";
        private const string SphereModelRed = "\\Models\\Cubes\\ShieldPassive09.mwm";


        private MyEntity _sphereEntity; private MyEntity _sphereEntity2; private MyEntity _sphereEntity3;
        private MyEntity3DSoundEmitter _alertAudio;
        MySoundPair _ZoneCaptured = new MySoundPair("Zone_Captured");
        MySoundPair _ZoneLost = new MySoundPair("Zone_Lost");
        MySoundPair _EnemyDestroyed = new MySoundPair("Enemy_Destroyed");
        MySoundPair _TeamDestroyed = new MySoundPair("Team_Destroyed");
        bool CapsoundPlayed = false;
        bool LostCapsoundPlayed = true;
        public bool SphereVisual = true;
        bool joinInit = false;
        private const double ViewDistSqr = 306250000;

        public static Dictionary<string, bool> weaponsDictionary = new Dictionary<string, bool>
{
{"MA_T2PDX",true},
{"MA_T2PDX_Slope",true},
{"MA_T2PDX_Slope2",true},
{"MA_Gimbal_Laser_T2",true},
{"MA_Gimbal_Laser_T2_Armored",true},
{"MA_Gimbal_Laser_T2_Armored_Slope",true},
{"MA_Gimbal_Laser_T2_Armored_Slope2",true},
{"MA_Gimbal_Laser_T2_Armored_Slope45",true},
{"MA_PDX",true},
{"MA_Gimbal_Laser",true},
{"MA_Gimbal_Laser_Armored",true},
{"MA_Gimbal_Laser_Armored_Slope",true},
{"MA_Gimbal_Laser_Armored_Slope2",true},
{"MA_Gimbal_Laser_Armored_Slope45",true},
{"MA_PDT",true},
{"MA_Fixed_000",true},
{"MA_Fixed_001",true},
{"MA_Fixed_002",true},
{"MA_Fixed_007",true},
{"MA_Fixed_003",true},
{"MA_Fixed_004",true},
{"MA_Fixed_005",true},
{"MA_Fixed_006",true},
{"MA_Fixed_T2",true},
{"MA_Fixed_T2_Naked",true},
{"MA_AC150",true},
{"MA_AC150_30",true},
{"MA_AC150_45",true},
{"MA_AC150_45_Gantry",true},
{"MA_AC150_Gantry",true},
{"MA_Gladius",true},
{"MA_Fixed_T3",true},
{"MA_Tiger",true},
{"MA_Crouching_Tiger",true},
{"K_SA_Gauss_APC",true},
{"K_SA_Gauss_AMS",true},
{"K_SA_Gauss_ERC",true},
{"AMSlaser",true},
{"ARYXGaussCannon",true},
{"AWGGG",true},
{"ARYXMagnetarCannon",true},
{"ARYXPlasmaPulser",true},
{"ARYXLargeRadar",true},
{"ARYXBurstTurret",true},
{"ARYXBurstTurretSlanted",true},
{"BFG_M",true},
{"BFTriCannon",true},
{"K_SA_HeavyMetal_Gauss_ERII",true},
{"K_SA_HeavyMetal_Gauss_ERIIRF",true},
{"K_SA_Launcher_FixedMountv2",true},
{"K_SA_Launcher_FixedMount",true},
{"K_SA_LoW_CapitalSpinalA",true},
{"K_SA_HeavyMetal_Gauss_ERFM",true},
{"K_SA_HeavyMetal_Gauss_A",true},
{"K_SA_HeavyMetal_Gauss_PGBC",true},
{"ARYXTempestCannon",true},
{"ARYXLightCoilgun",true},
{"ARYXFocusLance",true},
{"ARYXRailgun",true},
{"Static150mm",true},
{"MediumFocusLance",true},
{"MA_PDX_sm",true},
{"MA_PDT_sm",true},
{"RotaryCannon",true},
{"Starcore_PPC_Block",true},
{"Starcore_AMS_II_Block",true},
{"MA_Derecho",true},
{"K_SA_Gauss_AMSIIC",true},
{"SA_HMI_Erebos",true},
{"HAS_Thanatos",true},
{"HAS_Alecto",true},
{"HAS_Assault",true},
{"HAS_Mammon",true},
{"ARYXRailgunTurret",true},
{"MCRNRailgunLB",true},
{"K_SA_HeavyMetal_Spinal_Rotary",true},
{"K_SA_HeavyMetal_Spinal_Rotary_Reskin",true},
{"MetalStorm",true},
{"Odin_Rail_2",true},
{"Odin_Rail_1",true},
{"MCRN_Heavy_Torpedo",true},
{"OPA_Heavy_Torpedo",true},
{"OPA_Light_Missile",true},
{"UNN_Heavy_Torpedo",true},
{"UNN_Light_Torpedo",true},
{"Starcore_Fixed_Coil_Cannon",true},
{"Starcore_AMS_I_Block",true},
{"Odin_Torpedo",true},
{"Odin_Missile_Battery",true},
{"K_SA_Launcher_VIV",true},
{"K_SA_Launcher_VI",true},
{"Starcore_SSRM_Block",true},
{"ModularSRM8",true},
{"Starcore_MRM_Block",true},
{"Starcore_MRM45_Block",true},
{"ModularMRM10",true},
{"ModularMiddleMRM10",true},
{"ModularMRM10Angled",true},
{"ModularMRM10AngledReversed",true},
{"ModularLRM5",true},
{"ModularLRM5Angled",true},
{"ModularMiddleLRM5",true},
{"ModularLRM5AngledReversed",true},
{"Starcore_Arrow_Block",true},
{"SC_Flare",true},
{"Odin_Laser_Fixed",true},
{"Odin_Autocannon_Fixed",true},
{"Odin_PDC",true},
{"Odin_PDC_45_Slope",true},
{"Odin_PDC_Half_Slope_Tip",true},
{"Odin_PDC_Half",true},
{"Odin_PDC_Half_Slope_Top",true},
{"Odin_Defense_1x2",true},
{"Odin_Gatling_Laser",true},
{"Odin_5x5_Cannon",true},
{"BlinkDriveLarge",true},
{"Chet_Flak_Cannon",true},
{"Odin_CoilCannon",true},
{"Odin_Autocannon_2",true},
{"Starcore_M_Laser_Block",true},
{"Starcore_L_Laser_Block",true},
{"Starcore_Basic_Warhead",true},
{"Odin_7x7_Battleshipcannon",true},
{"Odin_7x7_Battleshipcannon_Surface",true},
{"Odin_5x5_Battleshipcannon",true},
{"Odin_5x5_Battleshipcannon_Surface",true},
{"Odin_3x3_Battleshipcannon",true},
{"Odin_3x3_Battleshipcannon_Surface",true},
{"JN_175Fixed",true},
{"AMP_ArcMelee",true},
{"AMP_ArcMeleeReskin",true},
{"AMP_FlameThrower",true},
{"AMP_CryoShotgun",true},
{"Hexcannon",true},
{"HakkeroBeam",true},
{"HakkeroProjectile",true},
{"HAS_Esper",true},
{"HAS_Vulcan",true},
{"SC_Coil_Cannon",true},
{"NHI_PD_Turret",true},
{"NHI_PD_Turret_Half",true},
{"NHI_PD_Turret_Half_Slope_Top",true},
{"NHI_PD_Turret_Half_Slope_Tip",true},
{"NHI_PD_Turret_45_Slope",true},
{"NHI_Light_Autocannon_Turret",true},
{"NHI_Autocannon_Turret",true},
{"NHI_Gatling_Laser_Turret",true},
{"NHI_Light_Railgun_Turret",true},
{"NHI_Heavy_Gun_Turret",true},
{"NHI_Mk3_Cannon_Turret",true},
{"NHI_Mk3_Cannon_Surface_Turret",true},
{"NHI_Mk2_Cannon_Turret",true},
{"NHI_Mk2_Cannon_Surface_Turret",true},
{"NHI_Mk1_Cannon_Turret",true},
{"NHI_Mk1_Cannon_Surface_Turret",true},
{"NHI_Fixed_Autocannon",true},
{"NHI_Fixed_Gatling_Laser",true},
{"NHI_Kinetic_Cannon_Turret",true},
{"CLB2X",true},
{"ERPPC",true},
{"SC_COV_Plasma_Turret",true},
{"banshee",true},
{"TaiidanHangarHuge",true},
{"TaiidanHangar",true},
{"TaiidanHangarCompact",true},
{"NHI_Fixed_Missile_Battery",true},
{"HakkeroProjectileMini",true},
{"HakkeroBeamMini",true},
{"SLAM",true},
{"TaiidanHangarFighter",true},
{"TaiidanRailFighter",true},
{"TaiidanRailBomber",true},
{"TaiidanHangarBomber",true},
{"TaiidanHangarBomberMedium",true},
{"TaiidanSingleHangar",true},
{"MA_Guardian",true},
{"Laser_Block",true},
{"Torp_Block",true},
{"Heavy_Repeater",true},
{"Fixed_Rockets",true},
{"Type18_Artillery",true},
{"Type21_Artillery",true},
{"Assault_Coil_Turret",true},
{"Light_Coil_Turret",true},
{"RailgunTurret_Block",true},
{"K_HS_9x9_K3_King",true},
{"K_HS_9x9_HSRB_Dreadnight",true},
{"Null_Point_Jump_Disruptor_Large",true},
{"LargeGatlingTurret_SC",true},
{"LargeMissileTurret_SC",true},
{"LargeBlockMediumCalibreTurret_SC",true},
{"LargeCalibreTurret_SC",true},
{"LargeRailgun_SC",true},
{"LargeBlockLargeCalibreGun_SC",true},
{"LargeMissileLauncher_SC",true},
{"Starcore_RWR_Projectiles",true},
{"NID_Pyroacid",true},
{"NID_HeavyPyroacid",true},
{"NID_Bioplasma",true},
{"NID_Hivedrone",true},
{"NID_BioplasmaHivedrone",true},
{"NID_Leap",true},
{"LightParticleWhip",true},
{"ParticleWhip",true},
{"NovaCannon",true},
{"longsword",true},
{"65_Launcher_FixedMount",true},
{"Hellfire_Laser_Block",true},
{"HAS_Cyclops",true},
{"HAS_Crossfield", true},
{"PriestReskin_Block", true}
};

        public static Dictionary<string, bool> pdDictionary = new Dictionary<string, bool>
        {
            {"MA_T2PDX",true},
{"MA_T2PDX_Slope",true},
{"MA_T2PDX_Slope2",true},
{"MA_Gimbal_Laser_T2",true},
{"MA_Gimbal_Laser_T2_Armored",true},
{"MA_Gimbal_Laser_T2_Armored_Slope",true},
{"MA_Gimbal_Laser_T2_Armored_Slope2",true},
{"MA_Gimbal_Laser_T2_Armored_Slope45",true},
{"MA_PDX",true},
{"MA_Gimbal_Laser",true},
{"MA_Gimbal_Laser_Armored",true},
{"MA_Gimbal_Laser_Armored_Slope",true},
{"MA_Gimbal_Laser_Armored_Slope2",true},
{"MA_Gimbal_Laser_Armored_Slope45",true},
{"MA_PDT",true},
{"MA_Fixed_000",true},
{"MA_Fixed_001",true},
{"MA_Fixed_002",true},
{"MA_Fixed_007",true},
{"MA_Fixed_003",true},
{"MA_Fixed_004",true},
{"MA_Fixed_005",true},
{"MA_Fixed_006",true},
{"MA_Fixed_T2",true},
{"MA_Fixed_T2_Naked",true},
{"SC_Flare",true},
{"Starcore_RWR_Projectiles",true},
{"SA_HMI_Erebos",true},
{"Laser_Block",true},
{"HAS_Crossfield", true},
{"TaiidanHangarFighter",true},
{"TaiidanRailFighter",true},
{"Starcore_AMS_I_Block",true},
{"LargeGatlingTurret_SC",true},
{"PriestReskin_Block", true},
            {"Heavy_Repeater",true},
            {"NHI_PD_Turret",true},
{"NHI_PD_Turret_Half",true},
{"NHI_PD_Turret_Half_Slope_Top",true},
{"NHI_PD_Turret_Half_Slope_Tip",true},
{"NHI_PD_Turret_45_Slope",true}
        };






        private void RefreshVisualState()
        {
            var cameraPos = MyAPIGateway.Session.Camera.Position;
            double distFromCenterSqr;
            Vector3D.DistanceSquared(ref cameraPos, ref Vector3D.Zero, out distFromCenterSqr);
            if (distFromCenterSqr <= ViewDistSqr)
            {
                if (!_sphereEntity.InScene)
                {
                    _sphereEntity.InScene = true;
                    _sphereEntity.Render.UpdateRenderObject(true, false);

                    _sphereEntity2.InScene = true;
                    _sphereEntity2.Render.UpdateRenderObject(true, false);

                    _sphereEntity3.InScene = true;
                    _sphereEntity3.Render.UpdateRenderObject(true, false);
                }
            }
            else if (_sphereEntity.InScene)
            {
                _sphereEntity.InScene = false;
                _sphereEntity.Render.RemoveRenderObjects();

                _sphereEntity2.InScene = false;
                _sphereEntity2.Render.RemoveRenderObjects();

                _sphereEntity3.InScene = false;
                _sphereEntity3.Render.RemoveRenderObjects();
            }
        }



        //end visual
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.MessageEntered += MessageEntered;
            MyNetworkHandler.Init();
            MyAPIGateway.Utilities.ShowMessage("ShipPoints v3.2 - Control Zone", 
                "Aim at a grid and press Shift+T to show stats, " +
                "Shift+M to track a grid, Shift+J to cycle nametag style. " +
                "Type '/sphere' to turn off/on the sphere visuals.");

            if (!NetworkAPI.IsInitialized)
            {
                NetworkAPI.Init(ComId, DisplayName, Keyword);
            }

            InitializeNetSyncVariables();

            _alertAudio = new MyEntity3DSoundEmitter(null, false, 1f);
        }

        private void InitializeNetSyncVariables()
        {
            CaptainCapTimerZ1T1 = CreateNetSync<int>(0);
            CaptainCapTimerZ1T2 = CreateNetSync<int>(0);
            CaptainCapTimerZ1T3 = CreateNetSync<int>(0);
            CaptainCapTimerZ2T1 = CreateNetSync<int>(0);
            CaptainCapTimerZ2T2 = CreateNetSync<int>(0);
            CaptainCapTimerZ2T3 = CreateNetSync<int>(0);
            CaptainCapTimerZ3T1 = CreateNetSync<int>(0);
            CaptainCapTimerZ3T2 = CreateNetSync<int>(0);
            CaptainCapTimerZ3T3 = CreateNetSync<int>(0);

            Team1Tickets = CreateNetSync<int>(0);
            Team2Tickets = CreateNetSync<int>(0);
            Team3Tickets = CreateNetSync<int>(0);

            team1 = CreateNetSync<string>("RED");
            team2 = CreateNetSync<string>("BLU");
            team3 = CreateNetSync<string>("NEU");

            ServerMatchState = CreateNetSync<int>(0);
            ServerSyncTimer = CreateNetSync<int>(0);

            ThreeTeams = CreateNetSync<int>(0);
            GameModeSwitch = CreateNetSync<int>(3);

            //ProblemSwitch = CreateNetSync<int>(0);

            CaptainRandVector3D = CreateNetSync<Vector3D>(ClientRandVector3D);
        }

        private NetSync<T> CreateNetSync<T>(T defaultValue)
        {
            return new NetSync<T>(this, TransferType.Both, defaultValue, false, false);
        }

        private void MessageEntered(string messageText, ref bool sendToOthers)
        {


            if (messageText.ToLower() == "/shields")
            {
                Static.MyNetwork.TransmitToServer(new BasicPacket(5)); sendToOthers = false;
            }

            if (messageText.Contains("/setmatchtime"))
            {
                try
                {
                    string[] tempdist = messageText.Split(' ');
                    MyAPIGateway.Utilities.ShowNotification("Match duration changed to " + tempdist[1].ToString() + " minutes.");
                    matchtime = int.Parse(tempdist[1]) * 60 * 60;
                    sendToOthers = true;
                }
                catch (Exception)
                { MyAPIGateway.Utilities.ShowNotification("Win time not changed, try /setmatchtime xxx (in minutes)"); }
            }


            if (messageText.Contains("/setteams"))
            {
                try
                {
                    string[] tempdist = messageText.Split(' ');
                    team1.Value = tempdist[1].ToUpper(); team2.Value = tempdist[2].ToUpper(); team3.Value = tempdist[3].ToUpper();
                    //team1_Local = tempdist[1].ToUpper(); team2_Local = tempdist[2].ToUpper(); team3_Local = tempdist[3].ToUpper();
                    MyAPIGateway.Utilities.ShowNotification("Teams changed to " + tempdist[1] + " vs " + tempdist[2] + " vs " + tempdist[3]); //sendToOthers = true;
                }
                catch (Exception)
                { MyAPIGateway.Utilities.ShowNotification("Teams not changed, try /setteams abc xyz"); }
            }

            if (messageText.Contains("/sphere"))
            {
                try
                { SphereVisual = !SphereVisual; UpdateCapZone1(); UpdateCapZone2(); UpdateCapZone3(); }
                catch (Exception w) { { MyLog.Default.WriteLineAndConsole($"Visual update failed: " + w); }; }
            }

            if (messageText.Contains("/settime"))
            {
                try
                {
                    string[] tempdist = messageText.Split(' '); wintime = int.Parse(tempdist[1]);
                    MyAPIGateway.Utilities.ShowNotification("Win time changed to " + wintime.ToString());
                    sendToOthers = true;
                }
                catch (Exception)
                { MyAPIGateway.Utilities.ShowNotification("Win time not changed, try /settime xxx (in seconds)"); }
            }

            if (messageText.Contains("/setdelay"))
            {
                try
                {
                    string[] tempdist = messageText.Split(' '); delaytime = int.Parse(tempdist[1]);
                    MyAPIGateway.Utilities.ShowNotification("Delay time changed to " + delaytime.ToString() + " minutes.");
                    delaytime = delaytime * 60 * 60;
                }
                catch (Exception)
                { MyAPIGateway.Utilities.ShowNotification("Delay time not changed, try /setdelay x (in minutes)"); }
            }

            if (messageText.Contains("/setdecay"))
            {
                try
                {
                    string[] tempdist = messageText.Split(' '); decaytime = int.Parse(tempdist[1]);
                    MyAPIGateway.Utilities.ShowNotification("Decay time changed to " + decaytime.ToString());
                    decaytime = decaytime * 60;
                    sendToOthers = true;
                }
                catch (Exception)
                { MyAPIGateway.Utilities.ShowNotification("Decay time not changed, try /setdecay xxx (in seconds)"); }
            }

            if (messageText.Contains("/start"))
            {
                Static.MyNetwork.TransmitToServer(new BasicPacket(6), true, true);
                IAmTheCaptainNow = true;
                Team1Tickets.Value = MatchTickets;
                Team2Tickets.Value = MatchTickets;
                //Team3Tickets.Value = MatchTickets;
                LocalMatchState = 1;
                MyAPIGateway.Utilities.ShowMessage("GM", "You are the captain now.");

            }

            if (messageText.Contains("/end"))
            {
                Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true);
                IAmTheCaptainNow = false;
                Team1Tickets.Value = MatchTickets;
                Team2Tickets.Value = MatchTickets;
                Team3Tickets.Value = MatchTickets;
                LocalMatchState = 0;
                CaptainCapTimerZ3T1.Value = 0;
                CaptainCapTimerZ3T2.Value = 0;
                CaptainCapTimerZ3T3.Value = 0;
                CaptainCapTimerZ2T1.Value = 0;
                CaptainCapTimerZ2T2.Value = 0;
                CaptainCapTimerZ2T3.Value = 0;
                CaptainCapTimerZ1T1.Value = 0;
                CaptainCapTimerZ1T2.Value = 0;
                CaptainCapTimerZ1T3.Value = 0;
                MyAPIGateway.Utilities.ShowMessage("GM", "Match Ended.");

            }

            if (messageText.Contains("/takeover"))
            {
                IAmTheCaptainNow = true;
                MyAPIGateway.Utilities.ShowMessage("GM", "You are the captain now.");
            }
            if (messageText.Contains("/giveup"))
            {
                IAmTheCaptainNow = false;
                MyAPIGateway.Utilities.ShowMessage("GM", "You are not the captain now.");

            }

            if (messageText.Contains("/t1t"))
            {
                try
                {
                    string[] temptickets1 = messageText.Split(' ');
                    //MyAPIGateway.Utilities.ShowNotification("Match duration changed to " + temptickets[1].ToString() + " minutes.");
                    Team1Tickets.Value = int.Parse(temptickets1[1]);
                    //sendToOthers = true;
                }
                catch (Exception)
                { }
            }
            if (messageText.Contains("/t2t"))
            {
                try
                {
                    string[] temptickets2 = messageText.Split(' ');
                    //MyAPIGateway.Utilities.ShowNotification("Match duration changed to " + temptickets[1].ToString() + " minutes.");
                    Team2Tickets.Value = int.Parse(temptickets2[1]);
                    //sendToOthers = true;
                }
                catch (Exception)
                { }
            }
            if (messageText.Contains("/t3t"))
            {
                try
                {
                    string[] temptickets3 = messageText.Split(' ');
                    //MyAPIGateway.Utilities.ShowNotification("Match duration changed to " + temptickets[1].ToString() + " minutes.");
                    Team3Tickets.Value = int.Parse(temptickets3[1]);
                    //sendToOthers = true;
                }
                catch (Exception)
                {
                }


            }

            if (messageText.Contains("/threeteams"))
            {
                MyAPIGateway.Utilities.ShowMessage("GM", "Teams set to three.");
                ThreeTeams.Value = 1;
                Team3Tickets.Value = MatchTickets;
            }
            if (messageText.Contains("/twoteams"))
            {
                MyAPIGateway.Utilities.ShowMessage("GM", "Teams set to two.");
                ThreeTeams.Value = 0;

            }
            if (messageText.Contains("/crazycap"))
            {
                MyAPIGateway.Utilities.ShowMessage("GM", "Capture zones set to crazy.");
                GameModeSwitch.Value = 5;
                Local_GameModeSwitch = 5;
                Static.MyNetwork.TransmitToServer(new BasicPacket(16), true, true);
            }
            if (messageText.Contains("/nocap"))
            {
                MyAPIGateway.Utilities.ShowMessage("GM", "Capture zones set to no.");
                GameModeSwitch.Value = 4;
                Local_GameModeSwitch = 4;
                Static.MyNetwork.TransmitToServer(new BasicPacket(15), true, true);
            }
            if (messageText.Contains("/onecap"))
            {
                MyAPIGateway.Utilities.ShowMessage("GM", "Capture zones set to one.");
                GameModeSwitch.Value = 1;
                Local_GameModeSwitch = 1;
                Static.MyNetwork.TransmitToServer(new BasicPacket(12), true, true);
            }
            if (messageText.Contains("/twocap"))
            {
                MyAPIGateway.Utilities.ShowMessage("GM", "Capture zones set to two.");
                GameModeSwitch.Value = 2;
                Local_GameModeSwitch = 2;
                Static.MyNetwork.TransmitToServer(new BasicPacket(13), true, true);
            }
            if (messageText.Contains("/threecap"))
            {
                MyAPIGateway.Utilities.ShowMessage("GM", "Capture zones set to three.");
                GameModeSwitch.Value = 3;
                Local_GameModeSwitch = 3;
                Static.MyNetwork.TransmitToServer(new BasicPacket(14), true, true);
            }
            
            if (messageText.Contains("/problem"))
            {
                MyAPIGateway.Utilities.ShowNotification("A problem has been reported.", 10000);
                sendToOthers = true;

                Local_ProblemSwitch = 1;
                Static.MyNetwork.TransmitToServer(new BasicPacket(17), true, true);

            }
            if (messageText.Contains("/fixed"))
            {
                MyAPIGateway.Utilities.ShowNotification("Fixed :^)", 10000);
                sendToOthers = true;

                Local_ProblemSwitch = 0;
                Static.MyNetwork.TransmitToServer(new BasicPacket(18), true, true);

            }

        }
        public static void Begin()
        {
            temp_ServerTimer = 0;
            timer = 0;
            broadcaststat = true;
            timerMessage.Visible = true;
            ticketmessage.Visible = true;
            LocalMatchState = 1;
            captimerZ3T1 = 0;
            captimerZ3T2 = 0;
            captimerZ3T3 = 0;
            captimerZ2T1 = 0;
            captimerZ2T2 = 0;
            captimerZ2T3 = 0;
            captimerZ1T1 = 0;
            captimerZ1T2 = 0;
            captimerZ1T3 = 0;

            MyAPIGateway.Utilities.ShowNotification("Commit die. Zone activates in " + delaytime / 3600 + "m, match ends in " + matchtime / 3600 + "m.");
        }
        public static void EndMatch()
        {
            temp_ServerTimer = 0;
            timer = 0;
            broadcaststat = false;
            timerMessage.Visible = false;
            ticketmessage.Visible = false;
            LocalMatchState = 0;
            IAmTheCaptainNow = false;
            captimerZ3T1 = 0;
            captimerZ3T2 = 0;
            captimerZ3T3 = 0;
            captimerZ2T1 = 0;
            captimerZ2T2 = 0;
            captimerZ2T3 = 0;
            captimerZ1T1 = 0;
            captimerZ1T2 = 0;
            captimerZ1T3 = 0;
            MyAPIGateway.Utilities.ShowNotification("Match Ended.");
        }
        public static void TrackYourselfMyMan()
        {
            try
            {
                if (broadcaststat && MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity is IMyCockpit)
                {
                    // Clear tracking and sending lists
                    Tracking.Clear();
                    Sending.Clear();

                    // Get the controlled cockpit
                    IMyCockpit cockpit = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit;

                    // Check if the grid data exists
                    if (Data.ContainsKey(cockpit.CubeGrid.EntityId))
                    {
                        // Dispose the current HUD
                        Data[cockpit.CubeGrid.EntityId].DisposeHud();

                        // Create a packet for the grid data
                        PacketGridData packet = new PacketGridData
                        {
                            id = cockpit.CubeGrid.EntityId,
                            value = (byte)(Tracking.Contains(cockpit.CubeGrid.EntityId) ? 2 : 1),
                        };

                        // Transmit the packet to the server
                        Static.MyNetwork.TransmitToServer(packet, true, true);

                        // Update tracking and HUD based on the packet value
                        if (packet.value == 1)
                        {
                            Tracking.Add(cockpit.CubeGrid.EntityId);
                            integretyMessage.Visible = true;
                            Data[cockpit.CubeGrid.EntityId].CreateHud();
                        }
                        else
                        {
                            Tracking.Remove(cockpit.CubeGrid.EntityId);
                            Data[cockpit.CubeGrid.EntityId].DisposeHud();
                            integretyMessage.Visible = false;

                            // Create another packet to re-track if necessary
                            PacketGridData packet_B = new PacketGridData
                            {
                                id = cockpit.CubeGrid.EntityId,
                                value = (byte)(Tracking.Contains(cockpit.CubeGrid.EntityId) ? 2 : 1),
                            };

                            // Transmit the packet to the server
                            Static.MyNetwork.TransmitToServer(packet_B, true, false);

                            if (packet_B.value == 1)
                            {
                                Tracking.Add(cockpit.CubeGrid.EntityId);
                                integretyMessage.Visible = true;
                                Data[cockpit.CubeGrid.EntityId].CreateHud();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public void AddPointValues(object obj)
        {
            // Deserialize the byte array (obj) into a string (var)
            string var = MyAPIGateway.Utilities.SerializeFromBinary<string>((byte[])obj);

            // Check if the deserialization was successful
            if (var != null)
            {
                // Split the string into an array of substrings using the ';' delimiter
                string[] split = var.Split(';');

                // Iterate through each substring (s) in the split array
                foreach (string s in split)
                {
                    // Split the substring (s) into an array of parts using the '@' delimiter
                    string[] parts = s.Split('@');
                    int value;

                    // Check if there are exactly 2 parts and if the second part is a valid integer (value)
                    if (parts.Length == 2 && int.TryParse(parts[1], out value))
                    {
                        // Trim the first part (name) and remove any extra whitespaces
                        string name = parts[0].Trim();
                        int lsIndex = name.IndexOf("{LS}");

                        // Check if the name contains "{LS}"
                        if (lsIndex != -1)
                        {
                            // Replace "{LS}" with "Large" and update the PointValues dictionary
                            string largeName = name.Substring(0, lsIndex) + "Large" + name.Substring(lsIndex + "{LS}".Length);
                            PointValues[largeName] = value;

                            // Replace "{LS}" with "Small" and update the PointValues dictionary
                            string smallName = name.Substring(0, lsIndex) + "Small" + name.Substring(lsIndex + "{LS}".Length);
                            PointValues[smallName] = value;
                        }
                        else
                        {
                            // Update the PointValues dictionary directly
                            PointValues[name] = value;
                        }
                    }
                }
            }
        }
        public override void LoadData()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(2546247, AddPointValues);
            //Log.Init($"{ModContext.ModName}.log");
        }
        public override void BeforeStart()
        {
            //base.BeforeStart();
            // Check if the current instance is not a dedicated server
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                // Initialize the sphere entities


                // Initialize the text_api with the HUDRegistered callback
                text_api = new HudAPIv2(HUDRegistered);
            }

            // Initialize the WC_api and load it if it's not null

            WC_api = new WcApi();
            if (WC_api != null)
            {
                WC_api.Load();
            }

            // Initialize the SH_api and load it if it's not null
            SH_api = new ShieldApi();
            if (SH_api != null)
            {
                SH_api.Load();
            }

            // Initialize the RTS_api and load it if it's not null
            RTS_api = new RtsApi();
            if (RTS_api != null)
            {
                RTS_api.Load();
            }
        }

        private void HUDRegistered()    
        {
            statMessage = new HudAPIv2.HUDMessage(Scale: 1f, Font: "BI_SEOutlined", Message: new StringBuilder(""), Origin: new Vector2D(-.99, .99), HideHud: false, Blend: BlendTypeEnum.PostPP)
            {
                //Blend = BlendTypeEnum.PostPP,
                Visible = false, //defaulted off?
                InitialColor = Color.Orange,
                //ShadowColor = Color.Black,
            };
            statMessage_Battle = new HudAPIv2.HUDMessage(Scale: 1.25f, Font: "BI_SEOutlined", Message: new StringBuilder(""), Origin: new Vector2D(-.54, -0.955), HideHud: false, Blend: BlendTypeEnum.PostPP)
            {
                //Blend = BlendTypeEnum.PostPP,
                Visible = false, //defaulted off?
                //ShadowColor = Color.Black,
            };
            statMessage_Battle_Gunlist = new HudAPIv2.HUDMessage(Scale: 1.25f, Font: "BI_SEOutlined", Message: new StringBuilder(""), Origin: new Vector2D(-.99, .99), HideHud: false, Shadowing: true, Blend: BlendTypeEnum.PostPP)
            {
                //Blend = BlendTypeEnum.PostPP,
                Visible = false, //defaulted off?
                //ShadowColor = Color.Black,
            };
            integretyMessage = new HudAPIv2.HUDMessage(Scale: 1.15f, Font: "BI_SEOutlined", Message: new StringBuilder(""), Origin: new Vector2D(.51, .95), HideHud: false, Blend: BlendTypeEnum.PostPP)
            {
                Visible = false,
                //InitialColor = Color.Orange
            };
            timerMessage = new HudAPIv2.HUDMessage(Scale: 1.2f, Font: "BI_SEOutlined", Message: new StringBuilder(""), Origin: new Vector2D(0.35, .99), HideHud: false, Shadowing: true, Blend: BlendTypeEnum.PostPP)
            {
                Visible = false, //defaulted off?
                InitialColor = Color.White,
                //ShadowColor = Color.Black
            };
            ticketmessage = new HudAPIv2.HUDMessage(Scale: 1f, Font: "BI_SEOutlined", Message: new StringBuilder(""), Origin: new Vector2D(0.51, .99), HideHud: false, Shadowing: true, Blend: BlendTypeEnum.PostPP)
            {
                Visible = false, //defaulted off?
                InitialColor = Color.White,
                //ShadowColor = Color.Black
            };

            problemmessage = new HudAPIv2.HUDMessage(Scale: 2f, Font: "BI_SEOutlined", Message: new StringBuilder(""), Origin: new Vector2D(-.99, 0), HideHud: false, Shadowing: true, Blend: BlendTypeEnum.PostPP)
            {
                Visible = false, //defaulted off?
                InitialColor = Color.White,
                //ShadowColor = Color.Black
            };
        }
        // Get the sphere model based on the given cap color
        private string GetCapZoneColorModel(int capColor)
        {
            switch (capColor)
            {
                case 1:
                    return SphereModelRed;
                case 2:
                    return SphereModelBlue;
                case 3:
                    return SphereModelOrange;
                default:
                    return SphereModel;
            }
        }

        internal void CapZoneColor1()
        {
            try
            {
                // Update Capsphere1 based on Capcolor1
                Capsphere1 = GetCapZoneColorModel(Capcolor1);
            }
            catch (Exception B1)
            {
                MyLog.Default.WriteLineAndConsole($"Visual update failed: " + B1);
            }
        }

        internal void CapZoneColor2()
        {
            try
            {
                // Update Capsphere2 based on Capcolor2
                Capsphere2 = GetCapZoneColorModel(Capcolor2);
            }
            catch (Exception B2)
            {
                MyLog.Default.WriteLineAndConsole($"Visual update failed: " + B2);
            }
        }

        internal void CapZoneColor3()
        {
            try
            {
                // Update Capsphere3 based on Capcolor3
                Capsphere3 = GetCapZoneColorModel(Capcolor3);
            }
            catch (Exception B3)
            {
                MyLog.Default.WriteLineAndConsole($"Visual update failed: " + B3);
            }
        }


        // Update the given sphere entity with the given Capsphere
        private void UpdateCapZone(MyEntity sphereEntity, string Capsphere)
        {
            if (sphereEntity == null) return;

            try
            {
                // Set the visibility of the sphere entity
                sphereEntity.Render.Visible = SphereVisual;

                // Refresh the model of the sphere entity
                sphereEntity.RefreshModels($"{ModContext.ModPath}{Capsphere}", null);

                // Remove and update the render object
                sphereEntity.Render.RemoveRenderObjects();
                sphereEntity.Render.UpdateRenderObject(true);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Updating Capzone failed: {ex}");
            }
        }

        internal void UpdateCapZone1()
        {
            // Update the first cap zone
            UpdateCapZone(_sphereEntity, Capsphere1);
        }

        internal void UpdateCapZone2()
        {
            // Update the second cap zone
            UpdateCapZone(_sphereEntity2, Capsphere2);
        }

        internal void UpdateCapZone3()
        {
            // Update the third cap zone
            UpdateCapZone(_sphereEntity3, Capsphere3);
        }

        public override void UpdateAfterSimulation()
        {

            temp_LocalTimer++;
            timer++;

            if (timer >= 144000)
            {
                timer = 0;
                temp_LocalTimer = 0;
                temp_ServerTimer = 0;
            }




            if (joinInit == true)
            {

            }


            if (MyAPIGateway.Utilities.IsDedicated && temp_ServerTimer % 60 == 0) //server update of ServerSyncTimer
            {
                ServerSyncTimer.Value = temp_ServerTimer;
                ServerSyncTimer.Push();

            }

            if (broadcaststat && !IAmTheCaptainNow && temp_LocalTimer % 60 == 0)
            //if you are not the captain or the server and your local timer 1 second, set your local time to server time
            {
                ServerSyncTimer.Fetch();
                timer = ServerSyncTimer.Value;
                temp_LocalTimer = 0;
            }




            try
            {
                if (!MyAPIGateway.Utilities.IsDedicated) //visual update of the spheres and loading hitch fix
                {
                    try
                    {
                        var tick100 = timer % 100 == 0;
                        if (timer - _fastStart < 300 || tick100)
                        {
                            RefreshVisualState();
                            _fastStart = timer;
                            if (joinInit == false)
                            {
                                Static.MyNetwork.TransmitToServer(new BasicPacket(7), true, true);

                                ServerMatchState.Fetch();
                                team1.Fetch();
                                team2.Fetch();
                                team3.Fetch();
                                ServerMatchState.Fetch(); ServerSyncTimer.Fetch();
                                Team1Tickets.Fetch();
                                Team2Tickets.Fetch();
                                Team3Tickets.Fetch();
                                ThreeTeams.Fetch();
                                GameModeSwitch.Fetch();
                                Local_GameModeSwitch = GameModeSwitch.Value;
                                joinInit = true;

                            }

                        }
                    }
                    catch { }
                }



                if (!MyAPIGateway.Utilities.IsDedicated && timer % 60 == 0)
                {
                    try
                    {
                        if (ServerMatchState.Value == 1 && broadcaststat == false) //force broadcaststat to match servermatch state
                        {
                            broadcaststat = true;
                        }

                        if (!MyAPIGateway.Utilities.IsDedicated && IAmTheCaptainNow) //force ServerMatchState to match LocalMatchState
                        {
                            ServerMatchState.Value = LocalMatchState;
                            //ServerMatchState.Push();
                        }
                        else if (!MyAPIGateway.Utilities.IsDedicated && !IAmTheCaptainNow) //force LocalMatchState to match ServerMatchState

                        {
                            LocalMatchState = ServerMatchState.Value;
                        }

                    }
                    catch
                    {
                    }
                }

                if (broadcaststat && timer % 60 == 0)
                {
                    if (IAmTheCaptainNow && ServerMatchState.Value != 1)
                    {
                        ServerMatchState.Value = 1;

                    }
                }

            }
            catch { }
            try
            {
                // Execute this block of code only if the timer is a multiple of 60
                if (timer % 60 == 0)
                {
                    // Clear the players lists and populate the all_players dictionary in one loop
                    all_players.Clear();
                    MyAPIGateway.Multiplayer.Players.GetPlayers(listPlayers, p =>
                    {
                        all_players.Add(p.IdentityId, p);
                        return false;
                    });

                    // Execute this block of code only if it's a server
                    if (MyAPIGateway.Session.IsServer)
                    {
                        // Iterate through each entity ID (x) in the Sending dictionary
                        foreach (var x in Sending.Keys)
                        {
                            ShipTracker shipTracker;
                            if (!Data.TryGetValue(x, out shipTracker))
                            {
                                // Get the entity with the entity ID (x) and check if it's a valid grid with physics
                                var e = MyEntities.GetEntityById(x) as IMyCubeGrid;
                                if (e != null && e.Physics != null)
                                {
                                    shipTracker = new ShipTracker(e);
                                    Data.Add(x, shipTracker);

                                    // If it's not a dedicated server, create a HUD for the grid
                                    if (!MyAPIGateway.Utilities.IsDedicated)
                                    {
                                        shipTracker.CreateHud();
                                    }
                                }
                            }
                            else
                            {
                                // Update the ShipTracker for the entity ID (x)
                                shipTracker.Update();
                            }

                            if (shipTracker != null)
                            {
                                // Iterate through each player ID (p) in the Sending dictionary for the entity ID (x)
                                foreach (var p in Sending[x])
                                {
                                    // Create a new packet with the entity ID and its ShipTracker
                                    PacketGridData packet = new PacketGridData
                                    {
                                        id = x,
                                        tracked = shipTracker
                                    };

                                    // Transmit the packet to the player with player ID (p)
                                    Static.MyNetwork.TransmitToPlayer(packet, p);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            try
            {
                // Run this block of code only if the timer is a multiple of 60 and it's not a dedicated server
                if (timer % 60 == 0)
                {
                    // Check if _count is a multiple of 100
                    bool tick100 = _count % 100 == 0;
                    _count++;

                    // Run this block of code if (_count - _fastStart) is less than 300 or _count is a multiple of 100
                    if (_count - _fastStart < 300 || tick100)
                    {
                        // Clear the managed entities list
                        _managedEntities.Clear();

                        // Get all top-most entities in a sphere (_combatMaxSphere) and store them in _managedEntities
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref _combatMaxSphere, _managedEntities, MyEntityQueryType.Dynamic);
                        var posZero = Vector3D.Zero;

                        // Iterate through each entity in the _managedEntities list
                        foreach (var entity in _managedEntities)
                        {
                            // Check if the entity is a grid
                            var grid = entity as MyCubeGrid;

                            // If the entity is a valid grid and has the specified block subtype ID, perform the following actions
                            if (grid != null && GridExtensions.HasBlockWithSubtypeId(grid, "RivalAIRemoteControlLarge"))
                            {
                                // Get the entity ID of the grid
                                long entityId = grid.EntityId;

                                // If the entity ID is not present in the Tracking list, perform the following actions
                                if (!Tracking.Contains(entityId))
                                {
                                    // Create a new packet with the entity ID and a value indicating whether the entity is in the Tracking list
                                    PacketGridData packet = new PacketGridData
                                    {
                                        id = entityId,
                                        value = (byte)(Tracking.Contains(entityId) ? 2 : 1),
                                    };

                                    // Transmit the packet to the server
                                    Static.MyNetwork.TransmitToServer(packet, true);


                                    // If the packet value is 1 (indicating the entity is not in the Tracking list), perform the following actions
                                    if (packet.value == 1)
                                    {
                                        // Show a notification that the grid was added to the tracker
                                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Added grid to tracker");

                                        // Add the entity ID to the Tracking list
                                        Tracking.Add(entityId);

                                        // Make the integrity message visible if it's not already visible
                                        if (!integretyMessage.Visible)
                                        {
                                            integretyMessage.Visible = true;
                                        }

                                        // Create the HUD for the grid
                                        Data[entityId].CreateHud();
                                    }
                                    else
                                    {
                                        // Show a notification that the grid was removed from the tracker
                                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Removed grid from tracker");

                                        // Remove the entity ID from the Tracking list
                                        Tracking.Remove(entityId);

                                        // Dispose the HUD for the grid
                                        Data[entityId].DisposeHud();
                                    }
                                }
                                // Update _fastStart to the current value of _count
                                _fastStart = _count;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        public override void Draw()
        { //if you are the server do nothing here
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }
            try
            {
                if (
                    MyAPIGateway.Session?.Camera != null 
                    && MyAPIGateway.Session.CameraController != null 
                    && !MyAPIGateway.Gui.ChatEntryVisible 
                    && !MyAPIGateway.Gui.IsCursorVisible 
                    && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
                {
                    /*
                    if (vState == ViewState.InView) { vState = ViewState.ExitView; }*/ //this disables shift T when match is go

                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.T))
                    {
                        if (vState == ViewState.None)
                        {
                            vState = ViewState.InView;
                        }
                        else if (vState == ViewState.InView)
                        {
                            vState = ViewState.InView2;
                        }
                        else if (vState == ViewState.InView2)
                        {
                            vState = ViewState.ExitView;
                        }
                    }

                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.Shift) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.M))
                    {
                        if (MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
                        {
                            var camMat = MyAPIGateway.Session.Camera.WorldMatrix;
                            IHitInfo hits = null; MyAPIGateway.Physics.CastRay(camMat.Translation + camMat.Forward * 0.5, camMat.Translation + camMat.Forward * 500, out hits);
                            if (hits != null && hits.HitEntity is IMyCubeGrid)
                            {
                                PacketGridData packet = new PacketGridData
                                {
                                    id = hits.HitEntity.EntityId,
                                    value = (byte)(Tracking.Contains(hits.HitEntity.EntityId) ? 2 : 1),
                                };
                                Static.MyNetwork.TransmitToServer(packet, true);
                                if (packet.value == 1)
                                {
                                    MyAPIGateway.Utilities.ShowNotification("ShipTracker: Added grid to tracker"); Tracking.Add(hits.HitEntity.EntityId);
                                    if (integretyMessage.Visible == false) { integretyMessage.Visible = true; }
                                    //fix for disappearing nameplates?
                                    Data[hits.HitEntity.EntityId].CreateHud();
                                    //end fix
                                }
                                else
                                {
                                    MyAPIGateway.Utilities.ShowNotification("ShipTracker: Removed grid from tracker");
                                    Tracking.Remove(hits.HitEntity.EntityId); Data[hits.HitEntity.EntityId].DisposeHud();
                                }
                            }
                        }
                    }
                    
                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.Shift) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.N))
                    {
                        if (MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
                        {
                            integretyMessage.Visible = !integretyMessage.Visible;
                            MyAPIGateway.Utilities.ShowNotification("ShipTracker: Hud visibility set to " + integretyMessage.Visible);
                        }
                    }

                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.Shift) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.B))
                    {
                        if (MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
                        { //integretyMessage.Visible = !integretyMessage.Visible;
                            timerMessage.Visible = !timerMessage.Visible; ticketmessage.Visible = !ticketmessage.Visible;
                            MyAPIGateway.Utilities.ShowNotification("ShipTracker: Timer visibility set to " + timerMessage.Visible);
                        }
                    }

                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.Shift) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.J))
                    {
                        if (MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
                        {
                            viewstat++;
                            if (viewstat == 4)
                            {
                                viewstat = 0;
                            }
                            if (viewstat == 3)
                            {
                                NameplateVisible = false;
                            }
                            else
                            {
                                NameplateVisible = true;
                            }
                            MyAPIGateway.Utilities.ShowNotification("ShipTracker: Nameplate visibility set to " + viewmode[viewstat]);
                        }
                    }
                
                    if (Local_ProblemSwitch == 1)
                    {
                        if (vStateP == ViewStateP.ThisIsFine) { vStateP = ViewStateP.ItsOver; }
                        
                    }
                    if (Local_ProblemSwitch == 0)
                    {
                        if (vStateP == ViewStateP.ItsOver) { vStateP = ViewStateP.ThisIsFine; }

                    }

                }

                if (text_api.Heartbeat)
                {
                    foreach (var x in Data.Keys)
                    {
                        if (Tracking.Contains(x))
                        {
                            Data[x].UpdateHud();
                        }
                        else
                        {
                            Data[x].DisposeHud();
                        }
                    }
                }

                if (vStateP == ViewStateP.ItsOver && problemmessage != null && text_api.Heartbeat)
                {
                    var temp_text = "<color=Red>" + "A PROBLEM HAS BEEN REPORTED," + "\n" + "CHECK WITH BOTH TEAMS AND THEN TYPE '/fixed' TO CLEAR THIS MESSAGE";

                    problemmessage.Message.Clear(); problemmessage.Message.Append(temp_text); problemmessage.Visible = true;
                }

                if (vStateP == ViewStateP.ThisIsFine && problemmessage != null && text_api.Heartbeat)
                {
                    problemmessage.Message.Clear(); problemmessage.Visible = false;
                }

                if (vState == ViewState.InView && statMessage != null && text_api.Heartbeat) //shift T menu
                {
                    var cockpit = MyAPIGateway.Session.ControlledObject?.Entity as IMyCockpit;
                    if (cockpit == null || MyAPIGateway.Session.IsCameraUserControlledSpectator)
                    {
                        //user is not in cockpit
                        //MyAPIGateway.Utilities.ShowNotification("NOTCOCKPIT");
                        var camMat = MyAPIGateway.Session.Camera.WorldMatrix; IHitInfo hits = null;
                        MyAPIGateway.Physics.CastRay(camMat.Translation + camMat.Forward * 0.5, camMat.Translation + camMat.Forward * 500, out hits);
                        if (hits != null && hits.HitEntity is IMyCubeGrid)
                        {
                            IMyCubeGrid icubeG = hits.HitEntity as IMyCubeGrid;
                            if (icubeG != null && icubeG.Physics != null)
                            {
                                if (timer % 60 == 0)
                                {

                                    ShipTracker tracked = new ShipTracker(icubeG);

                                    string pdInvestment = tracked.pdPercentage.ToString();
                                    string pdInvestmentNum = tracked.pdInvest.ToString();

                                    string total_shield_string = "None";
                                    if (tracked.TotalShieldStrength > 100)
                                    { total_shield_string = Math.Round((tracked.TotalShieldStrength / 100f), 2).ToString() + " M"; }
                                    if (tracked.TotalShieldStrength > 1 && tracked.TotalShieldStrength < 100)
                                    {
                                        total_shield_string = Math.Round((tracked.TotalShieldStrength), 0).ToString() + "0 K";
                                    }
                                    string gunText = "";
                                    foreach (var x in tracked.GunL.Keys)
                                    {
                                        gunText += "<color=Green>" + tracked.GunL[x] + "<color=White> x " + x + "\n";
                                    }
                                    string specialBlockText = "";
                                    foreach (var x in tracked.SBL.Keys)
                                    {
                                        specialBlockText += "<color=Green>" + tracked.SBL[x] + "<color=White> x " + x + "\n";
                                    }

                                    string massString = tracked.Mass.ToString();
                                    //float thrust = icubeG.GetMaxThrustInDirection(Base6Directions.Direction.Forward);
                                    float thrustInKilograms = icubeG.GetMaxThrustInDirection(Base6Directions.Direction.Backward) / 9.81f; // Convert thrust from N to kg
                                    float weight = tracked.Mass;

                                    float mass = tracked.Mass;
                                    float TWR = thrustInKilograms / weight;

                                    if (tracked.Mass > 1000000)
                                    {
                                        massString = Math.Round((tracked.Mass / 1000000f), 2).ToString() + "m";
                                        mass = tracked.Mass / 1000f; // Convert mass to metric tons
                                    }


                                    string TWRs = Math.Round((TWR),3).ToString();

                                    string thrustString = tracked.InstalledThrust.ToString();
                                    if (tracked.InstalledThrust > 1000000)
                                    {
                                        thrustString = Math.Round((tracked.InstalledThrust / 1000000f), 2).ToString() + "M";
                                    }

                                    string playerName = tracked.Owner == null ? tracked.GridName : tracked.Owner.DisplayName;

                                    //if (!string.IsNullOrEmpty(playerName) && playerName != tracked.GridName)
                                    // {
                                    //    playerName = playerName.Substring(1);
                                    // }

                                    string factionName = tracked.Owner == null ? "" : MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(tracked.OwnerID)?.Name;
                                    float speed = icubeG.GridSizeEnum == MyCubeSize.Large ? MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
                                    if (RTS_api != null && RTS_api.IsReady)
                                    {
                                        speed = (float)Math.Round(RTS_api.GetMaxSpeed(icubeG), 2);

                                    }



                                    string PWRNotation;

                                    PWRNotation = tracked.CurrentPower > 1000 ? "GW" : "MW";
                                    string tempPWR;
                                    if (tracked.CurrentPower > 1000)
                                    { tempPWR = (Math.Round(tracked.CurrentPower / 1000, 1)).ToString(); }
                                    else { tempPWR = tracked.CurrentPower.ToString(); }
                                    string PWR = tempPWR + PWRNotation;

                                    string GyroString = tracked.CurrentGyro.ToString();
                                    double tempGyro2;
                                    if (tracked.CurrentGyro >= 1000000)
                                    {
                                        tempGyro2 = Math.Round((tracked.CurrentGyro / 1000000f), 1);
                                        if (tempGyro2 > 1000) { GyroString = Math.Round((tempGyro2 / 1000), 1).ToString() + "G"; }
                                        else { GyroString = tempGyro2.ToString() + "M"; }
                                    }


                                    var temp_text =
                                            "----Basic Info----"
                                            + "\n" + "<color=Green>Name<color=White>: " + icubeG.DisplayName
                                            + "\n" + "<color=Green>Owner<color=White>: " + playerName
                                            + "\n" + "<color=Green>Faction<color=White>: " + factionName
                                            + "\n" + "<color=Green>Mass<color=White>: " + massString + " kg"
                                            + "\n" + "<color=Green>Heavy blocks<color=White>: " + tracked.Heavyblocks.ToString()
                                            + "\n" + "<color=Green>Total blocks<color=White>: " + tracked.BlockCount.ToString()
                                            + "\n" + "<color=Green>PCU<color=White>: " + tracked.PCU
                                            + "\n" + "<color=Green>Size<color=White>: " + (icubeG.Max + Vector3.Abs(icubeG.Min)).ToString()
                                            + "\n" + "<color=Green>Max Speed<color=White>: " + speed + " | <color=Green>TWR<color=White>: " + TWRs
                                            + "\n"
                                            + "\n" + "<color=Orange>----Battle Stats----"

                                            + "\n" + "<color=Green>Battle Points<color=White>: " + tracked.Bpts.ToString()
                                            + " <color=Orange>[<color=Red> " + tracked.offensivePercentage.ToString()
                                            + "<color=White>% <color=Orange>| <color=Green>"
                                            + tracked.powerPercentage.ToString()
                                            + "<color=White>% <color=Orange>| <color=DeepSkyBlue>"
                                            + tracked.movementPercentage.ToString()
                                            + "<color=White>% <color=Orange>| <color=LightGray>"
                                            + tracked.miscPercentage.ToString()
                                            + "<color=White>% <color=Orange>]"
                                            + "\n" + "<color=Green>PD Investment<color=White>: " + "<color=Orange>( <color=white>" + pdInvestmentNum + " <color=Orange>|<color=Crimson> " + pdInvestment + "<color=White>%<color=Orange> )"
                                            + "\n" + "<color=Green>Shield Max HP<color=White>: " + total_shield_string + " (" + (int)tracked.CurrentShieldStrength + "%)"
                                            + "\n" + "<color=Green>Thrust<color=White>: " + thrustString + "N"
                                            + "\n" + "<color=Green>Gyro<color=White>: " + GyroString + "N"
                                            + "\n" + "<color=Green>Power<color=White>: " + PWR
                                            + "\n"
                                            + "\n" + "<color=Orange>----Blocks----"
                                            + "\n" + specialBlockText
                                            + "\n"
                                            + "\n" + "<color=Orange>----Armament----"
                                            + "\n" + gunText;
                                    statMessage.Message.Clear(); statMessage.Message.Append(temp_text); statMessage.Visible = true;
                                }
                            }
                        }
                        else
                        {
                            if (statMessage != null && text_api.Heartbeat && statMessage.Visible)
                            {
                                statMessage.Message.Clear(); statMessage.Visible = false;
                            }
                        }
                    }
                    else
                    {
                        // user is in cockpit

                        //MyAPIGateway.Utilities.ShowNotification("INCOCKPIT");
                        IMyCubeGrid icubeG = cockpit.CubeGrid as IMyCubeGrid;
                        if (icubeG != null && icubeG.Physics != null)
                        {
                            if (timer % 60 == 0)
                            {

                                ShipTracker tracked = new ShipTracker(icubeG);
                                string pdInvestment = tracked.pdPercentage.ToString();
                                string pdInvestmentNum = tracked.pdInvest.ToString();

                                string total_shield_string = "None";
                                if (tracked.TotalShieldStrength > 100)
                                { total_shield_string = Math.Round((tracked.TotalShieldStrength / 100f), 2).ToString() + " M"; }
                                if (tracked.TotalShieldStrength > 1 && tracked.TotalShieldStrength < 100)
                                {
                                    total_shield_string = Math.Round((tracked.TotalShieldStrength), 0).ToString() + "0 K";
                                }
                                string gunText = "";
                                foreach (var x in tracked.GunL.Keys)
                                {
                                    gunText += "<color=Green>" + tracked.GunL[x] + "<color=White> x " + x + "\n";
                                }
                                string specialBlockText = "";
                                foreach (var x in tracked.SBL.Keys)
                                {
                                    specialBlockText += "<color=Green>" + tracked.SBL[x] + "<color=White> x " + x + "\n";
                                }
                                string massString = tracked.Mass.ToString();
                                //float thrust = icubeG.GetMaxThrustInDirection(Base6Directions.Direction.Forward);
                                float thrustInKilograms = icubeG.GetMaxThrustInDirection(Base6Directions.Direction.Backward) / 9.81f; // Convert thrust from N to kg
                                float weight = tracked.Mass; 

                                float mass = tracked.Mass;
                                float TWR = thrustInKilograms / weight;

                                if (tracked.Mass > 1000000)
                                {
                                    massString = Math.Round((tracked.Mass / 1000000f), 2).ToString() + "m";
                                    mass = tracked.Mass / 1000f; // Convert mass to metric tons
                                }


                                string TWRs = Math.Round((TWR), 3).ToString();

                                string thrustString = tracked.InstalledThrust.ToString();
                                if (tracked.InstalledThrust > 1000000)
                                {
                                    thrustString = Math.Round((tracked.InstalledThrust / 1000000f), 2).ToString() + "M";
                                }
                                string playerName = tracked.Owner == null ? tracked.GridName : tracked.Owner.DisplayName;

                                //  if (!string.IsNullOrEmpty(playerName) && playerName != tracked.GridName)
                                //  {
                                //     playerName = playerName.Substring(1);
                                // }
                                string factionName = tracked.Owner == null ? "" : MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(tracked.OwnerID)?.Name;
                                float speed = icubeG.GridSizeEnum == MyCubeSize.Large ? MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
                                if (RTS_api != null && RTS_api.IsReady)
                                {
                                    speed = (float)Math.Round(RTS_api.GetMaxSpeed(icubeG), 2);

                                }
                                string PWRNotation;


                                PWRNotation = tracked.CurrentPower > 1000 ? "GW" : "MW";
                                string tempPWR;
                                if (tracked.CurrentPower > 1000)
                                { tempPWR = (Math.Round(tracked.CurrentPower / 1000, 1)).ToString(); }
                                else { tempPWR = tracked.CurrentPower.ToString(); }
                                string PWR = tempPWR + PWRNotation;

                                string GyroString = tracked.CurrentGyro.ToString();
                                double tempGyro2;
                                if (tracked.CurrentGyro >= 1000000)
                                {
                                    tempGyro2 = Math.Round((tracked.CurrentGyro / 1000000f), 1);
                                    if (tempGyro2 > 1000) { GyroString = Math.Round((tempGyro2 / 1000), 1).ToString() + "G"; }
                                    else { GyroString = tempGyro2.ToString() + "M"; }
                                }


                                var temp_text =
                                        "----Basic Info----"
                                        + "\n" + "<color=Green>Name<color=White>: " + icubeG.DisplayName
                                        + "\n" + "<color=Green>Owner<color=White>: " + playerName
                                        + "\n" + "<color=Green>Faction<color=White>: " + factionName
                                        + "\n" + "<color=Green>Mass<color=White>: " + massString + " kg"
                                        + "\n" + "<color=Green>Heavy blocks<color=White>: " + tracked.Heavyblocks.ToString()
                                        + "\n" + "<color=Green>Total blocks<color=White>: " + tracked.BlockCount.ToString()
                                        + "\n" + "<color=Green>PCU<color=White>: " + tracked.PCU
                                        + "\n" + "<color=Green>Size<color=White>: " + (icubeG.Max + Vector3.Abs(icubeG.Min)).ToString()
                                        + "\n" + "<color=Green>Max Speed<color=White>: " + speed + " | <color=Green>TWR<color=White>: " + TWRs
                                        + "\n"
                                        + "\n" + "<color=Orange>----Battle Stats----"
                                            + "\n" + "<color=Green>Battle Points<color=White>: " + tracked.Bpts.ToString()
                                            + " <color=Orange>[<color=Red> " + tracked.offensivePercentage.ToString()
                                            + "<color=White>% <color=Orange>| <color=Green>"
                                            + tracked.powerPercentage.ToString()
                                            + "<color=White>% <color=Orange>| <color=DeepSkyBlue>"
                                            + tracked.movementPercentage.ToString()
                                            + "<color=White>% <color=Orange>| <color=LightGray>"
                                            + tracked.miscPercentage.ToString()
                                            + "<color=White>% <color=Orange>]"

                                        + "\n" + "<color=Green>PD Investment<color=White>: " + "<color=Orange>( <color=white>" + pdInvestmentNum + " <color=Orange>|<color=Crimson> " + pdInvestment + "<color=White>%<color=Orange> )"
                                        + "\n" + "<color=Green>Shield Max HP<color=White>: " + total_shield_string + " (" + (int)tracked.CurrentShieldStrength + "%)"
                                        + "\n" + "<color=Green>Thrust<color=White>: " + thrustString + "N"
                                        + "\n" + "<color=Green>Gyro<color=White>: " + GyroString + "N"
                                        + "\n" + "<color=Green>Power<color=White>: " + PWR

                                        + "\n"
                                        + "\n" + "<color=Orange>----Blocks----"
                                        + "\n" + specialBlockText
                                        + "\n"
                                        + "\n" + "<color=Orange>----Armament----"
                                        + "\n" + gunText;
                                statMessage.Message.Clear(); statMessage.Message.Append(temp_text); statMessage.Visible = true;
                            }
                        }

                        else
                        {
                            if (statMessage != null && text_api.Heartbeat && statMessage.Visible)
                            {
                                statMessage.Message.Clear(); statMessage.Visible = false;
                            }
                        }
                    }



                }

                if (vState == ViewState.InView2 && statMessage_Battle != null && text_api.Heartbeat) //shift T menu
                {
                    if (statMessage != null && text_api.Heartbeat)
                    {
                        if (statMessage.Visible)
                        {
                            statMessage.Message.Clear();
                            statMessage.Visible = false;
                        }
                    }
                    var cockpit = MyAPIGateway.Session.ControlledObject?.Entity as IMyCockpit;
                    if (cockpit == null || MyAPIGateway.Session.IsCameraUserControlledSpectator)
                    {
                        //user not in cockpit
                        var camMat = MyAPIGateway.Session.Camera.WorldMatrix; IHitInfo hits = null;
                        MyAPIGateway.Physics.CastRay(camMat.Translation + camMat.Forward * 0.5, camMat.Translation + camMat.Forward * 500, out hits);
                        if (hits != null && hits.HitEntity is IMyCubeGrid)
                        {
                            IMyCubeGrid icubeG = hits.HitEntity as IMyCubeGrid;
                            if (icubeG != null && icubeG.Physics != null)
                            {
                                if (timer % 60 == 0)
                                {
                                    ShipTracker tracked = new ShipTracker(icubeG);
                                    string total_shield_string = "None";
                                    if (tracked.TotalShieldStrength > 100)
                                    { total_shield_string = Math.Round((tracked.TotalShieldStrength / 100f), 2).ToString() + " M"; }
                                    if (tracked.TotalShieldStrength > 1 && tracked.TotalShieldStrength < 100)
                                    { total_shield_string = Math.Round((tracked.TotalShieldStrength), 0).ToString() + "0 K"; }
                                    var temp_text = "<color=White>" + total_shield_string + " (" + (int)tracked.CurrentShieldStrength + "%)";

                                    string GyroString = tracked.CurrentGyro.ToString();
                                    double tempGyro2;
                                    if (tracked.CurrentGyro >= 1000000)
                                    {
                                        tempGyro2 = Math.Round((tracked.CurrentGyro / 1000000f), 1);
                                        if (tempGyro2 > 1000) { GyroString = Math.Round((tempGyro2 / 1000), 1).ToString() + "G"; }
                                        else { GyroString = tempGyro2.ToString() + "M"; }
                                    }

                                    string thrustString = tracked.InstalledThrust.ToString();
                                    if (tracked.InstalledThrust > 1000000)
                                    {
                                        thrustString = Math.Round((tracked.InstalledThrust / 1000000f), 2).ToString() + "M";
                                    }
                                    string PWRNotation;

                                    PWRNotation = tracked.CurrentPower > 1000 ? "GW" : "MW";
                                    string tempPWR;
                                    if (tracked.CurrentPower > 1000)
                                    { tempPWR = (Math.Round(tracked.CurrentPower / 1000, 1)).ToString(); }
                                    else { tempPWR = tracked.CurrentPower.ToString(); }
                                    string PWR = tempPWR + PWRNotation;
                                    string gunText = "";
                                    foreach (var x in tracked.GunL.Keys)
                                    {
                                        gunText += "<color=Green>" + tracked.GunL[x] + "<color=White> x " + x + "\n";

                                    }
                                    gunText += "\n" + "<color=Green>Thrust<color=White>: " + thrustString + "N"
                                             + "\n" + "<color=Green>Gyro<color=White>: " + GyroString + "N"
                                             + "\n" + "<color=Green>Power<color=White>: " + PWR;


                                    statMessage_Battle_Gunlist.Message.Clear(); statMessage_Battle_Gunlist.Message.Append(gunText);
                                    statMessage_Battle.Message.Clear(); statMessage_Battle.Message.Append(temp_text);

                                    statMessage_Battle.Visible = true; statMessage_Battle_Gunlist.Visible = true;
                                }
                            }
                        }
                        else
                        {
                            if (statMessage_Battle != null && text_api.Heartbeat && statMessage_Battle.Visible)
                            {
                                statMessage_Battle.Message.Clear(); statMessage_Battle.Visible = false;
                                statMessage_Battle_Gunlist.Message.Clear(); statMessage_Battle_Gunlist.Visible = false;
                            }
                        }
                    }

                    else
                    {
                        //user is in cockpit

                        //MyAPIGateway.Utilities.ShowNotification("INCOCKPITB");
                        IMyCubeGrid icubeG = cockpit.CubeGrid as IMyCubeGrid;
                        if (icubeG != null && icubeG.Physics != null)
                        {
                            if (timer % 60 == 0)
                            {
                                ShipTracker tracked = new ShipTracker(icubeG);
                                string total_shield_string = "None";
                                if (tracked.TotalShieldStrength > 100)
                                { total_shield_string = Math.Round((tracked.TotalShieldStrength / 100f), 2).ToString() + " M"; }
                                if (tracked.TotalShieldStrength > 1 && tracked.TotalShieldStrength < 100)
                                { total_shield_string = Math.Round((tracked.TotalShieldStrength), 0).ToString() + "0 K"; }
                                var temp_text = "<color=White>" + total_shield_string + " (" + (int)tracked.CurrentShieldStrength + "%)";

                                string GyroString = tracked.CurrentGyro.ToString();
                                double tempGyro2;
                                if (tracked.CurrentGyro >= 1000000)
                                {
                                    tempGyro2 = Math.Round((tracked.CurrentGyro / 1000000f), 1);
                                    if (tempGyro2 > 1000) { GyroString = Math.Round((tempGyro2 / 1000), 1).ToString() + "G"; }
                                    else { GyroString = tempGyro2.ToString() + "M"; }
                                }

                                string thrustString = tracked.InstalledThrust.ToString();
                                if (tracked.InstalledThrust > 1000000)
                                {
                                    thrustString = Math.Round((tracked.InstalledThrust / 1000000f), 2).ToString() + "M";
                                }
                                string PWRNotation;

                                PWRNotation = tracked.CurrentPower > 1000 ? "GW" : "MW";
                                string tempPWR;
                                if (tracked.CurrentPower > 1000)
                                { tempPWR = (Math.Round(tracked.CurrentPower / 1000, 1)).ToString(); }
                                else { tempPWR = tracked.CurrentPower.ToString(); }
                                string PWR = tempPWR + PWRNotation;
                                string gunText = "";
                                foreach (var x in tracked.GunL.Keys)
                                {
                                    gunText += "<color=Green>" + tracked.GunL[x] + "<color=White> x " + x + "\n";

                                }
                                gunText += "\n" + "<color=Green>Thrust<color=White>: " + thrustString + "N"
                                         + "\n" + "<color=Green>Gyro<color=White>: " + GyroString + "N"
                                         + "\n" + "<color=Green>Power<color=White>: " + PWR;


                                statMessage_Battle_Gunlist.Message.Clear(); statMessage_Battle_Gunlist.Message.Append(gunText);
                                statMessage_Battle.Message.Clear(); statMessage_Battle.Message.Append(temp_text);

                                statMessage_Battle.Visible = true; statMessage_Battle_Gunlist.Visible = true;
                            }
                        }

                        else
                        {
                            if (statMessage_Battle != null && text_api.Heartbeat && statMessage_Battle.Visible)
                            {
                                statMessage_Battle.Message.Clear(); statMessage_Battle.Visible = false;
                                statMessage_Battle_Gunlist.Message.Clear(); statMessage_Battle_Gunlist.Visible = false;
                            }
                        }
                    }


                }

                if (timer % 60 == 0 && integretyMessage != null && text_api.Heartbeat)
                {

                    StringBuilder temp_text = new StringBuilder();
                    Dictionary<string, List<string>> trackedShips = new Dictionary<string, List<string>>();
                    Dictionary<string, double> totalMass = new Dictionary<string, double>();
                    Dictionary<string, int> totalBattlePoints = new Dictionary<string, int>();

                    Dictionary<string, int> totalMiscBps = new Dictionary<string, int>();
                    Dictionary<string, int> totalPowerBps = new Dictionary<string, int>();
                    Dictionary<string, int> totalOffensiveBps = new Dictionary<string, int>();
                    Dictionary<string, int> totalMovementBps = new Dictionary<string, int>();



                    foreach (var z in Tracking)
                    {
                        if (!Data.ContainsKey(z))
                        {
                            continue;

                        }

                        var data = Data[z];
                        data.LastUpdate--;
                        if (data.LastUpdate <= 0)
                        {
                            Data[z].DisposeHud();
                            Data.Remove(z);
                            continue;
                        }

                        string factionName = data.FactionName; bool notDead = data.IsFunctional;
                        string Ownername = data.OwnerName;

                        if (!trackedShips.ContainsKey(factionName))
                        {
                            trackedShips.Add(factionName, new List<string>());
                            totalMass.Add(factionName, 0);
                            totalBattlePoints.Add(factionName, 0);
                            totalMiscBps.Add(factionName, 0);
                            totalPowerBps.Add(factionName, 0);
                            totalOffensiveBps.Add(factionName, 0);
                            totalMovementBps.Add(factionName, 0);
                        }

                        if(notDead) { 
                        totalMass[factionName] += data.Mass;
                        totalBattlePoints[factionName] += data.Bpts;
                        }

                        totalMiscBps[factionName] += data.MiscBps;
                        totalPowerBps[factionName] += data.PowerBps;
                        totalOffensiveBps[factionName] += data.OffensiveBps;
                        totalMovementBps[factionName] += data.MovementBps;


                        int guns = 0; foreach (int s in data.GunL.Values)
                        {

                            guns += s;

                        } //center distance info
                        if (notDead)
                        {
                            if (Vector3.DistanceSquared(ctrpoint, data.Position) < capdistCenter * capdistCenter)
                            {
                                capstat = "C1";

                                if (!ZoneControl1.Contains(factionName))

                                {
                                    ZoneControl1 += factionName.ToUpper();

                                }
                            }
                            else if (Vector3.DistanceSquared(ctrpoint2, data.Position) < capdist * capdist)
                            {
                                capstat = "C2";

                                if (!ZoneControl2.Contains(factionName))

                                {
                                    ZoneControl2 += factionName.ToUpper();

                                }
                            }

                            else if (Vector3.DistanceSquared(ctrpoint3, data.Position) < capdist * capdist)
                            {
                                capstat = "C3";

                                if (!ZoneControl3.Contains(factionName))
                                {
                                    ZoneControl3 += factionName.ToUpper();

                                }
                            }
                            else
                            {
                                capstat = "";
                            }
                        }

                        else

                        {
                            capstat = "DED";
                        }

                        string PWRNotation;
                        PWRNotation = data.CurrentPower > 1000 ? "GW" : "MW";
                        string tempPWR;
                        if (data.CurrentPower > 1000)
                        { tempPWR = (Math.Round(data.CurrentPower / 1000, 1)).ToString(); }
                        else { tempPWR = data.CurrentPower.ToString(); }

                        string PWR = tempPWR + PWRNotation;
                        string thrustString = data.InstalledThrust.ToString();
                        double tempThrust2;

                        if (data.InstalledThrust >= 1000000)
                        {
                            tempThrust2 = Math.Round((data.InstalledThrust / 1000000f), 1);
                            if (tempThrust2 > 1000) { thrustString = Math.Round((tempThrust2 / 1000), 1).ToString() + "G"; }
                            else { thrustString = tempThrust2.ToString() + "M"; }
                        }

                        if (data.FactionName == team1.Value && !data.IsFunctional)
                        {
                            NewCountT1++;
                        }

                        if (data.FactionName == team2.Value && !data.IsFunctional)
                        {
                            NewCountT2++;
                        }

                        if (data.FactionName == team3.Value && !data.IsFunctional)
                        {
                            NewCountT3++;
                        }
                        string tempThrust = thrustString + "N";
                        trackedShips[factionName].Add(string.Format("<color={4}>{0,-8}{1,3}%<color={4}> P:<color=orange>{8,3}<color={4}> T:<color=orange>{9,3}<color={4}> W:<color={6}>{2,3}<color={4}> S:<color={5}>{3,3}%<color=white> {7,3}",
                            data.OwnerName?.Substring(0, Math.Min(data.OwnerName.Length, 7)) ?? data.GridName,
                            (int)(data.CurrentIntegrity / data.OriginalIntegrity * 100),
                            guns,
                            (int)data.CurrentShieldStrength,
                            data.IsFunctional ? "white" : "red",
                            (int)data.CurrentShieldStrength <= 0 ? "red" : $"{255},{255 - (data.ShieldHeat * 20)},{255 - (data.ShieldHeat * 20)}",
                            guns == 0 ? "red" : "orange",
                            capstat,
                            PWR,
                            tempThrust));
                    }



                    foreach (var x in trackedShips.Keys)
                    {
                        string massStr = Math.Round((totalMass[x] / 1000000f), 2).ToString() + "M";
                        float totalBps = totalOffensiveBps[x] + totalMovementBps[x] + totalPowerBps[x] + totalMiscBps[x];
                        float totalBpsInv = 100f / totalBattlePoints[x];
                        string MovementPercentage = ((int)(totalMovementBps[x] * totalBpsInv + 0.5f)).ToString();
                        string OffensivePercentage = ((int)(totalOffensiveBps[x] * totalBpsInv + 0.5f)).ToString();
                        string PowerPercentage = ((int)(totalPowerBps[x] * totalBpsInv + 0.5f)).ToString();
                        string MiscPercentage = ((int)(totalMiscBps[x] * totalBpsInv + 0.5f)).ToString();

                        temp_text.Append("<color=white>---- <color=orange>"
                            + x + " : " + massStr + " : "
                            + totalBattlePoints[x] + "bp <color=orange>[<color=Red>" + OffensivePercentage
                            + "<color=white>%<color=orange>|<color=Green>" + PowerPercentage + "<color=white>%<color=orange>|<color=DeepSkyBlue>"
                            + MovementPercentage + "<color=white>%<color=orange>|<color=LightGray>"
                            + MiscPercentage + "<color=white>%<color=orange>]"
                            + "<color=white> ---------\n");
                        foreach (var y in trackedShips[x])

                        {

                            temp_text.Append(y + "\n");

                        }
                    }

                    //winstates
                    if (IAmTheCaptainNow && timer >= matchtime && broadcaststat == true && ServerMatchState.Value != 0)
                    {
                        if ((Team1Tickets.Value > Team2Tickets.Value) && (Team1Tickets.Value > Team3Tickets.Value))
                        {
                            //MyAPIGateway.Utilities.ShowNotification(t_tempteam1 + "Wins");
                            Static.MyNetwork.TransmitToServer(new BasicPacket(9), true, true);
                            CaptainCapTimerZ1T1.Value = 0; CaptainCapTimerZ2T1.Value = 0; CaptainCapTimerZ3T1.Value = 0;
                            CaptainCapTimerZ1T2.Value = 0; CaptainCapTimerZ2T2.Value = 0; CaptainCapTimerZ3T2.Value = 0;
                            CaptainCapTimerZ1T3.Value = 0; CaptainCapTimerZ2T3.Value = 0; CaptainCapTimerZ3T3.Value = 0;
                            EndMatch();
                        }
                        else if ((Team2Tickets.Value > Team1Tickets.Value) && (Team2Tickets.Value > Team3Tickets.Value))
                        {
                            //MyAPIGateway.Utilities.ShowNotification(t_tempteam2 + "Wins");
                            Static.MyNetwork.TransmitToServer(new BasicPacket(10), true, true);
                            CaptainCapTimerZ1T1.Value = 0; CaptainCapTimerZ2T1.Value = 0; CaptainCapTimerZ3T1.Value = 0;
                            CaptainCapTimerZ1T2.Value = 0; CaptainCapTimerZ2T2.Value = 0; CaptainCapTimerZ3T2.Value = 0;
                            CaptainCapTimerZ1T3.Value = 0; CaptainCapTimerZ2T3.Value = 0; CaptainCapTimerZ3T3.Value = 0;
                            EndMatch();
                        }
                        else if ((Team3Tickets.Value > Team1Tickets.Value) && (Team3Tickets.Value > Team1Tickets.Value))
                        {
                            //MyAPIGateway.Utilities.ShowNotification(t_tempteam3 + "Wins");
                            Static.MyNetwork.TransmitToServer(new BasicPacket(11), true, true);
                            CaptainCapTimerZ1T1.Value = 0; CaptainCapTimerZ2T1.Value = 0; CaptainCapTimerZ3T1.Value = 0;
                            CaptainCapTimerZ1T2.Value = 0; CaptainCapTimerZ2T2.Value = 0; CaptainCapTimerZ3T2.Value = 0;
                            CaptainCapTimerZ1T3.Value = 0; CaptainCapTimerZ2T3.Value = 0; CaptainCapTimerZ3T3.Value = 0;
                            EndMatch();
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage("GM", "Match time ended");
                            Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true);
                        }
                        //MyAPIGateway.Utilities.ShowMessage("GM", "Match time ended"); 
                        //Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true); 

                    }

                    if (broadcaststat)
                    {
                        long myid = Session.Player.IdentityId; string factionName_L = myid == null ? "None" : MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(myid)?.Tag;
                        try
                        {
                            if (NewCountT1 > OldCountT1)
                            {
                                if (factionName_L.Contains(team1.Value))
                                { _alertAudio.Cleanup(); BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.TeamDestroyed); }
                                else if (factionName_L.Contains(team2.Value) || factionName_L.Contains(team3.Value))
                                { _alertAudio.Cleanup(); BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.EnemyDestroyed); }
                            }
                            if (NewCountT2 > OldCountT2)
                            {
                                if (factionName_L.Contains(team2.Value))
                                { _alertAudio.Cleanup(); BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.TeamDestroyed); }
                                else if (factionName_L.Contains(team1.Value) || factionName_L.Contains(team3.Value))
                                { _alertAudio.Cleanup(); BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.EnemyDestroyed); }
                            }
                            if (NewCountT3 > OldCountT3)
                            {
                                if (factionName_L.Contains(team3.Value))
                                { _alertAudio.Cleanup(); BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.TeamDestroyed); }
                                else if (factionName_L.Contains(team1.Value) || factionName_L.Contains(team3.Value))
                                { _alertAudio.Cleanup(); BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.EnemyDestroyed); }
                            }
                        }
                        catch
                        { }

                        if (IAmTheCaptainNow)
                        {
                            if (NewCountT1 > OldCountT1)
                            {
                                Team1Tickets.Value -= 200;
                            }

                            if (NewCountT2 > OldCountT2)
                            {
                                Team2Tickets.Value -= 200;
                            }

                            if (NewCountT3 > OldCountT3)
                            {
                                Team3Tickets.Value -= 200;
                            }
                        }

                        OldCountT1 = NewCountT1;
                        OldCountT2 = NewCountT2;
                        OldCountT3 = NewCountT3;
                        NewCountT1 = 0;
                        NewCountT2 = 0;
                        NewCountT3 = 0;
                        //Timer stuff //win via match duration

                        t_tempteam1 = team1.Value;
                        t_tempteam2 = team2.Value;
                        t_tempteam3 = team3.Value;
                        ServerMatchState.Value = LocalMatchState;

                        try
                        {
                            bool autotrack = true;
                            if (autotrack && timer % 60 == 0)
                            {
                                var controlledEntity = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity;
                                IMyCockpit cockpit = controlledEntity as IMyCockpit;
                                long entityId = cockpit.CubeGrid.EntityId;


                                if (cockpit != null && (!Tracking.Contains(entityId)))
                                {

                                    bool hasGyroscope = false;
                                    bool hasBatteryOrReactor = false;
                                    var gridBlocks = new List<IMySlimBlock>();
                                    cockpit.CubeGrid.GetBlocks(gridBlocks);


                                    foreach (var block in gridBlocks)
                                    {
                                        if (block.FatBlock is IMyGyro)
                                        {
                                            hasGyroscope = true;
                                        }
                                        else if (block.FatBlock is IMyBatteryBlock || block.FatBlock is IMyReactor)
                                        {
                                            hasBatteryOrReactor = true;
                                        }

                                        if (hasGyroscope && hasBatteryOrReactor)
                                        {
                                            break;  // Exit the loop as we've found both.
                                        }
                                    }

                                    if (hasGyroscope && hasBatteryOrReactor)
                                    {
                                        // Create a packet with the grid data
                                        PacketGridData packet = new PacketGridData
                                        {
                                            id = entityId,
                                            value = 1,
                                        };

                                        // Transmit the packet to the server
                                        Static.MyNetwork.TransmitToServer(packet, true);

                                        // Add the grid to the tracker
                                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Added grid to tracker");
                                        Tracking.Add(entityId);

                                        // Show the integrity message if it's not visible
                                        if (!integretyMessage.Visible)
                                        {
                                            integretyMessage.Visible = true;
                                        }

                                        // Create the HUD for the grid
                                        Data[entityId].CreateHud();
                                    }
                                }
                            }
                        }
                        catch (Exception AT)
                        {
                            // Consider logging the exception or handling it in some way.
                        }


                        if (IAmTheCaptainNow)
                        {
                            if (ZoneControl1.Contains(team1.Value) || ZoneControl1.Contains(team2.Value) || ZoneControl1.Contains(team3.Value))
                            {
                                if (ZoneControl1.Contains(team1.Value) && !ZoneControl1.Contains(team2.Value) && !ZoneControl1.Contains(team3.Value))
                                { captimerZ1T2 = 0; captimerZ1T3 = 0; if (captimerZ1T1 < CapOut) { captimerZ1T1++; } } //Team1 Capping, if team 2 or 3 has any progress, reset it
                                else if (ZoneControl1.Contains(team2.Value) && !ZoneControl1.Contains(team1.Value) && !ZoneControl1.Contains(team3.Value))
                                { captimerZ1T1 = 0; captimerZ1T3 = 0; if (captimerZ1T2 < CapOut) { captimerZ1T2++; } }//Team2 Capping, if team 3 or 1 has any progress, reset it
                                else if (ZoneControl1.Contains(team3.Value) && !ZoneControl1.Contains(team2.Value) && !ZoneControl1.Contains(team1.Value))
                                { captimerZ1T1 = 0; captimerZ1T2 = 0; if (captimerZ1T3 < CapOut) { captimerZ1T3++; } }//Team3 Capping, if team 1 or 2 has any progress, reset it
                            }
                            else if (!ZoneControl1.Contains(team1.Value) && !ZoneControl1.Contains(team2.Value) && !ZoneControl1.Contains(team3.Value))
                            {
                                if (captimerZ1T1 < CapOut) { captimerZ1T1 = 0; }
                                if (captimerZ1T2 < CapOut) { captimerZ1T2 = 0; }
                                if (captimerZ1T3 < CapOut) { captimerZ1T3 = 0; } //Zone 1 unoccupied, if nobody has captured it then set it to 0 if it is not already zero
                            }
                            CaptainCapTimerZ1T1.Value = captimerZ1T1; CaptainCapTimerZ1T2.Value = captimerZ1T2; CaptainCapTimerZ1T3.Value = captimerZ1T3;
                            if (ZoneControl2.Contains(team1.Value) || ZoneControl2.Contains(team2.Value) || ZoneControl2.Contains(team3.Value))
                            {
                                if (ZoneControl2.Contains(team1.Value) && !ZoneControl2.Contains(team2.Value) && !ZoneControl2.Contains(team3.Value))
                                { captimerZ2T2 = 0; captimerZ2T3 = 0; if (captimerZ2T1 < CapOut) { captimerZ2T1++; } } //Team1 Capping, if team 2 or 3 has any progress, reset it
                                else if (ZoneControl2.Contains(team2.Value) && !ZoneControl2.Contains(team1.Value) && !ZoneControl2.Contains(team3.Value))
                                { captimerZ2T1 = 0; captimerZ2T3 = 0; if (captimerZ2T2 < CapOut) { captimerZ2T2++; } }//Team2 Capping, if team1 has any progress, reset it
                                else if (ZoneControl2.Contains(team3.Value) && !ZoneControl2.Contains(team2.Value) && !ZoneControl2.Contains(team1.Value))
                                { captimerZ2T1 = 0; captimerZ2T2 = 0; if (captimerZ2T3 < CapOut) { captimerZ2T3++; } }
                            }//Team2 Capping, if team1 has any progress, reset it
                            else if (!ZoneControl2.Contains(team1.Value) && !ZoneControl2.Contains(team2.Value) && !ZoneControl2.Contains(team3.Value))
                            { if (captimerZ2T1 < CapOut) { captimerZ2T1 = 0; } if (captimerZ2T2 < CapOut) { captimerZ2T2 = 0; } if (captimerZ2T3 < CapOut) { captimerZ2T3 = 0; } } //Zone 1 unoccupied, if nobody has captured it then set it to 0 if it is not already zero
                            CaptainCapTimerZ2T1.Value = captimerZ2T1; CaptainCapTimerZ2T2.Value = captimerZ2T2; CaptainCapTimerZ2T3.Value = captimerZ2T3;
                            if (ZoneControl3.Contains(team1.Value) || ZoneControl3.Contains(team2.Value) || ZoneControl3.Contains(team3.Value))
                            {
                                if (ZoneControl3.Contains(team1.Value) && !ZoneControl3.Contains(team2.Value) && !ZoneControl3.Contains(team3.Value))
                                { captimerZ3T2 = 0; captimerZ3T3 = 0; if (captimerZ3T1 < CapOut) { captimerZ3T1++; } } //Team1 Capping, if team 2 or 3 has any progress, reset it
                                else if (ZoneControl3.Contains(team2.Value) && !ZoneControl3.Contains(team1.Value) && !ZoneControl3.Contains(team3.Value))
                                { captimerZ3T1 = 0; captimerZ3T3 = 0; if (captimerZ3T2 < CapOut) { captimerZ3T2++; } }//Team2 Capping, if team1 has any progress, reset it
                                else if (ZoneControl3.Contains(team3.Value) && !ZoneControl3.Contains(team2.Value) && !ZoneControl3.Contains(team1.Value))
                                { captimerZ3T1 = 0; captimerZ3T2 = 0; if (captimerZ3T3 < CapOut) { captimerZ3T3++; } }
                            }//Team2 Capping, if team1 has any progress, reset it
                            else if (!ZoneControl3.Contains(team1.Value) && !ZoneControl3.Contains(team2.Value) && !ZoneControl3.Contains(team3.Value))
                            { if (captimerZ3T1 < CapOut) { captimerZ3T1 = 0; } if (captimerZ3T2 < CapOut) { captimerZ3T2 = 0; } if (captimerZ3T3 < CapOut) { captimerZ3T3 = 0; } } //Zone 1 unoccupied, if nobody has captured it then set it to 0 if it is not already zero
                            CaptainCapTimerZ3T1.Value = captimerZ3T1; CaptainCapTimerZ3T2.Value = captimerZ3T2; CaptainCapTimerZ3T3.Value = captimerZ3T3;
                        }
                        else
                        {
                            //Non-captain players sync all timers to captain's timer
                            captimerZ3T1 = CaptainCapTimerZ3T1.Value;
                            captimerZ3T2 = CaptainCapTimerZ3T2.Value;
                            captimerZ3T3 = CaptainCapTimerZ3T3.Value;
                            captimerZ2T1 = CaptainCapTimerZ2T1.Value;
                            captimerZ2T2 = CaptainCapTimerZ2T2.Value;
                            captimerZ2T3 = CaptainCapTimerZ2T3.Value;
                            captimerZ1T1 = CaptainCapTimerZ1T1.Value;
                            captimerZ1T2 = CaptainCapTimerZ1T2.Value;
                            captimerZ1T3 = CaptainCapTimerZ1T3.Value;
                        }

                        string hudCap1 = "<color=white>C1";
                        string hudCap2 = "<color=white>C2";
                        string hudCap3 = "<color=white>C3";


                        //crazy king switch
                        if (GameModeSwitch.Value != 5)
                        {
                            if (IAmTheCaptainNow)
                            {
                                if (CaptainCapTimerZ1T1.Value == CapOut) { Team2Tickets.Value--; Team3Tickets.Value--; hudCap1 = "<color=tomato>" + "C1 "; }

                                if (CaptainCapTimerZ2T1.Value == CapOut) { Team2Tickets.Value--; Team3Tickets.Value--; hudCap2 = "<color=tomato>" + "C2 "; }

                                if (CaptainCapTimerZ3T1.Value == CapOut) { Team2Tickets.Value--; Team3Tickets.Value--; hudCap3 = "<color=tomato>" + "C3 "; }

                                if (CaptainCapTimerZ1T2.Value == CapOut) { Team1Tickets.Value--; Team3Tickets.Value--; hudCap1 = "<color=dodgerblue>" + "C1 "; }

                                if (CaptainCapTimerZ2T2.Value == CapOut) { Team1Tickets.Value--; Team3Tickets.Value--; hudCap2 = "<color=dodgerblue>" + "C2 "; }

                                if (CaptainCapTimerZ3T2.Value == CapOut) { Team1Tickets.Value--; Team3Tickets.Value--; hudCap3 = "<color=dodgerblue>" + "C3 "; }

                                if (CaptainCapTimerZ1T3.Value == CapOut) { Team2Tickets.Value--; Team1Tickets.Value--; hudCap1 = "<color=green>" + "C1 "; }

                                if (CaptainCapTimerZ2T3.Value == CapOut) { Team2Tickets.Value--; Team1Tickets.Value--; hudCap2 = "<color=green>" + "C2 "; }

                                if (CaptainCapTimerZ3T3.Value == CapOut) { Team2Tickets.Value--; Team1Tickets.Value--; hudCap3 = "<color=green>" + "C3 "; }

                                if (Team1Tickets.Value < 0)
                                {
                                    Team1Tickets.Value = 0;
                                }
                                if (Team2Tickets.Value < 0)
                                {
                                    Team2Tickets.Value = 0;
                                }
                                if (Team3Tickets.Value < 0)
                                {
                                    Team3Tickets.Value = 0;
                                }
                            }
                            else if (IAmTheCaptainNow && Team1Tickets.Value <= 0 && Team3Tickets.Value <= 0)
                            {
                                ServerMatchState.Value = 0;
                                //Team 2 victory
                                Team1Tickets.Value = 0; Team3Tickets.Value = 0;
                                //MyAPIGateway.Utilities.ShowNotification(t_tempteam2 + "Wins");
                                //Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true); 
                                Static.MyNetwork.TransmitToServer(new BasicPacket(10), true, true);
                            }
                            else if (IAmTheCaptainNow && Team2Tickets.Value <= 0 && Team1Tickets.Value <= 0)
                            {
                                ServerMatchState.Value = 0;
                                //Team 3 victory
                                Team1Tickets.Value = 0; Team2Tickets.Value = 0;
                                //MyAPIGateway.Utilities.ShowNotification(t_tempteam3 + "Wins");
                                //Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true); 
                                Static.MyNetwork.TransmitToServer(new BasicPacket(11), true, true);
                            }
                            else if (IAmTheCaptainNow && Team3Tickets.Value <= 0 && Team2Tickets.Value <= 0)
                            {
                                ServerMatchState.Value = 0;
                                //Team 1 victory
                                Team3Tickets.Value = 0; Team2Tickets.Value = 0;
                                //MyAPIGateway.Utilities.ShowNotification(t_tempteam1 + "Wins");
                                //Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true); 
                                Static.MyNetwork.TransmitToServer(new BasicPacket(9), true, true);

                            }

                            else if (!IAmTheCaptainNow)
                            {
                                //team1 Hud cap colors
                                if (captimerZ1T1 == CapOut) { hudCap1 = "<color=tomato>" + "C1"; }
                                if (captimerZ2T1 == CapOut) { hudCap2 = "<color=tomato>" + "C2"; }
                                if (captimerZ3T1 == CapOut) { hudCap3 = "<color=tomato>" + "C3"; }
                                //team2 Hud cap colors   
                                if (captimerZ1T2 == CapOut) { hudCap1 = "<color=dodgerblue>" + "C1"; }
                                if (captimerZ2T2 == CapOut) { hudCap2 = "<color=dodgerblue>" + "C2"; }
                                if (captimerZ3T2 == CapOut) { hudCap3 = "<color=dodgerblue>" + "C3"; }
                                //team3 Hud cap colors
                                if (captimerZ1T3 == CapOut) { hudCap1 = "<color=green>" + "C1"; }
                                if (captimerZ2T3 == CapOut) { hudCap2 = "<color=green>" + "C2"; }
                                if (captimerZ3T3 == CapOut) { hudCap3 = "<color=green>" + "C3"; }
                            }



                        }

                        else
                        {

                            if (GameModeSwitch.Value == 5)

                            {
                                // captimerZ1T1 = CaptainCapTimerZ1T1.Value;
                                // captimerZ1T2 = CaptainCapTimerZ1T2.Value;
                                // captimerZ1T3 = CaptainCapTimerZ1T3.Value;

                                //todo: Make capping faster with more friends

                                if (captimerZ1T1 > 0)
                                {
                                    capProgress1 = "<color=tomato>" + ((float)captimerZ1T1 / (float)CapOut).ToString("0.00%");
                                    //MyAPIGateway.Utilities.ShowMessage("GM", capProgress1); 
                                }

                                if (captimerZ1T2 > 0)
                                {
                                    capProgress2 = "<color=dodgerblue>" + ((float)captimerZ1T2 / (float)CapOut).ToString("0.00%");
                                    //MyAPIGateway.Utilities.ShowMessage("GM", capProgress2); 
                                }

                                if (captimerZ1T3 > 0 && ThreeTeams.Value == 1)
                                {
                                    capProgress3 = "<color=green>" + ((float)captimerZ1T3 / (float)CapOut).ToString("0.00%");
                                    //MyAPIGateway.Utilities.ShowMessage("GM", capProgress3); 
                                }

                                if (captimerZ1T1 >= CapOut) captimerZ1T1 = 0;
                                if (captimerZ1T2 >= CapOut) captimerZ1T2 = 0;
                                if (captimerZ1T3 >= CapOut) captimerZ1T3 = 0;

                                bool capSwitch = false;
                                int deduction = 200;

                                if (IAmTheCaptainNow && CaptainCapTimerZ1T1.Value == CapOut)
                                {
                                    capSwitch = true;
                                    Team2Tickets.Value -= deduction; Team3Tickets.Value -= deduction;
                                    captimerZ1T1 = 1; CaptainCapTimerZ1T1.Value = captimerZ1T1;
                                }
                                else if (!IAmTheCaptainNow && CaptainCapTimerZ1T1.Value == CapOut)
                                {
                                    captimerZ1T1 = CaptainCapTimerZ1T1.Value;
                                    capSwitch = true;
                                }

                                if (IAmTheCaptainNow && CaptainCapTimerZ1T2.Value == CapOut)
                                {
                                    capSwitch = true;
                                    Team1Tickets.Value -= deduction; Team3Tickets.Value -= deduction;
                                    captimerZ1T2 = 1;
                                    CaptainCapTimerZ1T2.Value = captimerZ1T2;
                                }
                                else if (!IAmTheCaptainNow && CaptainCapTimerZ1T2.Value == CapOut)
                                {
                                    captimerZ1T2 = CaptainCapTimerZ1T2.Value;
                                    capSwitch = true;
                                }


                                if (IAmTheCaptainNow && CaptainCapTimerZ1T3.Value == CapOut)
                                {
                                    capSwitch = true;
                                    Team2Tickets.Value -= deduction; Team1Tickets.Value -= deduction;
                                    captimerZ1T3 = 1;
                                    CaptainCapTimerZ1T3.Value = captimerZ1T3;
                                }
                                else if (!IAmTheCaptainNow && CaptainCapTimerZ1T3.Value == CapOut)
                                {
                                    captimerZ1T3 = CaptainCapTimerZ1T3.Value;
                                    capSwitch = true;
                                }

                                if (IAmTheCaptainNow && capSwitch)
                                {
                                    if (_sphereEntity == null) 
                                    _sphereEntity.Close();
                                    ctrpoint = CrazyKing();
                                    ClientRandVector3D = ctrpoint;
                                    CaptainRandVector3D.Value = ClientRandVector3D;
                                    CaptainRandVector3D.Push();
                                    GameMode_Set();

                                    capSwitch = false;
                                }
                                else if (!IAmTheCaptainNow && capSwitch)
                                {
                                    CaptainRandVector3D.Fetch();
                                    ClientRandVector3D = CaptainRandVector3D.Value;
                                    if (_sphereEntity == null) 
                                    _sphereEntity.Close();
                                    ctrpoint = ClientRandVector3D;
                                    GameMode_Set();

                                    capSwitch = false;
                                }


                                if (Team1Tickets.Value < 0)
                                {
                                    Team1Tickets.Value = 0;
                                }
                                if (Team2Tickets.Value < 0)
                                {
                                    Team2Tickets.Value = 0;
                                }
                                if (Team3Tickets.Value < 0)
                                {
                                    Team3Tickets.Value = 0;
                                }
                            }
                            else if (IAmTheCaptainNow && Team1Tickets.Value <= 0 && Team3Tickets.Value <= 0)
                            {
                                ServerMatchState.Value = 0;
                                //Team 2 victory
                                Team1Tickets.Value = 0; Team3Tickets.Value = 0;
                                //MyAPIGateway.Utilities.ShowNotification(t_tempteam2 + "Wins");
                                //Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true); 
                                Static.MyNetwork.TransmitToServer(new BasicPacket(10), true, true);
                            }
                            else if (IAmTheCaptainNow && Team2Tickets.Value <= 0 && Team1Tickets.Value <= 0)
                            {
                                ServerMatchState.Value = 0;
                                //Team 3 victory
                                Team1Tickets.Value = 0; Team2Tickets.Value = 0;
                                //MyAPIGateway.Utilities.ShowNotification(t_tempteam3 + "Wins");
                                //Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true); 
                                Static.MyNetwork.TransmitToServer(new BasicPacket(11), true, true);
                            }
                            else if (IAmTheCaptainNow && Team3Tickets.Value <= 0 && Team2Tickets.Value <= 0)
                            {
                                ServerMatchState.Value = 0;
                                //Team 1 victory
                                Team3Tickets.Value = 0; Team2Tickets.Value = 0;
                                //MyAPIGateway.Utilities.ShowNotification(t_tempteam1 + "Wins");
                                //Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true); 
                                Static.MyNetwork.TransmitToServer(new BasicPacket(9), true, true);

                            }

                            else if (!IAmTheCaptainNow)
                            {
                                //team1 Hud cap colors
                                if (captimerZ1T1 == CapOut) { hudCap1 = "<color=tomato>" + "C1"; }
                                if (captimerZ2T1 == CapOut) { hudCap2 = "<color=tomato>" + "C2"; }
                                if (captimerZ3T1 == CapOut) { hudCap3 = "<color=tomato>" + "C3"; }
                                //team2 Hud cap colors   
                                if (captimerZ1T2 == CapOut) { hudCap1 = "<color=dodgerblue>" + "C1"; }
                                if (captimerZ2T2 == CapOut) { hudCap2 = "<color=dodgerblue>" + "C2"; }
                                if (captimerZ3T2 == CapOut) { hudCap3 = "<color=dodgerblue>" + "C3"; }
                                //team3 Hud cap colors
                                if (captimerZ1T3 == CapOut) { hudCap1 = "<color=green>" + "C1"; }
                                if (captimerZ2T3 == CapOut) { hudCap2 = "<color=green>" + "C2"; }
                                if (captimerZ3T3 == CapOut) { hudCap3 = "<color=green>" + "C3"; }
                            }
                        }

                        //Zone color reset and sound reset
                        if (captimerZ1T1 == 0 && Capcolor1 != 0 && captimerZ1T2 == 0 && captimerZ1T3 == 0)
                        { Capcolor1 = 0; CapZoneColor1(); LostCapsoundPlayed = false; }
                        if (captimerZ2T1 == 0 && Capcolor2 != 0 && captimerZ2T2 == 0 && captimerZ2T3 == 0)
                        { Capcolor2 = 0; CapZoneColor2(); LostCapsoundPlayed = false; }
                        if (captimerZ3T1 == 0 && Capcolor3 != 0 && captimerZ3T2 == 0 && captimerZ3T3 == 0)
                        { Capcolor3 = 0; CapZoneColor3(); LostCapsoundPlayed = false; }

                        if (captimerZ1T2 == 0 && Capcolor1 != 0 && captimerZ1T1 == 0 && captimerZ1T3 == 0)
                        { Capcolor1 = 0; CapZoneColor1(); LostCapsoundPlayed = false; }
                        if (captimerZ2T2 == 0 && Capcolor2 != 0 && captimerZ2T1 == 0 && captimerZ2T3 == 0)
                        { Capcolor2 = 0; CapZoneColor2(); LostCapsoundPlayed = false; }
                        if (captimerZ3T2 == 0 && Capcolor3 != 0 && captimerZ3T1 == 0 && captimerZ3T3 == 0)
                        { Capcolor3 = 0; CapZoneColor3(); LostCapsoundPlayed = false; }

                        if (captimerZ1T3 == 0 && Capcolor1 != 0 && captimerZ1T1 == 0 && captimerZ1T2 == 0)
                        { Capcolor1 = 0; CapZoneColor1(); LostCapsoundPlayed = false; }
                        if (captimerZ2T3 == 0 && Capcolor2 != 0 && captimerZ2T1 == 0 && captimerZ2T2 == 0)
                        { Capcolor2 = 0; CapZoneColor2(); LostCapsoundPlayed = false; }
                        if (captimerZ3T3 == 0 && Capcolor3 != 0 && captimerZ3T1 == 0 && captimerZ3T2 == 0)
                        { Capcolor3 = 0; CapZoneColor3(); LostCapsoundPlayed = false; }
                        //Zone color set and sound trigger
                        if (captimerZ1T1 >= CapOut && Capcolor1 != 1 && captimerZ1T2 == 0 && captimerZ1T3 == 0)
                        { Capcolor1 = 1; CapZoneColor1(); CapsoundPlayed = false; }
                        if (captimerZ2T1 >= CapOut && Capcolor2 != 1 && captimerZ2T2 == 0 && captimerZ2T3 == 0)
                        { Capcolor2 = 1; CapZoneColor2(); CapsoundPlayed = false; }
                        if (captimerZ3T1 >= CapOut && Capcolor3 != 1 && captimerZ3T2 == 0 && captimerZ3T3 == 0)
                        { Capcolor3 = 1; CapZoneColor3(); CapsoundPlayed = false; }

                        if (captimerZ1T2 >= CapOut && Capcolor1 != 2 && captimerZ1T1 == 0 && captimerZ1T3 == 0)
                        {
                            Capcolor1 = 2; CapZoneColor1(); CapsoundPlayed = false;
                        }
                        if (captimerZ2T2 >= CapOut && Capcolor2 != 2 && captimerZ2T1 == 0 && captimerZ2T3 == 0)
                        {
                            Capcolor2 = 2; CapZoneColor2(); CapsoundPlayed = false;
                        }
                        if (captimerZ3T2 >= CapOut && Capcolor3 != 2 && captimerZ3T1 == 0 && captimerZ3T3 == 0)
                        {
                            Capcolor3 = 2; CapZoneColor3(); CapsoundPlayed = false;
                        }



                        if (captimerZ1T3 >= CapOut && Capcolor1 != 3 && captimerZ1T1 == 0 && captimerZ1T2 == 0)
                        {
                            Capcolor1 = 3; CapZoneColor1(); CapsoundPlayed = false;
                        }
                        if (captimerZ2T3 >= CapOut && Capcolor2 != 3 && captimerZ2T1 == 0 && captimerZ2T2 == 0)
                        {
                            Capcolor2 = 3; CapZoneColor2(); CapsoundPlayed = false;
                        }
                        if (captimerZ3T3 >= CapOut && Capcolor3 != 3 && captimerZ3T1 == 0 && captimerZ3T2 == 0)
                        {
                            Capcolor3 = 3; CapZoneColor3(); CapsoundPlayed = false;
                        }


                        //  CapZoneColor1();  CapZoneColor2();  CapZoneColor3();

                        long myid2 = Session.Player.IdentityId; string factionName_L2 = myid2 == null ? "None" : MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(myid2)?.Tag;
                        //Friendly/Enemy destroyed sound triggers
                        if (factionName_L2.Contains(team1.Value))
                        {
                            if ((Capcolor1 == 1 || Capcolor2 == 1 || Capcolor3 == 1) && !CapsoundPlayed) { _alertAudio.Cleanup(); CapsoundPlayed = true; BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.ZoneCaptured); }
                        }
                        if (factionName_L2.Contains(team2.Value))
                        {
                            if ((Capcolor1 == 2 || Capcolor2 == 2 || Capcolor3 == 2) && !CapsoundPlayed) { _alertAudio.Cleanup(); CapsoundPlayed = true; BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.ZoneCaptured); }
                        }
                        if (factionName_L2.Contains(team3.Value))
                        {
                            if ((Capcolor1 == 3 || Capcolor2 == 3 || Capcolor3 == 3) && !CapsoundPlayed) { _alertAudio.Cleanup(); CapsoundPlayed = true; BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.ZoneCaptured); }
                        }
                        //Lost cap sound trigger
                        if ((Capcolor1 == 0 || Capcolor2 == 0 || Capcolor3 == 0) && !LostCapsoundPlayed) { _alertAudio.Cleanup(); LostCapsoundPlayed = true; BroadcastSound((MyAPIGateway.Session.Player.Character), PlayerNotice.ZoneLost); }

                        int team1tickets = Team1Tickets.Value; int team2tickets = Team2Tickets.Value; int team3tickets = Team3Tickets.Value;
                        int tempVicCountT1 = 0; int tempVicCountT2 = 0; int tempVicCountT3 = 0;
                        if (captimerZ1T1 >= CapOut) { tempVicCountT1++; }
                        if (captimerZ2T1 >= CapOut) { tempVicCountT1++; }
                        if (captimerZ3T1 >= CapOut) { tempVicCountT1++; }
                        if (captimerZ1T2 >= CapOut) { tempVicCountT2++; }
                        if (captimerZ2T2 >= CapOut) { tempVicCountT2++; }
                        if (captimerZ3T2 >= CapOut) { tempVicCountT2++; }
                        if (captimerZ1T3 >= CapOut) { tempVicCountT3++; }
                        if (captimerZ2T3 >= CapOut) { tempVicCountT3++; }
                        if (captimerZ3T3 >= CapOut) { tempVicCountT3++; }
                        string VictoryTimerT1; string VictoryTimerT2; string VictoryTimerT3;
                        string VictoryTimerSecondsT1; string VictoryTimerSecondsT2; string VictoryTimerSecondsT3;
                        string VictoryTimerMinutesT1; string VictoryTimerMinutesT2; string VictoryTimerMinutesT3;
                        if (tempVicCountT1 == 0)

                        {
                            VictoryTimerT1 = " [INF]";
                        }
                        else
                        {
                            VictoryTimerMinutesT1 = (((team2tickets) / 60) / tempVicCountT1).ToString();
                            VictoryTimerSecondsT1 = ((team2tickets / tempVicCountT1) % 60).ToString();
                            if (VictoryTimerSecondsT1.Length == 1) { VictoryTimerSecondsT1 = "0" + VictoryTimerSecondsT1; }
                            if (VictoryTimerSecondsT1.Length == 0) { VictoryTimerSecondsT1 = "00"; }
                            if (VictoryTimerMinutesT1.Length == 1) { VictoryTimerMinutesT1 = "0" + VictoryTimerMinutesT1; }
                            if (VictoryTimerMinutesT1.Length == 0) { VictoryTimerMinutesT1 = "00"; }
                            VictoryTimerT1 = " [" + VictoryTimerMinutesT1 + ":" + VictoryTimerSecondsT1 + "] ";
                        }

                        if (tempVicCountT2 == 0)
                        {
                            VictoryTimerT2 = " [INF]";
                        }
                        else
                        {
                            VictoryTimerMinutesT2 = (((team1tickets) / 60) / tempVicCountT2).ToString();
                            VictoryTimerSecondsT2 = ((team1tickets / tempVicCountT2) % 60).ToString();

                            if (VictoryTimerSecondsT2.Length == 1)
                                VictoryTimerSecondsT2 = "0" + VictoryTimerSecondsT2;

                            if (VictoryTimerSecondsT2.Length == 0)
                                VictoryTimerSecondsT2 = "00";

                            if (VictoryTimerMinutesT2.Length == 1)
                                VictoryTimerMinutesT2 = "0" + VictoryTimerMinutesT2;

                            if (VictoryTimerMinutesT2.Length == 0)
                                VictoryTimerMinutesT2 = "00";

                            VictoryTimerT2 = " [" + VictoryTimerMinutesT2 + ":" + VictoryTimerSecondsT2 + "] ";

                        }

                        if (tempVicCountT3 == 0)
                        {
                            VictoryTimerT3 = " [INF]";
                        }
                        else
                        {
                            VictoryTimerMinutesT3 = (((team2tickets) / 60) / tempVicCountT3).ToString();
                            VictoryTimerSecondsT3 = ((team2tickets / tempVicCountT3) % 60).ToString();

                            if (VictoryTimerSecondsT3.Length == 1)
                                VictoryTimerSecondsT3 = "0" + VictoryTimerSecondsT3;

                            if (VictoryTimerSecondsT3.Length == 0)
                                VictoryTimerSecondsT3 = "00";

                            if (VictoryTimerMinutesT3.Length == 1)
                                VictoryTimerMinutesT3 = "0" + VictoryTimerMinutesT3;

                            if (VictoryTimerMinutesT3.Length == 0)
                                VictoryTimerMinutesT3 = "00";

                            VictoryTimerT3 = " [" + VictoryTimerMinutesT3 + ":" + VictoryTimerSecondsT3 + "] ";

                        }

                        string adjustedTicketsT1 = team1tickets.ToString();

                        if (adjustedTicketsT1.Length == 1)
                            adjustedTicketsT1 = "000" + adjustedTicketsT1;
                        if (adjustedTicketsT1.Length == 2)
                            adjustedTicketsT1 = "00" + adjustedTicketsT1;
                        if (adjustedTicketsT1.Length == 3)
                            adjustedTicketsT1 = "0" + adjustedTicketsT1;

                        string adjustedTicketsT2 = team2tickets.ToString();

                        if (adjustedTicketsT2.Length == 1)
                            adjustedTicketsT2 = "000" + adjustedTicketsT2;
                        if (adjustedTicketsT2.Length == 2)
                            adjustedTicketsT2 = "00" + adjustedTicketsT2;
                        if (adjustedTicketsT2.Length == 3)
                            adjustedTicketsT2 = "0" + adjustedTicketsT2;

                        string adjustedTicketsT3 = team3tickets.ToString();

                        if (adjustedTicketsT3.Length == 1)
                            adjustedTicketsT3 = "000" + adjustedTicketsT3;
                        if (adjustedTicketsT3.Length == 2)
                            adjustedTicketsT3 = "00" + adjustedTicketsT3;
                        if (adjustedTicketsT3.Length == 3)
                            adjustedTicketsT3 = "0" + adjustedTicketsT3;

                        ticketmessage.Message.Clear();

                        if (ThreeTeams.Value == 0)
                        {
                            ticketmessage.Message.Append(
                                "<color=tomato>" + t_tempteam1 + "<color=white> :" + adjustedTicketsT1 + VictoryTimerT1 + " vs " +
                                "<color=dodgerblue>" + t_tempteam2 + "<color=white> :" + adjustedTicketsT2 + VictoryTimerT2);
                        }
                        else
                        {
                            ticketmessage.Message.Append(
                                "<color=tomato>" + t_tempteam1 + "<color=white> :" + adjustedTicketsT1 + VictoryTimerT1 +
                                " <color=dodgerblue>" + t_tempteam2 + "<color=white> :" + adjustedTicketsT2 + VictoryTimerT2 +
                                " <color=green>" + t_tempteam3 + "<color=white> :" + adjustedTicketsT3 + VictoryTimerT3);
                        }

                        ZoneControl1 = ""; ZoneControl2 = ""; ZoneControl3 = "";
                        UpdateCapZone1(); UpdateCapZone2(); UpdateCapZone3();

                        string minutes = (((matchtime - timer) / 60) / 60).ToString();
                        if (minutes.Length == 1)
                        {
                            minutes = "0" + minutes;
                        }
                        if (minutes.Length == 0)
                        {
                            minutes = "00";
                        }

                        string seconds = ((matchtime - timer) / 60 - int.Parse(minutes) * 60).ToString();
                        if (seconds.Length == 1)
                        {
                            seconds = "0" + seconds;
                        }
                        if (seconds.Length == 0)
                        {
                            seconds = "00";
                        }
                        //this is where the timer is shown
                        if (GameModeSwitch.Value == 5)
                        {
                            string tempteam1 = team1.Value; string tempteam2 = team2.Value; string tempteam3 = team3.Value;
                            timerMessage.Message.Clear();
                            timerMessage.Message.Append(" Time: " + minutes + ":" + seconds
                                + "\n" + " " + capProgress1 + "\n" + capProgress2 + "\n" + capProgress3);
                        }
                        if (GameModeSwitch.Value != 5)
                        {
                            string tempteam1 = team1.Value; string tempteam2 = team2.Value; string tempteam3 = team3.Value;
                            timerMessage.Message.Clear();
                            timerMessage.Message.Append(" Time: " + minutes + ":" + seconds
                                + "\n" + " " + hudCap1 + " | " + hudCap2 + " | " + hudCap3);
                        }


                    }
                    integretyMessage.Message.Clear(); integretyMessage.Message.Append(temp_text);
                }

                if (vState == ViewState.ExitView)
                {
                    if (statMessage_Battle != null && text_api.Heartbeat)
                    {
                        if (statMessage_Battle.Visible)
                        {
                            statMessage_Battle.Message.Clear();
                            statMessage_Battle_Gunlist.Message.Clear();
                            statMessage_Battle.Visible = false;
                            statMessage_Battle_Gunlist.Visible = false;
                        }
                    }
                    vState = ViewState.None;
                }
            }
            catch (Exception e)
            {
            }

        }
        public static void Team1Wins()
        {
            LocalMatchState = 0;
            Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true);
            MyAPIGateway.Utilities.ShowMessage("GM", t_tempteam1 + "Wins");
        }
        public static void Team2Wins()
        {
            LocalMatchState = 0;
            Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true);
            MyAPIGateway.Utilities.ShowMessage("GM", t_tempteam2 + "Wins");
        }
        public static void Team3Wins()
        {
            LocalMatchState = 0;
            Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true);
            MyAPIGateway.Utilities.ShowMessage("GM", t_tempteam3 + "Wins");
        }


        public static void GameMode_1Cap()
        {
            //MyAPIGateway.Utilities.ShowMessage("GM" , "One Capture Zone Active");
            Local_GameModeSwitch = 1;
        }

        public static void GameMode_2Cap()
        {
            //MyAPIGateway.Utilities.ShowMessage("GM" , "Two Capture Zones Active");
            Local_GameModeSwitch = 2;
        }

        public static void GameMode_3Cap()
        {
            //MyAPIGateway.Utilities.ShowMessage("GM" , "Three Capture Zones Active");
            Local_GameModeSwitch = 3;
        }
        public static void GameMode_NoCap()
        {
            //MyAPIGateway.Utilities.ShowMessage("GM" , "Three Capture Zones Active");
            Local_GameModeSwitch = 4;
        }
        public static void GameMode_CrazyCap()
        {
            //MyAPIGateway.Utilities.ShowMessage("GM" , "Three Capture Zones Active");
            Local_GameModeSwitch = 5;
        }

        public static void There_Is_A_Problem()
        {
            Local_ProblemSwitch = 1;
        }

        public static void There_Is_A_Solution()
        {
            Local_ProblemSwitch = 0;
        }

        

        public void GameMode_Set()
        {
            GameModeSwitch.Value = Local_GameModeSwitch;

            switch (GameModeSwitch.Value)
            {
                case 1:
                    CapOut = 20;
                    ctrpoint = new Vector3(0, 0, 0);
                    ctrpoint2 = new Vector3(50000, 0, 0);
                    ctrpoint3 = new Vector3(0, 50000, 0);
                    capdist = 2500;
                    capdistCenter = 2500;
                    //MyAPIGateway.Utilities.ShowMessage("GM" , "capdist set to " + capdist);

                    break;
                case 2:
                    CapOut = 20;
                    ctrpoint = new Vector3(0, 3500, 0);
                    ctrpoint2 = new Vector3(0, -3500, 0);
                    ctrpoint3 = new Vector3(0, 50000, 0);
                    capdist = 1500;
                    capdistCenter = 1500;
                    //MyAPIGateway.Utilities.ShowMessage("GM" , "capdist set to " + capdist);
                    break;
                case 3:
                    CapOut = 20;
                    ctrpoint = new Vector3(0, 0, 0);
                    ctrpoint2 = new Vector3(4500, 1500, 9000);
                    ctrpoint3 = new Vector3(-4500, -1500, -9000);
                    capdist = 1000;
                    capdistCenter = 1000;
                    //MyAPIGateway.Utilities.ShowMessage("GM" , "capdist set to " + capdist);
                    break;
                case 4:
                    CapOut = 20;
                    ctrpoint = new Vector3(150000, 150000, 150000);
                    ctrpoint2 = new Vector3(150000, 150000, 150000);
                    ctrpoint3 = new Vector3(150000, 150000, 150000);
                    capdist = 1000;
                    capdistCenter = 1000;
                    //MyAPIGateway.Utilities.ShowMessage("GM" , "capdist set to " + capdist);
                    break;
                case 5:

                    if (IAmTheCaptainNow)
                    {
                        CapOut = 10;
                        ctrpoint = CrazyKing();
                        ClientRandVector3D = ctrpoint;
                        CaptainRandVector3D.Value = ClientRandVector3D;
                        CaptainRandVector3D.Push();
                        MyAPIGateway.Utilities.ShowMessage("GM", "RandomVector3d:" + ctrpoint.ToString());
                    }
                    else if (!IAmTheCaptainNow)
                    {
                        CapOut = 10;
                        CaptainRandVector3D.Fetch();
                        ClientRandVector3D = CaptainRandVector3D.Value;
                        ctrpoint = CaptainRandVector3D.Value;
                        MyAPIGateway.Utilities.ShowMessage("GM", "ClientRandomVector3d:" + ctrpoint.ToString());
                    }
                    ctrpoint2 = new Vector3(150000, 150000, 150000);
                    ctrpoint3 = new Vector3(150000, 150000, 150000);
                    capdist = 1000;
                    capdistCenter = 1000;
                    //MyAPIGateway.Utilities.ShowMessage("GM" , "capdist set to " + capdist);
                    break;
            }


        }
        public enum PlayerNotice
        {
            ZoneCaptured,
            ZoneLost,
            EnemyDestroyed,
            TeamDestroyed,
        }
        private void BroadcastSound(IMyCharacter character, PlayerNotice notice)
        {
            try
            {
                if (character == null || _alertAudio == null || _alertAudio.IsPlaying) return;

                _alertAudio.CustomVolume = MyAPIGateway.Session.Config.GameVolume * 3f;

                //_alertAudio.Entity = null;
                _alertAudio.Entity = (MyEntity)character;
                MySoundPair pair = null;
                switch (notice)
                {
                    case PlayerNotice.ZoneCaptured:
                        pair = _ZoneCaptured;
                        break;
                    case PlayerNotice.ZoneLost:
                        pair = _ZoneLost;
                        break;
                    case PlayerNotice.EnemyDestroyed:
                        pair = _EnemyDestroyed;
                        break;
                    case PlayerNotice.TeamDestroyed:
                        pair = _TeamDestroyed;
                        break;
                }
                if (_alertAudio.Entity != null && pair != null) _alertAudio.PlaySound(pair, false, false, false, true, false, null, true);
            }
            catch
            {
            }
        }
        public static IMyPlayer GetOwner(long v)
        {
            if (all_players != null && all_players.ContainsKey(v))
            {
                return all_players[v];
            }
            return null;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            if (text_api != null)
            {
                text_api.Unload();
            }
            if (WC_api != null)
            {
                WC_api.Unload();
            }
            if (SH_api != null)
            {
                SH_api.Unload();
            }
            if (PointValues != null)
            {
                PointValues.Clear();
                Sending.Clear();
                Data.Clear();
                all_players.Clear();
                listPlayers.Clear();
            }

            //NetworkAPI.Instance.Close();
            foreach (var x in Data.Keys)
            {
                if (Tracking.Contains(x))
                {
                    Data[x].UpdateHud();
                }
                else
                {
                    Data[x].DisposeHud();
                }
            }

            MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
            Static?.Dispose();
            MyAPIGateway.Utilities.UnregisterMessageHandler(2546247, AddPointValues);
        }



        public static Vector3 CrazyKing()
        {
            int randombracketlow = -3000, randombrackethigh = 3000;

            int RandomInt1 = MyUtils.GetRandomInt(randombracketlow, randombrackethigh);
            int RandomInt2 = MyUtils.GetRandomInt(-6000, 6000);
            int RandomInt3 = MyUtils.GetRandomInt(randombracketlow, randombrackethigh);
            Vector3 CrazyOutputVector = new Vector3(RandomInt1, RandomInt2, RandomInt3);

            //MyAPIGateway.Utilities.ShowMessage("GM", "RandomVector3d:" + CrazyOutputVector.ToString());
            return CrazyOutputVector;
        }
    }



    public static class GridExtensions
    {
        public static bool HasBlockWithSubtypeId(this IMyCubeGrid grid, string subtypeId)
        {
            bool found = false;

            grid.GetBlocks(null, delegate (IMySlimBlock block)
            {
                if (block.FatBlock != null && block.BlockDefinition.Id.SubtypeName == subtypeId)
                {
                    found = true;
                    return false; // Stop the GetBlocks iteration once a matching block is found
                }
                return false;
            });

            return found;
        }
    }


}