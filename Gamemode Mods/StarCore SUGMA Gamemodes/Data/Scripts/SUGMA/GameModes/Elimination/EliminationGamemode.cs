using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameModes.Elimination
{
    /// <summary>
    ///     Each faction starts with 1200 seconds. Deaths remove a fraction of seconds.
    /// </summary>
    internal class EliminationGamemode : GamemodeBase
    {
        public static double MatchDuration = 20;

        /// <summary>
        ///     Lists currently tracked factions. Mapped to grid count.
        /// </summary>
        public readonly Dictionary<IMyFaction, int> TrackedFactions = new Dictionary<IMyFaction, int>();

        private bool _remOuter = false, _remMiddle = false, _remInner = false;

        internal IMyFaction _winningFaction;


        public PointTracker PointTracker;

        internal ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;
        internal MatchTimer _matchTimer => SUGMA_SessionComponent.I.GetComponent<MatchTimer>("MatchTimer");
        public override string ReadableName { get; internal set; } = "Team Elimination";

        public override string Description { get; internal set; } =
            "Factions fight against eachother until tickets run out. Kill enemy players to remove tickets.";

        public EliminationGamemode()
        {
            ArgumentParser += new ArgumentParser(
                new ArgumentParser.ArgumentDefinition(
                    time => double.TryParse(time, out MatchDuration),
                    "t",
                    "match-time",
                    "Match time, in minutes.")
            );
        }

        public override void Close()
        {
            StopRound();
        }

        public override void UpdateActive()
        {
            if (PointTracker == null || _matchTimer == null ||
                TrackedFactions == null) // ten billion nullchecks of aristeas
                return;

            foreach (var factionKvp in TrackedFactions.Keys.ToArray())
                if (CalculateFactionPoints(factionKvp) <= 0)
                    // Stop updating if the game just ended
                    if (OnFactionKilled(factionKvp))
                        return;

            if (_matchTimer.IsMatchEnded && MyAPIGateway.Session.IsServer)
                StopRound();

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
            var covers = new List<IMyCubeGrid>();
            MyAPIGateway.Entities.GetEntities(null, ent =>
            {
                var grid = ent as IMyCubeGrid;
                if (grid == null || grid.DisplayName != "#EntityCover" || ((MyCubeGrid)grid).BlocksCount > 1 ||
                    grid.GetPosition().Length() <= maxDistanceFromCenter)
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
            SUGMA_SessionComponent.I.UnregisterComponent("ELMPointTracker");
            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.UnregisterComponent("elmHud");

            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                var faction = PlayerTracker.I.GetGridFaction(grid);
                if (faction == null || !ShareTrackApi.IsGridAlive(grid))
                    continue;

                if (!TrackedFactions.ContainsKey(faction))
                    TrackedFactions.Add(faction, 1);
                else
                    TrackedFactions[faction]++;
            }

            if (TrackedFactions.Count <= 1)
            {
                MyAPIGateway.Utilities.ShowNotification("There aren't any combatants, idiot!", 10000, "Red");
                StopRound();
                return;
            }

            SUGMA_SessionComponent.I.RegisterComponent("ELMPointTracker", PointTracker);

            ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);
            ShareTrackApi.RegisterOnTrack(OnGridTrackChanged);

            var factionNames = new List<string>();
            foreach (var factionKvp in TrackedFactions)
            {
                PointTracker.SetFactionPoints(factionKvp.Key, factionKvp.Value);
                factionNames.Add($"|{factionKvp.Key.Tag}|");
                foreach (var faction in TrackedFactions.Keys)
                {
                    if (faction == factionKvp.Key)
                        continue;

                    MyAPIGateway.Session.Factions.DeclareWar(factionKvp.Key.FactionId, faction.FactionId);
                    //MyAPIGateway.Utilities.ShowMessage("ELM", $"Declared war between {factionKvp.Key.Name} and {faction.Name}");
                }
            }

            base.StartRound(arguments);
            MyAPIGateway.Utilities.ShowNotification("Combatants: " + string.Join(" vs ", factionNames), 10000, "Red");
            _matchTimer.Start(MatchDuration);

            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.RegisterComponent("elmHud", new EliminationHud(this));

            string trackedGrids = "";
            foreach (var faction in TrackedFactions)
                trackedGrids += $"\n-   {faction.Key.Name}: {faction.Value} | {CalculateFactionPoints(faction.Key)}pts";

            Log.Info("Started an ELM match." +
                     $"\n- Combatants: {string.Join(" vs ", factionNames)}" +
                     "\n- Tracked grids:" + trackedGrids);
        }

        public override void StopRound()
        {
            bool setWinnerFromArgs = false;
            foreach (var arg in Arguments)
            {
                if (arg.StartsWith("win"))
                {
                    Log.Info("Winner in arguments found: " + arg);
                    long factionId;
                    long.TryParse(arg.Remove(0, 3), out factionId);

                    _winningFaction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
                    setWinnerFromArgs = true;
                    break;
                }
            }

            if (!setWinnerFromArgs && MyAPIGateway.Session.IsServer)
            {
                Arguments = Arguments.Concat(new[] { $"win{_winningFaction?.FactionId ?? -1}" }).ToArray();
            }

            SUGMA_SessionComponent.I.GetComponent<EliminationHud>("elmHud")?.MatchEnded(_winningFaction);

            foreach (var factionKvp in TrackedFactions)
            foreach (var faction in TrackedFactions.Keys)
            {
                if (faction == factionKvp.Key)
                    continue;

                MyAPIGateway.Session.Factions.SendPeaceRequest(factionKvp.Key.FactionId, faction.FactionId);
                MyAPIGateway.Session.Factions.AcceptPeace(faction.FactionId, factionKvp.Key.FactionId);
            }

            _matchTimer?.Stop();
            SUGMA_SessionComponent.I.UnregisterComponent("ELMPointTracker");
            ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);
            ShareTrackApi.UnregisterOnTrack(OnGridTrackChanged);

            base.StopRound();
            _winningFaction = null;
            TrackedFactions.Clear();
            PointTracker = null;
        }

        internal override void DisplayWinMessage()
        {
            if (_winningFaction == null)
            {
                MyAPIGateway.Utilities.ShowNotification("YOU ARE ALL LOSERS.", 10000, "Red");
                return;
            }
        
            MyAPIGateway.Utilities.ShowNotification($"A WINNER IS [{_winningFaction?.Name}]!", 10000);
        }


        public virtual int CalculateFactionPoints(IMyFaction faction)
        {
            if (!TrackedFactions.ContainsKey(faction))
                return -1;

            return (int)(_matchTimer.MatchDurationMinutes * 60 *
                         (PointTracker.GetFactionPoints(faction) / (float)TrackedFactions[faction]) -
                         _matchTimer.CurrentMatchTime.TotalSeconds);
        }

        internal virtual void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            Log.Info("GridAliveSet: " + grid.DisplayName + " -> " + isAlive);

            var gridFaction = PlayerTracker.I.GetGridFaction(grid);
            if (gridFaction == null)
                return;

            PointTracker.AddFactionPoints(gridFaction, isAlive ? 1 : -1);
            if (isAlive) MyAPIGateway.Utilities.ShowMessage("ELM", $"[{grid.DisplayName}] has returned to life!");
            //int newPoints = PointTracker.GetFactionPoints(gridFaction);
            //if (newPoints > TrackedFactions.GetValueOrDefault(gridFaction))
            //    TrackedFactions[gridFaction] = newPoints; // TODO the UI will break a little bit if this gets called.
        }

        internal virtual void OnGridTrackChanged(IMyCubeGrid grid, bool isTracked)
        {
            Log.Info("GridTrackSet: " + grid.DisplayName + " -> " + isTracked);

            if (ShareTrackApi.IsGridAlive(grid))
            {
                var gridFaction = PlayerTracker.I.GetGridFaction(grid);
                if (gridFaction == null)
                    return;

                PointTracker.AddFactionPoints(gridFaction, -1);
            }
        }

        internal virtual bool OnFactionKilled(IMyFaction faction)
        {
            TrackedFactions.Remove(faction);
            MyAPIGateway.Utilities.ShowNotification($"|{faction.Tag}| IS OUT!", 10000, "Red");
            Log.Info($"|{faction.Tag}| was killed!");

            if (TrackedFactions.Count == 1)
            {
                _winningFaction = TrackedFactions.Keys.First();
                StopRound();
                return true;
            }

            if (TrackedFactions.Count == 0)
            {
                _winningFaction = null;
                StopRound();
                return true;
            }

            return false;
        }
    }
}
