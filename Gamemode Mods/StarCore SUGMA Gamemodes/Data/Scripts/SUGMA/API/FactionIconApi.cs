using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace SC.SUGMA.API
{
    internal class FactionIconApi
    {
        private static FactionIconApi I;

        private List<MyStringId> _icons = new List<MyStringId>();
        private List<MyStringId> _iconsSmall = new List<MyStringId>();

        private FactionIconApi()
        {
            foreach (var mod in MyAPIGateway.Session.Mods)
            {
                if (MyAPIGateway.Utilities.FileExistsInModLocation("Data\\SC_FactionIcons.txt", mod))
                {
                    string result;
                    using (var icoReader = MyAPIGateway.Utilities.ReadFileInModLocation("Data\\SC_FactionIcons.txt", mod))
                    {
                        result = icoReader.ReadToEnd();
                    }

                    string[] split = result.Split('\n');
                    _icons.EnsureCapacity(split.Length);
                    foreach (var icon in split)
                    {
                        if (string.IsNullOrWhiteSpace(icon))
                            continue;

                        string trim = icon.Trim();

                        if (trim.EndsWith("_small"))
                            _iconsSmall.Add(MyStringId.GetOrCompute(trim));
                        else
                            _icons.Add(MyStringId.GetOrCompute(trim));
                    }
                        
                }
            }

            Log.Info("Found faction icons:");
            foreach (var icon in _icons)
            {
                Log.Info($"- {icon}");
            }
            foreach (var icon in _iconsSmall)
            {
                Log.Info($"- {icon}");
            }
        }

        public static void Init()
        {
            I = new FactionIconApi();
        }

        public static void Unload()
        {
            I = null;
        }

        public static bool TryGetIcon(IMyFaction faction, bool small, out MyStringId hash)
        {
            string facIdLower = faction.Tag.ToLower();
            foreach (var icon in (small ? I._iconsSmall : I._icons))
            {
                string lower = icon.String.ToLower();
                Log.Info($"{facIdLower} {lower} {lower.StartsWith(facIdLower)}");

                if (lower.StartsWith(facIdLower))
                {
                    hash = icon;
                    return true;
                }
            }

            hash = faction.FactionIcon ?? MyStringId.NullOrEmpty;
            return false;
        }
    }
}
