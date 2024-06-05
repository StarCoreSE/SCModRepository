using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
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
        internal ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;
        internal MatchTimer _matchTimer => SUGMA_SessionComponent.I.GetComponent<MatchTimer>("MatchTimer");


        public PointTracker PointTracker;
        public override string ReadableName { get; internal set; } = "Team Deathmatch";

        public override string Description { get; internal set; } =
            "Factions fight against eachother until tickets run out. Kill enemy players to remove tickets.";

        internal IMyFaction _winningFaction = null;

        private bool _remOuter = false, _remMiddle = false, _remInner = false;

        /// <summary>
        /// Lists currently tracked factions. Mapped to grid count.
        /// </summary>
        public readonly Dictionary<IMyFaction, int> TrackedFactions = new Dictionary<IMyFaction, int>();

        public override void Close()
        {
            StopRound();
        }

        public override void UpdateActive()
        {
            if (PointTracker == null)
                return;

            int basePoints = (int)(_matchTimer.MatchDurationMinutes * 60);
            double currentPoints = _matchTimer.CurrentMatchTime.TotalSeconds;

            foreach (var factionKvp in TrackedFactions.ToArray())
            {
                int factionPoints =
                    (int)(basePoints * (PointTracker.GetFactionPoints(factionKvp.Key) / (float)factionKvp.Value) -
                          currentPoints);
                if (factionPoints <= 0)
                {
                    OnFactionKilled(factionKvp.Key);
                    // TODO: Spawn keen explosion on remaining grids.
                }
            }

            //double matchRatio = 1 - currentPoints / basePoints;
            //
            //if (matchRatio <= 0.76) // If within 12 seconds of 15:00 matchtime (assuming 20:00 total)
            //{
            //    if (matchRatio >= 0.75)
            //        MyAPIGateway.Utilities.ShowNotification($"Removing outer cover in T-{basePoints*0.25 - currentPoints:N1}", 1000/60, "Red");
            //    else if (!_remOuter)
            //    {
            //        RemoveBlockers(9750);
            //        _remOuter = true;
            //    }
            //}
            //
            //if (matchRatio <= 0.51) // If within 12 seconds of 10:00 matchtime
            //{
            //    if (matchRatio >= 0.5)
            //        MyAPIGateway.Utilities.ShowNotification($"Removing middle cover in T-{basePoints*0.5 - currentPoints:N1}", 1000/60, "Red");
            //    else if (!_remMiddle)
            //    {
            //        RemoveBlockers(5750);
            //        _remMiddle = true;
            //    }
            //}
            //
            //if (matchRatio <= 0.26) // If within 12 seconds of 5:00 matchtime
            //{
            //    if (matchRatio >= 0.25)
            //        MyAPIGateway.Utilities.ShowNotification($"Removing inner cover in T-{basePoints*0.75 - currentPoints:N1}", 1000/60, "Red");
            //    else if (!_remInner)
            //    {
            //        RemoveBlockers(1750);
            //        _remInner = true;
            //    }
            //}
        }

        private void RemoveBlockers(float maxDistanceFromCenter)
        {
            List<IMyCubeGrid> covers = new List<IMyCubeGrid>();
            MyAPIGateway.Entities.GetEntities(null, (ent) =>
            {
                IMyCubeGrid grid = ent as IMyCubeGrid;
                if (grid == null || grid.DisplayName != "#EntityCover" || ((MyCubeGrid)grid).BlocksCount > 1 || grid.GetPosition().Length() <= maxDistanceFromCenter)
                    return false;

                covers.Add(grid);

                return false;
            });
            foreach (var grid in covers)
                grid.Close();
        }

        public override void StartRound(string[] arguments = null)
        {
            PointTracker = new PointTracker(3, 0);
            SUGMA_SessionComponent.I.RegisterComponent("TDMPointTracker", PointTracker);
            ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);

            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                IMyFaction faction = PlayerTracker.I.GetGridFaction(grid);
                if (faction == null || !ShareTrackApi.IsGridAlive(grid))
                    continue;

                if (!TrackedFactions.ContainsKey(faction))
                    TrackedFactions.Add(faction, 1);
                else
                    TrackedFactions[faction]++;
            }

            List<string> factionNames = new List<string>();
            foreach (var factionKvp in TrackedFactions)
            {
                PointTracker.SetFactionPoints(factionKvp.Key, factionKvp.Value);
                factionNames.Add($"|{factionKvp.Key.Tag}|");
            }

            base.StartRound(arguments);
            MyAPIGateway.Utilities.ShowNotification("Combatants: " + string.Join(" vs ", factionNames), 10000, "Red");
            _matchTimer.Start();

            if (TrackedFactions.Count <= 1)
            {
                MyAPIGateway.Utilities.ShowNotification("There aren't any combatants, idiot!", 10000, "Red");
                StopRound();
            }

            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.RegisterComponent("tdmHud", new TeamDeathmatchHud());
        }

        public override void StopRound()
        {
            SUGMA_SessionComponent.I.UnregisterComponent("tdmHud");
            _matchTimer.Stop();
            SUGMA_SessionComponent.I.UnregisterComponent("TDMPointTracker");
            ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);

            base.StopRound();
            _winningFaction = null;
            TrackedFactions.Clear();
            PointTracker = null;
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


        internal virtual void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            Log.Info("GridAliveSet: " + grid.DisplayName + " -> " + isAlive);
            if (isAlive)
                return;

            IMyFaction gridFaction = PlayerTracker.I.GetGridFaction(grid);
            if (gridFaction == null)
                return;

            PointTracker.AddFactionPoints(gridFaction, -1);
        }

        internal virtual void OnFactionKilled(IMyFaction faction)
        {
            TrackedFactions.Remove(faction);
            MyAPIGateway.Utilities.ShowNotification($"|{faction.Tag}| IS KILL", 10000, "Red");

            if (TrackedFactions.Count == 1)
            {
                _winningFaction = TrackedFactions.Keys.First();
                StopRound();
            }
            else if (TrackedFactions.Count == 0)
            {
                _winningFaction = null;
                StopRound();
            }
        }
    }
}