using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace MIG.SpecCores
{
    
    public class LimitedBlock : ILimitedBlock {
        public IMyTerminalBlock block;
        public IMyFunctionalBlock fblock;

        private Limits limits;
        private bool IsCurrentlyOnMainGrid = false;
        
        
        private LimitedBlockInfo info;
        private BlockId blockId;
        private bool IgnoreEvent = false;
        private bool CanBePunished = false;
        
        public LimitedBlockNetworking Component { get; private set; }
        public void Destroy() { BlockOnOnMarkForClose(block); }
        public bool WasInLimitLastTick { get; set; } = false;
        public bool CheckConditions(ISpecBlock specblock) { return info.CanWorkWithoutSpecCore || specblock != null; }
        public IMyTerminalBlock GetBlock() { return block; }
        public long EntityId() { return block.EntityId; }
        public Limits GetLimits() { return limits; }
        

        public float DisableOrder() { return blockId.DisableOrder; }
        public bool MatchesConditions() { return (IsCurrentlyOnMainGrid || info.CanWorkOnSubGrids); }
        public bool ShouldBeEnabled() { return Component.Settings.AutoEnable && (Component.Settings.WasDisabledBySpecCore || !Component.Settings.SmartTurnOn); }

        
        public LimitedBlock(IMyTerminalBlock Entity, LimitedBlockInfo info, BlockId blockId)
        {
            var line = 0;
            try
            {
                block = Entity;
                this.blockId = blockId;
                fblock = Entity as IMyFunctionalBlock;
                line = 1;

                this.info = info;
                this.limits = info.GetLimits(blockId);
                line = 2;
                
                
                if (!MyAPIGateway.Session.isTorchServer()) {
                    block.AppendingCustomInfo += BlockOnAppendingCustomInfo;
                    block.OnMarkForClose += BlockOnOnMarkForClose;
                    //Entity.RefreshCustomInfo(); //TODO Slime parallel
                }

                line = 3;

                if (!info.CanWorkOnSubGrids)
                {
                    FrameExecutor.addFrameLogic(new AutoTimer(OriginalSpecCoreSession.Instance.Settings.Timers.CheckBlocksOnSubGridsInterval, OriginalSpecCoreSession.Instance.Settings.Timers.CheckBlocksOnSubGridsInterval), block, Tick);
                }

                line = 4;
                if (MyAPIGateway.Session.IsServer)
                {
                    if (fblock != null)
                    {
                        fblock.EnabledChanged += OnEnabledChanged;
                    }
                }

                line = 5;
                Component = new LimitedBlockNetworking(this, Entity, info.DefaultBlockSettings);

                line = 6;
                GUI.LimitedBlockGui.CreateGui(Entity);
                line = 7;
            }
            catch (Exception e)
            {
                Log.ChatError($"At line {line}", e);
            }
            
        }

        public static void InitControls<Z>() where Z : IMyCubeBlock
        {
            try
            {
                MyAPIGateway.TerminalControls.CreateCheckbox<ILimitedBlock, Z>("SpecCores_AutoEnable", 
                    T.Translation(T.GUI_AutoEnable), 
                    T.Translation(T.GUI_AutoEnableToolTip), 
					(block) =>
                    {
                        var s = block?.Component?.Settings;
                        if (s == null) return false;
                        return s.AutoEnable;
                    },
                    (block, val) =>
                    {
                        block.Component.Settings.AutoEnable = val;
                        block.Component.NotifyAndSave();
                    }, 
                    (x)=>x.GetLimitedBlock(),
                    visible: (x) =>
                    {
                        var s = x?.Component?.Settings;
                        if (s == null) return false;
                        return s.AutoEnableShowGUI || s.AutoEnable;
                    });
            
                MyAPIGateway.TerminalControls.CreateCheckbox<ILimitedBlock, Z>("SpecCores_SmartAutoEnable", 
                    T.Translation(T.GUI_SmartAutoEnable), 
                    T.Translation(T.GUI_SmartAutoEnableToolTip), 
					(block) =>
                    {
                        var s = block?.Component?.Settings;
                        if (s == null) return false;
                        return s.SmartTurnOn;
                    },
                    (block, val) =>
                    {
                        block.Component.Settings.SmartTurnOn = val;
                        block.Component.NotifyAndSave();
                    }, 
                    (x)=>x.GetLimitedBlock(),
                    visible: (x) =>
                    {
                        var s = x?.Component?.Settings;
                        if (s == null) return false;
                        return s.SmartTurnOnShowGUI || s.SmartTurnOn;
                    });
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }


        #region EnableDisableConsuming

        public void Enable()
        {
            if (!MyAPIGateway.Session.IsServer) return;
           
            
            if (!MatchesConditions()) return;
            if (!ShouldBeEnabled()) return;
            

            foreach (var behavior in info.Behaviors)
            {
                if (behavior.EnableBehavior != EnableDisableBehaviorType.None)
                {
                    EnableOrDisable(behavior, behavior.EnableBehavior);
                }
            }
        }

        public void Disable(int reason)
        {
            //if (OriginalSpecCoreSession.IsDebug)
            //{
            //    Log.ChatError("Disable: " + reason);
            //}
            foreach (var behavior in info.Behaviors)
            {
                if (behavior.DisableBehavior != EnableDisableBehaviorType.None)
                {
                    EnableOrDisable(behavior, behavior.DisableBehavior);
                }
            }
        }
        
        
        public bool Punish(Dictionary<int, bool> shouldPunish)
        {
            if (!info.CanBePunished) return false;
            
            int lastId = 0;
            try
            {
                foreach (var behavior in info.Behaviors)
                {
                    if (behavior.PunishBehavior != EnableDisableBehaviorType.None)
                    {
                        foreach (var id in behavior.GetPunishedBy())
                        {
                            lastId = id;
                            if (shouldPunish[id])
                            {
                                if (EnableOrDisable(behavior, behavior.PunishBehavior))
                                {
                                    return true;
                                };
                                break;
                            }   
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.ChatError($"Punish {this.block.SlimBlock.BlockDefinition.Id} {lastId}");
            }
            return false;
        }
        
        public bool IsDrainingPoints()
        {
            foreach (var behavior in info.Behaviors)
            {
                if (behavior.ConsumeBehavior != ConsumeBehaviorType.None)
                {
                    if (SharedLogic.IsDrainingPoints(block, fblock, behavior, behavior.ConsumeBehavior))
                    {
                        //if (OriginalSpecCoreSession.IsDebug)
                        //{
                        //    Log.ChatError("IsDrainingPoints conditions:" + info.Behaviors.Length + $" : Draining : true : {behavior.ConsumeBehavior} ");
                        //}
                        return true;
                    }
                }
            }
            //if (OriginalSpecCoreSession.IsDebug)
            //{
            //    Log.ChatError("IsDrainingPoints conditions:" + info.Behaviors.Length + " : Draining : false");
            //}
            return false;
        }

        private static MyInventory DrainResourcesInventory = new MyInventory(9999999f,
            new Vector3(9999999f, 9999999f, 9999999f), MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend);
        
        public bool EnableOrDisable(Behavior behavior, EnableDisableBehaviorType type)
        {
            switch (type)
            {
                case EnableDisableBehaviorType.None:
                    return false;
                
                case EnableDisableBehaviorType.Destroy:
                    block.CubeGrid.RazeBlock(block.Position);
                    return true;
                case EnableDisableBehaviorType.SetArmed:
                    (block as IMyWarhead).IsArmed = !behavior.Reverse;
                    return true;
                case EnableDisableBehaviorType.SetDrillHarvestMlt:
                    (block as IMyShipDrill).DrillHarvestMultiplier = behavior.Value1;
                    return true;
                case EnableDisableBehaviorType.SetThrustMlt:
                    (block as IMyThrust).ThrustMultiplier = behavior.Value1;
                    return true;
                case EnableDisableBehaviorType.SetThrustPowerConsumptionMlt:
                    (block as IMyThrust).PowerConsumptionMultiplier = behavior.Value1;
                    return true;
                case EnableDisableBehaviorType.SetReactorPowerOutputMlt:
                    (block as IMyReactor).PowerOutputMultiplier = behavior.Value1;
                    return true;
                case EnableDisableBehaviorType.SetGasGeneratorMlt:
                    (block as IMyGasGenerator).ProductionCapacityMultiplier = behavior.Value1;
                    return true;
                case EnableDisableBehaviorType.WeldToFunctional:
                    var critical = (block.SlimBlock.BlockDefinition as MyCubeBlockDefinition).CriticalIntegrityRatio * block.SlimBlock.MaxIntegrity;
                    
                    //Log.ChatError($"WeldToFunctional : {block.SlimBlock.Integrity} {critical - behavior.Value2}");
                    if (block.SlimBlock.Integrity > critical - behavior.Value2 -  block.SlimBlock.MaxIntegrity * behavior.Value3)
                    {
                        block.SlimBlock.IncreaseMountLevelToFunctionalState(behavior.Value1, null, block.BuiltBy(), Component.Settings.BeforeDamageShareMode);
                    }
                    return true;
                case EnableDisableBehaviorType.WeldBy:
                    block.SlimBlock.IncreaseMountLevelByDesiredRatio(behavior.Value1, behavior.Value2,  null, block.BuiltBy(), Component.Settings.BeforeDamageShareMode);
                    return true;
                case EnableDisableBehaviorType.WeldTo:
                    block.SlimBlock.IncreaseMountLevelToDesiredRatio(behavior.Value1, behavior.Value2,  null, block.BuiltBy(), Component.Settings.BeforeDamageShareMode);
                    return true;
                case EnableDisableBehaviorType.SetEnabled:
                    fblock.Enabled = !behavior.Reverse;
                    if (behavior.Reverse)
                    {
                        Component.Settings.WasDisabledBySpecCore = true;
                        IgnoreEvent = true;
                        fblock.Enabled = false;
                        IgnoreEvent = false;
                    }
                    return true;

                case EnableDisableBehaviorType.GrindToFunctional:
                case EnableDisableBehaviorType.GrindBy:
                case EnableDisableBehaviorType.GrindTo:
                    if (block.IsFunctional)
                    {
                        Component.Settings.BeforeDamageOwnerId = block.OwnerId;
                        Component.Settings.BeforeDamageShareMode = MyOwnershipShareModeEnum.Faction; //TODO: block.BeforeDamageShareMode;
                        Component.SaveSettings();
                    }
                    try
                    {
                        switch (type)
                        {
                            case EnableDisableBehaviorType.GrindToFunctional:
                                block.SlimBlock.DecreaseMountLevelToFunctionalState(behavior.BoolValue ? DrainResourcesInventory : null, behavior.Value1);
                                DrainResourcesInventory.Clear();
                                return true;
                            case EnableDisableBehaviorType.GrindBy:
                                block.SlimBlock.DecreaseMountLevelByDesiredRatio(behavior.Value1, behavior.Value2, behavior.Reverse, behavior.BoolValue ? DrainResourcesInventory : null);
                                DrainResourcesInventory.Clear();
                                return true;
                            case EnableDisableBehaviorType.GrindTo:
                                block.SlimBlock.DecreaseMountLevelToDesiredRatio(behavior.Value1, behavior.Value2, behavior.Reverse, behavior.BoolValue ? DrainResourcesInventory : null);
                                DrainResourcesInventory.Clear();
                                return true;
                        }
                    }
                    catch (Exception e)
                    {
                        //Skip this exception
                    }
                    return true;

                case EnableDisableBehaviorType.SetInventoryMass:
                    var inv = block.GetInventory() as MyInventory;
                    var invMass = (double) inv.CurrentMass;
                    var blockMass = block.Mass;
                    inv.ExternalMass = (MyFixedPoint)(invMass * behavior.Value1 + blockMass * behavior.Value2 + behavior.Value3);
                    return true;
                
                case EnableDisableBehaviorType.CustomLogic:
                    //TODO SLIME
                    return true;
            }
            
            return true;
        }
        
        
        #endregion
        
        private void OnEnabledChanged(IMyTerminalBlock obj)
        {
            if (IgnoreEvent) return;
            Component.Settings.WasDisabledBySpecCore = false;
        }
        


        public void Tick(long frame)
        {
            IsCurrentlyOnMainGrid = IsOnMainGrid();
            if (!IsCurrentlyOnMainGrid)
            {
                Disable(3);
            }
        }
        
        private bool IsOnMainGrid()
        {
            var ship = block.CubeGrid.GetShip();
            if (ship != null)
            {
                foreach (var x in ship.Cockpits)
                {
                    if (x.IsMainControlledCockpit()) return true;
                }
            }
            return false;
        }
        
        protected virtual void BlockOnAppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2) { 
            arg2.AppendLine().AppendT(T.BlockInfo_StaticOnly).AppendLine();
            T.GetLimitsInfo(null, arg2, limits, null);
            if (!info.CanWorkOnSubGrids)
            {
                arg2.AppendLine().Append(T.Translation(T.BlockInfo_CantWorkOnSubGrids)).AppendLine();
            }
        }
        
        private void BlockOnOnMarkForClose(IMyEntity obj) {
            block.OnMarkForClose -= BlockOnOnMarkForClose;
            if (!MyAPIGateway.Session.isTorchServer()) {
                block.AppendingCustomInfo -= BlockOnAppendingCustomInfo;
            }
        }

    }
}