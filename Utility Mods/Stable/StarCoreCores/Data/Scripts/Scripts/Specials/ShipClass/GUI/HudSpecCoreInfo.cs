using System;
using MIG.SpecCores;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace ServerMod
{
    public class HudSpecCoreInfo : MyHStat
    {
        private string Error = String.Empty;
        
        public override void Update()
        {
            var cu = MyAPIGateway.Session.LocalHumanPlayer?.Controller?.ControlledEntity;
            if (cu == null)
            {
                Error = String.Empty;
                CurrentValue = 0;
                return;
            }
            
            var cockpit = cu as IMyCubeBlock;
            if (cockpit == null)
            {
                Error = String.Empty;
                CurrentValue = 0;
                return;
            }

            var cubeGrid = cockpit.CubeGrid;
            var core = (ISpecBlock)Hooks.GetMainSpecCore(cubeGrid);
            if (core == null)
            {
                Error = T.Translation(OriginalSpecCoreSession.Instance.Settings.HudNoSpecCoreText);
                CurrentValue = 2;
                return;
            }

            if (core.HasOverlimitedBlocks())
            {
                Error = T.Translation(OriginalSpecCoreSession.Instance.Settings.HudSpecCoreOverlimitText);
                CurrentValue = 1;
                return;
            }
            
            
            Error = T.Translation(OriginalSpecCoreSession.Instance.Settings.HudSpecCoreActiveText);
            Error = String.Format(Error, core.block.DisplayNameText);
            CurrentValue = 0.5f;
            ValueStringDirty();
            return;
        }

        public override string GetId()
        {
            return "SpecBlock_Errors";
        }

        
        public override string ToString()
        {
            return Error;
        }
    }
    
    public abstract class MyHStat : IMyHudStat
    {
        public virtual float MaxValue => 1f;
        public virtual float MinValue => 0.0f;

        private float m_currentValue;
        private string m_valueStringCache;

        public abstract void Update();
        public abstract String GetId();
        
        public MyStringHash Id { get; protected set; }

        public MyHStat()
        {
            Id = MyStringHash.GetOrCompute(GetId());
        }
        
        public float CurrentValue
        {
            get { return m_currentValue; }
            protected set
            {
                if (m_currentValue == value)
                {
                    return;
                }
                m_currentValue = value;
                ValueStringDirty();
            }
        }

        public void ValueStringDirty()
        {
            m_valueStringCache = null;
        }

        public string GetValueString()
        {
            if (m_valueStringCache == null)
            {
                m_valueStringCache = ToString();
            }
            return m_valueStringCache;
        }
        
        public override string ToString() => string.Format("{0:0}", (float)(CurrentValue * 100.0));
    }
    
}