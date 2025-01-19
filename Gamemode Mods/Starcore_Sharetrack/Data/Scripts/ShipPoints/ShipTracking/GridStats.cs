using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
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
        private WcApi WcApi => AllGridsList.I.WcApi;

        public bool NeedsUpdate { get; private set; } = true;
        public bool IsPrimaryGrid = true;

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
        public int TotalPowerBlocks { get; private set; }

        public float GridIntegrity { get; private set; }
        public float OriginalGridIntegrity { get; private set; }

        // BattlePoint Stats
        public int BattlePoints { get; private set; }
        public int OffensivePoints { get; private set; }
        public int PowerPoints { get; private set; }
        public int MovementPoints { get; private set; }
        public int PointDefensePoints { get; private set; }

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
            TotalPowerBlocks = 0;

            foreach (var block in _fatBlocks)
            {
                // Does not check for functionality of blocks because they can be welded.
                if (block is IMyCockpit)
                    CockpitCount++;
                else if (block is IMyThrust)
                {
                    TotalThrust += ((IMyThrust)block).MaxEffectiveThrust;
                }
                else if (block is IMyGyro)
                {
                    TotalTorque +=
                        ((MyGyroDefinition)MyDefinitionManager.Static.GetDefinition((block as IMyGyro).BlockDefinition))
                        .ForceMagnitude * (block as IMyGyro).GyroStrengthMultiplier;
                }
                else if (block is IMyPowerProducer)
                {
                    TotalPowerBlocks++;
                }

                if (!AllGridsList.I.WeaponSubtytes.Contains(block.BlockDefinition.SubtypeId))
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
            // Check that the block has points and is a weapon
            foreach (var weaponBlock in _fatBlocks.Where(b => AllGridsList.I.WeaponSubtytes.Contains(b.BlockDefinition.SubtypeId)))
            {
                var weaponDisplayName = weaponBlock.DefinitionDisplayNameText;

                float thisClimbingCostMult = 0;

                AllGridsList.ClimbingCostRename(ref weaponDisplayName, ref thisClimbingCostMult);

                if (!WeaponCounts.ContainsKey(weaponDisplayName))
                    WeaponCounts.Add(weaponDisplayName, 0);

                WeaponCounts[weaponDisplayName]++;
            }
        }

        private void CalculateCost(IMyCubeBlock block)
        {
            int blockPoints;
            var blockDisplayName = block.DefinitionDisplayNameText;
            if (!AllGridsList.PointValues.TryGetValue(block.BlockDefinition.SubtypeName, out blockPoints))
                return;

            float thisClimbingCostMult = 0;
            AllGridsList.ClimbingCostRename(ref blockDisplayName, ref thisClimbingCostMult);

            if (!BlockCounts.ContainsKey(blockDisplayName))
                BlockCounts.Add(blockDisplayName, 0);

            int thisSpecialBlocksCount = BlockCounts[blockDisplayName]++;

            string[] crossedGroup = AllGridsList.CrossedClimbingCostGroups.FirstOrDefault(l => l.Contains(blockDisplayName));
            if (crossedGroup != null)
            {
                thisSpecialBlocksCount = crossedGroup.Where(g => g != blockDisplayName).Sum(groupName => BlockCounts.GetValueOrDefault(groupName, 0));
            }

            if (thisClimbingCostMult > 0)
                blockPoints += (int)(blockPoints * thisSpecialBlocksCount * thisClimbingCostMult);

            if (block is IMyThrust || block is IMyGyro)
                MovementPoints += blockPoints;
            if (block is IMyPowerProducer)
                PowerPoints += blockPoints;
            if (AllGridsList.I.WeaponSubtytes.Contains(block.BlockDefinition.SubtypeId))
            {
                // Weapons on subgrids have an extra 20% cost applied
                //if (!IsPrimaryGrid)
                //    blockPoints = (int)(blockPoints * 1.2f);

                var validTargetTypes = new List<string>();
                WcApi.GetTurretTargetTypes((MyEntity)block, validTargetTypes);
                if (validTargetTypes.Contains("Projectiles"))
                    PointDefensePoints += blockPoints;
                else
                    OffensivePoints += blockPoints;
            }

            BattlePoints += blockPoints;
        }

        #endregion
    }
}
