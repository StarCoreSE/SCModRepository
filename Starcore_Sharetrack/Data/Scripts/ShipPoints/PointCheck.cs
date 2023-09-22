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
        private static readonly Dictionary<long, List<ulong>> SendingDictionary = new Dictionary<long, List<ulong>>();
        public static Dictionary<long, List<ulong>> Sending = SendingDictionary;
        private static readonly Dictionary<long, ShipTracker> DataDictionary = new Dictionary<long, ShipTracker>();
        public static Dictionary<long, ShipTracker> Data = DataDictionary;
        public static HashSet<long> Tracking = new HashSet<long>();
        public string capstat = "";
        public string ZoneControl1 = ""; public string ZoneControl2 = ""; public string ZoneControl3 = ""; public string ZoneControl = "";
        private static Dictionary<long, IMyPlayer> all_players = new Dictionary<long, IMyPlayer>();
        private static List<IMyPlayer> listPlayers = new List<IMyPlayer>();
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


        private readonly List<MyEntity> _managedEntities = new List<MyEntity>(1000);
        private int _count;
        private const double CombatRadius = 12500;
        private BoundingSphereD _combatMaxSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius + 22500);

        public enum ViewState { None, InView, InView2, GridSwitch, ExitView };
        ViewState vState = ViewState.None;

        public enum ViewStateP { ThisIsFine, ItsOver }
        ViewStateP vStateP = ViewStateP.ThisIsFine;

        HudAPIv2 text_api;
        public static HudAPIv2.HUDMessage statMessage, integretyMessage, timerMessage, ticketmessage, statMessage_Battle, statMessage_Battle_Gunlist, problemmessage;
        public static bool broadcaststat = false;
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
                { SphereVisual = !SphereVisual; }
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
            PointCheckHelpers.timer = 0;
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
            PointCheckHelpers.timer = 0;
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

        public static void AddPointValues(object obj)
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
                            // Replace "{LS}" with "Large" and update the PointValues SendingDictionary
                            string largeName = name.Substring(0, lsIndex) + "Large" + name.Substring(lsIndex + "{LS}".Length);
                            PointValues[largeName] = value;

                            // Replace "{LS}" with "Small" and update the PointValues SendingDictionary
                            string smallName = name.Substring(0, lsIndex) + "Small" + name.Substring(lsIndex + "{LS}".Length);
                            PointValues[smallName] = value;
                        }
                        else
                        {
                            // Update the PointValues SendingDictionary directly
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


        public override void UpdateAfterSimulation()
        {
            temp_LocalTimer++; PointCheckHelpers.timer++; if (PointCheckHelpers.timer >= 144000) { PointCheckHelpers.timer = 0; temp_LocalTimer = 0; temp_ServerTimer = 0; }
            if (joinInit == true)
            {
            }
            if (MyAPIGateway.Utilities.IsDedicated && temp_ServerTimer % 60 == 0 && broadcaststat)
            {
                ServerSyncTimer.Value = temp_ServerTimer; ServerSyncTimer.Push();
            }
            if (broadcaststat && !IAmTheCaptainNow && temp_LocalTimer % 60 == 0)
            {
                ServerSyncTimer.Fetch(); PointCheckHelpers.timer = ServerSyncTimer.Value; temp_LocalTimer = 0;
            }
            try
            {
                if (!MyAPIGateway.Utilities.IsDedicated && broadcaststat)
                {
                    bool tick100 = PointCheckHelpers.timer % 100 == 0; if (PointCheckHelpers.timer - _fastStart < 300 || tick100)
                    {
                        RefreshVisualState(); _fastStart = PointCheckHelpers.timer; if (joinInit == false) { Static.MyNetwork.TransmitToServer(new BasicPacket(7), true, true); ServerMatchState.Fetch(); team1.Fetch(); team2.Fetch(); team3.Fetch(); ServerMatchState.Fetch(); ServerSyncTimer.Fetch(); Team1Tickets.Fetch(); Team2Tickets.Fetch(); Team3Tickets.Fetch(); ThreeTeams.Fetch(); GameModeSwitch.Fetch(); Local_GameModeSwitch = GameModeSwitch.Value; joinInit = true; }
                    }
                }
                if (!MyAPIGateway.Utilities.IsDedicated && temp_LocalTimer % 60 == 0)
                {
                    if (ServerMatchState.Value == 1 && broadcaststat == false) { broadcaststat = true; }
                    if (!MyAPIGateway.Utilities.IsDedicated && IAmTheCaptainNow)
                    {
                        ServerMatchState.Value = LocalMatchState;
                    }
                    else if (!MyAPIGateway.Utilities.IsDedicated && !IAmTheCaptainNow)
                    {
                        LocalMatchState = ServerMatchState.Value;
                    }
                }
                if (broadcaststat && PointCheckHelpers.timer % 60 == 0)
                {
                    if (IAmTheCaptainNow && ServerMatchState.Value != 1) { ServerMatchState.Value = 1; }
                }
            }
            catch
            {
            }
            try
            {
                if (PointCheckHelpers.timer % 60 == 0 && broadcaststat)
                {
                    all_players.Clear(); MyAPIGateway.Multiplayer.Players.GetPlayers(listPlayers, delegate (IMyPlayer p) { all_players.Add(p.IdentityId, p); return false; }
                );
                    if (MyAPIGateway.Session.IsServer)
                    {
                        foreach (var x in Sending.Keys)
                        {
                            ShipTracker shipTracker; if (!Data.TryGetValue(x, out shipTracker))
                            {
                                var entity = MyEntities.GetEntityById(x) as IMyCubeGrid; if (entity != null && entity.Physics != null)
                                {
                                    shipTracker = new ShipTracker(entity); Data.Add(x, shipTracker); if (!MyAPIGateway.Utilities.IsDedicated) { shipTracker.CreateHud(); }
                                }
                            }
                            else
                            {
                                shipTracker.Update();
                            }
                            if (shipTracker != null)
                            {
                                foreach (var p in Sending[x])
                                {
                                    PacketGridData packet = new PacketGridData { id = x, tracked = shipTracker }
                                ;
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
                if (PointCheckHelpers.timer % 60 == 0 && broadcaststat)
                {
                    bool tick100 = _count % 100 == 0; _count++; if (_count - _fastStart < 300 || tick100)
                    {
                        _managedEntities.Clear(); MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref _combatMaxSphere, _managedEntities, MyEntityQueryType.Dynamic); var posZero = Vector3D.Zero; foreach (var entity in _managedEntities)
                        {
                            var grid = entity as MyCubeGrid; if (grid != null && GridExtensions.HasBlockWithSubtypeId(grid, "LargeFlightMovement"))
                            {
                                long entityId = grid.EntityId; if (!Tracking.Contains(entityId))
                                {
                                    PacketGridData packet = new PacketGridData { id = entityId, value = (byte)(Tracking.Contains(entityId) ? 2 : 1), }
                ;
                                    Static.MyNetwork.TransmitToServer(packet, true);
                                    if (packet.value == 1)
                                    {
                                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Added grid to tracker"); Tracking.Add(entityId); if (!integretyMessage.Visible) { integretyMessage.Visible = true; }
                                        Data[entityId].CreateHud();
                                    }
                                    else
                                    {
                                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Removed grid from tracker"); Tracking.Remove(entityId); Data[entityId].DisposeHud();
                                    }
                                }
                                _fastStart = _count;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }






        public override void Draw()
        { //if you are the server do nothing here
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }
            try
            {
                var session = MyAPIGateway.Session;
                var input = MyAPIGateway.Input;
                var gui = MyAPIGateway.Gui;
                var promoLevel = session.PromoteLevel;

                if (session?.Camera != null && session.CameraController != null && !gui.ChatEntryVisible && !gui.IsCursorVisible && gui.GetCurrentScreen == MyTerminalPageEnum.None)
                {
                    bool isShiftPressed = input.IsKeyPress(MyKeys.LeftShift) || input.IsKeyPress(MyKeys.Shift);
                    if (isShiftPressed)
                    {
                        if (input.IsNewKeyPressed(MyKeys.T))
                        {
                            vState = vState == ViewState.None ? ViewState.InView : (vState == ViewState.InView ? ViewState.InView2 : ViewState.ExitView);
                        }

                        if (promoLevel >= MyPromoteLevel.Moderator)
                        {
                            var camMat = session.Camera.WorldMatrix;
                            IHitInfo hits = null;
                            var keyAndActionPairs = new Dictionary<MyKeys, Action>
            {
                {
                    MyKeys.M, () =>
                    {
                        MyAPIGateway.Physics.CastRay(camMat.Translation + camMat.Forward * 0.5, camMat.Translation + camMat.Forward * 500, out hits);
                        if(hits != null && hits.HitEntity is IMyCubeGrid)
                        {
                            PacketGridData packet = new PacketGridData { id = hits.HitEntity.EntityId, value = (byte)(Tracking.Contains(hits.HitEntity.EntityId) ? 2 : 1) };
                            Static.MyNetwork.TransmitToServer(packet, true);

                            if(packet.value == 1)
                            {
                                MyAPIGateway.Utilities.ShowNotification("ShipTracker: Added grid to tracker");
                                Tracking.Add(hits.HitEntity.EntityId);
                                if (!integretyMessage.Visible) integretyMessage.Visible = true;
                                Data[hits.HitEntity.EntityId].CreateHud();
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowNotification("ShipTracker: Removed grid from tracker");
                                Tracking.Remove(hits.HitEntity.EntityId);
                                Data[hits.HitEntity.EntityId].DisposeHud();
                            }
                        }
                    }
                },
                {
                    MyKeys.N, () =>
                    {
                        integretyMessage.Visible = !integretyMessage.Visible;
                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Hud visibility set to " + integretyMessage.Visible);
                    }
                },
                {
                    MyKeys.B, () =>
                    {
                        timerMessage.Visible = !timerMessage.Visible;
                        ticketmessage.Visible = !ticketmessage.Visible;
                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Timer visibility set to " + timerMessage.Visible);
                    }
                },
                {
                    MyKeys.J, () =>
                    {
                        viewstat++;
                        if (viewstat == 4) viewstat = 0; PointCheckHelpers.NameplateVisible = viewstat != 3;
                        MyAPIGateway.Utilities.ShowNotification("ShipTracker: Nameplate visibility set to " + viewmode[viewstat]);
                    }
                }
            };

                            foreach (var pair in keyAndActionPairs)
                            {
                                if (input.IsNewKeyPressed(pair.Key))
                                {
                                    pair.Value.Invoke();
                                }
                            }
                        }
                    }
                }

                vStateP = Local_ProblemSwitch == 1 ? ViewStateP.ItsOver : (Local_ProblemSwitch == 0 ? ViewStateP.ThisIsFine : vStateP);

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
                                if (PointCheckHelpers.timer % 60 == 0)
                                {
                                    ShipTracker tracked = new ShipTracker(icubeG);
                                    string pdInvestment = tracked.pdPercentage.ToString();
                                    string pdInvestmentNum = tracked.pdInvest.ToString();
                                    string totalShieldString = "None";

                                    if (tracked.TotalShieldStrength > 100)
                                    {
                                        totalShieldString = Math.Round((tracked.TotalShieldStrength / 100f), 2).ToString() + " M";
                                    }
                                    else if (tracked.TotalShieldStrength > 1 && tracked.TotalShieldStrength < 100)
                                    {
                                        totalShieldString = Math.Round((tracked.TotalShieldStrength), 0).ToString() + "0 K";
                                    }

                                    string gunText = "";
                                    foreach (var x in tracked.GunL.Keys)
                                    {
                                        gunText += $"<color=Green>{tracked.GunL[x]}<color=White> x {x}\n";
                                    }

                                    string specialBlockText = "";
                                    foreach (var x in tracked.SBL.Keys)
                                    {
                                        specialBlockText += $"<color=Green>{tracked.SBL[x]}<color=White> x {x}\n";
                                    }

                                    string massString = tracked.Mass.ToString();
                                    float thrustInKilograms = icubeG.GetMaxThrustInDirection(Base6Directions.Direction.Backward) / 9.81f;
                                    float weight = tracked.Mass;
                                    float mass = tracked.Mass;
                                    float TWR = thrustInKilograms / weight;

                                    if (tracked.Mass > 1000000)
                                    {
                                        massString = Math.Round((tracked.Mass / 1000000f), 2).ToString() + "m";
                                        mass = tracked.Mass / 1000f;
                                    }

                                    string TWRs = Math.Round((TWR), 3).ToString();
                                    string thrustString = tracked.InstalledThrust.ToString();

                                    if (tracked.InstalledThrust > 1000000)
                                    {
                                        thrustString = Math.Round((tracked.InstalledThrust / 1000000f), 2).ToString() + "M";
                                    }

                                    string playerName = tracked.Owner == null ? tracked.GridName : tracked.Owner.DisplayName;
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
                                    {
                                        tempPWR = (Math.Round(tracked.CurrentPower / 1000, 1)).ToString();
                                    }
                                    else
                                    {
                                        tempPWR = tracked.CurrentPower.ToString();
                                    }

                                    string PWR = tempPWR + PWRNotation;
                                    string GyroString = tracked.CurrentGyro.ToString();
                                    double tempGyro2;

                                    if (tracked.CurrentGyro >= 1000000)
                                    {
                                        tempGyro2 = Math.Round((tracked.CurrentGyro / 1000000f), 1);

                                        if (tempGyro2 > 1000)
                                        {
                                            GyroString = Math.Round((tempGyro2 / 1000), 1).ToString() + "G";
                                        }
                                        else
                                        {
                                            GyroString = tempGyro2.ToString() + "M";
                                        }
                                    }

                                    var basicInfo = $"----Basic Info----\n" +
                                        $"<color=Green>Name<color=White>: {icubeG.DisplayName}\n" +
                                        $"<color=Green>Owner<color=White>: {playerName}\n" +
                                        $"<color=Green>Faction<color=White>: {factionName}\n" +
                                        $"<color=Green>Mass<color=White>: {massString} kg\n" +
                                        $"<color=Green>Heavy blocks<color=White>: {tracked.Heavyblocks}\n" +
                                        $"<color=Green>Total blocks<color=White>: {tracked.BlockCount}\n" +
                                        $"<color=Green>PCU<color=White>: {tracked.PCU}\n" +
                                        $"<color=Green>Size<color=White>: {(icubeG.Max + Vector3.Abs(icubeG.Min)).ToString()}\n" +
                                        $"<color=Green>Max Speed<color=White>: {speed} | <color=Green>TWR<color=White>: {TWRs}\n\n";

                                    var battleStats = $"<color=Orange>----Battle Stats----<color=White>\n" +
                                        $"<color=Green>Battle Points<color=White>: {tracked.Bpts} " +
                                        $"<color=Orange>[<color=Red> {tracked.offensivePercentage}% " +
                                        $"<color=White>| <color=Green>{tracked.powerPercentage}% " +
                                        $"<color=White>| <color=DeepSkyBlue>{tracked.movementPercentage}% " +
                                        $"<color=White>| <color=LightGray>{tracked.miscPercentage}% <color=Orange>]\n" +
                                        $"<color=Green>PD Investment<color=White>: <color=Orange>( <color=white>{pdInvestmentNum} " +
                                        $"<color=Orange>|<color=Crimson> {pdInvestment}<color=White>%<color=Orange> )\n" +
                                        $"<color=Green>Shield Max HP<color=White>: {totalShieldString} ({(int)tracked.CurrentShieldStrength}%)\n" +
                                        $"<color=Green>Thrust<color=White>: {thrustString}N\n" +
                                        $"<color=Green>Gyro<color=White>: {GyroString}N\n" +
                                        $"<color=Green>Power<color=White>: {PWR}\n\n";

                                    var blocksInfo = $"<color=Orange>----Blocks----<color=White>\n{specialBlockText}\n\n";
                                    var armamentInfo = $"<color=Orange>----Armament----<color=White>\n{gunText}";

                                    var tempText = basicInfo + battleStats + blocksInfo + armamentInfo;


                                    statMessage.Message.Clear();
                                    statMessage.Message.Append(tempText);
                                    statMessage.Visible = true;
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
                            if (PointCheckHelpers.timer % 60 == 0)
                            {

                                ShipTracker tracked = new ShipTracker(icubeG);
                                string pdInvestment = tracked.pdPercentage.ToString();
                                string pdInvestmentNum = tracked.pdInvest.ToString();

                                string total_shield_string = "None";

                                if (tracked.TotalShieldStrength > 100)
                                {
                                    total_shield_string = (tracked.TotalShieldStrength / 100f).ToString("0.00") + " M";
                                }
                                else if (tracked.TotalShieldStrength > 1)
                                {
                                    total_shield_string = tracked.TotalShieldStrength.ToString("0.00") + "0 K";
                                }

                                string gunText = string.Join("\n", tracked.GunL.Keys.Select(x => $"<color=Green>{tracked.GunL[x]}<color=White> x {x}"));

                                string specialBlockText = string.Join("\n", tracked.SBL.Keys.Select(x => $"<color=Green>{tracked.SBL[x]}<color=White> x {x}"));

                                string massString = tracked.Mass.ToString("0.00");

                                float thrustInKilograms = icubeG.GetMaxThrustInDirection(Base6Directions.Direction.Backward) / 9.81f; // Convert thrust from N to kg
                                float weight = tracked.Mass;
                                float mass = tracked.Mass;
                                float TWR = thrustInKilograms / weight;

                                if (tracked.Mass > 1000000)
                                {
                                    massString = (tracked.Mass / 1000000f).ToString("0.00") + "m";
                                    mass = tracked.Mass / 1000f; // Convert mass to metric tons
                                }

                                string TWRs = TWR.ToString("0.000");

                                string thrustString = tracked.InstalledThrust.ToString();

                                if (tracked.InstalledThrust > 1000000)
                                {
                                    thrustString = (tracked.InstalledThrust / 1000000f).ToString("0.00") + "M";
                                }

                                string playerName = tracked.Owner == null ? tracked.GridName : tracked.Owner.DisplayName;

                                string factionName = tracked.Owner == null ? "" : MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(tracked.OwnerID)?.Name;

                                float speed = icubeG.GridSizeEnum == MyCubeSize.Large
                                    ? MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed
                                    : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;

                                if (RTS_api != null && RTS_api.IsReady)
                                {
                                    speed = (float)Math.Round(RTS_api.GetMaxSpeed(icubeG), 2);
                                }

                                string PWRNotation = tracked.CurrentPower > 1000 ? "GW" : "MW";
                                string tempPWR = tracked.CurrentPower > 1000 ? (tracked.CurrentPower / 1000f).ToString("0.0") : tracked.CurrentPower.ToString();
                                string PWR = tempPWR + PWRNotation;

                                string GyroString = tracked.CurrentGyro.ToString();

                                if (tracked.CurrentGyro >= 1000000)
                                {
                                    double tempGyro2 = tracked.CurrentGyro / 1000000f;
                                    if (tempGyro2 > 1000)
                                    {
                                        GyroString = (tempGyro2 / 1000).ToString("0.0") + "G";
                                    }
                                    else
                                    {
                                        GyroString = tempGyro2.ToString("0.0") + "M";
                                    }
                                }


                                var temp_text = $@"----Basic Info----
<color=Green>Name<color=White>: {icubeG.DisplayName}
<color=Green>Owner<color=White>: {playerName}
<color=Green>Faction<color=White>: {factionName}
<color=Green>Mass<color=White>: {massString} kg
<color=Green>Heavy blocks<color=White>: {tracked.Heavyblocks}
<color=Green>Total blocks<color=White>: {tracked.BlockCount}
<color=Green>PCU<color=White>: {tracked.PCU}
<color=Green>Size<color=White>: {(icubeG.Max + Vector3.Abs(icubeG.Min))}
<color=Green>Max Speed<color=White>: {speed} | <color=Green>TWR<color=White>: {TWRs}

<color=Orange>----Battle Stats----
<color=Green>Battle Points<color=White>: {tracked.Bpts} <color=Orange>[<color=Red> {tracked.offensivePercentage} <color=White>% <color=Orange>| <color=Green> {tracked.powerPercentage} <color=White>% <color=Orange>| <color=DeepSkyBlue>{tracked.movementPercentage} <color=White>% <color=Orange>| <color=LightGray>{tracked.miscPercentage} <color=White>% <color=Orange>]
<color=Green>PD Investment<color=White>: <color=Orange>( <color=White>{pdInvestmentNum} <color=Orange>|<color=Crimson> {pdInvestment} <color=White>%<color=Orange> )
<color=Green>Shield Max HP<color=White>: {total_shield_string} ({(int)tracked.CurrentShieldStrength}%)
<color=Green>Thrust<color=White>: {thrustString}N
<color=Green>Gyro<color=White>: {GyroString}N
<color=Green>Power<color=White>: {PWR}

<color=Orange>----Blocks----
{specialBlockText}

<color=Orange>----Armament----
{gunText}";

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
                                if (PointCheckHelpers.timer % 60 == 0)
                                {
                                    ShipTracker tracked = new ShipTracker(icubeG);

                                    // Shield String
                                    float totalShield = tracked.TotalShieldStrength;
                                    string total_shield_string = totalShield > 100
                                        ? (Math.Round(totalShield / 100f, 2) + " M")
                                        : (totalShield > 1
                                            ? (Math.Round(totalShield, 0) + "0 K")
                                            : "None");

                                    // Gyro String
                                    double currentGyro = tracked.CurrentGyro;
                                    string GyroString = currentGyro >= 1000000
                                        ? ((Math.Round(currentGyro / 1000000f, 1) > 1000)
                                            ? (Math.Round(currentGyro / 1000000000f, 1) + "G")
                                            : (Math.Round(currentGyro / 1000000f, 1) + "M"))
                                        : currentGyro.ToString();

                                    // Thrust String
                                    double installedThrust = tracked.InstalledThrust;
                                    string thrustString = installedThrust > 1000000
                                        ? (Math.Round(installedThrust / 1000000f, 2) + "M")
                                        : installedThrust.ToString();

                                    // Power String
                                    double currentPower = tracked.CurrentPower;
                                    string PWR = currentPower > 1000
                                        ? (Math.Round(currentPower / 1000, 1) + "GW")
                                        : (currentPower + "MW");

                                    // Gun Text
                                    StringBuilder gunText = new StringBuilder();
                                    foreach (var x in tracked.GunL.Keys)
                                    {
                                        gunText.Append("<color=Green>").Append(tracked.GunL[x]).Append("<color=White> x ").Append(x).Append("\n");
                                    }

                                    gunText.Append("\n<color=Green>Thrust<color=White>: ").Append(thrustString).Append("N")
                                           .Append("\n<color=Green>Gyro<color=White>: ").Append(GyroString).Append("N")
                                           .Append("\n<color=Green>Power<color=White>: ").Append(PWR);

                                    statMessage_Battle_Gunlist.Message.Length = 0;
                                    statMessage_Battle_Gunlist.Message.Append(gunText);

                                    statMessage_Battle.Message.Length = 0;
                                    statMessage_Battle.Message.Append("<color=White>").Append(total_shield_string).Append(" (").Append((int)tracked.CurrentShieldStrength).Append("%)");

                                    statMessage_Battle.Visible = true;
                                    statMessage_Battle_Gunlist.Visible = true;

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
                            if (PointCheckHelpers.timer % 60 == 0)
                            {
                                ShipTracker tracked = new ShipTracker(icubeG);

                                // Optimize Shield Strength String
                                double totalShield = tracked.TotalShieldStrength;
                                string total_shield_string = "None";
                                if (totalShield > 100) { total_shield_string = (Math.Round(totalShield / 100f, 2) + " M"); }
                                else if (totalShield > 1) { total_shield_string = (Math.Round(totalShield, 0) + "0 K"); }
                                string temp_text = "<color=White>" + total_shield_string + " (" + (int)tracked.CurrentShieldStrength + "%)";

                                // Optimize Gyro String
                                double currentGyro = tracked.CurrentGyro;
                                string GyroString = currentGyro.ToString();
                                if (currentGyro >= 1000000)
                                {
                                    double tempGyro2 = Math.Round(currentGyro / 1000000f, 1);
                                    GyroString = tempGyro2 > 1000 ? (Math.Round(tempGyro2 / 1000, 1) + "G") : (tempGyro2 + "M");
                                }

                                // Optimize Thrust String
                                double installedThrust = tracked.InstalledThrust;
                                string thrustString = installedThrust > 1000000 ? (Math.Round(installedThrust / 1000000f, 2) + "M") : installedThrust.ToString();

                                // Optimize Power String
                                double currentPower = tracked.CurrentPower;
                                string PWRNotation = currentPower > 1000 ? "GW" : "MW";
                                string tempPWR = currentPower > 1000 ? Math.Round(currentPower / 1000, 1).ToString() : currentPower.ToString();
                                string PWR = tempPWR + PWRNotation;

                                // Optimize Gun Text
                                StringBuilder gunText = new StringBuilder();
                                foreach (var x in tracked.GunL.Keys)
                                {
                                    gunText.AppendLine("<color=Green>" + tracked.GunL[x] + "<color=White> x " + x);
                                }
                                gunText.AppendLine("<color=Green>Thrust<color=White>: " + thrustString + "N")
                                       .AppendLine("<color=Green>Gyro<color=White>: " + GyroString + "N")
                                       .Append("<color=Green>Power<color=White>: " + PWR);

                                // Update Messages
                                statMessage_Battle_Gunlist.Message.Clear().Append(gunText);
                                statMessage_Battle.Message.Clear().Append(temp_text);

                                // Set Visibility
                                statMessage_Battle.Visible = statMessage_Battle_Gunlist.Visible = true;
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

                if (PointCheckHelpers.timer % 60 == 0 && integretyMessage != null && text_api.Heartbeat)
                {
                    var tt = new StringBuilder();
                    var ts = new Dictionary<string, List<string>>();
                    var m = new Dictionary<string, double>();
                    var bp = new Dictionary<string, int>();
                    var mbp = new Dictionary<string, int>();
                    var pbp = new Dictionary<string, int>();
                    var obp = new Dictionary<string, int>();
                    var mobp = new Dictionary<string, int>();
                    foreach (var z in Tracking)
                    {
                        if (!Data.ContainsKey(z)) continue;
                        var d = Data[z];
                        d.LastUpdate--;
                        if (d.LastUpdate <= 0) { Data[z].DisposeHud(); Data.Remove(z); continue; }
                        var fn = d.FactionName;
                        var o = d.OwnerName;
                        var nd = d.IsFunctional;
                        if (!d.IsFunctional) continue; //disables readout when ship is dead
                        if (!ts.ContainsKey(fn)) { ts.Add(fn, new List<string>()); m[fn] = 0; bp[fn] = 0; mbp[fn] = 0; pbp[fn] = 0; obp[fn] = 0; mobp[fn] = 0; }
                        if (nd) { m[fn] += d.Mass; bp[fn] += d.Bpts; }
                        mbp[fn] += d.MiscBps; pbp[fn] += d.PowerBps; obp[fn] += d.OffensiveBps; mobp[fn] += d.MovementBps;
                        int g = 0; foreach (var s in d.GunL.Values) g += s;
                        var pwr = d.CurrentPower > 1000 ? Math.Round(d.CurrentPower / 1000, 1) + "GW" : d.CurrentPower + "MW";
                        var ts2 = d.InstalledThrust >= 1e6 ? (Math.Round(d.InstalledThrust / 1e6, 1) > 1e3 ? Math.Round(Math.Round(d.InstalledThrust / 1e6, 1) / 1e3, 1) + "G" : Math.Round(d.InstalledThrust / 1e6, 1) + "M") : d.InstalledThrust.ToString();
                        ts[fn].Add(string.Format("<color={9}>{0,-8}{1,3}%<color={9}> P:<color=orange>{7,3}<color={9}> T:<color=orange>{8,3}<color={9}> W:<color={6}>{2,3}<color={9}> S:<color={5}>{3,3}%<color=white> {4,3}", o?.Substring(0, Math.Min(o.Length, 7)) ?? d.GridName, (int)(d.CurrentIntegrity / d.OriginalIntegrity * 100), g, (int)d.CurrentShieldStrength, capstat, (int)d.CurrentShieldStrength <= 0 ? "red" : $"{255},{255 - (d.ShieldHeat * 20)},{255 - (d.ShieldHeat * 20)}", g == 0 ? "red" : "orange", pwr, ts2, nd ? "white" : "red"));
                    }
                    foreach (var x in ts.Keys)
                    {
                        var ms = Math.Round(m[x] / 1e6, 2) + "M";
                        float tbp = obp[x] + mobp[x] + pbp[x] + mbp[x], tbi = 100f / bp[x];
                        tt.Append($"<color=white>---- <color=orange>{x} : {ms} : {bp[x]}bp <color=orange>[<color=Red>{(int)(obp[x] * tbi + 0.5f)}<color=white>%<color=orange>|<color=Green>{(int)(pbp[x] * tbi + 0.5f)}<color=white>%<color=orange>|<color=DeepSkyBlue>{(int)(mobp[x] * tbi + 0.5f)}<color=white>%<color=orange>|<color=LightGray>{(int)(mbp[x] * tbi + 0.5f)}<color=white>%<color=orange>]<color=white> ---------\n");
                        foreach (var y in ts[x]) tt.Append(y + "\n");
                    }
                    try
                    {
                        var ce = MyAPIGateway.Session.Player?.Controller?.ControlledEntity?.Entity;
                        var ck = ce as IMyCockpit;
                        var eid = ck.CubeGrid.EntityId;
                        if (ck != null && !Tracking.Contains(eid))
                        {
                            bool hg = false, hbr = false;
                            var gb = new List<IMySlimBlock>();
                            ck.CubeGrid.GetBlocks(gb);
                            foreach (var b in gb) { if (b.FatBlock is IMyGyro) hg = true; else if (b.FatBlock is IMyBatteryBlock || b.FatBlock is IMyReactor) hbr = true; if (hg && hbr) break; }
                            if (hg && hbr) { var p = new PacketGridData { id = eid, value = 1, }; Static.MyNetwork.TransmitToServer(p, true); MyAPIGateway.Utilities.ShowNotification("ShipTracker: Added grid to tracker"); Tracking.Add(eid); if (!integretyMessage.Visible) integretyMessage.Visible = true; Data[eid].CreateHud(); }
                        }
                    }
                    catch (Exception) { }
                    integretyMessage.Message.Clear();
                    integretyMessage.Message.Append(tt);
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

        public static void There_Is_A_Problem()
        {
            Local_ProblemSwitch = 1;
        }
       
        public static void There_Is_A_Solution()
        {
            Local_ProblemSwitch = 0;
        }

        public enum PlayerNotice
        {
            ZoneCaptured,
            ZoneLost,
            EnemyDestroyed,
            TeamDestroyed,
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