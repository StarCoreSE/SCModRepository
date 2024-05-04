using System.Collections.Generic;
using System.Linq;
using CoreSystems.Api;
using DefenseShields;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace ShipPoints.ShipTracking
{
    internal class GridStats // TODO convert this to be event-driven. OnBlockPlace, etc. Keep a queue.
    {
        private readonly HashSet<IMyCubeBlock> _fatBlocks = new HashSet<IMyCubeBlock>();

        private readonly HashSet<IMySlimBlock> _slimBlocks;
        private ShieldApi ShieldApi => PointCheck.I.ShieldApi;
        private WcApi WcApi => PointCheck.I.WcApi;

        public bool NeedsUpdate { get; private set; } = true;

        #region Public Methods

        public GridStats(IMyCubeGrid grid)
        {
            Grid = grid;

            var allSlimBlocks = new List<IMySlimBlock>();
            Grid.GetBlocks(allSlimBlocks);
            _slimBlocks = allSlimBlocks.ToHashSet();

            foreach (var block in _slimBlocks)
            {
                if (block.FatBlock != null)
                    _fatBlocks.Add(block.FatBlock);
                GridIntegrity += block.Integrity;
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
        public float TotalPower { get; private set; }
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

            GridIntegrity += block.Integrity;

            NeedsUpdate = true;
        }

        private void OnBlockRemove(IMySlimBlock block)
        {
            if (block == null)
                return;

            _slimBlocks.Remove(block);
            if (block.FatBlock != null)
                _fatBlocks.Remove(block.FatBlock);

            GridIntegrity -= block.Integrity;

            NeedsUpdate = true;
        }

        #endregion

        #region Private Fields

        // TODO

        #endregion

        #region Private Methods

        private void UpdateGlobalStats()
        {
            BlockCounts.Clear();

            TotalThrust = 0;
            TotalTorque = 0;
            TotalPower = 0;

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

                else if (block is IMyPowerProducer && block.IsFunctional)
                {
                    TotalPower += ((IMyPowerProducer)block).MaxOutput;
                }

                else if (!WcApi.HasCoreWeapon((MyEntity)block))
                {
                    var blockDisplayName = block.DefinitionDisplayNameText;
                    if (blockDisplayName
                        .Contains("Armor")) // This is a bit stupid. TODO find a better way to sort out armor blocks.
                        continue;

                    float ignored = 0;
                    PointCheck.ClimbingCostRename(ref blockDisplayName, ref ignored);
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
            foreach (var weaponBlock in _fatBlocks)
            {
                // Check that the block has points and is a weapon
                int weaponPoints;
                var weaponDisplayName = weaponBlock.DefinitionDisplayNameText;
                if (!PointCheck.PointValues.TryGetValue(weaponBlock.BlockDefinition.SubtypeName, out weaponPoints) ||
                    !WcApi.HasCoreWeapon((MyEntity)weaponBlock))
                    continue;

                float thisClimbingCostMult = 0;

                PointCheck.ClimbingCostRename(ref weaponDisplayName, ref thisClimbingCostMult);

                if (!WeaponCounts.ContainsKey(weaponDisplayName))
                    WeaponCounts.Add(weaponDisplayName, 0);

                WeaponCounts[weaponDisplayName]++;
            }
        }

        private void CalculateCost(IMyCubeBlock block)
        {
            int blockPoints;
            var blockDisplayName = GetDDT(block);
            if (!PointCheck.PointValues.TryGetValue(block.BlockDefinition.SubtypeName, out blockPoints))
                return;

            float thisClimbingCostMult = 0;
            PointCheck.ClimbingCostRename(ref blockDisplayName, ref thisClimbingCostMult);

            if (!BlockCounts.ContainsKey(blockDisplayName))
                BlockCounts.Add(blockDisplayName, 0);

            var thisSpecialBlocksCount = BlockCounts[blockDisplayName]++;

            if (thisClimbingCostMult > 0 && thisSpecialBlocksCount > 1)
                blockPoints += (int)(blockPoints * thisSpecialBlocksCount * thisClimbingCostMult);

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
        }

        private string GetDDT(IMyCubeBlock block)
        {
            return block.DefinitionDisplayNameText;
        }

        #endregion
    }
}