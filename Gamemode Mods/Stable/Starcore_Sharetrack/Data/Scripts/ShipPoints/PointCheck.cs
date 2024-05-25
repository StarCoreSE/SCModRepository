using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreSystems.Api;
using DefenseShields;
using Draygo.API;
using RelativeTopSpeed;
using Sandbox.ModAPI;
using ShipPoints.HeartNetworking;
using ShipPoints.HeartNetworking.Custom;
using ShipPoints.MatchTiming;
using ShipPoints.ShipTracking;
using VRage;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace ShipPoints
{
    public class PointCheck
    {
        public enum MatchStateEnum
        {
            Stopped,
            Active
        }

        public static PointCheck I;
        public static MatchStateEnum MatchState;
        public static bool AmTheCaptainNow;


        public static Dictionary<string, int> PointValues = new Dictionary<string, int>();


        private static readonly Dictionary<long, IMyPlayer> AllPlayers = new Dictionary<long, IMyPlayer>();

        public static HudAPIv2.HUDMessage
            IntegretyMessage,
            TimerMessage,
            Ticketmessage,
            Problemmessage;

        public static ShipTracker.NametagSettings NametagViewState = ShipTracker.NametagSettings.PlayerName;
        public static int Delaytime = 60; //debug

        private readonly Dictionary<string, int> _bp = new Dictionary<string, int>(); // TODO refactor info storage


        private readonly Dictionary<MyKeys, Action> _keyAndActionPairs = new Dictionary<MyKeys, Action>
        {
            {
                MyKeys.M, () =>
                {
                    var castGrid = RaycastGridFromCamera();
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
                    if (NametagViewState > (ShipTracker.NametagSettings)3)
                        NametagViewState = 0;
                    MyAPIGateway.Utilities.ShowNotification(
                        "ShipTracker: Nameplate visibility set to " + NametagViewState);
                }
            }
        };

        private readonly List<IMyPlayer> _listPlayers = new List<IMyPlayer>();
        private readonly Dictionary<string, double> _m = new Dictionary<string, double>();
        private readonly Dictionary<string, int> _mbp = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _mobp = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _obp = new Dictionary<string, int>();

        private readonly Dictionary<string, int> _pbp = new Dictionary<string, int>();

        // todo: remove this and replace with old solution for just combining BP and mass
        private readonly Dictionary<string, List<string>> _ts = new Dictionary<string, List<string>>();

        // Get the sphere model based on the given cap color

        private bool _awaitingTrackRequest = true;
        private Func<string, MyTuple<string, float>> _climbingCostFunction;

        public string[] TeamNames = { "RED", "BLU" }; // TODO this doesn't actually do anything.

        private void ParsePointsDict(object message)
        {
            try
            {
                var dict = message as Dictionary<string, int>;
                if (dict != null)
                {
                    PointValues = dict;
                    return;
                }

                var climbCostFunc = message as Func<string, MyTuple<string, float>>;
                if (climbCostFunc != null)
                {
                    _climbingCostFunction = climbCostFunc;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public static void BeginMatch()
        {
            MatchTimer.I.Ticks = 0;
            if (TimerMessage != null)
                TimerMessage.Visible = true;
            if (Ticketmessage != null)
                Ticketmessage.Visible = true;
            MatchState = MatchStateEnum.Active;
            MatchTimer.I.Start();
            MyAPIGateway.Utilities.ShowNotification("Commit die. Zone activates in " + Delaytime / 3600 +
                                                    "m, match ends in " + MatchTimer.I.MatchDurationMinutes + "m.");
            Log.Info("Match started!");

            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(new GameStatePacket(I));
        }

        public static void EndMatch()
        {
            MatchTimer.I.Ticks = 0;
            if (TimerMessage != null)
                TimerMessage.Visible = false;
            if (Ticketmessage != null)
                Ticketmessage.Visible = false;
            MatchState = MatchStateEnum.Stopped;
            AmTheCaptainNow = false;
            MatchTimer.I.Stop();
            MyAPIGateway.Utilities.ShowNotification("Match Ended.");
            Log.Info("Match Ended.");

            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(new GameStatePacket(I));
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

        private string CreateDisplayString(string ownerName, ShipTracker tracker, int g, string power, string thrust)
        {
            var ownerDisplay = ownerName != null
                ? ownerName.Substring(0, Math.Min(ownerName.Length, 7))
                : tracker.GridName;
            var integrityPercent =
                (int)(tracker.GridIntegrity / tracker.OriginalGridIntegrity *
                      100); // TODO fix this to use hull integrity
            var shieldPercent = (int)tracker.CurrentShieldPercent;
            var shieldColor = shieldPercent <= 0
                ? "red"
                : $"{255},{255 - tracker.CurrentShieldHeat * 2.5f},{255 - tracker.CurrentShieldHeat * 2.5f}";
            var weaponColor = g == 0 ? "red" : "orange";
            var functionalColor = tracker.IsFunctional ? "white" : "red";
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

        public static IMyPlayer GetOwner(long v)
        {
            IMyPlayer owner;
            return AllPlayers.TryGetValue(v, out owner) ? owner : null;
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

        #region API Fields

        public HudAPIv2 TextHudApi { get; private set; }
        public WcApi WcApi { get; private set; }
        public ShieldApi ShieldApi { get; private set; }
        public RtsApi RtsApi { get; private set; }

        private HudPointsList _hudPointsList;

        #endregion


        #region Public Methods

        public void Init()
        {
            I = this;

            MyAPIGateway.Utilities.ShowMessage("ShipPoints v3.2 - Control Zone",
                "Aim at a grid and press Shift+T to show stats, " +
                "Shift+M to track a grid, Shift+J to cycle nametag style. ");

            MyAPIGateway.Utilities.RegisterMessageHandler(2546247, ParsePointsDict);

            // Check if the current instance is not a dedicated server
            if (!MyAPIGateway.Utilities.IsDedicated)
                // Initialize the sphere entities
                // Initialize the text_api with the HUDRegistered callback
                TextHudApi = new HudAPIv2(HudRegistered);

            // Avoid bootlock when opening world with autotracked grids.
            TrackingManager.Init();

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
            }

            MyAPIGateway.Utilities.UnregisterMessageHandler(2546247, ParsePointsDict);

            I = null;
        }

        public void UpdateAfterSimulation()
        {
            // Send request to server for tracked grids.
            if (_awaitingTrackRequest && !MyAPIGateway.Session.IsServer)
            {
                HeartNetwork.I.SendToServer(new SyncRequestPacket());
                _awaitingTrackRequest = false;
            }

            try
            {
                UpdateTrackingData();
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
                    MyAPIGateway.Multiplayer.Players.GetPlayers(_listPlayers, delegate(IMyPlayer p)
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
        }

        public void Draw()
        {
            //if you are the server do nothing here
            if (MyAPIGateway.Utilities.IsDedicated || !TextHudApi.Heartbeat)
                return;
            try
            {
                if (MyAPIGateway.Session?.Camera != null && MyAPIGateway.Session.CameraController != null &&
                    !MyAPIGateway.Gui.ChatEntryVisible &&
                    !MyAPIGateway.Gui.IsCursorVisible && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
                    HandleKeyInputs();

                foreach (var tracker in TrackingManager.I.TrackedGrids.Values)
                    tracker.UpdateHud();

                _hudPointsList?.UpdateDraw();
            }
            catch (Exception e)
            {
                Log.Error($"Exception in Draw: {e}");
            }
        }

        public void ReportProblem(string issueMessage = "", bool sync = true)
        {
            if (issueMessage.Length > 50)
                issueMessage = issueMessage.Substring(0, 50);

            Problemmessage.Message.Clear();
            Problemmessage.Message.Append(
                "<color=red>A PROBLEM HAS BEEN REPORTED.\n<color=white>CHECK WITH BOTH TEAMS AND THEN TYPE '/st fixed' TO CLEAR THIS MESSAGE.");
            if (issueMessage != "")
                Problemmessage.Message.Append("\n" + issueMessage);
            Problemmessage.Visible = true;

            if (sync)
                HeartNetwork.I.SendToEveryone(new ProblemReportPacket(true, issueMessage));
        }

        public void ResolvedProblem(bool sync = true)
        {
            Problemmessage.Message.Clear();
            Problemmessage.Visible = false;

            if (sync)
                HeartNetwork.I.SendToEveryone(new ProblemReportPacket(false));
        }

        public static void ClimbingCostRename(ref string blockDisplayName, ref float climbingCostMultiplier)
        {
            if (I._climbingCostFunction == null)
                return;
            var results = I._climbingCostFunction.Invoke(blockDisplayName);

            blockDisplayName = results.Item1;
            climbingCostMultiplier = results.Item2;
        }

        #endregion
    }
}