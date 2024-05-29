using System;
using System.Collections.Generic;
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
        [Flags]
        public enum NametagSettings
        {
            None = 0,
            PlayerName = 1,
            GridName = 2
        }

        private readonly Dictionary<IMyCubeGrid, GridStats> _gridStats = new Dictionary<IMyCubeGrid, GridStats>();


        private HudAPIv2.HUDMessage _nametag;


        private ShipTracker()
        {
        }

        public ShipTracker(IMyCubeGrid grid, bool showOnHud = true)
        {
            Grid = grid;

            var allAttachedGrids = new List<IMyCubeGrid>();
            Grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(allAttachedGrids);
            foreach (var attachedGrid in allAttachedGrids)
            {
                var stats = new GridStats(attachedGrid);
                _gridStats.Add(attachedGrid, stats);
                OriginalGridIntegrity += stats.OriginalGridIntegrity;
                if (((MyCubeGrid)attachedGrid).BlocksCount >
                    ((MyCubeGrid)Grid).BlocksCount) // Snap to the largest grid in the group.
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

            _nametag = new HudAPIv2.HUDMessage(new StringBuilder("Initializing..."), Vector2D.Zero,
                font: "BI_SEOutlined",
                blend: BlendTypeEnum.PostPP, hideHud: false, shadowing: true);
            UpdateHud();
        }

        private ShieldApi ShieldApi => PointCheck.I.ShieldApi;


        public IMyCubeGrid Grid { get; }
        public IMyPlayer Owner => MyAPIGateway.Players.GetPlayerControllingEntity(Grid) ?? PointCheck.GetOwner(OwnerId);
        public long OwnerId => Grid?.BigOwners.Count > 0 ? Grid?.BigOwners[0] ?? -1 : -1;


        public string GridName => Grid?.DisplayName;
        public float Mass => ((MyCubeGrid)Grid).GetCurrentMass();
        public Vector3 Position => Grid.Physics.CenterOfMassWorld;
        public IMyFaction OwnerFaction => MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(OwnerId);
        public string FactionName => OwnerFaction?.Name ?? "None";
        public Vector3 FactionColor => ColorMaskToRgb(OwnerFaction?.CustomColor ?? Vector3.Zero);
        public string OwnerName => Owner?.DisplayName ?? GridName;

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
            var stats = new GridStats(grid);
            _gridStats.Add(grid, stats);
            OriginalGridIntegrity += stats.OriginalGridIntegrity;
        }

        private void OnGridRemove(IMyGridGroupData groupData, IMyCubeGrid grid, IMyGridGroupData newGroupData)
        {
            if (!_gridStats.ContainsKey(grid))
                return;
            OriginalGridIntegrity -= _gridStats[grid].OriginalGridIntegrity;
            _gridStats[grid].Close();
            _gridStats.Remove(grid);
        }

        public static void SpecialBlockRename(ref string blockDisplayName, IMyCubeBlock block)
        {
            var subtype = block.BlockDefinition.SubtypeName;
            // WHY CAN'T WE JUST USE THE LATEST C# VERSION THIS IS UGLY AS HECK

            if (block is IMyGasGenerator)
                blockDisplayName = "H2O2Generator";
            else if (block is IMyGasTank)
                blockDisplayName = "HydrogenTank";
            else if (block is IMyMotorStator && subtype == "SubgridBase")
                blockDisplayName = "Invincible Subgrid";
            else if (block is IMyUpgradeModule)
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
            else if (block is IMyReactor)
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
            else if (block is IMyGyro)
                switch (subtype)
                {
                    case "LargeBlockGyro":
                        blockDisplayName = "Small Gyro";
                        break;
                    case "AQD_LG_LargeGyro":
                        blockDisplayName = "Large Gyro";
                        break;
                }
            else if (block is IMyCameraBlock)
                switch (subtype)
                {
                    case "MA_Buster_Camera":
                        blockDisplayName = "Buster Camera";
                        break;
                    case "LargeCameraBlock":
                        blockDisplayName = "Camera";
                        break;
                }
            else if (block is IMyConveyor || block is IMyConveyorTube) blockDisplayName = "Conveyor";

            if (blockDisplayName.Contains("Letter")) blockDisplayName = "Letter";
            else if (blockDisplayName.Contains("Beam Block")) blockDisplayName = "Beam Block";
            else if (blockDisplayName.Contains("Window") && !blockDisplayName.Contains("Buster"))
                blockDisplayName = "Window";
            else if (blockDisplayName.Contains("Neon"))
                blockDisplayName = "Neon Tube";
        }


        private static Vector3 ColorMaskToRgb(Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }

        /// <summary>
        ///     Updates the nametag display.
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
                var angle = GetAngleBetweenDegree(gridPosition - camera.WorldMatrix.Translation,
                    camera.WorldMatrix.Forward);

                var stealthed = ((uint)Grid.Flags & 0x1000000) > 0;
                var visible = !(newOrigin.X > 1 || newOrigin.X < -1 || newOrigin.Y > 1 || newOrigin.Y < -1) &&
                              angle <= fov && !stealthed;

                var distance = Vector3D.Distance(camera.WorldMatrix.Translation, gridPosition);
                _nametag.Scale = 1 - MathHelper.Clamp(distance / distanceThreshold, 0, 1) +
                                 30 / Math.Max(maxAngle, angle * angle * angle);
                _nametag.Origin = new Vector2D(targetHudPos.X,
                    targetHudPos.Y + MathHelper.Clamp(-0.000125 * distance + 0.25, 0.05, 0.25));
                _nametag.Visible = visible && PointCheck.NametagViewState != NametagSettings.None;

                _nametag.Message.Clear();

                var nameTagText = "";

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

        #region GridStats Pointers

        #region Global Stats

        public bool IsFunctional => TotalPower > 0 && TotalTorque > 0 && CockpitCount > 0;

        public int BlockCount
        {
            get
            {
                var total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.BlockCount;
                return total;
            }
        }

        public float GridIntegrity
        {
            get
            {
                float total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.GridIntegrity;
                return total;
            }
        }

        public float OriginalGridIntegrity;

        public int HeavyArmorCount
        {
            get
            {
                var total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.HeavyArmorCount;
                return total;
            }
        }

        public int CockpitCount
        {
            get
            {
                var total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.CockpitCount;
                return total;
            }
        }

        public int PCU
        {
            get
            {
                var total = 0;
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
                var blockCounts = new Dictionary<string, int>();
                foreach (var stats in _gridStats.Values)
                foreach (var kvp in stats.SpecialBlockCounts)
                {
                    if (!blockCounts.ContainsKey(kvp.Key))
                        blockCounts.Add(kvp.Key, 0);
                    blockCounts[kvp.Key] += kvp.Value;
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
                var total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.BattlePoints;
                return total;
            }
        }

        public int OffensivePoints
        {
            get
            {
                var total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.OffensivePoints;
                return total;
            }
        }

        public float OffensivePointsRatio => BattlePoints == 0 ? 0 : (float)OffensivePoints / BattlePoints;

        public int PowerPoints
        {
            get
            {
                var total = 0;
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
                var total = 0;
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
                var total = 0;
                foreach (var stats in _gridStats.Values)
                    total += stats.PointDefensePoints;
                return total;
            }
        }

        public float PointDefensePointsRatio => BattlePoints == 0 ? 0 : (float)PointDefensePoints / BattlePoints;


        public int RemainingPoints =>
            BattlePoints - OffensivePoints - PowerPoints - MovementPoints - PointDefensePoints;

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
                var blockCounts = new Dictionary<string, int>();
                foreach (var stats in _gridStats.Values)
                foreach (var kvp in stats.WeaponCounts)
                {
                    if (!blockCounts.ContainsKey(kvp.Key))
                        blockCounts.Add(kvp.Key, 0);
                    blockCounts[kvp.Key] += kvp.Value;
                }

                return blockCounts;
            }
        }

        #endregion

        #endregion
    }
}