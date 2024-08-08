using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using StarCore.ShareTrack.API;
using StarCore.ShareTrack.API.CoreSystem;
using StarCore.ShareTrack.HeartNetworking;
using StarCore.ShareTrack.HeartNetworking.Custom;
using StarCore.ShareTrack.ShipTracking;
using VRage;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace StarCore.ShareTrack
{
    public class AllGridsList
    {

        public static AllGridsList I;

        public static Dictionary<string, int> PointValues = new Dictionary<string, int>();


        private static readonly Dictionary<long, IMyPlayer> AllPlayers = new Dictionary<long, IMyPlayer>();

        public static HudAPIv2.HUDMessage
            IntegretyMessage,
            TimerMessage,
            Ticketmessage;

        public static ShipTracker.NametagSettings NametagViewState = ShipTracker.NametagSettings.PlayerName;

        private readonly Dictionary<string, int> _bp = new Dictionary<string, int>();

        private readonly Dictionary<MyKeys, Action> _keyAndActionPairs = new Dictionary<MyKeys, Action>
        {
            [MyKeys.M] = () =>
            {
                var castGrid = RaycastGridFromCamera();
                if (castGrid == null)
                    return;

                if (!TrackingManager.I.IsGridTracked(castGrid))
                    TrackingManager.I.TrackGrid(castGrid);
                else
                    TrackingManager.I.UntrackGrid(castGrid);
            },
            [MyKeys.N] = () =>
            {
                IntegretyMessage.Visible = !IntegretyMessage.Visible;
                MyAPIGateway.Utilities.ShowNotification("ShipTracker: Hud visibility set to " +
                                                        IntegretyMessage.Visible);
            },
            [MyKeys.B] = () =>
            {
                TimerMessage.Visible = !TimerMessage.Visible;
                Ticketmessage.Visible = !Ticketmessage.Visible;
                MyAPIGateway.Utilities.ShowNotification(
                    "ShipTracker: Timer visibility set to " + TimerMessage.Visible);
            },
            [MyKeys.J] = () =>
            {
                NametagViewState++;
                if (NametagViewState > (ShipTracker.NametagSettings)3)
                    NametagViewState = 0;
                MyAPIGateway.Utilities.ShowNotification(
                    "ShipTracker: Nameplate visibility set to " + NametagViewState);
            },
            [MyKeys.T] = () =>
            {
                I?._hudPointsList?.CycleViewState();
            },
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

        private void HudRegistered()
        {
            // Avoid bootlock when opening world with autotracked grids.
            TrackingManager.Init();

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
        }

        private void HandleKeyInputs()
        {
            if (!MyAPIGateway.Input.IsAnyShiftKeyPressed())
                return;

            foreach (var pair in _keyAndActionPairs)
                if (MyAPIGateway.Input.IsNewKeyPressed(pair.Key))
                    pair.Value.Invoke();
        }

        private void UpdateTrackingData()
        {
            if (MasterSession.I.Ticks % 59 != 0)
                return;

            lock (TrackingManager.I.TrackedGrids)
            {
                foreach (var shipTracker in TrackingManager.I.TrackedGrids.Values)
                {
                    shipTracker.Update();
                }
            }

            if (IntegretyMessage == null || !TextHudApi.Heartbeat)
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

            TeamBpCalc(tt, _ts, _m, _bp, _mbp, _pbp, _obp, _mobp);

            if (!IntegretyMessage.Message.Equals(tt))
            {
                IntegretyMessage.Message = tt;
            }
            
            IntegretyMessage.Origin = new Vector2D(0.975 - IntegretyMessage.GetTextLength().X, IntegretyMessage.Origin.Y);
        }


        private void MainTrackerUpdate(Dictionary<string, List<string>> ts, Dictionary<string, double> m,
            Dictionary<string, int> bp, Dictionary<string, int> mbp, Dictionary<string, int> pbp,
            Dictionary<string, int> obp, Dictionary<string, int> mobp)
        {
            foreach (var shipTracker in TrackingManager.I.TrackedGrids.Values)
            {
                var fn = shipTracker.FactionName.Length > 6 ? shipTracker.FactionName.Substring(0, 6) : shipTracker.FactionName;
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

                var wep = 0;
                foreach (var kvp in shipTracker.WeaponCounts)
                {
                    wep += kvp.Value;
                }

                var pwr = FormatPower(Math.Round(shipTracker.TotalPower, 1));
                var ts2 = FormatThrust(Math.Round(shipTracker.TotalThrust, 2));

                ts[fn].Add(CreateDisplayString(o, shipTracker, wep, pwr, ts2));
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

        private string CreateDisplayString(string ownerName, ShipTracker tracker, int wep, string power, string thrust)
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
            var weaponColor = wep == 0 ? "red" : "Orange";

            var functionalColor = tracker.IsFunctional ? "white" : "red";
            var integrityColor = integrityPercent >= 75 ? "White" : integrityPercent >= 50 ? "LightCoral" : integrityPercent >= 25 ? "IndianRed" : "FireBrick";

            return
                $"<color={functionalColor}>{ownerDisplay,-8}<color={integrityColor}>{integrityPercent,3}%<color={functionalColor}> P:<color=orange>{power,3}<color={functionalColor}> T:<color=orange>{thrust,3}<color={functionalColor}> W:<color={weaponColor}>{wep}<color={functionalColor}> S:<color={shieldColor}>{shieldPercent,3}%<color=white>";
        }


        private static void TeamBpCalc(StringBuilder tt, Dictionary<string, List<string>> trackedShip,
            Dictionary<string, double> m, Dictionary<string, int> bp, Dictionary<string, int> mbp,
            Dictionary<string, int> pbp, Dictionary<string, int> obp, Dictionary<string, int> mobp)
        {
            foreach (var faction in trackedShip.Keys)
            {
                if (trackedShip[faction] == null || trackedShip[faction].Count == 0) continue;

                var msValue = m[faction] / 1e6;
                var tbi = 100f / bp[faction];

                tt.Append("<color=orange>---- ")
                    .Append(faction)
                    .Append(" : ")
                    .AppendFormat("{0:0.00}M : {1}bp <color=orange>[", msValue, bp[faction]);

                tt.AppendFormat("<color=Red>{0}<color=white>%<color=orange>|", (int)(obp[faction] * tbi + 0.5f))
                    .AppendFormat("<color=Green>{0}<color=white>%<color=orange>|", (int)(pbp[faction] * tbi + 0.5f))
                    .AppendFormat("<color=DeepSkyBlue>{0}<color=white>%<color=orange>|", (int)(mobp[faction] * tbi + 0.5f))
                    .AppendFormat("<color=LightGray>{0}<color=white>%<color=orange>]", (int)(mbp[faction] * tbi + 0.5f))
                    .AppendLine(" ---------");

                foreach (var y in trackedShip[faction]) tt.AppendLine(y);
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

            MyAPIGateway.Utilities.ShowMessage("ShareTrack",
                "Aim at a grid and press:" +
                "\n- Shift+T to show grid stats." +
                "\n- Shift+M to track a grid." +
                "\n- Shift+J to cycle nametag style."
                );

            MyAPIGateway.Utilities.RegisterMessageHandler(2546247, ParsePointsDict);

            // Check if the current instance is not a dedicated server
            if (!MyAPIGateway.Utilities.IsDedicated)
                // Initialize the sphere entities
                // Initialize the text_api with the HUDRegistered callback
                TextHudApi = new HudAPIv2(HudRegistered);
            else
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
                if (MasterSession.I.Ticks % 61 == 0)
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