using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SC.SUGMA.Utilities
{
    public static class SUtils
    {
        public static Random Random = new Random();

        private const int DamageToggleInt = 0x1;
        private const int MatchPermsInt = 0x29B;

        private const int FullPermsInt = 0x3FF;

        // Good lord this is horrendous, thanks Digi
        public static T CastProhibit<T>(T ptr, object val)
        {
            return (T)val;
        }

        public static void SetDamageEnabled(bool value)
        {
            MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Global damage {(value ? "enabled" : "disabled")}.");
            var existing = (int)MySessionComponentSafeZones.AllowedActions;
            MySessionComponentSafeZones.AllowedActions = CastProhibit(MySessionComponentSafeZones.AllowedActions,
                value ? existing | DamageToggleInt : existing & ~DamageToggleInt);
        }

        public static void SetWorldPermissionsForMatch(bool matchActive)
        {
            MyAPIGateway.Utilities.ShowMessage("SUGMA",
                $"Match global permissions {(matchActive ? "enabled" : "disabled")}.");

            MySessionComponentSafeZones.AllowedActions = CastProhibit(MySessionComponentSafeZones.AllowedActions,
                matchActive ? MatchPermsInt : FullPermsInt);
        }

        public static IMyFaction GetFaction(this IMyCubeGrid grid)
        {
            return PlayerTracker.I.GetGridFaction(grid);
        }

        public static IMyPlayer GetOwner(this IMyCubeGrid grid)
        {
            return PlayerTracker.I.GetGridOwner(grid);
        }

        public static Color ColorMaskToRgb(this Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }


        public static void ReportProblem(string issueMessage = "")
        {
            if (MyAPIGateway.Session.IsServer)
                new ProblemReportPacket(true, issueMessage).Received(0);
            else
                HeartNetwork.I.SendToServer(new ProblemReportPacket(true, issueMessage));
        }

        public static void ResolvedProblem()
        {
            if (MyAPIGateway.Session.IsServer)
                new ProblemReportPacket(false).Received(0);
            else
                HeartNetwork.I.SendToServer(new ProblemReportPacket(false));
        }

        public static void ShieldCharge()
        {
            foreach (var g in MyEntities.GetEntities())
                if (g != null && !g.MarkedForClose && g is MyCubeGrid)
                {
                    var grid = g as MyCubeGrid;
                    var block = SUGMA_SessionComponent.I.ShieldApi.GetShieldBlock(grid);
                    if (block != null) SUGMA_SessionComponent.I.ShieldApi.SetCharge(block, 99999999999);
                }

            MyAPIGateway.Utilities.ShowMessage("Shields", "Charged");
        }

        public static Vector3D RandVector()
        {
            var theta = Random.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * Random.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(Random.NextDouble(), 1/3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }

        public static void PlaySound(MySoundPair sound)
        {
            MyAPIGateway.Session?.Player?.Character?.Components.Get<MyCharacterSoundComponent>()?.PlayActionSound(sound);
        }

        public static Dictionary<IMyFaction, IMyCubeGrid> GetFactionSpawns()
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