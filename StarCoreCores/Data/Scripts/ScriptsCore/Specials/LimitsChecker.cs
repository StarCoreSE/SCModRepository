using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace MIG.SpecCores
{
    public class LimitsChecker {
        public const int TYPE_PRIORITY = -1;
        
        //TODO
        public const int TYPE_PRIORITY_MIN = -2;
        public const int TYPE_PRIORITY_MAX = -3;
        public const int TYPE_SKIP_CHECKS = -4;
        
        public const int TYPE_MAX_SMALLGRIDS = -5;
        public const int TYPE_MAX_LARGEGRIDS = -6;
        public const int TYPE_MAX_GRIDS = -7;
        public const int TYPE_MAX_PCU = -8;
        public const int TYPE_MAX_BLOCKS = -9;
        
        public const int TYPE_MIN_SMALLGRIDS = -15;
        public const int TYPE_MIN_LARGEGRIDS = -16;
        public const int TYPE_MIN_GRIDS = -17;
        public const int TYPE_MIN_PCU = -18;
        public const int TYPE_MIN_BLOCKS = -19;
        

        
        
        public const float MaxValue = 999999999;
        
        private static HashSet<int> allSumKeys = new HashSet<int>();
        private static HashSet<int> allLessKeys = new HashSet<int>();
        
        private static Limits bufferSum = new Limits();
        private static Limits zeroLimits = new Limits();
        private static Dictionary<int, bool> punishIds = new Dictionary<int, bool>();
        private static List<ISpecBlock> bufferProducers = new List<ISpecBlock>(10);
        private static List<ILimitedBlock> bufferConsumers = new List<ILimitedBlock>(100);
        private static List<ILimitedBlock> notDrainingConsumers = new List<ILimitedBlock>(100);

        private static List<IMyCubeGrid> gridBuffer1 = new List<IMyCubeGrid>();
        private static List<IMyCubeGrid> gridBuffer2 = new List<IMyCubeGrid>();

        private static GridGroupInfo GridGroupInfoInstance = new GridGroupInfo();



        private static Limits clientLessTotal = new Limits();
        private static Limits clientLessFound = new Limits();
        private static Limits clientFoundPoints = new Limits();
        private static Limits clientTotalPoints = new Limits();
        
        public static void RegisterKey(LimitPoint lp)
        {

            if (lp.Id == 0)
            {
                OriginalSpecCoreSession.AddLoadingError($"Can't register LimitedPoint with id = 0; Info: {lp.Name} {lp.IDD}");
                return;
            }
            
            var key = lp.Id;
            clientTotalPoints[key] = 0;
            clientFoundPoints[key] = 0;
            
            zeroLimits[key] = 0;
            switch (lp.Behavior)
            {
                case PointBehavior.SumLessOrEqual:
                    allSumKeys.Add(key);
                    punishIds.Add(key, false);
                    break;
                case PointBehavior.LessOrEqual:
                    allLessKeys.Add(key);
                    punishIds.Add(key, false);
                    break;
                case PointBehavior.MoreOrEqual:
                    punishIds.Add(key, false);
                    break;
                    
            }
        }

        public static GridGroupInfo GetGridInfo(IMyTerminalBlock block)
        {
            block.CubeGrid.GetConnectedGrids(OriginalSpecCoreSession.Instance.Settings.ConnectionType, gridBuffer2, true);
            GridGroupInfoInstance.Calculate(gridBuffer2);
            return GridGroupInfoInstance;
        }

        public static int Compare(float a, float b, bool aa, bool bb)
        {
            if (aa != bb)
            {
                if (aa)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            var d = a - b;
            return d > 0 ? 1 : d < 0 ? -1 : 0;
        }

        
        
        public static void CheckLimitsInGrid(IMyCubeGrid grid) {

            bool changed = false;
            bool changedCore = false;
            ISpecBlock selectedSpecBlock = null;
            if (grid.isFrozen()) return;
            var grids = grid.GetConnectedGrids(OriginalSpecCoreSession.Instance.Settings.ConnectionType, gridBuffer1, true);

            int step = 0;
            try
            {
                
                PrepareAndClear();
                step = 1;
                
                GridGroupInfoInstance.Calculate(grids);

                //var s = new StringBuilder();
                //foreach (var g in grids)
                //{
                //    s.Append(g.DisplayName + "   ");
                //}
                step = 3;

                Phase_1_CollectInfo(grids);

                step = 4;
                
                Limits maxLimits;
                ISpecBlock producer = null;
                bool evenDefaultFailed = false;
                step = -44;
                if (bufferProducers.Count > 0)
                {
                    step = -51;
                    bufferProducers.Sort((a, b) =>
                    {
                        var result = b.Priority.CompareTo(a.Priority);
                        if (result == 0)
                        {
                            result = b.block.EntityId.CompareTo(a.block.EntityId);
                        }

                        return result;
                    });
                    step = -52;
                    producer = bufferProducers[0];
                    maxLimits = producer.GetLimits();
                }
                else
                {
                    step = -60;
                    var type = GridGroupInfoInstance.GetTypeOfGridGroup(grids);
                    step = -61;

                    if (OriginalSpecCoreSession.Instance.Settings.NoSpecCoreSettings != null)
                    {
                        maxLimits = OriginalSpecCoreSession.Instance.Settings.NoSpecCoreSettings.GetLimits(type);
                    }
                    else
                    {
                        maxLimits = new Limits();
                    }
                    
                    step = -62;
                    string error;
                    if (!GridGroupInfoInstance.CanBeApplied(maxLimits, out error))
                    {
                        if (OriginalSpecCoreSession.IsDebug)
                        {
                            Log.ChatError("Cant be applied even no SpecCore settings:" + error);
                        }
                        maxLimits = zeroLimits;
                    }
                    step = -63;
                }
                
                step = 7;

                Phase_2_TurnOffAllNotMatching(producer, maxLimits, ref changed);

                step = 9;
                
                Phase_3_TurnOnAllMatching(producer, maxLimits, ref changed);

                step = 11;

                if (OriginalSpecCoreSession.IsDebug)
                {
                    //Log.ChatError($"CheckLimitsInGrid {bufferProducers.Count} | {grids.Count} Limits: {producer?.GetLimits().Print((k,v)=>$"{k}:{v}", separator:" ") ?? "null"}");
                }

                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    Phase_4_ClientLimitInfo(producer, grids, evenDefaultFailed);
                }
                
                step = 13;

                
                
                if (!bufferSum.IsOneKeyMoreThan(maxLimits))
                {
                    selectedSpecBlock = producer;

                    if (OriginalSpecCoreSession.IsDebug)
                    {
                        //Log.ChatError($"All good : {bufferSum.Print(" ", SharedLogic.NoZeroLimits)} \r\n\r\n {maxLimits.Print(" ", SharedLogic.NoZeroLimits)}");
                    }
                    
                   
                    if (!OriginalSpecCoreSession.RandomPunishment)
                    {
                        foreach (var block in bufferConsumers)
                        {
                            block.WasInLimitLastTick = true;
                        }
                    }

                    UpdateCachedCore(grids, producer, ref changed, ref changedCore);
                    return;
                }
                
                step = 15;

                if (OriginalSpecCoreSession.IsDebug)
                {
                    //Log.ChatError("Exceeding limits:" + bufferSum.Print(" | ", SharedLogic.NoZeroLimits));
                    //Log.ChatError("Max limits:" + maxLimits.Print(" | ", SharedLogic.NoZeroLimits));
                    //Log.ChatError("Producer:" + producer?.block?.DisplayNameText ?? "Null");
                    //Log.ChatError("Possible Producers:" + bufferProducers.Count);
                    //Log.ChatError("BufferConsumers was fine:" + bufferConsumers.Count((x)=>x.WasInLimitLastTick));
                }
                
                
                
                bufferConsumers.Sort((a, b) => Compare (b.DisableOrder(),a.DisableOrder(), b.WasInLimitLastTick, a.WasInLimitLastTick));

                step = 17;

                if (MyAPIGateway.Session.IsServer)
                {
                    foreach (var x in bufferConsumers)
                    {
                        var consumerLimits = x.GetLimits();
                        
                        if (IsInOverLimit(bufferSum, consumerLimits, maxLimits))
                        {
                            x.Disable(2);
                            x.WasInLimitLastTick = false;
                            changed = true;
                            if (!x.IsDrainingPoints())
                            {
                                bufferSum.MinusIfContains(consumerLimits);
                            }
                        }
                    }
                }
                
                step = 19;

                //Still violating => disable all blocks;

                var punishAll = !bufferSum.GetLess(maxLimits, punishIds);

                if (!punishAll)
                {
                    foreach (var block in bufferConsumers)
                    {
                        var consumerLimits = block.GetLimits();
                        if (!CheckLessLimitsAndRecordPunishIds(maxLimits, consumerLimits, punishIds))
                        {
                            punishAll = true;
                            break;
                        }
                    }
                }
                
                if (punishAll)
                {
                    //Still violating => disable all blocks;
                    if (MyAPIGateway.Session.IsServer)
                    {
                        foreach (var x in bufferConsumers)
                        {
                            x.Punish(punishIds);
                        }
                    }
                    UpdateCachedCore(grids, null, ref changed, ref changedCore);
                }
                else
                {
                    selectedSpecBlock = producer;
                    UpdateCachedCore(grids, producer, ref changed, ref changedCore);
                }
                
                step = 21;
            }
            catch (Exception e)
            {
                Log.ChatError($"Checker: Step={step} " + e.ToString());
            }
            finally
            {
                if (changed)
                {
                    SendDirtyCustomInfo(grids);
                }

                if (changedCore)
                {
                    Hooks.TriggerOnSpecCoreChanged(selectedSpecBlock, grids);
                }
            }
        }
        
        private static void PrepareAndClear()
        {
            foreach (var x in allSumKeys)
            {
                bufferSum[x] = 0;
                clientFoundPoints[x] = 0;
                clientTotalPoints[x] = 0;
            }

            
            foreach (var x in allLessKeys)
            {
                clientLessFound[x] = 0;
                clientLessTotal[x] = 0;
            }

            bufferProducers.Clear();
            bufferConsumers.Clear();
            notDrainingConsumers.Clear();
        }

        private static void SendDirtyCustomInfo(List<IMyCubeGrid> grids)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var g in grids)
                {
                    var ship = g.GetShip();
                    if (ship != null)
                    {
                        foreach (var s in ship.SpecBlocks)
                        {
                            var block = s.Key as IMyTerminalBlock;
                            if (block != null)
                            {
                                block.ShowOnHUD = !block.ShowOnHUD;
                                block.ShowOnHUD = !block.ShowOnHUD;
                            }
                        }
                    }
                }
            }
        }
        
        private static void Phase_1_CollectInfo(List<IMyCubeGrid> grids)
        {
            foreach (var g in grids)
            {
                if ((g as MyCubeGrid).Projector != null)
                {
                    continue;
                }

                var ship = g.GetShip();
                if (ship == null) continue;
                ship.ResetLimitsTimer(); //Resetting checks    

                //Log.ChatError($"CheckLimitsInGrid {g} {ship.LimitedBlocks.Count} | {ship.SpecBlocks.Count}");

               
                
                foreach (var x in ship.LimitedBlocks.Values)
                {
                    if (!x.IsDrainingPoints())
                    {
                        notDrainingConsumers.Add(x);
                        continue;
                    }
                    bufferConsumers.Add(x);
                    bufferSum.PlusIfContains(x.GetLimits());
                }
                
                if (OriginalSpecCoreSession.IsDebug)
                {
                    //Log.ChatError($"Limited blocks:{bufferConsumers.Count}/{ship.LimitedBlocks.Count} {bufferSum.Print(" ", SharedLogic.NoZeroLimits)}");
                }

                foreach (var x in ship.SpecBlocks.Values)
                {
                    //allBufferProducers.Add(x);
                    if (x.CanBeApplied(grids, GridGroupInfoInstance))
                    {
                        bufferProducers.Add(x);
                    }
                }
            }
        }
        
        private static void Phase_2_TurnOffAllNotMatching(ISpecBlock producer, Limits maxLimits, ref bool changed)
        {
            
            for (var x=0; x<bufferConsumers.Count; x++)
            {
                var consumer = bufferConsumers[x];
                Limits consumerLimits = consumer.GetLimits();
                
                if (!CheckLessLimits(maxLimits, consumerLimits) || !consumer.CheckConditions(producer))
                {
                    consumer.Disable(4);
                    changed = true;
                    if (!consumer.IsDrainingPoints())
                    {
                        bufferConsumers.RemoveAt(x);
                        x--;
                        bufferSum.MinusIfContains(consumerLimits);
                    }
                }
            }
        }
        
        private static void Phase_3_TurnOnAllMatching(ISpecBlock producer, Limits maxLimits, ref bool changed)
        {
            foreach (var consumer in notDrainingConsumers)
            {
                var consumerLimits = consumer.GetLimits();
                if (consumer.CheckConditions(producer) && !bufferSum.IsOneKeyMoreThan(consumerLimits, maxLimits) && CheckLessLimits(maxLimits, consumerLimits))
                {
                    consumer.Enable();
                    changed = true;
                    if (consumer.IsDrainingPoints())
                    {
                        bufferSum.PlusIfContains(consumerLimits);
                        bufferConsumers.Add(consumer);
                    }
                }
                else
                {
                    //if (OriginalSpecCoreSession.IsDebug)
                    //{
                    //    Log.ChatError($"Phase_3_TurnOnAllMatching : {consumer.CheckConditions(producer)} {bufferSum.IsOneKeyMoreThan(consumerLimits, maxLimits)} {CheckLessLimits(maxLimits, consumerLimits)}");
                    //}
                }
            }
            
            //if (OriginalSpecCoreSession.IsDebug)
            //{
            //    Log.ChatError($"Phase_3_TurnOnAllMatching : {notDrainingConsumers.Count}");
            //}
        }
        
        private static void Phase_4_ClientLimitInfo(ISpecBlock producer, List<IMyCubeGrid> grids, bool defaultGGWasFine)
        {
            //var s = new StringBuilder();
            foreach (var g in grids)
            {
                if ((g as MyCubeGrid).Projector != null)
                {
                    continue;
                }
                var ship = g.GetShip();
                if (ship == null) continue;
                foreach (var x in ship.LimitedBlocks.Values)
                {
                    var l = x.GetLimits();
                    clientLessTotal.MaxIfContains(l);
                    clientTotalPoints.PlusIfContains(l);
                    if (x.IsDrainingPoints())
                    {
                        clientLessFound.MaxIfContains(l);
                        clientFoundPoints.PlusIfContains(l);
                    }
                }
            }

            clientFoundPoints.SetValues(clientLessFound);
            clientTotalPoints.SetValues(clientLessTotal);
            //Log.ChatError("AFTER:" + grids[0].DisplayName + "\n" + clientFoundPoints.Print("   ") + "\n" + clientTotalPoints.Print("   "));


            var status = producer != null ? T.ActivationStatus_ErrorOtherCore :
                defaultGGWasFine ? T.ActivationStatus_ErrorUsingGridGroupDefault :
                T.ActivationStatus_ErrorEvenGridGroupFail;

            foreach (var x in bufferProducers)
            {
                x.status = status;
            }

            if (producer != null)
            {
                producer.status = T.ActivationStatus_CurrentCore;
            }

            foreach (var g in grids)
            {
                var ship = g.GetShip();
                if (ship == null) continue;
                foreach (var x in ship.SpecBlocks.Values)
                {
                    x.FoundLimits.Clear();
                    x.TotalLimits.Clear();
                    x.FoundLimits.Sum(clientFoundPoints);
                    x.TotalLimits.Sum(clientTotalPoints);
                    x.block.RefreshCustomInfo();
                }
            }
        }

        public static bool IsInOverLimit<T> (IDictionary<T, float> buffer, IDictionary<T, float> consumer, IDictionary<T, float> maxLimits)
        {
            foreach (var y in buffer)
            {
                try
                {
                    var v = consumer.GetOr(y.Key, 0);
                    if (v == 0) continue;
                    if (y.Value > maxLimits[y.Key])
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.ChatError("IsInOverLimit: " + y.Key + " " + e);
                }

            }
            return false;
        }
        
        private static bool CheckLessLimits(Limits producer, Limits consumer)
        {
            foreach (var key in allLessKeys)
            {
                try
                {
                    float value;
                    if (consumer.TryGetValue(key, out value) && producer[key] < value)
                    {
                        //Log.ChatError($"CheckLessLimits : {producer[key] }: {value}");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Log.ChatError($"Key[{key}] not found : " + producer.Print(", "));
                }
                
            }

            return true;
        }
        
        private static bool CheckLessLimitsAndRecordPunishIds(Limits producer, Limits consumer, Dictionary<int, bool> punishIds)
        {
            bool isOk = true;
            foreach (var key in allLessKeys)
            {
                try
                {
                    float value;
                    if (consumer.TryGetValue(key, out value) && producer[key] < value)
                    {
                        punishIds[key] = true;
                        isOk = false;
                    }
                }
                catch (Exception e)
                {
                    Log.ChatError($"Key[{key}] not found : " + producer.Print(", "));
                }
                
            }

            return isOk;
        }

        private static void UpdateCachedCore(List<IMyCubeGrid> grids, ISpecBlock specBlock, ref bool changed, ref bool changedCore)
        {
            foreach (var g in grids)
            {
                var ship = g.GetShip();
                if (ship != null)
                {
                    if (ship.CachedCore != specBlock)
                    {
                        changed = true;
                        changedCore = true;
                        ship.CachedCore = specBlock;
                    }
                }
            }
        }

        public static void OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2) {
            if (MyAPIGateway.Session.IsServer) { return; }
            if (!arg1.InScene) { return; }
            CheckLimitsInGrid(arg1);
            if (!arg2.InScene) { return; }
            CheckLimitsInGrid(arg2);
        }
    }
}