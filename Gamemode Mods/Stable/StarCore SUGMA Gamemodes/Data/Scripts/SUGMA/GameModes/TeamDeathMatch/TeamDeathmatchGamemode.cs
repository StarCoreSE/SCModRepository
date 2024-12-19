using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameModes.TeamDeathmatch
{
    internal class TeamDeathmatchGamemode : GamemodeBase
    {
        public static double MatchDuration = 10;
        public static int VictoryPoints = -1;
        public static double RespawnTimeSeconds = 10;

        public override string ReadableName { get; internal set; } = "Team Deathmatch";
        public override string Description { get; internal set; } = "Teams fight to for who can get the most kills within a time limit.";

        internal static ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;

        internal PointTracker PointTracker;
        internal RespawnManager RespawnManager;
        internal MatchTimer MatchTimer => SUGMA_SessionComponent.I.GetComponent<MatchTimer>("MatchTimer");

        internal IMyFaction WinningFaction;

        public TeamDeathmatchGamemode()
        {
            ArgumentParser += new ArgumentParser(
                new ArgumentParser.ArgumentDefinition(
                    time => double.TryParse(time, out MatchDuration),
                    "t",
                    "match-time",
                    "Match time, in minutes."),
                new ArgumentParser.ArgumentDefinition(
                    time => int.TryParse(time, out VictoryPoints),
                    "k",
                    "victory-kills",
                    "Number of kills to end the match."),
                new ArgumentParser.ArgumentDefinition(
                    time => double.TryParse(time, out RespawnTimeSeconds),
                    "r",
                    "respawn-time",
                    "Respawn time in seconds.")
            );
        }

        internal HashSet<IMyFaction> TrackedFactions = new HashSet<IMyFaction>();



        public override void Close()
        {
            StopRound();
        }

        public override void UpdateActive()
        {
            //if (PointTracker == null || MatchTimer == null ||
            //    TrackedFactions == null) // ten billion nullchecks of aristeas
            //    return;
            if (MatchTimer.IsMatchEnded && MyAPIGateway.Session.IsServer)
                StopRound();
        }

        public override void StartRound(string[] arguments)
        {
            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                if (grid.GetFaction() != null)
                    TrackedFactions.Add(grid.GetFaction());
            }

            if (TrackedFactions.Count < 2)
            {
                MyAPIGateway.Utilities.ShowNotification("There aren't any combatants, idiot!", 10000, "Red");
                StopRound();
                return;
            }

            base.StartRound(arguments);

            PointTracker = new PointTracker(0, VictoryPoints);
            RespawnManager = new RespawnManager(RespawnTimeSeconds);
            SUGMA_SessionComponent.I.RegisterComponent("TDMPointTracker", PointTracker);
            SUGMA_SessionComponent.I.RegisterComponent("TDMRespawnManager", RespawnManager);

            ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);
            PointTracker.OnFactionWin += faction =>
            {
                WinningFaction = faction;
                StopRound();
            };
            
            var factionNames = new List<string>();
            foreach (var faction in TrackedFactions)
            {
                factionNames.Add($"|{faction.Tag}|");
                foreach (var otherFaction in TrackedFactions)
                {
                    if (otherFaction == faction)
                        continue;

                    MyAPIGateway.Session.Factions.DeclareWar(faction.FactionId, otherFaction.FactionId);
                    //MyAPIGateway.Utilities.ShowMessage("TDM", $"Declared war between {factionKvp.Key.Name} and {faction.Name}");
                }
            }

            MyAPIGateway.Utilities.ShowNotification("Combatants: " + string.Join(" vs ", factionNames), 10000, "Red");
            MatchTimer.Start(MatchDuration);

            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.RegisterComponent("tdmHud", new TeamDeathmatchHud(this));

            Log.Info("Started a TDM match." +
                     $"\nDuration: {MatchDuration}m" + 
                     $"\nVictory Points: {VictoryPoints}" + 
                     $"\n- Combatants: {string.Join(" vs ", factionNames)}");
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

                    WinningFaction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
                    setWinnerFromArgs = true;
                    break;
                }
            }

            if (!setWinnerFromArgs && MyAPIGateway.Session.IsServer)
            {
                if (WinningFaction == null)
                    WinningFaction = PointTracker.FactionPoints.MaxBy(f => f.Value).Key;

                Arguments = Arguments.Concat(new[] { $"win{WinningFaction?.FactionId ?? -1}" }).ToArray();
            }

            SUGMA_SessionComponent.I.GetComponent<TeamDeathmatchHud>("tdmHud")?.MatchEnded(WinningFaction);

            foreach (var factionKvp in TrackedFactions)
            {
                foreach (var faction in TrackedFactions)
                {
                    if (faction == factionKvp)
                        continue;

                    MyAPIGateway.Session.Factions.SendPeaceRequest(factionKvp.FactionId, faction.FactionId);
                    MyAPIGateway.Session.Factions.AcceptPeace(faction.FactionId, factionKvp.FactionId);
                }
            }
            

            MatchTimer?.Stop();
            SUGMA_SessionComponent.I.UnregisterComponent("TDMPointTracker");
            SUGMA_SessionComponent.I.UnregisterComponent("TDMRespawnManager");
            ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);

            base.StopRound();
            WinningFaction = null;
            TrackedFactions.Clear();
            PointTracker = null;
            RespawnManager = null;
        }

        internal override void DisplayWinMessage()
        {
            if (WinningFaction == null)
            {
                MyAPIGateway.Utilities.ShowNotification("YOU ARE ALL LOSERS.", 10000, "Red");
                return;
            }

            MyAPIGateway.Utilities.ShowNotification($"A WINNER IS [{WinningFaction?.Name}]!", 10000);
        }

        private void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            if (isAlive)
                return;

            var gridFaction = grid.GetFaction();

            foreach (var faction in TrackedFactions)
            {
                if (faction != gridFaction)
                    PointTracker.AddFactionPoints(faction, 1);
            }
        }
    }
}
