using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using DefenseShields;
using Draygo.API;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace ShipPoints.ShipTracking
{
    public class ShipTracker
    {
        private ShieldApi ShieldApi => PointCheck.I.ShieldApi;


        public IMyCubeGrid Grid { get; private set; }
        public IMyPlayer Owner => MyAPIGateway.Players.GetPlayerControllingEntity(Grid) ?? PointCheck.GetOwner(OwnerId);
        public long OwnerId => Grid?.BigOwners.Count > 0 ? Grid?.BigOwners[0] ?? -1 : -1;


        public string GridName => Grid?.DisplayName;
        public float Mass => ((MyCubeGrid)Grid).GetCurrentMass();
        public Vector3 Position => Grid.Physics.CenterOfMassWorld;
        public IMyFaction OwnerFaction => MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(OwnerId);
        public string FactionName => OwnerFaction?.Name ?? "None";
        public Vector3 FactionColor => ColorMaskToRgb(OwnerFaction?.CustomColor ?? Vector3.Zero);
        public string OwnerName => Owner?.DisplayName ?? GridName;

        #region GridStats Pointers

        #region Global Stats

        public bool IsFunctional => TotalPower > 0 && TotalTorque > 0 && CockpitCount > 0;
        public int BlockCount
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.BlockCount;
                return total;
            }
        }
        public int HeavyArmorCount
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.HeavyArmorCount;
                return total;
            }
        }
        public int CockpitCount
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.CockpitCount;
                return total;
            }
        }
        public int PCU
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.PCU;
                return total;
            }
        }
        public float TotalThrust
        {
            get
            {
                float total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.TotalThrust;
                return total;
            }
        }
        public float TotalTorque
        {
            get
            {
                float total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.TotalTorque;
                return total;
            }
        }
        public float TotalPower
        {
            get
            {
                float total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.TotalPower;
                return total;
            }
        }
        public Dictionary<string, int> SpecialBlockCounts
        {
            get
            {
                Dictionary<string, int> blockCounts = new Dictionary<string, int>();
                foreach (var stats in _gridStats.Values)
                {
                    foreach (var kvp in stats.SpecialBlockCounts)
                    {
                        if (!blockCounts.ContainsKey(kvp.Key))
                            blockCounts.Add(kvp.Key, 0);
                        blockCounts[kvp.Key] += kvp.Value;
                    }
                }

                return blockCounts;
            }
        }

        #endregion

        #region BattlePoint Stats
        public int BattlePoints
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.BattlePoints;
                return total;
            }
        }
        public int OffensivePoints
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.OffensivePoints;
                return total;
            }
        }

        public float OffensivePointsRatio => BattlePoints == 0 ? 0 : (float) OffensivePoints / BattlePoints;
        public int PowerPoints
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.PowerPoints;
                return total;
            }
        }
        public float PowerPointsRatio => BattlePoints == 0 ? 0 : (float)PowerPoints / BattlePoints;

        public int MovementPoints
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.MovementPoints;
                return total;
            }
        }
        public float MovementPointsRatio => BattlePoints == 0 ? 0 : (float)MovementPoints / BattlePoints;

        public int PointDefensePoints
        {
            get
            {
                int total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.PointDefensePoints;
                return total;
            }
        }
        public float PointDefensePointsRatio => BattlePoints == 0 ? 0 : (float) PointDefensePoints / BattlePoints;


        public int RemainingPoints => BattlePoints - OffensivePoints - PowerPoints - MovementPoints - PointDefensePoints;
        public int RemainingPointsRatio => BattlePoints == 0 ? 0 : RemainingPoints / BattlePoints;

        #endregion

        #region Shield Stats

        public float OriginalMaxShieldHealth = -1;
        public float MaxShieldHealth
        {
            get
            {
                var shieldController = ShieldApi.GetShieldBlock(Grid);
                if (shieldController == null)
                    return -1;
                return ShieldApi.GetMaxHpCap(shieldController);
            }
        }
        public float CurrentShieldPercent
        {
            get
            {
                var shieldController = ShieldApi.GetShieldBlock(Grid);
                if (shieldController == null)
                    return -1;
                return ShieldApi.GetShieldPercent(shieldController);
            }
        }
        public float CurrentShieldHeat
        {
            get
            {
                var shieldController = ShieldApi.GetShieldBlock(Grid);
                if (shieldController == null)
                    return -1;
                return ShieldApi.GetShieldHeat(shieldController);
            }
        }

        #endregion

        #region Weapon Stats 

        public Dictionary<string, int> WeaponCounts
        {
            get
            {
                Dictionary<string, int> blockCounts = new Dictionary<string, int>();
                foreach (var stats in _gridStats.Values)
                {
                    foreach (var kvp in stats.WeaponCounts)
                    {
                        if (!blockCounts.ContainsKey(kvp.Key))
                            blockCounts.Add(kvp.Key, 0);
                        blockCounts[kvp.Key] += kvp.Value;
                    }
                }

                return blockCounts;
            }
        }

        #endregion

        #endregion




        private HudAPIv2.HUDMessage _nametag;

        private readonly Dictionary<IMyCubeGrid, GridStats> _gridStats = new Dictionary<IMyCubeGrid, GridStats>();


        private ShipTracker()
        {
        }

        public ShipTracker(IMyCubeGrid grid, bool showOnHud = true)
        {
            Grid = grid;
            //_gridStats.Add(Grid, new GridStats(Grid));

            List<IMyCubeGrid> allAttachedGrids = new List<IMyCubeGrid>();
            Grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(allAttachedGrids);
            foreach (var attachedGrid in allAttachedGrids)
            {
                _gridStats.Add(attachedGrid, new GridStats(attachedGrid));
                if (((MyCubeGrid)attachedGrid).BlocksCount > ((MyCubeGrid)Grid).BlocksCount) // Snap to the largest grid in the group.
                    Grid = attachedGrid;
            }

            Update();

            if (!showOnHud)
                return;

            Grid.OnClose += OnClose;
            Grid.GetGridGroup(GridLinkTypeEnum.Physical).OnGridAdded += OnGridAdd;
            Grid.GetGridGroup(GridLinkTypeEnum.Physical).OnGridRemoved += OnGridRemove;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            _nametag = new HudAPIv2.HUDMessage(new StringBuilder("Initializing..."), Vector2D.Zero, font: "BI_SEOutlined",
                blend: BlendTypeEnum.PostPP, hideHud: false, shadowing: true);
            UpdateHud();
        }

        public void OnClose(IMyEntity e)
        {
            if (Grid != null)
            {
                Grid.OnClose -= OnClose;
                var gridGroup = Grid.GetGridGroup(GridLinkTypeEnum.Physical);
                if (gridGroup != null)
                {
                    gridGroup.OnGridAdded -= OnGridAdd;
                    gridGroup.OnGridRemoved -= OnGridRemove;
                }
            }

            TrackingManager.I.TrackedGrids.Remove(Grid);

            DisposeHud();
        }

        public void Update()
        {
            if (Grid?.Physics == null) // TODO transfer to a different grid
                return;

            var shieldController = ShieldApi.GetShieldBlock(Grid);
            if (shieldController == null)
                OriginalMaxShieldHealth = -1;
            if (OriginalMaxShieldHealth == -1 && !ShieldApi.IsFortified(shieldController))
                OriginalMaxShieldHealth = MaxShieldHealth;

            // TODO: Update pilots
            foreach (var gridStat in _gridStats.Values)
                gridStat.Update();
        }

        private void OnGridAdd(IMyGridGroupData groupData, IMyCubeGrid grid, IMyGridGroupData previousGroupData)
        {
            if (_gridStats.ContainsKey(grid))
                return;
            _gridStats.Add(grid, new GridStats(grid));
        }

        private void OnGridRemove(IMyGridGroupData groupData, IMyCubeGrid grid, IMyGridGroupData newGroupData)
        {
            if (!_gridStats.ContainsKey(grid))
                return;
            _gridStats[grid].Close();
            _gridStats.Remove(grid);
        }

        public static void ClimbingCostRename(ref string costGroupName, ref float costMultiplier)
        {
            switch (costGroupName)
            {
                case "Blink Drive Large":
                    costGroupName = "Blink Drive";
                    costMultiplier = 0.15f;
                    break;
                case "Project Pluto (SLAM)":
                case "SLAM":
                    costGroupName = "SLAM";
                    costMultiplier = 0.25f;
                    break;
                case "[BTI] MRM-10 Modular Launcher 45":
                case "[BTI] MRM-10 Modular Launcher 45 Reversed":
                case "[BTI] MRM-10 Modular Launcher":
                case "[BTI] MRM-10 Modular Launcher Middle":
                case "[BTI] MRM-10 Launcher":
                    costGroupName = "MRM-10 Launcher";
                    costMultiplier = 0.04f;
                    break;
                case "[BTI] LRM-5 Modular Launcher 45 Reversed":
                case "[BTI] LRM-5 Modular Launcher 45":
                case "[BTI] LRM-5 Modular Launcher Middle":
                case "[BTI] LRM-5 Modular Launcher":
                case "[BTI] LRM-5 Launcher":
                    costGroupName = "LRM-5 Launcher";
                    costMultiplier = 0.10f;
                    break;
                case "[MA] Gimbal Laser T2 Armored":
                case "[MA] Gimbal Laser T2 Armored Slope 45":
                case "[MA] Gimbal Laser T2 Armored Slope 2":
                case "[MA] Gimbal Laser T2 Armored Slope":
                case "[MA] Gimbal Laser T2":
                    costGroupName = "Gimbal Laser T2";
                    costMultiplier = 0f;
                    break;
                case "[MA] Gimbal Laser Armored Slope 45":
                case "[MA] Gimbal Laser Armored Slope 2":
                case "[MA] Gimbal Laser Armored Slope":
                case "[MA] Gimbal Laser Armored":
                case "[MA] Gimbal Laser":
                    costGroupName = "Gimbal Laser";
                    costMultiplier = 0f;
                    break;
                case "[ONYX] BR-RT7 Afflictor Slanted Burst Cannon":
                case "[ONYX] BR-RT7 Afflictor 70mm Burst Cannon":
                case "[ONYX] Afflictor":
                    costGroupName = "Afflictor";
                    costMultiplier = 0f;
                    break;
                case "[MA] Slinger AC 150mm Sloped 30":
                case "[MA] Slinger AC 150mm Sloped 45":
                case "[MA] Slinger AC 150mm Gantry Style":
                case "[MA] Slinger AC 150mm Sloped 45 Gantry":
                case "[MA] Slinger AC 150mm":
                case "[MA] Slinger":
                    costGroupName = "Slinger";
                    costMultiplier = 0f;
                    break;
                case "[ONYX] Heliod Plasma Pulser":
                    costGroupName = "Heliod Plasma Pulser";
                    costMultiplier = 0.50f;
                    break;
                case "[MA] UNN Heavy Torpedo Launcher":
                    costGroupName = "UNN Heavy Torpedo Launcher";
                    costMultiplier = 0.15f;
                    break;
                case "[BTI] SRM-8":
                    costGroupName = "SRM-8";
                    costMultiplier = 0.15f;
                    break;
                case "[BTI] Starcore Arrow-IV Launcher":
                    costGroupName = "Starcore Arrow-IV Launcher";
                    costMultiplier = 0.15f;
                    break;
                case "[HAS] Tartarus VIII":
                    costGroupName = "Tartarus VIII";
                    costMultiplier = 0.15f;
                    break;
                case "[HAS] Cocytus IX":
                    costGroupName = "Cocytus IX";
                    costMultiplier = 0.15f;
                    break;
                case "[MA] MCRN Torpedo Launcher":
                    costGroupName = "MCRN Torpedo Launcher";
                    costMultiplier = 0.15f;
                    break;
                case "Flares":
                    costGroupName = "Flares";
                    costMultiplier = 0.25f;
                    break;
                case "[EXO] Chiasm [Arc Emitter]":
                    costGroupName = "Chiasm [Arc Emitter]";
                    costMultiplier = 0.15f;
                    break;
                case "[BTI] Medium Laser":
                case "[BTI] Large Laser":
                    costGroupName = " Laser";
                    costMultiplier = 0.15f;
                    break;
                case "Reinforced Blastplate":
                case "Active Blastplate":
                case "Standard Blastplate A":
                case "Standard Blastplate B":
                case "Standard Blastplate C":
                case "Elongated Blastplate":
                case "7x7 Basedplate":
                    costGroupName = "Blastplate";
                    costMultiplier = 1.00f;
                    break;
                case "[EXO] Taiidan":
                case "[EXO] Taiidan Fighter Launch Rail":
                case "[EXO] Taiidan Bomber Launch Rail":
                case "[EXO] Taiidan Fighter Hangar Bay":
                case "[EXO] Taiidan Bomber Hangar Bay":
                case "[EXO] Taiidan Bomber Hangar Bay Medium":
                case "[EXO] Taiidan Fighter Small Bay":
                    costGroupName = "Taiidan";
                    costMultiplier = 0.25f;
                    break;
                case "[40K] Gothic Torpedo Launcher":
                    costGroupName = "Gothic Torpedo Launcher";
                    costMultiplier = 0.15f;
                    break;
                case "[MID] AX 'Spitfire' Light Rocket Turret":
                    costGroupName = "Spitfire Turret";
                    costMultiplier = 0.15f;
                    break;
                case "[FLAW] Naval RL-10x 'Avalanche' Medium Range Launchers":
                case "[FLAW] Naval RL-10x 'Avalanche' Angled Medium Range Launchers":
                    costGroupName = "RL-10x Avalanche";
                    costMultiplier = 0.15f;
                    break;
                case "[MID] LK 'Bonfire' Guided Rocket Turret":
                    costGroupName = "Bonfire Turret";
                    costMultiplier = 0.2f;
                    break;
                case "[FLAW] Warp Beacon - Longsword":
                    costGroupName = "Longsword Bomber";
                    costMultiplier = 0.2f;
                    break;
                case "[FLAW] Phoenix Snubfighter Launch Bay":
                    costGroupName = "Snubfighters";
                    costMultiplier = 0.1f;
                    break;
                case "[FLAW] Hadean Superheavy Plasma Blastguns":
                    costGroupName = "Plasma Blastgun";
                    costMultiplier = 0.121f;
                    break;
                case "[FLAW] Vindicator Kinetic Battery":
                    costGroupName = "Kinetic Battery";
                    costMultiplier = 0.120f;
                    break;
                case "[FLAW] Goalkeeper Casemate Flak Battery":
                    costGroupName = "Goalkeeper Flakwall";
                    costMultiplier = 0.119f;
                    break;
                case "Shield Controller":
                case "Shield Controller Table":
                case "Structural Integrity Field Generator":
                    costGroupName = "Defensive Generator";
                    costMultiplier = 50.00f;
                    break;
            }
        }

        public static void SpecialBlockRename(ref string blockDisplayName, IMyCubeBlock block)
        {
            string subtype = block.BlockDefinition.SubtypeName;
            // WHY CAN'T WE JUST USE THE LATEST C# VERSION THIS IS UGLY AS HECK

            if (block is IMyGasGenerator)
            {
                blockDisplayName = "H2O2Generator";
            }
            else if (block is IMyGasTank)
            {
                blockDisplayName = "HydrogenTank";
            }
            else if (block is IMyMotorStator && subtype == "SubgridBase")
            {
                blockDisplayName = "Invincible Subgrid";
            }
            else if (block is IMyUpgradeModule)
            {
                switch (subtype)
                {
                    case "LargeEnhancer":
                        blockDisplayName = "Shield Enhancer";
                        break;
                    case "EmitterL":
                    case "EmitterLA":
                        blockDisplayName = "Shield Emitter";
                        break;
                    case "LargeShieldModulator":
                        blockDisplayName = "Shield Modulator";
                        break;
                    case "DSControlLarge":
                    case "DSControlTable":
                        blockDisplayName = "Shield Controller";
                        break;
                    case "AQD_LG_GyroBooster":
                        blockDisplayName = "Gyro Booster";
                        break;
                    case "AQD_LG_GyroUpgrade":
                        blockDisplayName = "Large Gyro Booster";
                        break;
                }
            }
            else if (block is IMyReactor)
            {
                switch (subtype)
                {
                    case "LargeBlockLargeGenerator":
                    case "LargeBlockLargeGeneratorWarfare2":
                        blockDisplayName = "Large Reactor";
                        break;
                    case "LargeBlockSmallGenerator":
                    case "LargeBlockSmallGeneratorWarfare2":
                        blockDisplayName = "Small Reactor";
                        break;
                }
            }
            else if (block is IMyGyro)
            {
                switch (subtype)
                {
                    case "LargeBlockGyro":
                        blockDisplayName = "Small Gyro";
                        break;
                    case "AQD_LG_LargeGyro":
                        blockDisplayName = "Large Gyro";
                        break;
                }
            }
            else if (block is IMyCameraBlock)
            {
                switch (subtype)
                {
                    case "MA_Buster_Camera":
                        blockDisplayName = "Buster Camera";
                        break;
                    case "LargeCameraBlock":
                        blockDisplayName = "Camera";
                        break;
                }
            }
            else if (block is IMyConveyor || block is IMyConveyorTube)
            {
                blockDisplayName = "Conveyor";
            }
        }


        private static Vector3 ColorMaskToRgb(Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }

        /// <summary>
        /// Updates the nametag display.
        /// </summary>
        public void UpdateHud()
        {
            if (_nametag == null || MyAPIGateway.Utilities.IsDedicated)
                return;

            try
            {
                var camera = MyAPIGateway.Session.Camera;
                const int distanceThreshold = 20000;
                const int maxAngle = 60; // Adjust this angle as needed

                Vector3D gridPosition = Position;

                var targetHudPos = camera.WorldToScreen(ref gridPosition);
                var newOrigin = new Vector2D(targetHudPos.X, targetHudPos.Y);

                _nametag.InitialColor = new Color(FactionColor);
                var fov = camera.FieldOfViewAngle;
                var angle = GetAngleBetweenDegree(gridPosition - camera.WorldMatrix.Translation, camera.WorldMatrix.Forward);

                var stealthed = ((uint)Grid.Flags & 0x1000000) > 0;
                var visible = !(newOrigin.X > 1 || newOrigin.X < -1 || newOrigin.Y > 1 || newOrigin.Y < -1) &&
                              angle <= fov && !stealthed;

                var distance = Vector3D.Distance(camera.WorldMatrix.Translation, gridPosition);
                _nametag.Scale = 1 - MathHelper.Clamp(distance / distanceThreshold, 0, 1) +
                                 30 / Math.Max(maxAngle, angle * angle * angle);
                _nametag.Origin = new Vector2D(targetHudPos.X,
                    targetHudPos.Y + MathHelper.Clamp(-0.000125 * distance + 0.25, 0.05, 0.25));
                _nametag.Visible = PointCheckHelpers.NameplateVisible && visible;

                _nametag.Message.Clear();

                string nameTagText = "";

                if ((PointCheck.NametagViewState & NametagSettings.PlayerName) > 0)
                    nameTagText += OwnerName;
                if ((PointCheck.NametagViewState & NametagSettings.GridName) > 0)
                    nameTagText += "\n" + GridName;
                if (!IsFunctional)
                    nameTagText += "<color=white>:[Dead]";

                _nametag.Message.Append(nameTagText.TrimStart('\n'));
                _nametag.Offset = -_nametag.GetTextLength() / 2;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private double GetAngleBetweenDegree(Vector3D vectorA, Vector3D vectorB)
        {
            vectorA.Normalize();
            vectorB.Normalize();
            return Math.Acos(MathHelper.Clamp(vectorA.Dot(vectorB), -1, 1)) * (180.0 / Math.PI);
        }

        public void DisposeHud()
        {
            if (_nametag != null)
            {
                _nametag.Visible = false;
                _nametag.Message.Clear();
                _nametag.DeleteMessage();
            }

            _nametag = null;
        }

        [Flags]
        public enum NametagSettings
        {
            None = 0,
            PlayerName = 1,
            GridName = 2,
        }
    }
}