using SC.SUGMA.API;
using SC.SUGMA.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using RichHudFramework;
using VRage.Game.ModAPI;
using SC.SUGMA.Utilities;
using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using Sandbox.Game;
using VRageMath;
using VRage.ModAPI;

namespace SC.SUGMA.GameModes.RocketCore
{
    internal class RocketCoreGamemode : GamemodeBase
    {
        public static double MatchDuration = 20;

        /// <summary>
        ///     Lists currently tracked factions.
        /// </summary>
        public readonly List<IMyFaction> TrackedFactions = new List<IMyFaction>();
        public List<IMyFaction> InFactions { get; private set; } = new List<IMyFaction>();
        protected IMyFaction _winningFaction;

        public PointTracker PointTracker;

        protected ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;
        public MatchTimer MatchTimer => SUGMA_SessionComponent.I.GetComponent<MatchTimer>("MatchTimer");
        public Dictionary<IMyFaction, SphereZone> FactionGoals = new Dictionary<IMyFaction, SphereZone>();
        private bool _waitingForBallSpawn = false;
        public override string ReadableName { get; internal set; } = "RocketCore";

        public IMyCubeGrid BallEntity = null;

        public override string Description { get; internal set; } =
            "Score by pushing the ball into the enemy team's goal! Grids are made invincible.";

        public RocketCoreGamemode()
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
            if (PointTracker == null || MatchTimer == null ||
                TrackedFactions == null) // ten billion nullchecks of aristeas
                return;

            if (MatchTimer.IsMatchEnded && MyAPIGateway.Session.IsServer)
                StopRound();

            if (_waitingForBallSpawn)
                return;

            foreach (var zoneSet in FactionGoals)
            {
                if (zoneSet.Value.ContainedGrids.Count <= 0)
                    continue;
                // Goal was made
                PointTracker.AddFactionPoints(zoneSet.Key, -1);
                SUGMA_SessionComponent.I.GetComponent<RocketCoreHud>("rocHud")?.GoalScored(zoneSet.Key);

                if (_winningFaction != null)
                    break;

                SpawnBall();
                Log.Info($"Goal was scored against {zoneSet.Key.Name}! New points: {PointTracker.GetFactionPoints(zoneSet.Key)}");

                _waitingForBallSpawn = true;
            }
        }

        public override void StartRound(string[] arguments = null)
        {
            _waitingForBallSpawn = false;
            _winningFaction = null;
            PointTracker = new PointTracker(3, 0);
            PointTracker.OnFactionWin += OnFactionLose;

            SUGMA_SessionComponent.I.UnregisterComponent("ROCPointTracker");
            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.UnregisterComponent("rocHud");

            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                var faction = PlayerTracker.I.GetGridFaction(grid);
                if (faction == null || !ShareTrackApi.IsGridAlive(grid))
                    continue;

                if (!TrackedFactions.Contains(faction))
                    TrackedFactions.Add(faction);

                List<IMyCubeGrid> subGrids = new List<IMyCubeGrid>();
                grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(subGrids);
                foreach (var subGrid in subGrids)
                {
                    ((MyCubeGrid)subGrid).Immune = true;
                    ((MyCubeGrid)subGrid).DestructibleBlocks = false;
                }
            }

            if (TrackedFactions.Count <= 1)
            {
                MyAPIGateway.Utilities.ShowNotification("There aren't any combatants, idiot!", 10000, "Red");
                StopRound();
                return;
            }

            SUGMA_SessionComponent.I.RegisterComponent("ROCPointTracker", PointTracker);

            var factionNames = new List<string>();
            var factionSpawns = SUtils.GetFactionSpawns();
            foreach (var faction in TrackedFactions)
            {
                factionNames.Add($"|{faction.Tag}|");
                foreach (var compareFaction in TrackedFactions)
                {
                    if (faction == compareFaction)
                        continue;

                    MyAPIGateway.Session.Factions.DeclareWar(faction.FactionId, compareFaction.FactionId);
                    //MyAPIGateway.Utilities.ShowMessage("ROC", $"Declared war between {factionKvp.Key.Name} and {faction.Name}");
                }

                if (factionSpawns.ContainsKey(faction))
                {
                    var zone = new SphereZone(
                        factionSpawns[faction].GetPosition() - factionSpawns[faction].GetPosition().Normalized() * 2500,
                        750)
                    {
                        SphereDrawColor = faction.CustomColor.ColorMaskToRgb().SetAlphaPct(0.25f),
                        GridFilter = Array.Empty<IMyCubeGrid>()
                    };
                    FactionGoals[faction] = zone;
                    SUGMA_SessionComponent.I.RegisterComponent($"RocZone{faction.FactionId}", zone);
                }
            }

