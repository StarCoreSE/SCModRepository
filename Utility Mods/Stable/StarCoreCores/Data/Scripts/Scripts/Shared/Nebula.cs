using System;

namespace Nebula
{
    public static class Nebula
    {
        public static int GetTier(this string Id)
        {
            try
            {
                var parts = Id.Split(new string[] {"_"}, StringSplitOptions.None);
                var n = parts[parts.Length-1].Substring(1);
                if (n.StartsWith("0")) n = n.Substring(1);
                return int.Parse(n);
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        
        public static string GetBlockSpecialName(string Id)
        {
            var splt = Id.Split(new string[] {"_"}, StringSplitOptions.None);
            if (splt.Length == 3 && (splt[0].StartsWith("L") || splt[0].StartsWith("S")))
            {
                return splt[1];
            }

            
            if (Id.Contains("Armor"))
            {
                if (Id.Contains("Heavy"))
                {
                    return "HeavyArmor";
                }
                return "LightArmor";
            }
            
            if (Id.Contains("Stator") || Id.Contains("Rotor"))
            {
                return "Rotor";
            }
            return null;
        }
    }
}