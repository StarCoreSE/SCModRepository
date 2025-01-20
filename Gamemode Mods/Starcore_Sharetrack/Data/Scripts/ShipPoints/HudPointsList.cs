using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using StarCore.ShareTrack.API;
using StarCore.ShareTrack.API.CoreSystem;
using StarCore.ShareTrack.ShipTracking;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using static VRageRender.MyBillboard;

namespace StarCore.ShareTrack
{
    /// <summary>
    ///     Shift-T screen
    /// </summary>
    internal class HudPointsList
    {
        private readonly StringBuilder _gunTextBuilder = new StringBuilder();
        private readonly StringBuilder _speedTextBuilder = new StringBuilder();

        private ShipTracker _shipTracker = null;


        private readonly HudAPIv2.HUDMessage
            _statMessage = new HudAPIv2.HUDMessage(scale: 1f, font: "BI_SEOutlined", Message: new StringBuilder(""),
                origin: new Vector2D(-.99, .99), hideHud: false, blend: BlendTypeEnum.PostPP)
            {
                Visible = false,
                InitialColor = Color.Orange
            },
            _statMessageBattleWeaponCountsist = new HudAPIv2.HUDMessage(scale: 1.25f, font: "BI_SEOutlined",
                Message: new StringBuilder(""), origin: new Vector2D(-.99, .99), hideHud: false, shadowing: true,
                blend: BlendTypeEnum.PostPP)
            {
                Visible = false
            },
            _statMessageBattle = new HudAPIv2.HUDMessage(scale: 1.25f, font: "BI_SEOutlined",
                Message: new StringBuilder(""), origin: new Vector2D(-.54, -0.955), hideHud: false,
                blend: BlendTypeEnum.PostPP)
            {
                Visible = false
            };

        private ViewState _viewState = ViewState.None;
        private Queue<double> _executionTimes = new Queue<double>();
        private const int _sampleSize = 1;  // Number of samples to consider for the average
        private double _executionTimeSum = 0;  // Running total of execution times
        private static IMyCubeGrid GetFocusedGrid()
        {
            var cockpit = MyAPIGateway.Session.ControlledObject?.Entity as IMyCockpit;
            if (cockpit == null || MyAPIGateway.Session.IsCameraUserControlledSpectator)
                return AllGridsList.RaycastGridFromCamera();
            return cockpit.CubeGrid?.Physics != null
                ? // user is in cockpit
                cockpit.CubeGrid
                : null;
        }

        private void ShiftTHandling()
        {
            var focusedGrid = GetFocusedGrid();
            if (focusedGrid != null)
            {
                ShiftTCalcs(focusedGrid);
            }
            else if (_statMessage.Visible)
            {
                _shipTracker = null;
                _statMessage.Message.Clear();
                _statMessage.Visible = false;
            }
        }

        private void BattleShiftTHandling()
        {
            if (_statMessage.Visible)
            {
                _statMessage.Message.Clear();
                _statMessage.Visible = false;
            }

            var focusedGrid = GetFocusedGrid();
            if (focusedGrid != null)
            {
                BattleShiftTCalcs(focusedGrid);
            }
            else if (_statMessageBattle.Visible)
            {
                _shipTracker = null;
                _statMessageBattle.Message.Clear();
                _statMessageBattle.Visible = false;
                _statMessageBattleWeaponCountsist.Message.Clear();
                _statMessageBattleWeaponCountsist.Visible = false;
            }
        }

