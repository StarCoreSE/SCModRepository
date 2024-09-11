using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using StarCore.ShareTrack.API;
using StarCore.ShareTrack.API.CoreSystem;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace StarCore.ShareTrack.ShipTracking
{
    internal class GridStats // TODO convert this to be event-driven. OnBlockPlace, etc. Keep a queue.
    {
        private readonly HashSet<IMyCubeBlock> _fatBlocks = new HashSet<IMyCubeBlock>();

        private readonly HashSet<IMySlimBlock> _slimBlocks;
        private ShieldApi ShieldApi => AllGridsList.I.ShieldApi;
        private WcApi WcApi => AllGridsList.I.WcApi;

        public bool NeedsUpdate { get; private set; } = true;

        #region Public Methods

        public GridStats(IMyCubeGrid grid)
        {
            if (grid == null)
            {
                Log.Error("GridStats constructor called with null grid");
                throw new ArgumentNullException(nameof(grid));
            }

            Grid = grid;
            var allSlimBlocks = new List<IMySlimBlock>();
            Grid.GetBlocks(allSlimBlocks);
            _slimBlocks = allSlimBlocks.ToHashSet();

            foreach (var block in _slimBlocks)
            {
                if (block?.FatBlock != null)
                {
                    _fatBlocks.Add(block.FatBlock);
                    GridIntegrity += block.Integrity;
                }
            }

            OriginalGridIntegrity = GridIntegrity;
            Grid.OnBlockAdded += OnBlockAdd;
            Grid.OnBlockRemoved += OnBlockRemove;
            Update();
        }

        public void Close()
        {
            Grid.OnBlockAdded -= OnBlockAdd;
            Grid.OnBlockRemoved -= OnBlockRemove;

            _slimBlocks.Clear();
            _fatBlocks.Clear();
        }

        public void UpdateAfterSim()
        {
            UpdateCounter++;

            if (Grid != null)
            {
                int updateCount = (int)(Grid.EntityId % UpdateInterval);

                if (UpdateCounter % UpdateInterval == updateCount)
                {
                    float tempGridInteg = 0;

                    foreach (var block in _slimBlocks)
                    {
                        if (block.FatBlock != null) // Remove To Count All Blocks
                        {
                            tempGridInteg += block.Integrity;
                        }
                    }

                    GridIntegrity = tempGridInteg;
                }
            }

            if (UpdateCounter >= int.MaxValue - UpdateInterval)
            {
                UpdateCounter = 0;
            }             
        }

        public void Update()
        {
            if (!NeedsUpdate) // Modscripts changing the output of a block (i.e. Fusion Systems) without adding/removing blocks will not be updated properly.
                return; // I am willing to make this sacrifice.

            BattlePoints = 0;
            OffensivePoints = 0;
            PowerPoints = 0;
            MovementPoints = 0;
            PointDefensePoints = 0;
            CockpitCount = 0;

            // Setting battlepoints first so that calcs can do calc stuff
            foreach (var block in
                     _fatBlocks) // If slimblock points become necessary in the future, change this to _slimBlock
                CalculateCost(block);

            UpdateGlobalStats();
            UpdateWeaponStats();
            NeedsUpdate = false;
        }

        #endregion

        #region Public Fields

        public readonly IMyCubeGrid Grid;

        // Global Stats
        public int BlockCount { get; private set; }
        public int HeavyArmorCount { get; private set; }
        public int CockpitCount { get; private set; }
        public int PCU { get; private set; }
        public readonly Dictionary<string, int> BlockCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, int> SpecialBlockCounts = new Dictionary<string, int>();
        public float TotalThrust { get; private set; }
        public float TotalTorque { get; private set; }

        public float GridIntegrity { get; private set; }
        public float OriginalGridIntegrity { get; private set; }

        // BattlePoint Stats
        public double BattlePoints { get; private set; }
        public double OffensivePoints { get; private set; }
        public double PowerPoints { get; private set; }
        public double MovementPoints { get; private set; }
        public double PointDefensePoints { get; private set; }

        // Weapon Stats
        public readonly Dictionary<string, int> WeaponCounts = new Dictionary<string, int>();

        #endregion

        #region Private Actions

        private void OnBlockAdd(IMySlimBlock block)
        {
            if (block == null)
                return;

            _slimBlocks.Add(block);
            if (block.FatBlock != null)
                _fatBlocks.Add(block.FatBlock);

            NeedsUpdate = true;
        }

        private void OnBlockRemove(IMySlimBlock block)
        {
            if (block == null)
                return;

            _slimBlocks.Remove(block);
            if (block.FatBlock != null)
                _fatBlocks.Remove(block.FatBlock);

            NeedsUpdate = true;
        }

        #endregion

        #region Private Fields

        private int UpdateCounter = 0;
        private int UpdateInterval = 100;
        // TODO

        #endregion

        #region Private Methods

        private void UpdateGlobalStats()
        {
            BlockCounts.Clear();
            SpecialBlockCounts.Clear();

            TotalThrust = 0;
            TotalTorque = 0;

            foreach (var block in _fatBlocks)
            {
                if (block is IMyCockpit && block.IsFunctional)
                    CockpitCount++;

                if (block is IMyThrust && block.IsFunctional)
                {
                    TotalThrust += ((IMyThrust)block).MaxEffectiveThrust;
                }

                else if (block is IMyGyro && block.IsFunctional)
                {
                    TotalTorque +=
                        ((MyGyroDefinition)MyDefinitionManager.Static.GetDefinition((block as IMyGyro).BlockDefinition))
                        .ForceMagnitude * (block as IMyGyro).GyroStrengthMultiplier;
                }

                if (!(block is IMyConveyorSorter) || !WcApi.HasCoreWeapon((MyEntity)block))
                {
                    var blockDisplayName = block.DefinitionDisplayNameText;
                    if (blockDisplayName
                        .Contains("Armor") && !blockDisplayName.StartsWith("Armor Laser")) // This is a bit stupid. TODO find a better way to sort out armor blocks.
                        continue;

                    if (!AllGridsList.PointValues.ContainsKey(block.BlockDefinition.SubtypeName))
                        continue;

                    float ignored = 0;
                    AllGridsList.ClimbingCostRename(ref blockDisplayName, ref ignored);
                    ShipTracker.SpecialBlockRename(ref blockDisplayName, block);
                    if (!SpecialBlockCounts.ContainsKey(blockDisplayName))
                        SpecialBlockCounts.Add(blockDisplayName, 0);
                    SpecialBlockCounts[blockDisplayName]++;
                }
            }

            BlockCount = ((MyCubeGrid)Grid).BlocksCount;
            PCU = ((MyCubeGrid)Grid).BlocksPCU;
            HeavyArmorCount = 0;
            foreach (var slimBlock in _slimBlocks)
            {
                if (slimBlock.FatBlock != null)
                    continue;

                if (slimBlock.BlockDefinition.Id.SubtypeName.Contains("Heavy"))
                    HeavyArmorCount++;
            }
        }

        private void UpdateWeaponStats()
        {
            WeaponCounts.Clear();
            foreach (var block in _fatBlocks)
            {
                double weaponPoints;
                var weaponDisplayName = block.DefinitionDisplayNameText;

                // Check for WeaponCore weapons
                if (AllGridsList.PointValues.TryGetValue(block.BlockDefinition.SubtypeName, out weaponPoints) && WcApi.HasCoreWeapon((MyEntity)block))
                {
                    float thisClimbingCostMult = 0;
                    AllGridsList.ClimbingCostRename(ref weaponDisplayName, ref thisClimbingCostMult);
                    AddWeaponCount(weaponDisplayName);
                    continue;
                }

                // Check for vanilla and modded weapons using IMyGunObject<MyGunBase>
                var gunObject = block as IMyGunObject<MyGunBase>;
                if (gunObject != null && block is IMyCubeBlock) // Ensure it's a block, not a hand weapon
                {
                    AddWeaponCount(weaponDisplayName);
                }
            }
        }

        private string GetWeaponDisplayName(IMyCubeBlock block, IMyGunObject<MyGunBase> gunObject)
        {
            // You can customize this method to categorize weapons as needed
            if (block is IMyLargeTurretBase)
                return $"{block.DefinitionDisplayNameText} Turret";
            else
                return block.DefinitionDisplayNameText;
        }

        private void AddWeaponCount(string weaponDisplayName)
        {
            if (!WeaponCounts.ContainsKey(weaponDisplayName))
                WeaponCounts.Add(weaponDisplayName, 0);
            WeaponCounts[weaponDisplayName]++;
        }

        private void CalculateCost(IMyCubeBlock block)
        {
            double blockPoints = 0;
            foreach (var kvp in AllGridsList.PointValues)
            {
                if (block.BlockDefinition.SubtypeName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    blockPoints = kvp.Value;
                    break;
                }
            }
            if (blockPoints == 0) return;

            var blockDisplayName = block.DefinitionDisplayNameText;
            float thisClimbingCostMult = 0;
            AllGridsList.ClimbingCostRename(ref blockDisplayName, ref thisClimbingCostMult);

            if (!BlockCounts.ContainsKey(blockDisplayName))
                BlockCounts.Add(blockDisplayName, 0);

            var thisSpecialBlocksCount = BlockCounts[blockDisplayName]++;

            if (thisClimbingCostMult > 0)
                blockPoints += (blockPoints * thisSpecialBlocksCount * thisClimbingCostMult);

            if (block is IMyThrust || block is IMyGyro)
                MovementPoints += blockPoints;
            if (block is IMyPowerProducer)
                PowerPoints += blockPoints;

            if (WcApi.HasCoreWeapon((MyEntity)block))
            {
                var validTargetTypes = new List<string>();
                WcApi.GetTurretTargetTypes((MyEntity)block, validTargetTypes);
                if (validTargetTypes.Contains("Projectiles"))
                    PointDefensePoints += blockPoints;
                else
                    OffensivePoints += blockPoints;
            }

            BattlePoints += blockPoints;

            //MyAPIGateway.Utilities.ShowNotification($"EEEEEEEEE{blockPoints}", 3000);
        }

        #endregion
    }
}