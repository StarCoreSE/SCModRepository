using SC.SUGMA.API;
using SC.SUGMA.GameModes.TeamDeathMatch;
using SC.SUGMA.GameState;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathMatch_Zones
{
    internal class TDMZonesGamemode : TeamDeathmatchGamemode
    {
        public override void StartRound(string[] arguments = null)
        {
            base.StartRound(arguments);

            // Here you could init visuals for the zones using the above arguments.
        }

        public override void StopRound()
        {
            base.StopRound();

            // Here you could close visuals for the zones
        }

        public override void UpdateActive()
        {
            base.UpdateActive();

            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                if (grid.GetPosition()
                    .IsInsideInclusive(ref Vector3D.Right, ref Vector3D.Left)) // pretend this is a check for zones
                {
                    PointTracker.AddFactionPoints(PlayerTracker.I.GetGridFaction(grid), -1);
                }
            }
        }
    }
}