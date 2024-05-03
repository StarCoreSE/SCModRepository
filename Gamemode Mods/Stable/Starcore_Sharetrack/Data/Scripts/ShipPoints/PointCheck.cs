using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreSystems.Api;
using DefenseShields;
using Draygo.API;
using Math0424.ShipPoints;
using RelativeTopSpeed;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SENetworkAPI;
using ShipPoints.Data.Scripts.ShipPoints.Networking;
using ShipPoints.MatchTiming;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using ShipPoints.ShipTracking;
using static Math0424.Networking.MyNetworkHandler;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace ShipPoints
{
    public class PointCheck
    {
        public static PointCheck I;

        public enum ProblemReportState
        {
            ThisIsFine,
            ItsOver
        }

        public static NetSync<int> ServerMatchState;
        public static int LocalMatchState;
        public static bool AmTheCaptainNow;
        public static int LocalProblemSwitch;
        public static Dictionary<string, int> PointValues = new Dictionary<string, int>();

        private static readonly Dictionary<long, IMyPlayer> AllPlayers = new Dictionary<long, IMyPlayer>();
        private static readonly List<IMyPlayer> ListPlayers = new List<IMyPlayer>();

        public static HudAPIv2.HUDMessage
            IntegretyMessage,
            TimerMessage,
            Ticketmessage,
            Problemmessage;

        public static bool Broadcaststat;
        public static ShipTracker.NametagSettings NametagViewState = ShipTracker.NametagSettings.PlayerName;
        public static int Decaytime = 180;
        public static int Delaytime = 60; //debug


        private HashSet<IMyEntity> _managedEntities = new HashSet<IMyEntity>();

        private int _count;
        private int _fastStart;


        private readonly Dictionary<string, int> _bp = new Dictionary<string, int>();

        // Get the sphere model based on the given cap color

        private bool _awaitingTrackRequest = true;
        private bool _joinInit;
        private readonly Dictionary<string, double> _m = new Dictionary<string, double>();
        private readonly Dictionary<string, int> _mbp = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _mobp = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _obp = new Dictionary<string, int>();

        private readonly Dictionary<string, int> _pbp = new Dictionary<string, int>();

        //Old cap
        public bool SphereVisual = true;
        public NetSync<string> Team1;
        public NetSync<string> Team2;
        public NetSync<string> Team3;


        public NetSync<int> ThreeTeams;

        // todo: remove this and replace with old solution for just combining BP and mass
        private readonly Dictionary<string, List<string>> _ts = new Dictionary<string, List<string>>();

        public HudAPIv2 TextHudApi { get; private set; }
        public WcApi WcApi { get; private set; }
        public ShieldApi ShieldApi { get; private set; }
        public RtsApi RtsApi { get; private set; }

        private HudPointsList _hudPointsList;


        #region Public Methods

        public void Init()
        {
            I = this;

            MyAPIGateway.Utilities.ShowMessage("ShipPoints v3.2 - Control Zone",
                "Aim at a grid and press Shift+T to show stats, " +
                "Shift+M to track a grid, Shift+J to cycle nametag style. ");

            InitializeNetSyncVariables();
            MyAPIGateway.Utilities.RegisterMessageHandler(2546247, AddPointValues);

            // Check if the current instance is not a dedicated server
            if (!MyAPIGateway.Utilities.IsDedicated)
                // Initialize the sphere entities
                // Initialize the text_api with the HUDRegistered callback
                TextHudApi = new HudAPIv2(HudRegistered);

            // Initialize the WC_api and load it if it's not null

            WcApi = new WcApi();
            WcApi?.Load();

            // Initialize the SH_api and load it if it's not null
            ShieldApi = new ShieldApi();
            ShieldApi?.Load();

            // Initialize the RTS_api and load it if it's not null
            RtsApi = new RtsApi();
            RtsApi?.Load();
        }

        public void Close()
        {
            Log.Info("Start PointCheck.UnloadData()");

            TextHudApi?.Unload();
            WcApi?.Unload();
            ShieldApi?.Unload();
            if (PointValues != null)
            {
                PointValues.Clear();
                AllPlayers.Clear();
                ListPlayers.Clear();
            }

            MyAPIGateway.Utilities.UnregisterMessageHandler(2546247, AddPointValues);

            I = null;
        }

        public void UpdateAfterSimulation()
        {
            // Send request to server for tracked grids.
            if (_awaitingTrackRequest && !MyAPIGateway.Session.IsServer)
            {
                Static.MyNetwork.TransmitToServer(new SyncRequestPacket(), false);
                _awaitingTrackRequest = false;
            }

            try
            {
                UpdateTrackingData();

                if (!MyAPIGateway.Utilities.IsDedicated && Broadcaststat)
                {
                    var tick100 = MatchTimer.I.Ticks % 100 == 0;
                    if (MatchTimer.I.Ticks - _fastStart < 300 || tick100)
                    {
                        _fastStart = MatchTimer.I.Ticks;
                        if (_joinInit == false)
                        {
                            Static.MyNetwork.TransmitToServer(new BasicPacket(7), true, true);
                            ServerMatchState.Fetch();
                            Team1.Fetch();
                            Team2.Fetch();
                            Team3.Fetch();
                            ServerMatchState.Fetch();
                            ThreeTeams.Fetch();
                            _joinInit = true;
                        }
                    }
                }

                if (!MyAPIGateway.Utilities.IsDedicated && MatchTimer.I.Ticks % 60 == 0)
                {
                    if (ServerMatchState.Value == 1 && Broadcaststat == false) Broadcaststat = true;
                    if (!MyAPIGateway.Utilities.IsDedicated && AmTheCaptainNow)
                        ServerMatchState.Value = LocalMatchState;
                    else if (!MyAPIGateway.Utilities.IsDedicated && !AmTheCaptainNow)
                        LocalMatchState = ServerMatchState.Value;
                }

                if (Broadcaststat && MatchTimer.I.Ticks % 60 == 0)
                    if (AmTheCaptainNow && ServerMatchState.Value != 1)
                        ServerMatchState.Value = 1;
            }
            catch (Exception e)
            {
                Log.Error($"Exception in UpdateAfterSimulation TryCatch 01: {e}");
            }

            try
            {
                if (MatchTimer.I.Ticks % 60 == 0)
                {
                    AllPlayers.Clear();
                    MyAPIGateway.Multiplayer.Players.GetPlayers(ListPlayers, delegate (IMyPlayer p)
                    {
                        AllPlayers.Add(p.IdentityId, p);
                        return false;
                    }
                    );
                }
            }
            catch (Exception e)
            {
                Log.Error($"Exception in UpdateAfterSimulation TryCatch 02: {e}");
            }

            try
            {
                if (MatchTimer.I.Ticks % 60 == 0 && Broadcaststat)
                {
                    _count++;
                    if (_count - _fastStart < 300 || _count % 100 == 0)
                    {
                        _managedEntities.Clear();
                        MyAPIGateway.Entities.GetEntities(_managedEntities, entity => entity is IMyCubeGrid);
                        foreach (var entity in _managedEntities)
                        {
                            var grid = entity as MyCubeGrid;
                            if (grid == null || !grid.HasSpecialBlocksWithSubtypeId("LargeFlightMovement", "RivalAIRemoteControlLarge"))
                                continue;

                            TrackingManager.I.TrackGrid(grid, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Exception in UpdateAfterSimulation TryCatch 03: {e}");
            }
        }

        public void Draw()
        {
            //if you are the server do nothing here
            if (MyAPIGateway.Utilities.IsDedicated || !TextHudApi.Heartbeat)
                return;
            try
            {
                if (MyAPIGateway.Session?.Camera != null && MyAPIGateway.Session.CameraController != null && !MyAPIGateway.Gui.ChatEntryVisible &&
                    !MyAPIGateway.Gui.IsCursorVisible && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
                {
                    HandleKeyInputs();
                }

                foreach (var tracker in TrackingManager.I.TrackedGrids.Values)
                    tracker.UpdateHud();

                Problemmessage.Message.Clear();
                switch ((ProblemReportState)LocalProblemSwitch)
                {
                    case ProblemReportState.ItsOver:
                        const string tempText = "<color=Red>" + "A PROBLEM HAS BEEN REPORTED," + "\n" +
                                                "CHECK WITH BOTH TEAMS AND THEN TYPE '/st fixed' TO CLEAR THIS MESSAGE";
                        Problemmessage.Message.Append(tempText);
                        Problemmessage.Visible = true;
                        break;
                    case ProblemReportState.ThisIsFine:
                        Problemmessage.Visible = false;
                        break;
                }

                _hudPointsList?.UpdateDraw();
            }
            catch (Exception e)
            {
                Log.Error($"Exception in Draw: {e}");
            }
        }

        #endregion

        private void InitializeNetSyncVariables()
        {
            Team1 = CreateNetSync("RED");
            Team2 = CreateNetSync("BLU");
            Team3 = CreateNetSync("NEU");

            ServerMatchState = CreateNetSync(0);

            ThreeTeams = CreateNetSync(0);

            //ProblemSwitch = CreateNetSync<int>(0);
        }

        private NetSync<T> CreateNetSync<T>(T defaultValue)
        {
            return new NetSync<T>(MasterSession.I, TransferType.Both, defaultValue, false, false);
        }

        public static void Begin()
        {
            MatchTimer.I.Ticks = 0;
            Broadcaststat = true;
            if (TimerMessage != null)
                TimerMessage.Visible = true;
            if (Ticketmessage != null)
                Ticketmessage.Visible = true;
            LocalMatchState = 1;
            MatchTimer.I.Start();
            MyAPIGateway.Utilities.ShowNotification("Commit die. Zone activates in " + Delaytime / 3600 +
                                                    "m, match ends in " + MatchTimer.I.MatchDurationMinutes + "m.");
            MyLog.Default.WriteLineAndConsole("Match started!");
        }

        public static void EndMatch()
        {
            MatchTimer.I.Ticks = 0;
            Broadcaststat = false;
            if (TimerMessage != null)
                TimerMessage.Visible = false;
            if (Ticketmessage != null)
                Ticketmessage.Visible = false;
            LocalMatchState = 0;
            AmTheCaptainNow = false;
            MatchTimer.I.Stop();
            MyAPIGateway.Utilities.ShowNotification("Match Ended.");
        }

        public static void AddPointValues(object obj)
        {
            // Deserialize the byte array (obj) into a string (var)
            var var = MyAPIGateway.Utilities.SerializeFromBinary<string>((byte[])obj);

            // Check if the deserialization was successful
            if (var == null)
                return;

            // Split the string into an array of substrings using the ';' delimiter
            var split = var.Split(';');

            // Iterate through each substring (s) in the split array
            foreach (var s in split)
            {
                // Split the substring (s) into an array of parts using the '@' delimiter
                var parts = s.Split('@');
                int value;

                // Check if there are exactly 2 parts and if the second part is a valid integer (value)
                if (parts.Length != 2 || !int.TryParse(parts[1], out value))
                    continue;

                // Trim the first part (name) and remove any extra whitespaces
                var name = parts[0].Trim();
                var lsIndex = name.IndexOf("{LS}");

                // Check if the name contains "{LS}"
                if (lsIndex != -1)
                {
                    // Replace "{LS}" with "Large" and update the PointValues SendingDictionary
                    var largeName = name.Substring(0, lsIndex) + "Large" +
                                    name.Substring(lsIndex + "{LS}".Length);
                    PointValues[largeName] = value;

                    // Replace "{LS}" with "Small" and update the PointValues SendingDictionary
                    var smallName = name.Substring(0, lsIndex) + "Small" +
                                    name.Substring(lsIndex + "{LS}".Length);
                    PointValues[smallName] = value;
                }
                else
                {
                    // Update the PointValues SendingDictionary directly
                    PointValues[name] = value;
                }
            }
        }

        private void HudRegistered()
        {
            _hudPointsList = new HudPointsList();
            
            IntegretyMessage = new HudAPIv2.HUDMessage(scale: 1.15f, font: "BI_SEOutlined",
                Message: new StringBuilder(""), origin: new Vector2D(.51, .95), hideHud: false,
                blend: BlendTypeEnum.PostPP)
            {
                Visible = true
            };
            TimerMessage = new HudAPIv2.HUDMessage(scale: 1.2f, font: "BI_SEOutlined", Message: new StringBuilder(""),
                origin: new Vector2D(0.35, .99), hideHud: false, shadowing: true, blend: BlendTypeEnum.PostPP)
            {
                Visible = false, //defaulted off?
                InitialColor = Color.White
            };
            Ticketmessage = new HudAPIv2.HUDMessage(scale: 1f, font: "BI_SEOutlined", Message: new StringBuilder(""),
                origin: new Vector2D(0.51, .99), hideHud: false, shadowing: true, blend: BlendTypeEnum.PostPP)
            {
                Visible = false, //defaulted off?
                InitialColor = Color.White
            };

            Problemmessage = new HudAPIv2.HUDMessage(scale: 2f, font: "BI_SEOutlined", Message: new StringBuilder(""),
                origin: new Vector2D(-.99, 0), hideHud: false, shadowing: true, blend: BlendTypeEnum.PostPP)
            {
                Visible = false, //defaulted off?
                InitialColor = Color.White
            };
        }


        private readonly Dictionary<MyKeys, Action> _keyAndActionPairs = new Dictionary<MyKeys, Action>
        {
            {
                MyKeys.M, () =>
                {
                    IMyCubeGrid castGrid = RaycastGridFromCamera();
                    if (castGrid == null)
                        return;

                    if (!TrackingManager.I.IsGridTracked(castGrid))
                        TrackingManager.I.TrackGrid(castGrid);
                    else
                        TrackingManager.I.UntrackGrid(castGrid);
                }
            },
            {
                MyKeys.N, () =>
                {
                    IntegretyMessage.Visible = !IntegretyMessage.Visible;
                    MyAPIGateway.Utilities.ShowNotification("ShipTracker: Hud visibility set to " +
                                                            IntegretyMessage.Visible);
                }
            },
            {
                MyKeys.B, () =>
                {
                    TimerMessage.Visible = !TimerMessage.Visible;
                    Ticketmessage.Visible = !Ticketmessage.Visible;
                    MyAPIGateway.Utilities.ShowNotification(
                        "ShipTracker: Timer visibility set to " + TimerMessage.Visible);
                }
            },
            {
                MyKeys.J, () =>
                {
                    NametagViewState++;
                    if (NametagViewState > (ShipTracker.NametagSettings) 3)
                        NametagViewState = 0;
                    PointCheckHelpers.NameplateVisible = NametagViewState != 0;
                    MyAPIGateway.Utilities.ShowNotification(
                        "ShipTracker: Nameplate visibility set to " + NametagViewState);
                }
            }
        };

        private void HandleKeyInputs()
        {
            if (!MyAPIGateway.Input.IsAnyShiftKeyPressed())
                return;

            if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.T))
                _hudPointsList?.CycleViewState();

            foreach (var pair in _keyAndActionPairs)
                if (MyAPIGateway.Input.IsNewKeyPressed(pair.Key))
                    pair.Value.Invoke();
        }


        private void UpdateTrackingData()
        {
            if (MatchTimer.I.Ticks % 60 != 0 || IntegretyMessage == null || !TextHudApi.Heartbeat)
                return;

            var tt = new StringBuilder();

            // Clear the dictionaries to remove old data
            _ts.Clear();
            _m.Clear();
            _bp.Clear();
            _mbp.Clear();
            _pbp.Clear();
            _obp.Clear();
            _mobp.Clear();

            MainTrackerUpdate(_ts, _m, _bp, _mbp, _pbp, _obp, _mobp);

            // Match time
            tt.Append("<color=orange>----                 <color=white>Match Time: ")
                .Append(MatchTimer.I.CurrentMatchTime.ToString(@"mm\:ss"))
                .Append('/')
                .Append(MatchTimer.I.MatchDurationString)
                .Append("                 <color=orange>----\n");

            TeamBpCalc(tt, _ts, _m, _bp, _mbp, _pbp, _obp, _mobp);

            // TODO re-introduce autotrack.

            IntegretyMessage.Message.Clear();
            IntegretyMessage.Message.Append(tt);
        }


        private void MainTrackerUpdate(Dictionary<string, List<string>> ts, Dictionary<string, double> m,
            Dictionary<string, int> bp, Dictionary<string, int> mbp, Dictionary<string, int> pbp,
            Dictionary<string, int> obp, Dictionary<string, int> mobp)
        {
            foreach (var shipTracker in TrackingManager.I.TrackedGrids.Values)
            {
                shipTracker.Update();

                var fn = shipTracker.FactionName;
                var o = shipTracker.OwnerName;
                var nd = shipTracker.IsFunctional;

                if (!ts.ContainsKey(fn))
                {
                    ts.Add(fn, new List<string>());
                    m[fn] = 0;
                    bp[fn] = 0;
                    mbp[fn] = 0;
                    pbp[fn] = 0;
                    obp[fn] = 0;
                    mobp[fn] = 0;
                }

                if (nd)
                {
                    m[fn] += shipTracker.Mass;
                    bp[fn] += shipTracker.BattlePoints;
                }
                else
                {
                    continue;
                }

                mbp[fn] += shipTracker.RemainingPoints;
                pbp[fn] += shipTracker.PowerPoints;
                obp[fn] += shipTracker.OffensivePoints;
                mobp[fn] += shipTracker.MovementPoints;

                var g = shipTracker.WeaponCounts.Values.Sum();
                var pwr = FormatPower(Math.Round(shipTracker.TotalPower, 1));
                var ts2 = FormatThrust(Math.Round(shipTracker.TotalThrust, 2));

                ts[fn].Add(CreateDisplayString(o, shipTracker, g, pwr, ts2));
            }
        }

        private string FormatPower(double currentPower)
        {
            return currentPower > 1000 ? $"{Math.Round(currentPower / 1000, 1)}GW" : $"{currentPower}MW";
        }

        private string FormatThrust(double installedThrust)
        {
            var thrustInMega = Math.Round(installedThrust / 1e6, 1);
            return thrustInMega > 1e2 ? $"{Math.Round(thrustInMega / 1e3, 2)}GN" : $"{thrustInMega}MN";
        }

        private string CreateDisplayString(string ownerName, ShipTracker d, int g, string power, string thrust)
        {
            var ownerDisplay = ownerName != null ? ownerName.Substring(0, Math.Min(ownerName.Length, 7)) : d.GridName;
            var integrityPercent = (int)(d.MaxShieldHealth / d.OriginalMaxShieldHealth * 100);
            var shieldPercent = (int)d.CurrentShieldPercent;
            var shieldColor = shieldPercent <= 0
                ? "red"
                : $"{255},{255 - d.CurrentShieldHeat * 20},{255 - d.CurrentShieldHeat * 20}";
            var weaponColor = g == 0 ? "red" : "orange";
            var functionalColor = d.IsFunctional ? "white" : "red";
            return
                $"<color={functionalColor}>{ownerDisplay,-8}{integrityPercent,3}%<color={functionalColor}> P:<color=orange>{power,3}<color={functionalColor}> T:<color=orange>{thrust,3}<color={functionalColor}> W:<color={weaponColor}>{g,3}<color={functionalColor}> S:<color={shieldColor}>{shieldPercent,3}%<color=white>";
        }


        private static void TeamBpCalc(StringBuilder tt, Dictionary<string, List<string>> trackedShip,
            Dictionary<string, double> m, Dictionary<string, int> bp, Dictionary<string, int> mbp,
            Dictionary<string, int> pbp, Dictionary<string, int> obp, Dictionary<string, int> mobp)
        {
            foreach (var x in trackedShip.Keys)
            {
                var msValue = m[x] / 1e6;
                var tbi = 100f / bp[x];

                tt.Append("<color=orange>---- ")
                    .Append(x)
                    .Append(" : ")
                    .AppendFormat("{0:0.00}M : {1}bp <color=orange>[", msValue, bp[x]);

                tt.AppendFormat("<color=Red>{0}<color=white>%<color=orange>|", (int)(obp[x] * tbi + 0.5f))
                    .AppendFormat("<color=Green>{0}<color=white>%<color=orange>|", (int)(pbp[x] * tbi + 0.5f))
                    .AppendFormat("<color=DeepSkyBlue>{0}<color=white>%<color=orange>|", (int)(mobp[x] * tbi + 0.5f))
                    .AppendFormat("<color=LightGray>{0}<color=white>%<color=orange>]", (int)(mbp[x] * tbi + 0.5f))
                    .AppendLine(" ---------");

                foreach (var y in trackedShip[x]) tt.AppendLine(y);
            }
        }

        public static void There_Is_A_Problem()
        {
            LocalProblemSwitch = 1;
        }

        public static void There_Is_A_Solution()
        {
            LocalProblemSwitch = 0;
        }

        public static IMyPlayer GetOwner(long v)
        {
            if (AllPlayers != null && AllPlayers.ContainsKey(v)) return AllPlayers[v];
            return null;
        }

        public static IMyCubeGrid RaycastGridFromCamera()
        {
            var camMat = MyAPIGateway.Session.Camera.WorldMatrix;
            var hits = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(camMat.Translation, camMat.Translation + camMat.Forward * 500, hits);
            foreach (var hit in hits)
            {
                var grid = hit.HitEntity as IMyCubeGrid;

                if (grid?.Physics != null)
                    return grid;
            }

            return null;
        }
    }


    public static class GridExtensions
    {
        public static bool HasSpecialBlocksWithSubtypeId(this IMyCubeGrid grid, params string[] subtypeId)
        {
            List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();
            grid?.GetBlocks(allBlocks, block => block.FatBlock != null);

            foreach (IMySlimBlock block in allBlocks)
                if (subtypeId.Contains(block.BlockDefinition.Id.SubtypeName))
                    return true;

            return false;
        }
    }
}