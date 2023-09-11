using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public abstract class GUIIniter
    {
        private readonly HashSet<Type> initedGUI = new HashSet<Type>();
        private readonly HashSet<Type> initedInterfacesGUI = new HashSet<Type>();
        private List<Func<IMyCubeBlock, bool>> possibleBlocksToInit = new List<Func<IMyCubeBlock, bool>>();

        private bool iMyTerminalWasInited = false;
        protected abstract void InitControls<T>() where T : IMyCubeBlock;

        public void CreateGui(IMyCubeBlock block)
        {
            if (block == null) return;
            var type = block.GetType();
            if (initedGUI.Contains(type)) return;

            lock (this)
            {
                if (initedGUI.Contains(type)) return;
                initedGUI.Add(type);
                for (int i = 0; i < possibleBlocksToInit.Count; i++)
                {
                    var fx = possibleBlocksToInit[i];
                    var added = fx(block);
                    if (added) return;
                }

                if (!iMyTerminalWasInited)
                {
                    iMyTerminalWasInited = true;
                    InitControls<IMyTerminalBlock>();
                }
            }
        }


        public void AddType<T>() where T: IMyCubeBlock
        {
            possibleBlocksToInit.Add(Init<T,T>);
        }

        private bool Init<T,Z>(IMyCubeBlock entity) where T : IMyCubeBlock where Z : IMyCubeBlock
        {
            if (entity is T)
            {
                var added = initedInterfacesGUI.Add(typeof(T));
                if (added)
                {
                    InitControls<T>();
                }

                return true;
            }

            return false;
        }
    }
}