using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Scripts.Shared
{
    public static class GuiControlDuplicateRemover
    {
        private static string[] duplicates;
        public static void Init(params string[] dups)
        {
            duplicates = dups;
            MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControlsOnCustomControlGetter;
        }

        private static void TerminalControlsOnCustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            HashSet<string> exists = new HashSet<string>();
            for (var index = 0; index < controls.Count; index++)
            {
                var action = controls[index];
                if (exists.Contains(action.Id))
                {
                    controls.RemoveAt(index);
                    index--;
                    continue;
                }

                foreach (var dup in duplicates)
                {
                    if (action.Id.Contains(dup))
                    {
                        exists.Add(action.Id);
                    }
                }
            }
        }
    }
}