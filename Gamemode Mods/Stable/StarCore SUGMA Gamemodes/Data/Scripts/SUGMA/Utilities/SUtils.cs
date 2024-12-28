using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
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

            if (matchActive && MyAPIGateway.Session.IsServer)
                ClearImageLcds();
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

        public static bool IsPaused { get; private set; } = false;
        public static void Pause()
        {
            if (IsPaused)
            {
                DisconnectHandler.I.UnfreezeGrids(true);
                MyAPIGateway.Utilities.SendMessage("Paused the game!");
                IsPaused = false;
            }
            else
            {
                MyAPIGateway.Entities.GetEntities(null, DisconnectHandler.I.FreezeGrids);
                MyAPIGateway.Utilities.SendMessage("Unpaused the game!");
                IsPaused = true;
            }
        }

        /// <summary>
        /// Kills all players, ends the match, and deletes all player grids.
        /// </summary>
        public static void ClearBoard(bool resetFactions)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            var playerIds = new List<long>();

            foreach (var faction in PlayerTracker.I.GetPlayerFactions())
                playerIds.AddRange(faction.Members.Values.Select(player => player.PlayerId));

            var greenSpawn = GetFactionSpawns().FirstOrDefault(b => b.Key.Tag == "NEU").Value;
            foreach (var player in PlayerTracker.I.AllPlayers.Where(p => playerIds.Contains(p.Key)))
            {
                if (greenSpawn != null)
                    player.Value.Character?.SetWorldMatrix(greenSpawn.WorldMatrix);
                else
                    player.Value.Character?.Kill();
            }

            SUGMA_SessionComponent.I.StopGamemode(true);

            MyAPIGateway.Entities.GetEntities(null, g =>
            {
                IMyCubeGrid grid = g as IMyCubeGrid;
                if (grid == null)
                    return false;

                // If this ever becomes an issue with deleting existing subgrids, change it to a GridGroup check.
                if (!grid.IsStatic)
                    grid.Close();

                return false;
            });

            if (resetFactions)
            {
                IMyFaction neutralFaction = MyAPIGateway.Session.Factions.Factions.Values.FirstOrDefault(f => f.Tag == "NEU") ?? PlayerTracker.I.GetPlayerFactions().First();
                foreach (var player in PlayerTracker.I.AllPlayers)
                {
                    MyVisualScriptLogicProvider.SetPlayersFaction(player.Key, neutralFaction.Tag);
                }
            }

            MyAPIGateway.Utilities.SendMessage("Board cleared.");
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

        public static Vector3D RandVector(double min = 0, double max = 1)
        {
            if (max < min)
                max = min;

            var theta = Random.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * Random.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(Random.NextDouble() * (max - min) + min, 1/3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
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

        /// <summary>
        /// Text-image LCDs are *bad for performance*. This clears all text larger than 1000 chars from all LCDs in the world.
        /// </summary>
        public static void ClearImageLcds()
        {
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

            int lcdCount = 0;
            StringBuilder text = new StringBuilder();
            foreach (var ent in entities)
            {
                IMyCubeGrid grid = ent as IMyCubeGrid;
                if (grid == null)
                    continue;

                foreach (var lcd in grid.GetFatBlocks<IMyTextPanel>())
                {
                    lcd.ReadText(text);
                    if (text.Length > 1000)
                    {
                        lcd.WriteText("");
                        lcdCount++;
                    }
                }
            }

            if (lcdCount == 0)
                return;

            MyAPIGateway.Utilities.ShowMessage("", $"{lcdCount} LCD(s) Cleared.");
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Utilities.SendMessage($"{lcdCount} LCD(s) Cleared.");
        }
    }
}