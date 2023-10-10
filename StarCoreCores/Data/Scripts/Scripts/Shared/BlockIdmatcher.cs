using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRage.Game;

namespace SharedLib
{

    public class BlockIdMatcherWithCache : BlockIdMatcher
    {
        private Dictionary<MyDefinitionId, bool> matches = new Dictionary<MyDefinitionId, bool>();
        public BlockIdMatcherWithCache(string s) : base(s)
        {
        }

        public override bool Matches(MyDefinitionId Id)
        {
            bool result;
            if (!matches.TryGetValue(Id, out result))
            {
                result = base.Matches(Id);
                matches[Id] = result;
                return result;
            }

            return result;
        }
    }

    /// <summary>
    /// Examples: */*AdminBlock* */*Admin-Block A/*Admin-Block *!Admin-Block */* a/a
    /// </summary>
    public class BlockIdMatcher
    {
        private static Regex regex = new Regex("([*\\w-_0-9]+)?(?:\\/)?([*\\w-_0-9]+)");
        public List<TypeSubtypeMatcher> checks = new List<TypeSubtypeMatcher>();

        public BlockIdMatcher(string s)
        {
            if (s == null) { return; }
            var m = regex.Matches(s);
            for (var x = 0; x < m.Count; x++)
            {
                var match = m[x];
                switch (match.Groups.Count)
                {
                    case 2:
                        {
                            var w = match.Groups[1].Value;
                            checks.Add(new TypeSubtypeMatcher(TypeSubtypeMatcher.MODE_ANY, String.Empty, GetMode(w), w.Replace("*", "")));
                            break;
                        }
                    case 3:
                        {
                            var w1 = match.Groups[1].Value;
                            var w2 = match.Groups[2].Value;
                            checks.Add(new TypeSubtypeMatcher(GetMode(w1), w1.Replace("*", ""), GetMode(w2), w2.Replace("*", "")));
                            break;
                        }
                }
            }
        }

        public override string ToString()
        {
            var s = "";
            foreach(var x in checks)
            {
                s += x.ToString() + " ";
            }

            return $"BlockIdMatcher:{checks.Count} [{s}]";
        }

        private int GetMode(string w)
        {
            var v = !w.Contains("!");
            if (w == "*") return TypeSubtypeMatcher.MODE_ANY;
            if (w.StartsWith("*") && w.EndsWith("*")) return v ? TypeSubtypeMatcher.MODE_CONTAINS : TypeSubtypeMatcher.MODE_NOT_CONTAINS;
            if (w.StartsWith("*")) return v ? TypeSubtypeMatcher.MODE_ENDS : TypeSubtypeMatcher.MODE_NOT_ENDS;
            if (w.EndsWith("*")) return v ? TypeSubtypeMatcher.MODE_STARTS : TypeSubtypeMatcher.MODE_NOT_STARTS;
            return TypeSubtypeMatcher.MODE_EXACT;
        }

        public virtual bool Matches(string type, string subtype)
        {
            foreach (var x in checks)
            {
                if (x.Matches(type, subtype))
                {
                    return true;
                }
            }
            return false;
        }


        public virtual bool Matches(MyDefinitionId Id)
        {
            return Matches(Id.TypeId.ToString().Replace("MyObjectBuilder_", ""), Id.SubtypeName);
        }

        public class TypeSubtypeMatcher
        {
            public const int MODE_EXACT = 0;
            
            public const int MODE_STARTS = 1;
            public const int MODE_ENDS = 2;
            public const int MODE_CONTAINS = 3;

            public const int MODE_NOT_STARTS = 4;
            public const int MODE_NOT_ENDS = 5;
            public const int MODE_NOT_CONTAINS = 6;

            public const int MODE_ANY = 7;


            public int modeType = 0;
            public int modeSubType = 0;
            public string typeString = null;
            public string subtypeString = null;


            public TypeSubtypeMatcher(int modeType, string typeString, int modeSubType, string subtypeString)
            {
                this.modeType = modeType;
                this.typeString = typeString;
                this.modeSubType = modeSubType;
                this.subtypeString = subtypeString;
            }

            public bool MatchesType(string type)
            {
                switch (modeType)
                {
                    case MODE_EXACT:
                        if (type != typeString) return false;
                        break;
                    case MODE_STARTS:
                        if (!type.StartsWith(typeString)) return false;
                        break;
                    case MODE_NOT_STARTS:
                        if (type.StartsWith(typeString)) return false;
                        break;
                    case MODE_ENDS:
                        if (!type.EndsWith(typeString)) return false;
                        break;
                    case MODE_NOT_ENDS:
                        if (type.EndsWith(typeString)) return false;
                        break;
                    case MODE_CONTAINS:
                        if (!type.Contains(typeString)) return false;
                        break;
                    case MODE_NOT_CONTAINS:
                        if (type.Contains(typeString)) return false;
                        break;
                }

                return true;
            }
            
            public bool MatchesSubtype(string subtype)
            {
                switch (modeSubType)
                {
                    case MODE_EXACT:
                        if (subtype != subtypeString) return false;
                        break;
                    case MODE_STARTS:
                        if (!subtype.StartsWith(subtypeString)) return false;
                        break;
                    case MODE_NOT_STARTS:
                        if (subtype.StartsWith(subtypeString)) return false;
                        break;
                    case MODE_ENDS:
                        if (!subtype.EndsWith(subtypeString)) return false;
                        break;
                    case MODE_NOT_ENDS:
                        if (subtype.EndsWith(subtypeString)) return false;
                        break;
                    case MODE_CONTAINS:
                        if (!subtype.Contains(subtypeString)) return false;
                        break;
                    case MODE_NOT_CONTAINS:
                        if (subtype.Contains(subtypeString)) return false;
                        break;
                }

                return true;
            }

            public bool Matches(string type, string subtype)
            {
                return MatchesType(type) && MatchesSubtype(subtype);
            }

            public override string ToString()
            {
                return typeString + "/" + subtypeString + "[" + modeType + "/" + modeSubType + "]";
            }
        }
    }
}
