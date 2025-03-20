using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using VRage.Utils;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces.Terminal;
using Jakaria.API;
using System;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;

namespace StealthSystem
{
    public partial class StealthSession
    {
        private void Init()
        {
            if (Inited) return;
            Inited = true;

            MyAPIGateway.GridGroups.OnGridGroupCreated += GridGroupsOnOnGridGroupCreated;
            MyAPIGateway.GridGroups.OnGridGroupDestroyed += GridGroupsOnOnGridGroupDestroyed;
        }

        internal void ModCheck()
        {
            foreach (var mod in Session.Mods)
            {
                if (mod.PublishedFileId == 1918681825 || mod.PublishedFileId == 2496225055 || mod.PublishedFileId == 2726343161)
                    WcActive = true;

                else if (mod.Name == "WeaponCore" || mod.Name == "CoreSystems")
                    WcActive = true;

                else if (mod.PublishedFileId == 2200451495)
                    WaterMod = true;

                else if (mod.PublishedFileId == 1354870812)
                    RecolourableThrust = true;
                        
            }
        }

        internal bool PlayerInit()
        {
            try
            {
                //if (MyAPIGateway.Session.LocalHumanPlayer == null)
                //    return false;

                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(players);

                for (int i = 0; i < players.Count; i++)
                    PlayerConnected(players[i].IdentityId);

                return true;
            }
            catch (Exception ex)
            {
                Logs.WriteLine($"Caught exception in PlayerInit() - {ex}");
            }

            return false;
        }

        internal void UpdateWaters()
        {
            if (IsClient && PlayersLoaded && MyAPIGateway.Session.Player?.Character != null)
            {
                var character = MyAPIGateway.Session.Player.Character.PositionComp.WorldAABB.Center;
                var closestPlanet = MyGamePruningStructure.GetClosestPlanet(character);
                if (closestPlanet.EntityId != 0 && !PlanetMap.ContainsKey(closestPlanet.EntityId))
                    PlanetTemp.TryAdd(closestPlanet, closestPlanet.EntityId);
            }

            if (!PlanetTemp.IsEmpty)
            {
                foreach (var planetToAdd in PlanetTemp)
                {
                    if (planetToAdd.Key.EntityId != 0)
                        PlanetMap.TryAdd(planetToAdd.Key.EntityId, planetToAdd.Key);
                }

                PlanetTemp.Clear();
            }

            foreach (var planet in PlanetMap.Values)
            {
                WaterData data;
                if (WaterModAPI.HasWater(planet))
                {
                    if (!WaterMap.TryGetValue(planet.EntityId, out data))
                    {
                        data = new WaterData(planet);
                        WaterMap[planet.EntityId] = data;
                    }

                    var radiusInfo = WaterModAPI.GetPhysical(planet);
                    data.Centre = radiusInfo.Item1;
                    data.Radius = radiusInfo.Item2;
                }
                else WaterMap.TryRemove(planet.EntityId, out data);
            }
        }

        internal void UpdateEnforcement(StealthSettings settings)
        {
            Enforced = true;
            Logs.WriteLine($"Config settings loaded");

            JumpPenalty = settings.JumpPenalty;
            Transparency = settings.Transparency;
            ShieldDelay = settings.ShieldDelay;
            FadeTime = settings.FadeTime;
            DamageThreshold = settings.DamageThreshold;
            DisableShields = settings.DisableShields;
            DisableWeapons = settings.DisableWeapons;
            HideThrusterFlames = settings.HideThrusterFlames;
            WorkInWater = settings.WorkInWater;
            WorkOutOfWater = settings.WorkOutOfWater;
            WaterTransitionDepth = settings.WaterTransitionDepth;
            RevealOnDamage = settings.RevealOnDamage;

            WaterOffsetSqr = WaterTransitionDepth * Math.Abs(WaterTransitionDepth);
            TrackWater = WaterMod && WorkInWater != WorkOutOfWater;
            TrackDamage = DamageThreshold > 0;

            FadeSteps = FadeTime / FADE_INTERVAL + 1;

            foreach (var drive in settings.DriveConfigs)
            {
                var def = new Definitions.DriveDefinition(drive.Duration, drive.PowerScale, drive.SignalRangeScale);
                DriveDefinitions[drive.Subtype] = def;
            }

            foreach (var sink in settings.SinkConfigs)
            {
                var def = new Definitions.SinkDefinition(sink.Duration, sink.Power, sink.DoDamage);
                SinkDefinitions[sink.Subtype] = def;
            }

            StealthFlag = (EntityFlags)(DisableWeapons ? IsStealthedFlag + 4 : IsStealthedFlag);
        }

        internal void RemoveEdges()
        {
            var defs = MyDefinitionManager.Static.GetAllDefinitions();
            foreach (var def in defs)
            {
                if (def is MyCubeBlockDefinition && def.Id.SubtypeName.Contains("Armor"))
                {
                    var armorDef = (MyCubeBlockDefinition)def;
                    if (armorDef.CubeDefinition == null)
                        continue;

                    armorDef.CubeDefinition.ShowEdges = false;
                }
            }
        }

        internal void RefreshTerminal(IMyFunctionalBlock block, IMyTerminalControlOnOffSwitch control)
        {
            block.RefreshCustomInfo();

            if (control != null)
            {
                var originalSetting = control.Getter(block);
                control.Setter(block, !originalSetting);
                control.Setter(block, originalSetting);
            }
        }

        internal static void DrawBox(MyOrientedBoundingBoxD obb, Color color)
        {
            var box = new BoundingBoxD(-obb.HalfExtent, obb.HalfExtent);
            var wm = MatrixD.CreateFromTransformScale(obb.Orientation, obb.Center, Vector3D.One);
            var material = MyStringId.GetOrCompute("Square");
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref box, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.01f, null, material);
        }

        internal static void DrawScaledPoint(Vector3D pos, double radius, Color color, int divideRatio = 20, bool solid = true, float lineWidth = 0.5f)
        {
            var posMatCenterScaled = MatrixD.CreateTranslation(pos);
            var posMatScaler = MatrixD.Rescale(posMatCenterScaled, radius);
            var material = MyStringId.GetOrCompute("Square");
            MySimpleObjectDraw.DrawTransparentSphere(ref posMatScaler, 1f, ref color, solid ? MySimpleObjectRasterizer.Solid : MySimpleObjectRasterizer.Wireframe, divideRatio, null, material, lineWidth);
        }

        internal static void DrawLine(Vector3D start, Vector3D end, Vector4 color, float width)
        {
            var c = color;
            MySimpleObjectDraw.DrawLine(start, end, _square, ref c, width);
        }

        internal static void DrawLine(Vector3D start, Vector3D dir, Vector4 color, float width, float length)
        {
            var c = color;
            MySimpleObjectDraw.DrawLine(start, start + (dir * length), _square, ref c, width);
        }
    }
}
