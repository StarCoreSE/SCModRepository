using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameState;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameModes.TeamDeathMatch
{
    /// <summary>
    /// Each faction starts with 1200 seconds. Deaths remove a fraction of seconds.
    /// </summary>
    internal class TeamDeathmatchGamemode : GamemodeBase
    {
        private ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;
        private MatchTimer _matchTimer => SUGMA_SessionComponent.I.GetComponent<MatchTimer>("MatchTimer");


        private PointTracker _pointTracker;
        public override string ReadableName { get; internal set; } = "Team Deathmatch";
        public override string Description { get; internal set; } = "Factions fight against eachother until tickets run out. Kill enemy players to remove tickets.";

        private IMyFaction _winningFaction = null;
        /// <summary>
        /// Lists currently tracked factions. Mapped to grid count.
        /// </summary>
        Dictionary<IMyFaction, int> _trackedFactions = new Dictionary<IMyFaction, int>();

        public override void Init(string id)
        {
            base.Init(id);
            // TODO move this into StartRound().
            ShareTrackApi.RegisterOnTrack(OnTracked);
        }

        public override void Close()
        {
            ShareTrackApi.UnregisterOnTrack(OnTracked);
            StopRound();
        }

        public override void UpdateActive()
        {
            int basePoints = (int)(_matchTimer.MatchDurationMinutes * 60);
            TimeSpan matchTime = _matchTimer.CurrentMatchTime;
            int remainingBasePoints = (int) (basePoints - matchTime.TotalSeconds);

            foreach (var factionKvp in _trackedFactions)
            {
                int factionPoints = (int) (basePoints * (_pointTracker.GetFactionPoints(factionKvp.Key) / (float) factionKvp.Value) - matchTime.TotalSeconds);
                if (factionPoints <= 0)
                {
                    OnFactionKilled(factionKvp.Key);
                    // TODO: Spawn keen explosion on remaining grids.
                }

                MyAPIGateway.Utilities.ShowNotification($"{factionKvp.Key.Tag}: {factionPoints}", 1000/60);
            }
        }

        public override void StartRound()
        {
            _pointTracker = new PointTracker(3, 0);
            SUGMA_SessionComponent.I.RegisterComponent("TDMPointTracker", _pointTracker);
            ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);

            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                IMyFaction faction = PlayerTracker.I.GetGridFaction(grid);
                if (faction == null)
                    continue;

                if (!_trackedFactions.ContainsKey(faction))
                    _trackedFactions.Add(faction, 1);
                else
                    _trackedFactions[faction]++;

            }

            List<string> factionNames = new List<string>();
            foreach (var factionKvp in _trackedFactions)
            {
                _pointTracker.SetFactionPoints(factionKvp.Key, factionKvp.Value);
                factionNames.Add($"|{factionKvp.Key.Tag}|");
            }

            base.StartRound();
            MyAPIGateway.Utilities.ShowNotification("Combatants: " + string.Join(" vs ", factionNames),10000, "Red");
            _matchTimer.Start();
        }

        public override void StopRound()
        {
            _matchTimer.Stop();
            SUGMA_SessionComponent.I.UnregisterComponent("TDMPointTracker");
            ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);

            base.StopRound();
            _winningFaction = null;
            _trackedFactions.Clear();
            _pointTracker = null;
            SUGMA_SessionComponent.I.CurrentGamemode = null;
        }

        internal override void DisplayWinMessage()
        {
            if (_winningFaction == null)
            {
                MyAPIGateway.Utilities.ShowNotification("YOU ARE ALL LOSERS", 10000, "Red");
                return;
            }
            MyAPIGateway.Utilities.ShowNotification($"A WINNER IS [{_winningFaction?.Name}]", 10000);
        }









        private void OnTracked(IMyCubeGrid grid, bool isTracked)
        {
            MyAPIGateway.Utilities.ShowNotification($"T {grid.DisplayName}: {isTracked}");
        }

        private void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            if (isAlive)
                return;

            IMyFaction gridFaction = PlayerTracker.I.GetGridFaction(grid);
            if (gridFaction == null)
                return;

            _pointTracker.AddFactionPoints(gridFaction, -1);
        }

        private void OnFactionKilled(IMyFaction faction)
        {
            _trackedFactions.Remove(faction);
            MyAPIGateway.Utilities.ShowNotification($"|{faction.Tag}| IS KILL", 10000, "Red");

            if (_trackedFactions.Count == 1)
            {
                _winningFaction = _trackedFactions.Keys.First();
                StopRound();
            }
            else if (_trackedFactions.Count == 0)
            {
                _winningFaction = null;
                StopRound();
            }
        }
    }
}
