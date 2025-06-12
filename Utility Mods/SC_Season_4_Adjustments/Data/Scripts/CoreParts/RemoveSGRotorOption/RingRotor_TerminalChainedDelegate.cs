using System;
using Sandbox.ModAPI;

namespace StarCore.RemoveRingSGOptions
{
    public class RingRotor_TerminalChainedDelegate
    {
        public static Func<IMyTerminalBlock, bool> Create(Func<IMyTerminalBlock, bool> originalFunc, Func<IMyTerminalBlock, bool> customFunc, bool checkOR = false)
        {
            return new RingRotor_TerminalChainedDelegate(originalFunc, customFunc, checkOR).ResultFunc;
        }

        readonly Func<IMyTerminalBlock, bool> OriginalFunc;
        readonly Func<IMyTerminalBlock, bool> CustomFunc;
        readonly bool CheckOR;

        RingRotor_TerminalChainedDelegate(Func<IMyTerminalBlock, bool> originalFunc, Func<IMyTerminalBlock, bool> customFunc, bool checkOR)
        {
            OriginalFunc = originalFunc;
            CustomFunc = customFunc;
            CheckOR = checkOR;
        }

        bool ResultFunc(IMyTerminalBlock block)
        {
            if(block?.CubeGrid == null)
                return false;

            bool originalCondition = (OriginalFunc == null ? true : OriginalFunc.Invoke(block));
            bool customCondition = (CustomFunc == null ? true : CustomFunc.Invoke(block));

            if(CheckOR)
                return originalCondition || customCondition;
            else
                return originalCondition && customCondition;
        }
    }
}
