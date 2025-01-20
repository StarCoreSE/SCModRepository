using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.Utilities
{
    public static class TeamBalancer
    {
        public const float MassWeightModifier = 20/1000000f;
        private static ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;

        public static void PerformBalancing(float randomOffset = 0.1f)
        {
            if (!ShareTrackApi.AreTrackedGridsLoaded())
                throw new Exception("Not all tracked grids are loaded!");

            // Only server should run this
            if (!MyAPIGateway.Session.IsServer)
            {
                HeartNetwork.I.SendToServer(new AutoBalancePacket());
                return;
            }

            Dictionary<IMyFaction, IMyCubeGrid> factionSpawns = SUtils.GetFactionSpawns();

            SUtils.SetDamageEnabled(false);

            foreach (var factionKvp in AssignTeams(ShareTrackApi.GetTrackedGrids(), randomOffset))
            {
                IMyCubeGrid spawnGrid;
                if (!factionSpawns.TryGetValue(factionKvp.Key, out spawnGrid))
                {
                    MyAPIGateway.Utilities.SendMessage("Failed to find faction spawn for " + factionKvp.Key);
                    continue;
                }

                Vector3D spawnPos = spawnGrid.Physics.CenterOfMassWorld;
                Vector3D spawnDirection = -spawnPos.Normalized();
                Vector3D spawnLineDirection = Vector3D.Cross(spawnDirection, Vector3D.Up);

                spawnPos += spawnDirection * 250;

                Vector3D initialSpawnPos = spawnPos - (spawnLineDirection * 250 * factionKvp.Value.Count / 2f);
                for (int i = 0; i < factionKvp.Value.Count; i++)
                    factionKvp.Value[i].Teleport(MatrixD.CreateWorld(initialSpawnPos + spawnLineDirection * 250 * i, spawnDirection, Vector3D.Up));
            }

            MyAPIGateway.Utilities.SendMessage($"Autobalance completed.");
        }

        private static Dictionary<IMyFaction, List<IMyCubeGrid>> AssignTeams(IMyCubeGrid[] grids, float randomOffset)
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

            Dictionary<IMyCubeGrid, int> gridPoints = new Dictionary<IMyCubeGrid, int>(grids.Length);
            foreach (var grid in grids)
            {
                gridPoints[grid] = ShareTrackApi.GetGridPoints(grid) +
                                       (int)((grid.Physics?.Mass ?? 0) * MassWeightModifier);
                gridPoints[grid] += (int)(gridPoints[grid] * SUtils.Random.NextDouble() * randomOffset);
            }

            Array.Sort(grids, (a, b) => gridPoints[b] - gridPoints[a]);
            for (int i = 0; i < grids.Length; i++)
            {
                IMyFaction lowestFaction = factionPoints.MinBy(a => a.Value).Key;

                factionPoints[lowestFaction] += gridPoints[grids[i]];

                if (grids[i].BigOwners.Count < 1)
                {
                    MyAPIGateway.Utilities.SendMessage($"Grid {grids[i].DisplayName} has no owner!");
                    continue;
                }

                MyVisualScriptLogicProvider.SetPlayersFaction(grids[i].BigOwners[0], lowestFaction.Tag);
                factionGrids[lowestFaction].Add(grids[i]);
                MyAPIGateway.Utilities.SendMessage($"[{lowestFaction.Tag}] +{gridPoints[grids[i]]/1000f:N1}pts: {grids[i].CustomName}");
            }

            return factionGrids;
        }
    }
}