        private void ShiftTCalcs(IMyCubeGrid focusedGrid)
        {
            // Update once per second
            if (MasterSession.I.Ticks % 59 != 0)
                return;
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            _shipTracker = TrackingManager.I.TryGetTracker(focusedGrid);
            if (_shipTracker == null)
            {
                _shipTracker = new ShipTracker(focusedGrid, false);
                Log.Info($"ShiftTCalcs Tracked grid {focusedGrid.DisplayName}. Visible: false");
            }

            _shipTracker.Update();

            var totalShieldString = "None";

            if (_shipTracker.MaxShieldHealth > 100)
                totalShieldString = $"{_shipTracker.MaxShieldHealth / 100f:F2} M";
            else if (_shipTracker.MaxShieldHealth > 1 && _shipTracker.MaxShieldHealth < 100)
                totalShieldString = $"{_shipTracker.MaxShieldHealth:F0}0 K";

            var gunTextBuilder = new StringBuilder();
            foreach (var x in _shipTracker.WeaponCounts.Keys)
                gunTextBuilder.AppendFormat("<color=Green>{0}<color=White> x {1}\n", _shipTracker.WeaponCounts[x], x);
            var gunText = gunTextBuilder.ToString();

            var specialBlockTextBuilder = new StringBuilder();
            foreach (var x in _shipTracker.SpecialBlockCounts.Keys)
            {
                specialBlockTextBuilder.AppendFormat("<color=Green>{0}<color=White> x {1}\n",
                    _shipTracker.SpecialBlockCounts[x], x);
            }
                
            var specialBlockText = specialBlockTextBuilder.ToString();

            var massString = $"{_shipTracker.Mass}";

            var thrustInKilograms = focusedGrid.GetMaxThrustInDirection(Base6Directions.Direction.Backward) / 9.81f;
            var mass = _shipTracker.Mass;
            var twr = (float)Math.Round(thrustInKilograms / mass, 1);

            if (_shipTracker.Mass > 1000000) massString = $"{Math.Round(_shipTracker.Mass / 1000000f, 1):F2}m";

            var twRs = $"{twr:F3}";
            var thrustString = $"{Math.Round(_shipTracker.TotalThrust, 1)}";

            if (_shipTracker.TotalThrust > 1000000)
                thrustString = $"{Math.Round(_shipTracker.TotalThrust / 1000000f, 1):F2}M";

            var playerName = _shipTracker.Owner == null ? _shipTracker.GridName : _shipTracker.Owner.DisplayName;
            var factionName = _shipTracker.Owner == null
                ? ""
                : MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(_shipTracker.OwnerId)?.Name;

            var speed = focusedGrid.GridSizeEnum == MyCubeSize.Large
                ? MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed
                : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
            var reducedAngularSpeed = 0f;

            if (RtsApi != null && RtsApi.IsReady)
            {
                speed = (float)Math.Round(RtsApi.GetMaxSpeed(focusedGrid), 2);
                reducedAngularSpeed = RtsApi.GetReducedAngularSpeed(focusedGrid);
            }


            var pwrNotation = _shipTracker.TotalPower > 1000 ? "GW" : "MW";
            var tempPwr = _shipTracker.TotalPower > 1000
                ? $"{Math.Round(_shipTracker.TotalPower / 1000, 1):F1}"
                : Math.Round(_shipTracker.TotalPower, 1).ToString();
            var pwr = tempPwr + pwrNotation;

            var gyroString = $"{Math.Round(_shipTracker.TotalTorque, 1)}";

            if (_shipTracker.TotalTorque >= 1000000)
            {
                var tempGyro2 = Math.Round(_shipTracker.TotalTorque / 1000000f, 1);
                gyroString = tempGyro2 > 1000
                    ? $"{Math.Round(tempGyro2 / 1000, 1):F1}G"
                    : $"{Math.Round(tempGyro2, 1):F1}M";
            }


            var sb = new StringBuilder();
            double lastExecutionTime = _executionTimes.Count > 0 ? _executionTimes.Last() : 0;


            sb.AppendLine($"Last Update took: {lastExecutionTime:F2} ms");
            // Basic Info
            sb.AppendLine("----Basic Info----");
            sb.AppendFormat("<color=White>{0} ", focusedGrid.DisplayName);
            sb.AppendFormat("<color=Green>Owner<color=White>: {0} ", playerName);
            sb.AppendFormat("<color=Green>Faction<color=White>: {0}\n", factionName);
            sb.AppendFormat("<color=Green>Mass<color=White>: {0} kg\n", massString);
            sb.AppendFormat("<color=Green>Heavy blocks<color=White>: {0}\n", _shipTracker.HeavyArmorCount);
            sb.AppendFormat("<color=Green>Total blocks<color=White>: {0}\n", _shipTracker.BlockCount);
            sb.AppendFormat("<color=Green>PCU<color=White>: {0}\n", _shipTracker.PCU);
            sb.AppendFormat("<color=Green>Size<color=White>: {0}\n",
                (focusedGrid.Max - focusedGrid.Min + Vector3I.One).ToString());
            // sb.AppendFormat("<color=Green>Max Speed<color=White>: {0} | <color=Green>TWR<color=White>: {1}\n", speed, TWRs);
            sb.AppendFormat(
                "<color=Green>Max Speed<color=White>: {0} | <color=Green>Reduced Angular Speed<color=White>: {1:F2} | <color=Green>TWR<color=White>: {2}\n",
                speed, reducedAngularSpeed, twRs);
            sb.AppendLine(); //blank line

            // Battle Stats
            sb.AppendLine("<color=Orange>----Battle Stats----");
            sb.AppendFormat("<color=Green>Battle Points<color=White>: {0}\n", _shipTracker.BattlePoints);
            sb.AppendFormat(
                "<color=Orange>[<color=Red> {0}% <color=Orange>| <color=Green>{1}% <color=Orange>| <color=DeepSkyBlue>{2}% <color=Orange>| <color=LightGray>{3}% <color=Orange>]\n",
                Math.Round(_shipTracker.OffensivePointsRatio * 100f), Math.Round(_shipTracker.PowerPointsRatio * 100f),
                Math.Round(_shipTracker.MovementPointsRatio * 100f),
                Math.Round(_shipTracker.RemainingPointsRatio * 100f));
            sb.Append(
                $"<color=Green>PD Investment<color=White>: <color=Orange>( <color=white>{_shipTracker.PointDefensePointsRatio * 100:N0}% <color=Orange>|<color=Crimson> {(_shipTracker.OffensivePoints == 0 ? 0 : (float)_shipTracker.PointDefensePoints / _shipTracker.OffensivePoints) * 100f:N0}%<color=Orange> )\n");
            sb.AppendFormat(
                "<color=Green>Shield Max HP<color=White>: {0} <color=Orange>(<color=White>{1:N0}%<color=Orange>)\n",
                totalShieldString, _shipTracker.CurrentShieldPercent);
            sb.AppendFormat("<color=Green>Thrust<color=White>: {0}N\n", thrustString);
            sb.AppendFormat("<color=Green>Gyro<color=White>: {0}N\n", gyroString);
            sb.AppendFormat("<color=Green>Power<color=White>: {0}\n", pwr);
            sb.AppendLine(); //blank line

            // Armament Info
            sb.AppendLine("<color=Orange>----Armament----");
            sb.Append(gunText);
            sb.AppendLine(); //blank line

            // Blocks Info
            sb.AppendLine("<color=Orange>----Blocks----");
            sb.AppendLine(specialBlockText);

            if (!_statMessage.Message.Equals(sb))
                _statMessage.Message = sb;
            _statMessage.Visible = true;
            stopwatch.Stop();
            UpdateExecutionTimes(stopwatch.Elapsed.TotalMilliseconds);
        }

