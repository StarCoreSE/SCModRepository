using System;
using Sandbox.ModAPI;

namespace DynamicResistence
{
    public class CombineFunc
    {
        private readonly Func<IMyTerminalBlock, bool> originalFunc;
        private readonly Func<IMyTerminalBlock, bool> customFunc;

        private CombineFunc(Func<IMyTerminalBlock, bool> originalFunc, Func<IMyTerminalBlock, bool> customFunc)
        {
            this.originalFunc = originalFunc;
            this.customFunc = customFunc;
        }

        private bool ResultFunc(IMyTerminalBlock block)
        {
            if(block?.CubeGrid == null)
                return false;

            bool originallyVisible = (originalFunc == null ? true : originalFunc.Invoke(block));
            return originallyVisible && customFunc.Invoke(block);
        }

        public static Func<IMyTerminalBlock, bool> Create(Func<IMyTerminalBlock, bool> originalFunc, Func<IMyTerminalBlock, bool> customFunc)
        {
            return new CombineFunc(originalFunc, customFunc).ResultFunc;
        }
    }
}
