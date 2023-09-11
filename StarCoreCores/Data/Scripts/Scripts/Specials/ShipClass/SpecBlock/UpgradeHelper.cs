using System;
using System.Collections.Generic;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;

namespace MIG.SpecCores
{
    public class UpgradeHelper
    {
        public Dictionary<string, List<Upgrade>> hardLocks = new Dictionary<string, List<Upgrade>>();
        public Dictionary<string, List<Upgrade>> softLocks = new Dictionary<string, List<Upgrade>>();
        public UpgradeLevel lastUpgradeLevel;
        public List<int> desiredUpgrades;
        public int index;
        
        public UpgradeHelper(SpecBlock specBlock, List<int> desiredUpgradesOriginal, int extraUpgrade)
        {
            var copy = new List<int>(desiredUpgradesOriginal);
            copy.Add(extraUpgrade);
            desiredUpgrades = copy;
            index = GetIndexOfLastUpgradeThatCanBeApplied(specBlock, desiredUpgrades);
        }
        
        public UpgradeHelper(SpecBlock specBlock, List<int> desiredUpgradesOriginal)
        {
            var copy = new List<int>(desiredUpgradesOriginal);
            desiredUpgrades = copy;
            index = GetIndexOfLastUpgradeThatCanBeApplied(specBlock, copy);
        }
        
        
        public bool CanBeApplied()
        {
            return index == desiredUpgrades.Count - 1;
        }
        
        public List<int> GetMaxPossibleUpgradeList()
        {
            var copy = new List<int>();
            for (int i = 0; i <= index; i++)
            {
                copy.Add(desiredUpgrades[i]);
            }
            return copy;
        }

        public Dictionary<string, bool> GetLocksAndUnlocks(UpgradeHelper helper)
        {
            var diff = new Dictionary<string, bool>();

            var wasHardLocks = new Dictionary<string, List<Upgrade>>(hardLocks);
            var wasSoftLocks = new Dictionary<string, List<Upgrade>>(softLocks);
            var wouldBeHardLocks = new Dictionary<string, List<Upgrade>>(helper.hardLocks);
            var wouldBeSoftLocks = new Dictionary<string, List<Upgrade>>(helper.hardLocks);
            
            //wasHardLocks.RemoveDuplicateKeys(wouldBeHardLocks);
            //wasSoftLocks.RemoveDuplicateKeys(wouldBeSoftLocks);
            //wouldBeHardLocks.RemoveDuplicateKeys(wasHardLocks);
            //wouldBeSoftLocks.RemoveDuplicateKeys(wasSoftLocks);

            foreach (var hardLock in wasHardLocks)
            {
                diff[hardLock.Key] = false;
            }
            foreach (var softLock in wasSoftLocks)
            {
                diff[softLock.Key] = false;
            }
            
            foreach (var hardLock in wouldBeHardLocks)
            {
                diff[hardLock.Key] = true;
            }
            foreach (var softLock in wouldBeSoftLocks)
            {
                diff[softLock.Key] = true;
            }
            
            return diff;
        }
        
        private int GetIndexOfLastUpgradeThatCanBeApplied(SpecBlock specBlock, List<int> desiredUpgrades)
        {
            var line = 0;
            
            try
            {
                var limits = specBlock.GetLimits(false);
                OriginalSpecCoreSession.RemoveNonUpgradePoints(limits);

                Dictionary<int, float> usedPoints = new Dictionary<int, float>();
                Dictionary<int, int> currentUpgrades = new Dictionary<int, int>();
                
                HashSet<string> currentLockGroups = new HashSet<string>();
                

                line = 1;
                
                int index = -1;
                foreach (var u in desiredUpgrades)
                {
                    currentUpgrades.Sum(u, 1);
                    var upgrades = OriginalSpecCoreSession.Instance.Upgrades;
                    if (!upgrades.ContainsKey(u))
                    {
                        return index;
                    }

                    line = 2;
                    var upgrade = upgrades[u];
                    var currentUpgradeLevel = currentUpgrades[u]-1;

                    if (currentUpgradeLevel >= upgrade.Levels.Length)
                    {
                        //Log.ChatError($"Going over max level {currentUpgradeLevel+1} / {upgrade.Levels.Length}");
                        return index;
                    }

                    line = 3;

                    var level = upgrade.Levels[currentUpgradeLevel];
                    foreach (var group in level.Locks.LockGroups)
                    {
                        if ((hardLocks.GetOr(group, null)?.Count ?? 0) > 0 || (softLocks.GetOr(group, null)?.Count ?? 0) > 0 )
                        {
                            //Log.ChatError($"Blocked upgrade {u}");
                            return index;
                        }
                    }
                    

                    line = 4;
                    
                    if (level.Locks.AddHardLocks.Length > 0)
                    {
                        foreach (var lockName in level.Locks.AddHardLocks)
                        {
                            if (currentLockGroups.Contains(lockName)) return index;
                        }
                    }

                    line = 5;

                    if (currentUpgradeLevel != 0)
                    {
                        var prev = upgrade.Levels[currentUpgradeLevel-1];
                        foreach (var cost in prev.Costs)
                        {
                            usedPoints.Sum(cost.PointId, -cost.Value);
                        }
                    }

                    line = 6;
                    
                    foreach (var cost in level.Costs)
                    {
                        usedPoints.Sum(cost.PointId, cost.Value);
                    }

                    line = 7;

                    if (usedPoints.IsOneKeyMoreThan(limits))
                    {
                        //Log.ChatError($"One key is over max upgrades {usedPoints.Print(" ", SharedLogic.NoZeroLimits)} / {limits.Print(" ", SharedLogic.NoZeroLimits)}");
                        return index;
                    }

                    line = 8;
                        
                    //Remove old and add new upgrades
                    if (currentUpgradeLevel != 0)
                    {
                        foreach (var blockedUpgrade in hardLocks) blockedUpgrade.Value.Remove(upgrade);
                        foreach (var blockedUpgrade in softLocks) blockedUpgrade.Value.Remove(upgrade);
                    }
                    
                    line = 9;
                    
                    foreach (var blockedId in level.Locks.AddHardLocks) hardLocks.GetOrNew(blockedId).Add(upgrade);
                    foreach (var blockedId in level.Locks.AddSoftLocks) softLocks.GetOrNew(blockedId).Add(upgrade);
                    
                    foreach (var blockedId in level.Locks.RemoveHardLocks) { hardLocks.Remove(blockedId); currentLockGroups.Remove(blockedId); }
                    foreach (var blockedId in level.Locks.RemoveHardLocks) { softLocks.Remove(blockedId); currentLockGroups.Remove(blockedId); }

                    foreach (var g in level.Locks.LockGroups) currentLockGroups.Add(g);

                    lastUpgradeLevel = level;

                    line = 10;

                    index++;
                }
                return index;
            }
            catch (Exception e)
            {
                Log.ChatError($"GetIndexOfLastUpgradeThatCanBeApplied line={line} {e}");
                return 0;
            }
        }
    }
}