        private void BattleShiftTCalcs(IMyCubeGrid focusedGrid)
        {
            if (MasterSession.I.Ticks % 59 != 0)
                return;

            ShipTracker tracked;
            TrackingManager.I.TrackedGrids.TryGetValue(focusedGrid, out tracked);
            if (tracked == null)
            {
                tracked = new ShipTracker(focusedGrid, false);
                Log.Info($"BattleShiftTCalcs Tracked grid {focusedGrid.DisplayName}. Visible: false");
            }

            var totalShieldString = "None";

            if (tracked.MaxShieldHealth > 100)
                totalShieldString = $"{tracked.MaxShieldHealth / 100f:F2} M";
            else if (tracked.MaxShieldHealth > 1 && tracked.MaxShieldHealth < 100)
                totalShieldString = $"{tracked.MaxShieldHealth:F0}0 K";

            var maxSpeed = focusedGrid.GridSizeEnum == MyCubeSize.Large
                ? MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed
                : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
            var reducedAngularSpeed = 0f;
            var negativeInfluence = 0f;

            if (RtsApi != null && RtsApi.IsReady)
            {
                maxSpeed = (float)Math.Round(RtsApi.GetMaxSpeed(focusedGrid), 2);
                reducedAngularSpeed = RtsApi.GetReducedAngularSpeed(focusedGrid);
                negativeInfluence = RtsApi.GetNegativeInfluence(focusedGrid);
            }

            _speedTextBuilder.Clear();
            _speedTextBuilder.Append($"\n<color=Green>Max Speed<color=White>: {maxSpeed:F2} m/s");
            _speedTextBuilder.Append(
                $"\n<color=Green>Reduced Angular Speed<color=White>: {reducedAngularSpeed:F2} rad/s");
            _speedTextBuilder.Append($"\n<color=Green>Negative Influence<color=White>: {negativeInfluence:F2}");

            _gunTextBuilder.Clear();
            foreach (var x in tracked.WeaponCounts)
                _gunTextBuilder.Append($"<color=Green>{x.Value} x <color=White>{x.Key}\n");

            var thrustString = $"{Math.Round(tracked.TotalThrust, 1)}";
            if (tracked.TotalThrust > 1000000)
                thrustString = $"{Math.Round(tracked.TotalThrust / 1000000f, 1):F2}M";

            var gyroString = $"{Math.Round(tracked.TotalTorque, 1)}";
            double tempGyro2;
            if (tracked.TotalTorque >= 1000000)
            {
                tempGyro2 = Math.Round(tracked.TotalTorque / 1000000f, 1);
                if (tempGyro2 > 1000)
                    gyroString = $"{Math.Round(tempGyro2 / 1000, 1):F1}G";
                else
                    gyroString = $"{Math.Round(tempGyro2, 1):F1}M";
            }

            var pwrNotation = tracked.TotalPower > 1000 ? "GW" : "MW";
            var tempPwr = tracked.TotalPower > 1000
                ? $"{Math.Round(tracked.TotalPower / 1000, 1):F1}"
                : Math.Round(tracked.TotalPower, 1).ToString();
            var pwr = tempPwr + pwrNotation;

            _gunTextBuilder.Append($"\n<color=Green>Thrust<color=White>: {thrustString} N")
                .Append($"\n<color=Green>Gyro<color=White>: {gyroString} N")
                .Append($"\n<color=Green>Power<color=White>: {pwr}")
                .Append(_speedTextBuilder);

            _statMessageBattleWeaponCountsist.Message.Length = 0;
            _statMessageBattleWeaponCountsist.Message.Append(_gunTextBuilder);

            _statMessageBattle.Message.Length = 0;
            _statMessageBattle.Message.Append($"<color=White>{totalShieldString} ({(int)tracked.CurrentShieldPercent}%)");

            _statMessageBattle.Visible = true;
            _statMessageBattleWeaponCountsist.Visible = true;
        }
        private void UpdateExecutionTimes(double elapsedTime)
        {
            if (_executionTimes.Count >= _sampleSize)
                _executionTimes.Dequeue();  // Remove the oldest time if at capacity
            _executionTimes.Enqueue(elapsedTime);
        }
        private enum ViewState
        {
            None,
            InView,
            InView2
        }

        #region APIs

        private WcApi WcApi => AllGridsList.I.WcApi;
        private ShieldApi ShApi => AllGridsList.I.ShieldApi;
        private RtsApi RtsApi => AllGridsList.I.RtsApi;
        private HudAPIv2 TextHudApi => MasterSession.I.TextHudApi;

        #endregion

        #region Public Methods

        public void CycleViewState()
        {
            _viewState++;
            if (_viewState > ViewState.InView2)
            {
                _statMessageBattle.Message.Clear();
                _statMessageBattleWeaponCountsist.Message.Clear();
                _statMessageBattle.Visible = false;
                _statMessageBattleWeaponCountsist.Visible = false;

                _viewState = ViewState.None;
            }
        }

        public void UpdateDraw()
        {
            if (!TextHudApi.Heartbeat)
                return;

            switch (_viewState)
            {
                case ViewState.InView:
                    ShiftTHandling();
                    break;
                case ViewState.InView2:
                    BattleShiftTHandling();
                    break;
            }
        }

        #endregion
    }
}