            InFactions = new List<IMyFaction>(TrackedFactions);

            base.StartRound(arguments);
            MyAPIGateway.Utilities.ShowNotification("Combatants: " + string.Join(" vs ", factionNames), 10000, "Red");
            MatchTimer.Start(MatchDuration);

            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.RegisterComponent("rocHud", new RocketCoreHud(this));

            SpawnBall();

            Log.Info("Started a ROC match." +
                     $"\n- Combatants: {string.Join(" vs ", factionNames)}");
        }

        private void OnFactionLose(IMyFaction loser)
        {
            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                var faction = PlayerTracker.I.GetGridFaction(grid);
                if (faction == null || !ShareTrackApi.IsGridAlive(grid) || faction != loser) continue;

                List<IMyCubeGrid> subGrids = new List<IMyCubeGrid>();
                grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(subGrids);
                foreach (var subGrid in subGrids)
                {
                    ((MyCubeGrid)subGrid).Immune = false;
                    ((MyCubeGrid)subGrid).DestructibleBlocks = true;
                }

                FactionGoals[loser].IsVisible = false;
                InFactions.Remove(loser);
            }

            if (InFactions.Count > 1) return;

            _winningFaction = InFactions[0];
            StopRound();
        }

        public override void StopRound()
        {
            BallEntity?.Close();
            bool setWinnerFromArgs = false;
            foreach (var arg in Arguments)
            {
                if (arg.StartsWith("win"))
                {
                    long factionId;
                    long.TryParse(arg.Remove(0, 3), out factionId);

                    _winningFaction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
                    setWinnerFromArgs = true;
                    Log.Info($"Winner in arguments found: {factionId} ({_winningFaction?.Name})");
                    break;
                }
            }

            if (!setWinnerFromArgs && MyAPIGateway.Session.IsServer)
            {
                Arguments = Arguments.Concat(new[] { $"win{_winningFaction?.FactionId ?? -1}" }).ToArray();
            }

            SUGMA_SessionComponent.I.GetComponent<RocketCoreHud>("rocHud")?.MatchEnded(_winningFaction);

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

            // Reset destructibility
            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                List<IMyCubeGrid> subGrids = new List<IMyCubeGrid>();
                grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(subGrids);
                foreach (var subGrid in subGrids)
                {
                    ((MyCubeGrid)subGrid).Immune = false;
                    ((MyCubeGrid)subGrid).DestructibleBlocks = true;
                }
            }

            foreach (var zone in FactionGoals)
                SUGMA_SessionComponent.I.UnregisterComponent(zone.Value.ComponentId);

            MatchTimer?.Stop();
            SUGMA_SessionComponent.I.UnregisterComponent("PointTracker");

            base.StopRound();
            InFactions.Clear();
            TrackedFactions.Clear();
            FactionGoals.Clear();
            PointTracker = null;
        }

        protected void SpawnBall()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            BallEntity?.Close();
            MyVisualScriptLogicProvider.PrefabSpawned += PrefabSpawned;
            MyVisualScriptLogicProvider.SpawnPrefab("THE BALL", SUtils.RandVector().Normalized() * 250, Vector3D.Forward, Vector3D.Up, entityName: "SugmaTheBall");
        }

        private void PrefabSpawned(string entityName)
        {
            try
            {
                IMyEntity ballEnt;
                if (!MyAPIGateway.Entities.TryGetEntityByName(entityName, out ballEnt))
                    throw new Exception("Could not find ball entity!");

                BallEntity = (IMyCubeGrid) ballEnt;
                SUGMA_SessionComponent.I.ShareTrackApi.TrackGrid(BallEntity);

                MyVisualScriptLogicProvider.PrefabSpawned -= PrefabSpawned;
                Log.Info("RocketCoreGamemode spawned ball entity " + entityName + " at " + BallEntity.GetPosition());

                var array = new[]
                {
                    BallEntity
                };

                foreach (var zone in FactionGoals)
                    zone.Value.GridFilter = array;
                _waitingForBallSpawn = false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(RocketCoreGamemode));
            }
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
    }
}
