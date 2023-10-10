using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SharedLib;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageRender;

namespace MIG.SpecCores
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class OriginalSpecCoreSession : SessionComponentExternalSettings<SpecCoreSettings>
    {
        public const float MaxValue = 99999999;

        public SpecCoreSettings GetDefaultSettings()
        {
            FrameExecutor.addDelayedLogic(100, (x) => Log.ChatError("Loading default settings"));
            return new SpecCoreSettings();
        }
        protected override SpecCoreSettings GetDefault() { return GetDefaultSettings(); }
        protected override string GetFileName() { return "SpecCoreSettings"; }

        private readonly List<Ship> addlist = new List<Ship>();
        private readonly List<long> removelist = new List<long>();
        public readonly Dictionary<long, Ship> gridToShip = new Dictionary<long, Ship>();
        
        public readonly Dictionary<int, LimitPoint> Points = new Dictionary<int, LimitPoint>();
        public readonly Dictionary<string, LimitPoint> PointsByName = new Dictionary<string, LimitPoint>();
        
        public readonly Dictionary<int, Upgrade> Upgrades = new Dictionary<int, Upgrade>();
        public readonly Dictionary<string, Upgrade> UpgradesByName = new Dictionary<string, Upgrade>();
        
        public readonly List<string> LoadingErrors = new List<string>();
        

       
        
        
        public static OriginalSpecCoreSession Instance = null;
        public static Dictionary<MyDefinitionId, Pair<BlockId, object>> IdToBlock = new Dictionary<MyDefinitionId, Pair<BlockId, object>>();
        public static bool IsDebug => Instance.Settings.DebugMode;
        public static bool RandomPunishment => Instance.Settings.RandomPunishment;
        
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            Instance = this;
            SpecBlock.Init();
            LimitedBlockNetworking.Init();

            int step = 0;
            try
            {
                OnSettingsChanged(ref step);
            }
            catch (Exception e)
            {
                AddLoadingError($"Loading settings error at: {step} "+e);   
            }
            
            GUI.InitLimitedBlockGui(Settings.LimitedBlocksCanBe);
            GUI.InitSpecBlockGui(Settings.SpecBlocksCanBe);
            
            if (LoadingErrors.Count > 0)
            {
                FrameExecutor.addDelayedLogic(10, (x) =>
                {
                    var sb = new StringBuilder();
                    foreach (var error in LoadingErrors)
                    {
                        sb.AppendLine(error);
                    }
                    LoadingErrors.Clear();
                    Log.ChatError("Loading XML errors found: \r\n"+sb.ToString());
                });
            }
        }


        private Dictionary<string, List<string>> MatcherCache;
        private Stopwatch idMatchingStopwatch = new Stopwatch();
        private void GetAllDefinitionIdsMatching(string matcher, List<string> outList)
        {
            idMatchingStopwatch.Start();
            
            if (MatcherCache == null)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                MatcherCache = new Dictionary<string, List<string>>();
                
                var allDefinitions = MyDefinitionManager.Static.GetAllDefinitions();
                foreach (var definition in allDefinitions)
                {
                    if (definition is MyCubeBlockDefinition)
                    {
                        MatcherCache.GetOrNew(definition.Id.TypeId.ToString().Replace("MyObjectBuilder_", "")).Add(definition.Id.SubtypeName);
                    }
                }

                //AddLoadingError($"Total Defs: {MyDefinitionManager.Static.Loading} {allDefinitions.Count()}");

                var total = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Stop();
            }
            
            
            var idMatcher = new BlockIdMatcher(matcher);
            foreach (var submatcher in idMatcher.checks)
            {
                if (submatcher.modeType == BlockIdMatcher.TypeSubtypeMatcher.MODE_EXACT)
                {
                    var list = MatcherCache.GetOr(submatcher.typeString, null);
                    if (list != null)
                    {
                        foreach (var subId in list)
                        {
                            if (submatcher.MatchesSubtype(subId))
                            {
                                outList.Add(submatcher.typeString+"/"+subId);
                            }
                        }
                    }
                    else
                    {
                        AddLoadingError($"Not Found blocks for:[{submatcher.typeString}] / [{matcher}]");
                    }
                }
                else
                {
                    foreach (var blockTypeAndSubIds in MatcherCache)
                    {
                        if (submatcher.MatchesType(blockTypeAndSubIds.Key))
                        {
                            foreach (var subId in blockTypeAndSubIds.Value)
                            {
                                if (submatcher.MatchesSubtype(subId))
                                {
                                    outList.Add(blockTypeAndSubIds.Key+"/"+subId);
                                }
                            }
                        }
                    }
                }
            }
            idMatchingStopwatch.Stop();
        }

        private void OnSettingsChanged(ref int step)
        {
            step = 1;
            InitPoints();
            step = 2;
            InitUpgrades();
            step = 3;
            InitLimitedBlocks();
            step = 4;
            //AddLoadingError("Total elapsed:" + idMatchingStopwatch.Elapsed.TotalMilliseconds);
            step = 5;
            if (Settings.NoSpecCoreSettings != null)
            {
                Settings.NoSpecCoreSettings.AfterDeserialize();
            }
            step = 6;
            InitSpecBlocks();
            step = 7;

            Log.CanWriteToChat = Settings.EnableLogs;
            step = 8;
            
        }

        private void InitSpecBlocks()
        {
            if (Settings.SpecBlocks != null)
            {
                foreach (var u in Settings.SpecBlocks)
                {
                    u.AfterDeserialize();
                    try
                    {
                        var ids = u.BlockIds.ToStrings();
                        foreach (var id in ids)
                        {
                            var defId = MyDefinitionId.Parse("MyObjectBuilder_" + id);
                            IdToBlock[defId] = new Pair<BlockId, object>(new BlockId() { Value = u.BlockIds }, u);
                        }
                    }
                    catch (Exception e)
                    {
                        AddLoadingError($"SpecBlock {u.BlockIds} loading error: {e}");
                    }
                }
            }
        }
        private void InitLimitedBlocks()
        {
            if (Settings.LimitedBlocks != null)
            {
                var cacheList = new List<string>();
                foreach (var u in Settings.LimitedBlocks)
                {
                    u.AfterDeserialize();
                    foreach (var uId in u.BlockIds)
                    {
                        try
                        {
                            if (uId.Matcher != null)
                            {
                                cacheList.Clear();
                                GetAllDefinitionIdsMatching(uId.Matcher, cacheList);
                                
                                foreach (var subId in cacheList)
                                {
                                    var id = MyDefinitionId.Parse("MyObjectBuilder_" + subId);
                                    IdToBlock[id] = new Pair<BlockId, object>(new BlockId(uId, subId), u);
                                }
                                
                                //AddLoadingError($"Found: {cacheList.Count} matching {uId.Matcher}");
                            }

                            if (uId.Value != null)
                            {
                                var strings = uId.Value.Split(new string[] {",", "\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var subId in strings)
                                {
                                    var id = MyDefinitionId.Parse("MyObjectBuilder_" + subId);
                                    IdToBlock[id] = new Pair<BlockId, object>(new BlockId(uId, subId), u);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            AddLoadingError($"Was unable to load [{uId.Value}/{uId.Matcher}] LimitedBlock {e}");
                        }
                    }
                }
            }
        }
        
        private void InitUpgrades()
        {
            if (Settings.Upgrades != null)
            {
                foreach (var u in Settings.Upgrades)
                {
                    u.AfterDeserialize();
                    
                    if (u.Name == null)
                    {
                        AddLoadingError($"Upgrade with NullName is not supported! [{u}]");
                        continue;
                    }
                    
                    if (Upgrades.ContainsKey(u.NId))
                    {
                        AddLoadingError($"Upgrade with NId {u.NId} already exists! [{u}]");
                        continue;
                    }
                    
                    if (UpgradesByName.ContainsKey(u.Name))
                    {
                        AddLoadingError($"Upgrade with name {u.Name} already exists! [{u}]");
                        continue;
                    }
                    Upgrades[u.NId] = u;
                    UpgradesByName[u.Name] = u;
                }
            }
        }

        /// <summary>
        /// Must be first as everyone depend on it
        /// </summary>
        private void InitPoints()
        {
            var pointFormats = new Dictionary<string, LimitPointFormat>();
            if (Settings.PointsFormats != null)
            {
                foreach (var format in Settings.PointsFormats)
                {
                    pointFormats[format.Id] = format;
                    //AddLoadingError($"Loading format: {format.Id} {format.Format}");
                }
            }

            LimitPointFormat.Init(pointFormats);
            
            if (Settings.Points != null)
            {
                foreach (var u in Settings.Points)
                {
                    if (u.FormatId != null)
                    {
                        u.Format = pointFormats.GetOr(u.FormatId, null);
                        //AddLoadingError($"Loading for Limited: {u.FormatId} {u.Format != null}");
                    } 
                    
                    if (u.Format == null)
                    {
                        u.Format = LimitPointFormat.GetDefault(u);
                        //AddLoadingError($"Loading default format for : {u.Name}");
                    }

                    if (Points.ContainsKey(u.Id))
                    {
                        AddLoadingError($"Point with NID {u.Id} already exists!");
                    }
                    else
                    {
                        Points[u.Id] = u;
                    }
                    
                    if (PointsByName.ContainsKey(u.IDD))
                    {
                        AddLoadingError($"Point with ID {u.IDD} already exists!");
                    }
                    else
                    {
                        PointsByName[u.IDD] = u;
                    }
                    
                    LimitsChecker.RegisterKey(u);
                }
            }
            
            
        }
        

        public static bool GetLimitedBlock(IMyCubeBlock block, out ILimitedBlock limitedBlock, out ISpecBlock specBlock)
        {
            limitedBlock = null;
            specBlock = null;
            var tBlock = block as IMyTerminalBlock;
            if (tBlock == null) return false;
            
            object blockInfo;
            Pair<BlockId, object> blockIdToInfo;
            if (!IdToBlock.TryGetValue(block.SlimBlock.BlockDefinition.Id, out blockIdToInfo))
            {
                return false;
            }

            blockInfo = blockIdToInfo.v;
            var consumer = blockInfo as LimitedBlockInfo;
            if (consumer != null)
            {
                limitedBlock = Instance.GetLimitedBlock(tBlock, consumer, blockIdToInfo.k);
                if (limitedBlock != null) Hooks.TriggerOnLimitedBlockCreated(limitedBlock);
                return limitedBlock != null;
            }
            
            var specBlockInfo = blockInfo as SpecBlockInfo;
            if (specBlockInfo != null)
            {
                specBlock = Instance.GetSpecBlock(tBlock, specBlockInfo);
                if (specBlock != null) Hooks.TriggerOnSpecBlockCreated(specBlock);
                return specBlock != null;
            }

            return false;
        }

        public ILimitedBlock GetLimitedBlock(IMyTerminalBlock block, LimitedBlockInfo info, BlockId blockId)
        {
            if (!string.IsNullOrEmpty(info.CustomLogicId))
            {
                HookedLimiterInfo info2;
                if (!Hooks.HookedConsumerInfos.TryGetValue(info.CustomLogicId, out info2))
                {
                    Log.ChatError($"Was unable to get hooked limited block [{info.CustomLogicId}]");
                    return null;
                }
                return new HookedLimitedBlock(block, info, info2, blockId);
            }
            return new LimitedBlock(block, info, blockId);
        }
        
        public ISpecBlock GetSpecBlock(IMyTerminalBlock block, SpecBlockInfo info)
        {
            return new SpecBlock(block, info);
        }

        public override void LoadData()
        {
            Hooks.Init();
            Common.Init();
            FrameExecutor.addDelayedLogic(360, (frame) => { Common.SendChatMessage("[MIG] SpecCores v1.2.0 inited!"); });
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
        }

        protected override void UnloadData()
        {
            Hooks.Close();
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
        }

        private void OnEntityAdded(IMyEntity ent)
        {
            try
            {
                var grid = ent as IMyCubeGrid;
                if (grid != null)
                {
                    if (!gridToShip.ContainsKey(grid.EntityId))
                    {
                        var shipGrid = new Ship(grid);
                        addlist.Add(shipGrid);
                        grid.OnMarkForClose += OnMarkForClose;
                    }
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }
        
        private void OnMarkForClose(IMyEntity ent)
        {
            if (ent is IMyCubeGrid)
            {
                removelist.Add(ent.EntityId);
                ent.OnMarkForClose -= OnMarkForClose;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            
            foreach (var x in addlist)
            {
                if (x == null)
                {
                    Log.Error("GB: Add: NULL SHIP");
                    continue;
                }

                if (x.grid == null)
                {
                    Log.Error("GB: Add: NULL GRID SHIP");
                    continue;
                }

                if (!gridToShip.ContainsKey(x.grid.EntityId))
                    gridToShip.Add(x.grid.EntityId, x);
                else
                    gridToShip[x.grid.EntityId] = x;
            }

            addlist.Clear();
            
            foreach (var x in removelist) gridToShip.Remove(x);
            removelist.Clear();
            
            foreach (var x in gridToShip) x.Value.BeforeSimulation();
            
            FrameExecutor.Update();
        }

        public float GetTier(BlockId blockId)
        {
            if (Settings.Tiers != null)
            {
                foreach (var tier in Settings.Tiers)
                {
                    if (tier.EndsWith != null && blockId.Value.Contains(tier.EndsWith)) return tier.Value;
                    if (tier.Contains != null && blockId.Value.Contains(tier.Contains)) return tier.Value;
                }    
            }
            return Settings.DefaultTier;
        }

        public static int GetPointId(string pointName)
        {
            LimitPoint id;
            if (Instance.PointsByName.TryGetValue(pointName, out id))
            {
                return id.Id;
            }
            else
            {
                AddLoadingError($"Point {pointName} not found:");
                return -1;
            }
        }
        
        public static void AddLoadingError(string name)
        {
            Instance.LoadingErrors.Add(name);
        }
        
        //public override void SaveData()
        //{
        //    base.SaveData();
        //    var s = MyAPIGateway.Utilities.SerializeToXML(Settings);
        //    Log.ChatError(s);
        //}
        
        
        public static void RemoveNonUpgradePoints(Limits maxPoints)
        {
            foreach (var point in Instance.Points)
            {
                if (point.Value.Behavior != PointBehavior.UpgradePoint)
                {
                    maxPoints.Remove(point.Key);
                }
            }
        }
    }
}