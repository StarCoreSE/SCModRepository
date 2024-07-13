using System.Collections.Generic;
using System.Security.Policy;
using Sandbox.ModAPI;
using SC.SUGMA.GameModes.TeamDeathMatch;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathMatch_Zones
{
    internal class TDMZonesGamemode : TeamDeathmatchGamemode
    {
        /// <summary>
        ///     The number of seconds to drain one ticket per captured zone.
        /// </summary>
        public const float ZoneTicketDrainRate = 1.5f;

        private PointTracker _zonePointTracker;
        public List<FactionSphereZone> FactionZones = new List<FactionSphereZone>();
        public int RandomZoneCount = 3;
        public bool DoRandomZones = false;

        public override string ReadableName { get; internal set; } = "Team Deathmatch w/ Zones";

        public override string Description { get; internal set; } =
            "Factions fight against eachother until tickets run out. Kill enemy players or capture zones to remove tickets.";

        internal ZoneDef[] FixedZonePositions = {
            new ZoneDef
            {
                Position = Vector3D.Zero,
                Radius = 1000,
                CaptureTime = 20,
            },
            new ZoneDef
            {
                Position = new Vector3D(0, 0, 4000),
                Radius = 500,
                CaptureTime = 15,
            },
            new ZoneDef
            {
                Position = new Vector3D(0, 0, -4000),
                Radius = 500,
                CaptureTime = 15,
            },
        };

        //public TDMZonesGamemode()
        //{
        //    ArgumentParser += new ArgumentParser(
        //        new ArgumentParser.ArgumentDefinition(text => int.TryParse(text, out RandomZoneCount), "zc", "zone-count", "The number of zones in the arena. Only applies if randomization is enabled."),
        //        new ArgumentParser.ArgumentDefinition(text => DoRandomZones = true, "rz", "random-zones", "Enables randomly-placed zones.")
        //        );
        //}

        public override void StartRound(string[] arguments = null)
        {
            _zonePointTracker = new PointTracker(0, 0);

            base.StartRound(arguments);

            if (TrackedFactions.Count <= 1)
                return;

            SUGMA_SessionComponent.I.RegisterComponent("tdmZonePointTracker", _zonePointTracker);

            // Commented out because this would need to be synced.
            //if (DoRandomZones)
            //{
            //    for (int i = 0; i < RandomZoneCount; i++)
            //        FactionZones.Add(new FactionSphereZone(SUtils.RandVector() * 20000, 500, 15));
            //}
            //else
            //{
                foreach (var zone in FixedZonePositions)
                    FactionZones.Add(new FactionSphereZone(zone.Position, zone.Radius, zone.CaptureTime));
            //}

            for (var i = 0; i < FactionZones.Count; i++)
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

            if (_matchTimer.Ticks % (int)(ZoneTicketDrainRate * 60) != 0)
                return;

            foreach (var zone in FactionZones)
            {
                if (zone.Faction == null)
                    continue;

                foreach (var faction in TrackedFactions.Keys)
                    if (faction != zone.Faction)
                        _zonePointTracker.AddFactionPoints(faction, 1); // Each zone doubles the ticket drain rate
            }
        }

        public override int CalculateFactionPoints(IMyFaction faction)
        {
            var points = base.CalculateFactionPoints(faction) + (int)_matchTimer.CurrentMatchTime.TotalSeconds;
            return points == -1 ? -1 : points - _zonePointTracker.GetFactionPoints(faction);
        }

        internal class ZoneDef
        {
            public Vector3D Position;
            public double Radius;
            public float CaptureTime;
        }
    }
}