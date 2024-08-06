using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameState;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.Utilities
{
    public static class TeamBalancer
    {
        private static ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;

        public static void PerformBalancing()
        {
            if (!ShareTrackApi.AreTrackedGridsLoaded())
                throw new Exception("Not all tracked grids are loaded!");

            // Only server should run this
            if (!MyAPIGateway.Session.IsServer)
                return;

            Dictionary<IMyFaction, IMyCubeGrid> factionSpawns = GetFactionSpawns();

            foreach (var factionKvp in AssignTeams(ShareTrackApi.GetTrackedGrids()))
            {
                IMyCubeGrid spawnGrid;
                if (!factionSpawns.TryGetValue(factionKvp.Key, out spawnGrid))
                {
                    MyAPIGateway.Utilities.SendMessage("Failed to find faction spawn for " + factionKvp.Key);
                    continue;
                }

                Vector3D spawnPos = spawnGrid.GetPosition();
                Vector3D spawnDirection = -spawnPos.Normalized();
                Vector3D spawnLineDirection = Vector3D.Cross(spawnDirection, Vector3D.Up);

                spawnPos += spawnDirection * 250;

                Vector3D initialSpawnPos = spawnPos - spawnLineDirection * 250 * factionKvp.Value.Count;
                for (int i = 0; i < factionKvp.Value.Count; i++)
                {
                    factionKvp.Value[i].Teleport(MatrixD.CreateWorld(initialSpawnPos + spawnLineDirection * 250 * i, spawnDirection, Vector3D.Up));
                }
            }

            MyAPIGateway.Utilities.SendMessage($"Autobalance completed.");
        }

        private static Dictionary<IMyFaction, List<IMyCubeGrid>> AssignTeams(IMyCubeGrid[] grids)
        {
            // Sort grids by BP
            // Highest BP grid goes into lowest BP faction
            // Repeat until out of grids

            MyAPIGateway.Utilities.SendMessage($"Autobalancing {grids.Length} grids...");

            IEnumerable<IMyFaction> factions = PlayerTracker.I.GetPlayerFactions();

            Dictionary<IMyFaction, int> factionPoints = new Dictionary<IMyFaction, int>();
            Dictionary<IMyFaction, List<IMyCubeGrid>> factionGrids = new Dictionary<IMyFaction, List<IMyCubeGrid>>();
            foreach (var faction in factions)
            {
                factionPoints.Add(faction, 0);
                factionGrids.Add(faction, new List<IMyCubeGrid>());
            }

            Array.Sort(grids, (a, b) => ShareTrackApi.GetGridPoints(b) - ShareTrackApi.GetGridPoints(a));
            for (int i = 0; i < grids.Length; i++)
            {
                IMyFaction lowestFaction = factionPoints.MinBy(a => a.Value).Key;
                factionPoints[lowestFaction] += ShareTrackApi.GetGridPoints(grids[i]);

                if (grids[i].BigOwners.Count < 1)
                {
                    MyAPIGateway.Utilities.SendMessage($"Grid {grids[i].DisplayName} has no owner!");
                    continue;
                }

                MyVisualScriptLogicProvider.SetPlayersFaction(grids[i].BigOwners[0], lowestFaction.Tag);
                factionGrids[lowestFaction].Add(grids[i]);
            }

            return factionGrids;
        }

        private static Dictionary<IMyFaction, IMyCubeGrid> GetFactionSpawns()
        {
            HashSet<IMyCubeGrid> allGrids = new HashSet<IMyCubeGrid>();
            MyAPIGateway.Entities.GetEntities(null, e =>
            {
                if (e is IMyCubeGrid)
                    allGrids.Add((IMyCubeGrid) e);
                return false;
            });

            Dictionary<IMyFaction, IMyCubeGrid> factionSpawns = new Dictionary<IMyFaction, IMyCubeGrid>();

            foreach (var grid in allGrids.Where(g => g.IsStatic && g.DisplayName.EndsWith(" Spawn")))
            {
                if (grid.BigOwners.Count < 1)
                    continue;
                factionSpawns[grid.GetFaction()] = grid;
            }

            return factionSpawns;
        }
    }
}
