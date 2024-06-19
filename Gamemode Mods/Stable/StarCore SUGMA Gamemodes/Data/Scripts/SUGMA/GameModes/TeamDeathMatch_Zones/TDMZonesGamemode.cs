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
        /// <summary>
        /// The number of seconds to drain one ticket per captured zone.
        /// </summary>
        public const float ZoneTicketDrainRate = 1;

        public override string ReadableName { get; internal set; } = "Team Deathmatch w/ Zones";

        public override string Description { get; internal set; } =
            "Factions fight against eachother until tickets run out. Kill enemy players or capture zones to remove tickets.";

        private PointTracker _zonePointTracker;
        public List<FactionSphereZone> FactionZones = new List<FactionSphereZone>();

        public override void StartRound(string[] arguments = null)
        {
            _zonePointTracker = new PointTracker(0, 0);
            SUGMA_SessionComponent.I.RegisterComponent("tdmZonePointTracker", _zonePointTracker);

            base.StartRound(arguments);

            // Here you could init visuals for the zones using the above arguments.
            FactionZones.Add(new FactionSphereZone(Vector3D.Zero, 1000, 20));
            FactionZones.Add(new FactionSphereZone(new Vector3D(4000, 0, 0), 500, 15));
            FactionZones.Add(new FactionSphereZone(new Vector3D(-4000, 0, 0), 500, 15));

            for (int i = 0; i < FactionZones.Count; i++)
                SUGMA_SessionComponent.I.RegisterComponent("TDMZONE_FAC_" + i, FactionZones[i]);

            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.RegisterComponent("TDMZonesHud", new TDMZonesHud(this));
        }

        public override void StopRound()
        {
            base.StopRound();
            SUGMA_SessionComponent.I.UnregisterComponent("TDMZonesHud");
            SUGMA_SessionComponent.I.UnregisterComponent("tdmZonePointTracker");

            // Here you could close visuals for the zones
            foreach (var zone in FactionZones)
                SUGMA_SessionComponent.I.UnregisterComponent(zone.ComponentId);
            FactionZones.Clear();

        }

        public override void UpdateActive()
        {
            base.UpdateActive();

            if (_matchTimer.Ticks % (int)(ZoneTicketDrainRate*60) != 0)
                return;

            foreach (var zone in FactionZones)
            {
                if (zone.Faction == null)
                    continue;

                foreach (var faction in TrackedFactions.Keys)
                {
                    if (faction != zone.Faction)
                        _zonePointTracker.AddFactionPoints(faction, 1); // Each zone doubles the ticket drain rate
                }
            }
        }

        public override int CalculateFactionPoints(IMyFaction faction)
        {
            int points = base.CalculateFactionPoints(faction);
            return points == -1 ? -1 : points - _zonePointTracker.GetFactionPoints(faction);
        }
    }
}