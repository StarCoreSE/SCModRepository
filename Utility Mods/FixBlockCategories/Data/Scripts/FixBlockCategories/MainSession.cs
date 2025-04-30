using System;
using Sandbox.Definitions;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Network;
using VRage.Utils;

namespace SC.FixBlockCategories
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, int.MaxValue)] // load last
    internal class MainSession : MySessionComponentBase
    {
        private HashSet<string> _nullSubtypes = new HashSet<string>();
        private Dictionary<string, string> _subtypeToTypePairing = new Dictionary<string, string>();

        public override void LoadData()
        {
            MyLog.Default.WriteLineAndConsole($"[FixBlockCategories] Start check...");

            _subtypeToTypePairing = new Dictionary<string, string>();
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var blockdef = def as MyCubeBlockDefinition;
                if (blockdef == null)
                    continue;

                if (string.IsNullOrEmpty(blockdef.Id.SubtypeName))
                {
                    _nullSubtypes.Add(blockdef.Id.TypeId.ToString().Replace("MyObjectBuilder_", ""));
                    continue;
                }

                _subtypeToTypePairing[blockdef.Id.SubtypeName] = blockdef.Id.TypeId.ToString().Replace("MyObjectBuilder_", "");
            }

            foreach (var catdef in MyDefinitionManager.Static.GetCategories().Values)
            {
                MyLog.Default.WriteLineAndConsole($"[FixBlockCategories] CHK {catdef.Name}");
                var replaced = new HashSet<string>();
                foreach (var item in catdef.ItemIds)
                {
                    var name = FixItemName(catdef.Name, item);
                    if (name != null)
                        replaced.Add(name);
                }
                catdef.ItemIds = replaced;
            }

            MyLog.Default.WriteLineAndConsole($"[FixBlockCategories] Finished.");
        }

        private string FixItemName(string categoryName, string subtypeId)
        {
            // already valid
            if (subtypeId.Contains("/"))
                return subtypeId;

            string typeId;
            if (_subtypeToTypePairing.TryGetValue(subtypeId, out typeId)) // keen broke block category items with just subtypeid
            {
                MyLog.Default.WriteLineAndConsole($"[FixBlockCategories] Replaced {categoryName}::{typeId + "/" + subtypeId}");
                return typeId + "/" + subtypeId;
            }
            else if (_nullSubtypes.Contains(subtypeId))
            {
                // subtype-less blocks (i.e. gravity generator)
                MyLog.Default.WriteLineAndConsole($"[FixBlockCategories] Replaced {categoryName}::{subtypeId + "/(null)"}");
                return subtypeId + "/(null)";
            }

            MyLog.Default.WriteLineAndConsole($"[FixBlockCategories] Removed {categoryName}::{subtypeId} (does the block exist?)");
            return null;
        }
    }
}
