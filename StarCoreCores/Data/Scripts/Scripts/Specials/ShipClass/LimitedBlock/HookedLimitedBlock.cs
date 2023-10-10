using System.Collections.Generic;
using System.Text;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace MIG.SpecCores
{
    public class HookedLimitedBlock : ILimitedBlock {
        public IMyTerminalBlock block;

        protected Limits limits;
        bool IsCurrentlyOnMainGrid = false;
        private LimitedBlockInfo info;
        private BlockId blockId;

        public bool WasInLimitLastTick { get; set; } = false;
        
        public LimitedBlockNetworking Component { get; private set; }
        private HookedLimiterInfo hooks;
        private object logic;
        
        public void Destroy() { BlockOnOnMarkForClose(block); }
        public bool CheckConditions(ISpecBlock specblock) { return (info.CanWorkWithoutSpecCore || specblock != null) && hooks.CheckConditions(specblock?.block, logic); }
        public IMyTerminalBlock GetBlock() { return block; }
        public long EntityId() { return block.EntityId; }
        public Limits GetLimits() { return limits; }
        public bool IsDrainingPoints() { return hooks.IsDrainingPoints(logic); }
        public bool MatchesConditions() { return (IsCurrentlyOnMainGrid || info.CanWorkOnSubGrids); }
        public bool ShouldBeEnabled() { return Component.Settings.AutoEnable && (Component.Settings.WasDisabledBySpecCore || !Component.Settings.SmartTurnOn); }

        public bool Punish(Dictionary<int, bool> shouldPunish)
        {
            return true;
            //TODO SLIME
        }

        public float DisableOrder() { return blockId.DisableOrder; }
        
        public HookedLimitedBlock(IMyTerminalBlock Entity, LimitedBlockInfo info, HookedLimiterInfo hooks, BlockId blockId)
        {
            Component = new LimitedBlockNetworking(this, Entity, info.DefaultBlockSettings);
            block = Entity;
            this.hooks = hooks;
            this.info = info;
            this.blockId = blockId;
            this.limits = info.GetLimits(blockId);

            if (!MyAPIGateway.Session.isTorchServer()) {
                block.AppendingCustomInfo += BlockOnAppendingCustomInfo;
                block.OnMarkForClose += BlockOnOnMarkForClose;
            }

            if (!info.CanWorkOnSubGrids)
            {
                FrameExecutor.addFrameLogic(new AutoTimer(OriginalSpecCoreSession.Instance.Settings.Timers.CheckBlocksOnSubGridsInterval, OriginalSpecCoreSession.Instance.Settings.Timers.CheckBlocksOnSubGridsInterval), block, Tick);
            }
            
            logic = hooks.OnNewConsumerRegistered(block);
            
            GUI.LimitedBlockGui.CreateGui(Entity);
        }

        

        public void Tick(long frame)
        {
            IsCurrentlyOnMainGrid = IsOnMainGrid();
            if (!IsCurrentlyOnMainGrid)
            {
                Disable(1);
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
        
        private void BlockOnOnMarkForClose(IMyEntity obj) {
            block.OnMarkForClose -= BlockOnOnMarkForClose;
            if (!MyAPIGateway.Session.isTorchServer()) {
                block.AppendingCustomInfo -= BlockOnAppendingCustomInfo;
            }
        }
        
        protected virtual void BlockOnAppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2) { 
            arg2.AppendLine().AppendT(T.BlockInfo_Header).AppendLine();
            T.GetLimitsInfo(null, arg2, limits, null);
            if (!info.CanWorkOnSubGrids)
            {
                arg2.AppendLine().Append(T.Translation(T.BlockInfo_CantWorkOnSubGrids)).AppendLine();
            }
        }

        public void Disable(int reason)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            hooks.Disable(logic);
        }

        
        public void Enable()
        {
            if (!MyAPIGateway.Session.IsServer) return;
            if (!MatchesConditions()) return;
            if (!ShouldBeEnabled()) return;
            hooks?.CanWork(logic);
        }

        
    }
}