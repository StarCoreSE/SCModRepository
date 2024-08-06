using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameState;
using VRage.Game.ModAPI;

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

            CalculateOptions(ShareTrackApi.GetTrackedGrids(), PlayerTracker.I.GetPlayerFactions());
        }

        private static void CalculateOptions(IMyCubeGrid[] grids, IEnumerable<IMyFaction> factions)
        {
            // Sort grids by BP
            // Highest BP grid goes into lowest BP faction
            // Repeat until out of grids

            MyAPIGateway.Utilities.SendMessage($"Autobalancing {grids.Length} grids...");

            Dictionary<IMyFaction, int> factionPoints = new Dictionary<IMyFaction, int>();
            foreach (var faction in factions)
                factionPoints.Add(faction, 0);

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
            }
        }
    }
}
