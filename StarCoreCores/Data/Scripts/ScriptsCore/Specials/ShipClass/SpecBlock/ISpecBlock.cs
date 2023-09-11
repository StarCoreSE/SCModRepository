using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace MIG.SpecCores
{
    public abstract class ISpecBlock {
        public IMyTerminalBlock block;
        public IMyFunctionalBlock fblock;

        public Limits StaticLimits;
        public Limits DynamicLimits;
        public Limits FoundLimits = new Limits();
        public Limits TotalLimits = new Limits();
        public string status;



        private FrameExecutor.ActionWrapper RefreshCustomInfo;
        public float Priority => GetLimits(false)[LimitsChecker.TYPE_PRIORITY];

        public Limits GetLimits(bool hooks = true)
        {
            var limits = IsStatic.Value ? StaticLimits : DynamicLimits;
            if (hooks)
            {
                Hooks.InvokeLimitsInterceptor(this, StaticLimits, DynamicLimits);
            }
            return new Limits(limits);
        }

        protected DecayingByFramesLazy<bool> IsStatic = new DecayingByFramesLazy<bool>(6);


        public bool HasOverlimitedBlocks()
        {
            var l = GetLimits();
            foreach (var kv in TotalLimits)
            {
                if (!l.ContainsKey(kv.Key))
                {
                    if (OriginalSpecCoreSession.IsDebug)
                    {
                        Log.ChatError("No key:" + kv.Key);
                    }
                    return true;
                }

                if (kv.Value > l[kv.Key])
                {
                    if (OriginalSpecCoreSession.IsDebug)
                    {
                        Log.ChatError(kv.Key + " " + kv.Value + " " + l[kv.Key]);
                    }
                    return true;
                }
            }

            return false;
        }
        
        public ISpecBlock(IMyTerminalBlock Entity)
        {
            IsStatic.SetGetter(GetIsStatic);

            block = (Entity as IMyTerminalBlock);
            fblock = (Entity as IMyFunctionalBlock);
            
            if (!MyAPIGateway.Session.isTorchServer()) {
                block.AppendingCustomInfo += BlockOnAppendingCustomInfo;
                block.OnMarkForClose += BlockOnOnMarkForClose;
            }
        }
        
        public void Destroy()
        {
            BlockOnOnMarkForClose(block);
        }

        private static List<IMyCubeGrid> m_gridBuffer = new List<IMyCubeGrid>();
        private bool GetIsStatic(bool oldValue)
        {
            block.CubeGrid.GetConnectedGrids(OriginalSpecCoreSession.Instance.Settings.ConnectionType, m_gridBuffer, true);
            foreach (var g in m_gridBuffer)
            {
                if (g.IsStatic) return true;
            }
            return false;
        }
        
        protected void SetOptions(Limits staticLimits, Limits dynamicLimits = null) {
            this.StaticLimits = staticLimits;
            this.DynamicLimits = dynamicLimits;
        }

        

        public abstract bool CanBeApplied(List<IMyCubeGrid> grids, GridGroupInfo info);

        private void BlockOnOnMarkForClose(IMyEntity obj) {
            block.AppendingCustomInfo -= BlockOnAppendingCustomInfo;
            block.OnMarkForClose -= BlockOnOnMarkForClose;
        }

        protected virtual void BlockOnAppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2) { }
    }

    
}