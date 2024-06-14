using System.Collections.Generic;
using RichHudFramework;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameModes.TeamDeathMatch;
using SC.SUGMA.GameState;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathMatch_Zones
{
    internal class TDMZonesGamemode : TeamDeathmatchGamemode
    {
        private PointTracker _zonePointTracker;

        public override void StartRound(string[] arguments = null)
        {
            _zonePointTracker = new PointTracker(0, 0);
            SUGMA_SessionComponent.I.RegisterComponent("tdmZonePointTracker", _zonePointTracker);

            base.StartRound(arguments);

            // Here you could init visuals for the zones using the above arguments.
        }

        public override void StopRound()
        {
            base.StopRound();
            SUGMA_SessionComponent.I.UnregisterComponent("tdmZonePointTracker");

            // Here you could close visuals for the zones
        }

        public override void UpdateActive()
        {
            base.UpdateActive();

            IMyCubeGrid[] trackedGrids = ShareTrackApi.GetTrackedGrids();

            foreach (var grid in new List<IMyCubeGrid>()) // TODO: Add support 
            {
                if (false) // pretend this is a check for zones
                {
                    PointTracker.AddFactionPoints(PlayerTracker.I.GetGridFaction(grid), -1);
                }
            }
        }

        public override int CalculateFactionPoints(IMyFaction faction)
        {
            int points = base.CalculateFactionPoints(faction);
            return points == -1 ? -1 : points - _zonePointTracker.GetFactionPoints(faction); // TODO
        }
    }
}