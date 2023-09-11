using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public partial class SpecBlock : ISpecBlock
    {
        private static Guid Guid = new Guid("82f6e121-550f-4042-a2b8-185ed2a52abc");

        public static Sync<UpgradableSpecBlockSettings, SpecBlock> Sync;
        private const byte APPLY_RANDOM = 125;
        private const byte APPLY_SELECTED = 126;

        public UpgradableSpecBlockSettings Settings;
        public bool ArrivedDataFromServer = false;
        private SpecBlockInfo info;
        
        

        public static void Init()
        {
            Sync = new Sync<UpgradableSpecBlockSettings, SpecBlock>(17273, (x) => x.Settings, Handler, entityLogicGetter: (id) =>
            {
                try
                {
                    var block = id.As<IMyCubeBlock>();
                    if (block == null) return null;
                    var ship = block.CubeGrid.GetShip();
                    if (ship == null) return null;
                    ISpecBlock shipSpecBlock;
                    if (ship.SpecBlocks.TryGetValue(block, out shipSpecBlock))
                    {
                        return (SpecBlock)shipSpecBlock;
                    }
                    return null;
                }
                catch (Exception e)
                {
                    Log.ChatError("ELG:" + e);
                }
                return null;
            });
        }



        public SpecBlock(IMyTerminalBlock Entity, SpecBlockInfo info) : base(Entity)
        {
            this.info = info;
            Settings = new UpgradableSpecBlockSettings();

            if (MyAPIGateway.Session.IsServer)
            {
                if (!Entity.TryGetStorageData(Guid, out Settings, true))
                {
                    Settings = new UpgradableSpecBlockSettings();
                    Settings.Upgrades = new List<int>();
                }

                ApplyUpgrades();
            }
            else
            {
                ApplyUpgrades();
                Sync.RequestData(Entity.EntityId);
                FrameExecutor.addDelayedLogic(100, new Resyncher(this));
            }
            
            

            GUI.SpecBlockGui.CreateGui(Entity);
        }


        private class Resyncher : Action1<long>
        {
            public SpecBlock Block;

            public Resyncher(SpecBlock block)
            {
                Block = block;
            }

            public void run(long t)
            {
                if (Block == null)
                {
                    return;
                }

                if (Block.block == null || Block.block.Closed || Block.block.MarkedForClose)
                {
                    return;
                } 
                
                if (Block.ArrivedDataFromServer)
                {
                    return;
                }
            
                Sync.RequestData(Block.block.EntityId);
                FrameExecutor.addDelayedLogic(100, new Resyncher(Block));
            }
        }


        private void ApplyDataFromClient(UpgradableSpecBlockSettings blockSettings, ulong arg4, byte arg3)
        {
            //Log.ChatError($"[HandleMessage settings: {blockSettings}");
            if (!CanPickUpgrades())
            {
                Log.ChatError($"[{arg4.Identity()?.DisplayName}/{arg4.IdentityId()}] Core already has upgrades: {Settings}: {Settings.Upgrades.Count}");
                return;
            }

            if (!block.HasPlayerAccess(arg4.Identity().IdentityId))
            {
                Log.ChatError($"[{arg4.Identity()?.DisplayName}/{arg4.IdentityId()}] You dont have access");
                return;
            }

            //if (arg3 == APPLY_RANDOM)
            //{
            //    Settings.Upgrades = GenerateRandomUpgrades(new List<int>(info.PossibleUpgrades),
            //        info.MaxUpgrades + info.ExtraRandomUpgrades);
            //}
            //else 
            
            if (arg3 == APPLY_SELECTED)
            {
                //TODO
                //var upgradeCost = GetUpgradeCost(blockSettings.Upgrades);
                //if (upgradeCost > info.MaxUpgrades)
                //{
                //    Log.ChatError($"Exceeding limit {upgradeCost} / {info.MaxUpgrades}");
                //    return;
                //}
                
                Settings.Upgrades = blockSettings.Upgrades;
            }
        }

        public bool CanPickUpgrades()
        {
            return Settings.Upgrades.Count == 0 && info.PossibleUpgrades.Length > 0;
        }

        public void ApplyUpgrades()
        {
            Limits copyStatic, copyDynamic;
            GetUpgradedValues(Settings.Upgrades.SumDuplicates(), out copyStatic, out copyDynamic, true);
            SetOptions(copyStatic, copyDynamic);
        }

        // ================================================================================

        private static void Handler(SpecBlock block, UpgradableSpecBlockSettings settings, byte type, ulong userSteamId, bool isFromServer)
        {
            if (isFromServer && !MyAPIGateway.Session.IsServer)
            {
                block.Settings = settings;
                block.ArrivedDataFromServer = true;
                block.OnSettingsChanged();
            }
            else
            {
                block.ApplyDataFromClient(settings, userSteamId, type);
                block.NotifyAndSave();
                block.OnSettingsChanged();
            }
        }

        public void OnSettingsChanged()
        {
            ApplyUpgrades();
        }

        protected void NotifyAndSave(byte type = 255, bool forceSave = false)
        {
            try
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    Sync.SendMessageToOthers(block.EntityId, Settings, type: type);
                    SaveSettings(forceSave);
                }
                else
                {
                    if (Sync != null)
                    {
                        Sync.SendMessageToServer(block.EntityId, Settings, type: type);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ChatError($"NotifyAndSave {type} Exception {ex} {ex.StackTrace}");
            }
        }

        public void SaveSettings(bool forceSave = false)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                block.SetStorageData(Guid, Settings, true);
            }
        }


        private void GetUpgradedValues(Dictionary<int, int> dict, out Limits copyStatic, out Limits copyDynamic, bool hooks)
        {
            int upgradeId = 0;
            int levelNum = 0;
            
            copyStatic = info.DefaultStaticValues.Copy();
            copyDynamic = info.DefaultDynamicValues.Copy();
            var upgrades = OriginalSpecCoreSession.Instance.Upgrades;
            try
            {
                Dictionary<int, float> sumStaticBefore = new Dictionary<int, float>();
                Dictionary<int, float> sumDynamicBefore = new Dictionary<int, float>();
                Dictionary<int, float> sumStaticAfter = new Dictionary<int, float>();
                Dictionary<int, float> sumDynamicAfter = new Dictionary<int, float>();
                Dictionary<int, float> mltStatic = new Dictionary<int, float>();
                Dictionary<int, float> mltDynamic = new Dictionary<int, float>();

                foreach (var u in dict)
                {
                    if (u.Value == 0)
                    {
                        continue;
                    }
                    
                    if (!upgrades.ContainsKey(u.Key))
                    {
                        Log.ChatError($"Can't find upgrade with id {u.Key}");
                        continue;
                    }

                    upgradeId = u.Key;
                    levelNum = u.Value-1;
                

                    var upgrade = upgrades[u.Key];
                    if (upgrade.Levels.Length < levelNum)
                    {
                        continue;
                    }
                    
                    var level = upgrade.Levels[levelNum];
                    if (level.Modificators != null)
                    {
                        foreach (var am in level.Modificators)
                        {
                            sumStaticBefore.Sum(am.PointId, am.SumBefore);
                            sumStaticBefore.Sum(am.PointId, am.SumStaticBefore);
                            sumDynamicBefore.Sum(am.PointId, am.SumBefore);
                            sumDynamicBefore.Sum(am.PointId, am.SumDynamicBefore);

                            mltStatic.Mlt(am.PointId, am.Mlt);
                            mltStatic.Mlt(am.PointId, am.MltStatic);
                            mltDynamic.Mlt(am.PointId, am.Mlt);
                            mltDynamic.Mlt(am.PointId, am.MltDynamic);
                            
                            sumStaticAfter.Sum(am.PointId, am.SumAfter);
                            sumStaticAfter.Sum(am.PointId, am.SumStaticAfter);
                            sumDynamicAfter.Sum(am.PointId, am.SumAfter);
                            sumDynamicAfter.Sum(am.PointId, am.SumDynamicAfter);
                        }
                    }
                }
                
                copyStatic.Sum(sumStaticBefore);
                copyDynamic.Sum(sumDynamicBefore);
                
                copyStatic.Mlt(mltStatic, true);
                copyStatic.Mlt(mltDynamic, true);
                
                copyStatic.Sum(sumStaticAfter);
                copyDynamic.Sum(sumDynamicAfter);
                
                copyDynamic.Sum(Settings.CustomDynamic);
                copyStatic.Sum(Settings.CustomStatic);

                if (hooks)
                {
                    Hooks.InvokeLimitsInterceptor(this, copyStatic, copyDynamic);
                }
            }
            catch (Exception e)
            {
                Log.ChatError($"Not found: {upgradeId} {levelNum} [{upgrades.Print((x,y)=> $"{y.NId} {y.Levels.Length} {y.DisplayName}")}]", e);
            }
        }
        
        public string ProvideLimitsError()
        {
            if (info.Behaviors != null)
            {
                foreach (var behavior in info.Behaviors)
                {
                    if (behavior.ProvideBehavior != ConsumeBehaviorType.None)
                    {
                        if (!SharedLogic.IsDrainingPoints(block, fblock, behavior, behavior.ProvideBehavior))
                        {
                            return behavior.ProvideLimitsError;
                        }
                    }
                }
            }
            else
            {
                if (OriginalSpecCoreSession.IsDebug)
                {
                    Log.ChatError($"{info.BlockIds} doesn't have Behaviors : Possible error");
                }
            }
            

            return null;
        }

        public override bool CanBeApplied(List<IMyCubeGrid> grids, GridGroupInfo info)
        {
            var limits = GetLimits();
            string str;
            if (!info.CanBeApplied(limits, out str))
            {
                status = str;
                return false;
            }

            
            var provideError = ProvideLimitsError();
            if (provideError != null)
            {
                //if (OriginalSpecCoreSession.IsDebug)
                //{
                //    Log.ChatError("ProvideLimitsError: " + T.Translation(provideError));
                //}
                status = provideError;
                return false;
            }
            
            //if (OriginalSpecCoreSession.IsDebug)
            //{
            //    Log.ChatError($"Grid core: {block.DisplayNameText ?? "Not a terminal block"} {this.info.Id} Provide limits");
            //}

            foreach (var kv in limits)
            {
                if (kv.Value < LimitsChecker.MaxValue)
                {
                    var lp = OriginalSpecCoreSession.Instance.Points.GetOr(kv.Key, null);
                    if (lp == null) continue;
                    if (!lp.IsCustom) continue;
                    var result = Hooks.GetCurrentPointValueForSpecCore(this, grids, lp);
                    
                    if (result > limits[kv.Key] && lp.Behavior == PointBehavior.LessOrEqual)
                    {
                        status = lp.ActivationError;
                        return false;
                    }
                    else if (result < limits[kv.Key] && lp.Behavior == PointBehavior.MoreOrEqual)
                    {
                        status = lp.ActivationError;
                        return false;
                    }
                }
            }

            var hookError = Hooks.CanBeApplied(this, grids);
            if (hookError != null)
            {
                status = hookError;
                return false;
            }

            status = T.Infinity;//T.ActivationStatus_CurrentCore;
            return true;
        }

        private static List<IMyCubeGrid> m_appendingCustomInfoBuffer = new List<IMyCubeGrid>();

        

        #region Other

        
        
        private static string GetTooltip(SpecBlock specBlock, int u, Dictionary<int, int> upgrades,Limits originalStatic, Limits originalDynamic, StringBuilder sb)
        {
            sb.Clear();
            Limits valuesStaticCopy, valuesDynamicCopy;

            upgrades = new Dictionary<int, int>(upgrades);
            upgrades.Sum(u, 1);
            specBlock.GetUpgradedValues(upgrades, out valuesStaticCopy, out valuesDynamicCopy, false);
            upgrades.Sum(u, -1);
            

            valuesStaticCopy.RemoveDuplicates(originalStatic);
            valuesDynamicCopy.RemoveDuplicates(originalDynamic);


            var duplicates = valuesDynamicCopy.GetDuplicates<int, float, Limits>(valuesStaticCopy);
            if (duplicates.Count > 0)
            {
                sb.AppendLine(T.Translation(T.BlockInfo_StaticOrDynamic));
                T.GetLimitsInfo(null, sb, duplicates, null);
                sb.AppendLine();
            }

            valuesStaticCopy.RemoveDuplicates(duplicates);
            valuesDynamicCopy.RemoveDuplicates(duplicates);

            if (valuesStaticCopy.Count > 0)
            {
                sb.AppendLine(T.Translation(T.BlockInfo_StaticOnly));
                T.GetLimitsInfo(null, sb, valuesStaticCopy, null);
                sb.AppendLine();
            }
            if (valuesDynamicCopy.Count > 0)
            {
                sb.AppendLine(T.Translation(T.BlockInfo_DynamicOnly));
                T.GetLimitsInfo(null, sb, valuesDynamicCopy, null);
                sb.AppendLine();
            }

            return sb.ToString();
        }
        
        private static void UpdateControls(SpecBlock x)
        {
            try
            {
                bool orig = x.block.ShowInToolbarConfig;
                x.block.ShowInToolbarConfig = !orig;
                x.block.ShowInToolbarConfig = orig;
            }
            catch (Exception e)
            {
                Log.ChatError("UpdateControls", e);
            }
        }

        #endregion
        
        #region GUI

        protected override void BlockOnAppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2)
        {
            try
            {
                if (arg1 == null || arg1.CubeGrid == null || arg1.MarkedForClose || arg1.CubeGrid.MarkedForClose)
                    return;

                if (arg1.CubeGrid.GetShip() == null)
                {
                    //Log.ChatError($"BlockOnAppendingCustomInfo: Ship is null WTF? {arg1.CubeGrid.WorldMatrix.Translation} / {arg1.CubeGrid.Closed} {arg1.DisplayName}/{arg1.CustomName} {arg1.Closed}");
                    return;
                }

                var upgrades = (CanPickUpgrades() ? AddedUpgrades : Settings.Upgrades).SumDuplicates();

                Limits copyStatic, copyDynamic;
                GetUpgradedValues(upgrades, out copyStatic, out copyDynamic, true);


                arg2.AppendLine().AppendT(T.BlockInfo_StatusRow, T.Translation(status));

                if (CanPickUpgrades())
                {
                    arg2.AppendLine(T.Translation(T.BlockInfo_NoSpecializations));
                }
                
                var grids = block.CubeGrid.GetConnectedGrids(OriginalSpecCoreSession.Instance.Settings.ConnectionType, m_appendingCustomInfoBuffer, true);

                var limits = IsStatic.Value ? copyStatic : copyDynamic;
                foreach (var kv in limits)
                {
                    var lp = OriginalSpecCoreSession.Instance.Points.GetOr(kv.Key, null);
                    if (lp == null) continue;
                    if (lp.IsCustom)
                    {
                        var result = Hooks.GetCurrentPointValueForSpecCore(this, grids, lp);
                        FoundLimits[kv.Key] = result;
                    }
                }
                
                var gridInfo = LimitsChecker.GetGridInfo(arg1);
                var lps = OriginalSpecCoreSession.Instance.Points;
                if (lps.ContainsKey(LimitsChecker.TYPE_MAX_SMALLGRIDS)) FoundLimits[LimitsChecker.TYPE_MAX_SMALLGRIDS] = gridInfo.SmallGrids;
                if (lps.ContainsKey(LimitsChecker.TYPE_MAX_LARGEGRIDS)) FoundLimits[LimitsChecker.TYPE_MAX_LARGEGRIDS] = gridInfo.LargeGrids;
                if (lps.ContainsKey(LimitsChecker.TYPE_MAX_GRIDS)) FoundLimits[LimitsChecker.TYPE_MAX_GRIDS] = gridInfo.LargeGrids + gridInfo.SmallGrids;
                if (lps.ContainsKey(LimitsChecker.TYPE_MAX_PCU)) FoundLimits[LimitsChecker.TYPE_MAX_PCU] = gridInfo.TotalPCU;
                if (lps.ContainsKey(LimitsChecker.TYPE_MAX_BLOCKS)) FoundLimits[LimitsChecker.TYPE_MAX_BLOCKS] = gridInfo.TotalBlocks;
                
                if (lps.ContainsKey(LimitsChecker.TYPE_MIN_SMALLGRIDS)) FoundLimits[LimitsChecker.TYPE_MIN_SMALLGRIDS] = gridInfo.SmallGrids;
                if (lps.ContainsKey(LimitsChecker.TYPE_MIN_LARGEGRIDS)) FoundLimits[LimitsChecker.TYPE_MIN_LARGEGRIDS] = gridInfo.LargeGrids;
                if (lps.ContainsKey(LimitsChecker.TYPE_MIN_GRIDS)) FoundLimits[LimitsChecker.TYPE_MIN_GRIDS] = gridInfo.LargeGrids + gridInfo.SmallGrids;
                if (lps.ContainsKey(LimitsChecker.TYPE_MIN_PCU)) FoundLimits[LimitsChecker.TYPE_MIN_PCU] = gridInfo.TotalPCU;
                if (lps.ContainsKey(LimitsChecker.TYPE_MIN_BLOCKS)) FoundLimits[LimitsChecker.TYPE_MIN_BLOCKS] = gridInfo.TotalBlocks;

                T.GetLimitsInfo(FoundLimits, arg2, limits, TotalLimits);


                if (info.PossibleUpgrades.Length > 0)
                {
                    var ucount = 0;
                    foreach (var u in upgrades)
                    {
                        ucount += u.Value;
                    }
                    arg2.AppendT(T.BlockInfo_UpgradesHeader, ucount);

                    foreach (var u in upgrades)
                    {
                        Upgrade up;
                        if (OriginalSpecCoreSession.Instance.Upgrades.TryGetValue(u.Key, out up))
                        {
                            var txt = T.Translation(T.BlockInfo_UpgradesRow, T.Translation(up.DisplayName), u.Value); 
                            arg2.Append(txt).AppendLine();
                        }
                        else
                        {
                            arg2.Append($"Unknown upgrade {u.Key}").Append(": ").Append(u.Value+1).AppendLine();
                        }
                    }
                }
            }
            catch (Exception e)
            {
              arg2.Append(e.ToString());
            }

        }
        
        
        
        

        #endregion
    }
}
