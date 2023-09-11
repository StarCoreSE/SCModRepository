using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Digi;
using MIG.Shared.SE;
using MIG.SpecCores;
using VRage.Game.ModAPI;
using VRageMath;

namespace MIG.Shared.CSharp {

    static class ToStr
    {
        public static string printContent<T>(this List<T> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append(x).Append(",\n");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string printContent(this List<IMyPlayer> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append(x.DisplayName  + "/" + x.PlayerID).Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string printContent(this List<IMyFaction> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append(x.Name + "/" +x.FactionId).Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }


        public static string printContent(this List<MyProductionQueueItem> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append("{").Append(x.ItemId).Append("/").Append(x.Amount).Append("/").Append(x.Blueprint).Append("},\n");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static Dictionary<K, V> Copy<K,V>(this Dictionary<K,V> dict)
        {
            return new Dictionary<K, V>(dict);
        }
    }

    static class SharpUtils {

        [Obsolete("Use Sharp.msTimeStamp")]
        public static long msTimeStamp () {
            return (long)(DateTime.UtcNow.Subtract(utcZero)).TotalMilliseconds;
        }

        public static DateTime utcZero = new DateTime(1970, 1, 1);
        public static DateTime y2020 = new DateTime(2020, 1, 1);

        public static double Degree (Vector3D v1, Vector3D v2)
        {
            return Math.Acos(v1.Dot(v2) / (v1.Length() * v2.Length())).toDegree();
        }

        public static double Degree2 (Vector3D v1, Vector3D v2)
        {
            var d = Degree(v1, v2);

            if ((v1+v2).LengthSquared () < (v1 - v2).LengthSquared())
            {
                d*=-1;
            }
            return d;
        }

        public static long timeStamp () {
            return (long)(DateTime.UtcNow.Subtract(utcZero)).TotalSeconds;
        }

        public static long timeUtcDif()
        {
            return Math.Abs((long)DateTime.UtcNow.Subtract(DateTime.Now).TotalSeconds);
        }



        public static TimeSpan StripMilliseconds(this TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }


        public static void AddOrRemove<T>(this HashSet<T> set, T data, bool add) {
            if (add) { set.Add(data); } else { set.Remove(data);  }
        }

        public static void RemoveWhere<K,V>(this Dictionary<K,V> set, Func<K,V,bool> filter)
        {
            var list = new List<K>();
            foreach (var t in set)
            {
                if (filter(t.Key, t.Value))
                {
                    list.Add(t.Key);
                }
            }

            foreach (var t in list)
            {
                set.Remove(t);
            }
        }

        public static string Print<T, K>(this IDictionary<T, K> dict, string separator = "\n", Func<T,K, bool> Where = null) {
            StringBuilder sb = new StringBuilder();
            sb.Append("Dict[");
            foreach (var x in dict) {
                if (Where != null && !Where.Invoke(x.Key, x.Value)) continue;
                sb.Append(x.Key).Append("->").Append(x.Value).Append(separator);
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string Print<T>(this List<T> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in list)
            {
                if (x == null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append(x);
                }
                sb.Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }




        public static string toHumanWeight (this double num) {
            if (num <0.000001) return String.Format ("{0:N2} µg", num *1000000000);
            if (num <0.001) return String.Format ("{0:N2} mg", num *1000000);
            if (num <1) return String.Format ("{0:N2} g", num *1000);
            if (num <1000) return String.Format ("{0:N2} kg", num);
            if (num <1000000) return String.Format ("{0:N2} t", num /1000);
            if (num <1000000000) return String.Format ("{0:N2} kt", num /1000000);
            if (num <1000000000000) return String.Format ("{0:N2} Mt", num /1000000000);
            if (num <1000000000000000) return String.Format ("{0:N2} Gt", num /1000000000000);
            return "TONS";
        }


        public static string toHumanWeight2 (this double num)
        {
            if (num <1000) return String.Format(CultureInfo.InvariantCulture, "{0:N0} Kg", num);
            if (num <1000000) return String.Format (CultureInfo.InvariantCulture, "{0:N1} Ton", num / 1000).Replace(".0", "");
            if (num <1000000000) return String.Format (CultureInfo.InvariantCulture, "{0:N1} kTon", num /1000000).Replace(".0", "");
            if (num <1000000000000) return String.Format (CultureInfo.InvariantCulture, "{0:N1} MTon", num /1000000000).Replace(".0", "");
            if (num <1000000000000000) return String.Format (CultureInfo.InvariantCulture, "{0:N1} GTon", num /1000000000000).Replace(".0", "");
            return "TONS";
        }

        public static string toHumanQuantity (this double num) {
            if (num <1000) return String.Format ("{0:N2}", num);
            if (num <1000000) return String.Format ("{0:N2} K", num /1000);
            if (num <1000000000) return String.Format ("{0:N2} M", num /1000000);
            return "TONS";
        }

        public static string toPhysicQuantity (this double num, String s) {
            var k = 1000d;
            if (num <k) return String.Format ("{0:N2} ", num)+s;
            if (num <k*k) return String.Format ("{0:N2} k", num / k)+s;
            if (num <k*k*k) return String.Format ("{0:N2} M", num /(k*k))+s;
            if (num <k*k*k*k) return String.Format ("{0:N2} G", num / (k*k*k))+s;
            if (num <k*k*k*k*k) return String.Format ("{0:N2} T", num / (k*k*k*k))+s;
            return String.Format ("{0:N2} P", num / (k*k*k*k*k));
        }

        public static string toHumanQuantityCeiled (this double num) {
            if (num <1000) return String.Format ("{0:N0}", num);
            if (num <1000000) return String.Format ("{0:N2} K", num /1000);
            if (num <1000000000) return String.Format ("{0:N2} M", num /1000000);
            if (num <1000000000) return String.Format ("{0:N2} B", num /1000000);
            return "TONS";
        }

        public static string toHumanQuantityEnergy (this double num)
        {
            if (Math.Abs(num) > 1000) return $"{num / 1000 :N0} GW";
            if (Math.Abs(num) > 1) return $"{num :N2} MW";
            if (Math.Abs(num) > 0.001) return $"{num * 1000 :N0} KW";
            return $"{num * 1000000 :N0} W";
        }

        public static string toHumanQuantityVolume (this double num)
        {
            if (Math.Abs(num) > 1000000000) return $"{num / 1000000000 :N2} GL";
            if (Math.Abs(num) > 1000000) return $"{num / 1000000 :N2} ML";
            if (Math.Abs(num) > 1000) return $"{num / 1000 :N2} kL";
            if (Math.Abs(num) > 100) return $"{num / 100 :N2} hL";
            if (Math.Abs(num) > 10) return $"{num / 10 :N2} daL";
            if (Math.Abs(num) > 1) return $"{num :N2} L";
            if (Math.Abs(num) > 0.1) return $"{num * 10 :N2} dL";
            if (Math.Abs(num) > 0.01) return $"{num * 100 :N2} cL";
            return $"{num * 1000 :N2} mL";
        }

        public static string toPercentage (this double num) {
            return String.Format ("{0:N2}%", num*100);
        }

        public static string toMlt (this double num) {
            return String.Format ("{0:N2}", num);
        }

        public static string toHumanTime (this double num) {
            if (num <120) return String.Format ("{0:N0} s", num);
            if (num <3600) return String.Format ("{0:N0} min", num /60);
            if (num <3600*24) return String.Format ("{0:N0} h", num /3600);
            return String.Format ("{0:N0} days", num/3600/24);
        }
        public static string toHumanTime2 (this int num, bool isFullWithDays = false) {
            if (num < 60) return num + "s";
            if (num < 3600) return num / 60 + "m " + num % 60 + "s";
            if (num < 3600 * 24) return num / 3600 + "h " + (num / 60) % 60 + "m " + num % 60 + "s";
            if (num / 3600 / 24 == 1) return isFullWithDays ? "1 day " + num / 3600 + "h " + (num / 60) % 60 + "m " + num % 60 + "s" : "1 day";
            return isFullWithDays ? num / 3600 / 24 + " days " + num / 3600 % 24 + "h " + (num / 60) % 60 + "m " + num % 60 + "s" : num / 3600 / 24 + " days";
        }


        public static string Print<K,V> (this IDictionary<K,V> dict, Func<K,V, string> printer, string separator = "\r\n")
        {
            var s = new StringBuilder();
            int c = 0;
            foreach (var kv in dict)
            {
                s.Append(printer (kv.Key, kv.Value));
                if (c != dict.Count-1)
                {
                    s.Append(separator);
                }
            }

            return s.ToString();
        }
        


        public static bool IsOneKeyMoreThan<T> (this IDictionary<T, int> buffer, IDictionary<T, int> maxLimits)
        {
            foreach (var y in buffer)
            {
                if (y.Value > maxLimits[y.Key])
                {
                    return true;
                }
            }
            return false;
        }





        public static Action<A> TryCatch<A>(this Action<A> func, string debugName)
        {
            return (a) =>
            {
                try
                {
                    func.Invoke(a);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                }
            };
        }

        public static Action<A,B> TryCatch<A,B>(this Action<A,B> func, string debugName)
        {
            return (a,b) =>
            {
                try
                {
                    func.Invoke(a,b);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                }
            };
        }

        public static Action<A,B,C> TryCatch<A,B,C>(this Action<A,B,C> func, string debugName)
        {
            return (a,b,c) =>
            {
                try
                {
                    func.Invoke(a,b,c);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                }
            };
        }

        public static Action<A,B,C,D> TryCatch<A,B,C,D>(this Action<A,B,C,D> func, string debugName)
        {
            return (a,b,c,d) =>
            {
                try
                {
                    func.Invoke(a,b,c,d);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                }
            };
        }

        public static Func<A, R> TryCatch<A,R>(this Func<A, R> func, string debugName)
        {
            return (a) =>
            {
                try
                {
                    return func.Invoke(a);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                    return default(R);
                }
            };
        }


        public static Func<A, B, R> TryCatch<A,B,R>(this Func<A, B, R> func, string debugName)
        {
            return (a,b) =>
            {
                try
                {
                    return func.Invoke(a,b);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                    return default(R);
                }
            };
        }

        public static Func<A, B, C, R> TryCatch<A,B,C,R>(this Func<A, B,C, R> func, string debugName)
        {
            return (a,b,c) =>
            {
                try
                {
                    return func.Invoke(a,b,c);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                    return default(R);
                }
            };
        }

        public static Func<A, B,C,D, R> TryCatch<A,B,C,D,R>(this Func<A, B,C,D, R> func, string debugName)
        {
            return (a,b,c,d) =>
            {
                try
                {
                    return func.Invoke(a,b,c,d);
                }
                catch (Exception e)
                {
                    Log.ChatError(debugName, e);
                    return default(R);
                }
            };
        }

        
        
        public static bool GetLess<T> (this IDictionary<T, float> current, IDictionary<T, float> maxLimits, Dictionary<T, bool> shouldPunish)
        {
            bool has = false;
            foreach (var y in current)
            {
                try
                {
                    var max = maxLimits[y.Key];
                    var temp = max < y.Value;
                    has |= temp;
                    shouldPunish[y.Key] = temp;
                }
                catch (Exception e)
                {
                    Log.ChatError($"GetLess: Missing Point id: {y.Key} [{current.Print()}]/[{maxLimits.Print()}] {e}");
                }

            }
            return has;
        }

        public static bool IsOneKeyMoreThan<T> (this IDictionary<T, float> buffer, IDictionary<T, float> maxLimits)
        {
            foreach (var y in buffer)
            {
                try
                {
                    if (y.Value > maxLimits[y.Key])
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.ChatError($"IsOneKeyMoreThan: Missing Point id {y.Key}: Buffer:{buffer.Print()}\r\nMax:{maxLimits.Print()}  {e}");
                }
            }
            return false;
        }
        
        public static bool IsOneKeyMoreThan<T> (this IDictionary<T, float> buffer, IDictionary<T, float> extraBuffer, IDictionary<T, float> maxLimits)
        {
            foreach (var y in buffer)
            {
                var v = extraBuffer.GetOr(y.Key, 0);
                if (v <= 0) continue;

                try
                {
                    if (y.Value + v > maxLimits[y.Key])
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.ChatError($"IsOneKeyMoreThan2: Missing Point id: {y.Key} {e}");
                }

            }
            return false;
        }

        public static StringBuilder Append(this StringBuilder sb, IMyPlayer player, IMyFaction faction) {
            sb.Append (player.DisplayName);
            if (faction != null) {
                sb.Append("[").Append(faction.Tag).Append("]");
            }

            return sb;
        }

        public static StringBuilder Append(this StringBuilder sb, IMyIdentity player, IMyFaction faction) {
            sb.Append (player.DisplayName);
            if (faction != null) {
                sb.Append("[").Append(faction.Tag).Append("]");
            }

            return sb;
        }

        public static K Set<T, K>(this Dictionary<T, K> dict, T t, K k) {
            K old = default(K);
            if (dict.ContainsKey(t)) {
                old = dict[t];
                dict.Remove(t);
            }
            dict.Add(t, k);
            return old;
        }

        public static int DayOfWeek (DateTime time) //Saturday = 6, Sunday = 7
        {
            var utcZero = new DateTime(1970, 1, 1);
            if (time < utcZero) return 1;
            var d = (y2020 - utcZero).TotalDays;
            var dd = (int)d + d%1>0 ? 1 : 0;
            dd = dd- (dd / 7)*7 + 4; //1970 was Thursday
            if (dd > 7) dd -=7;
            return dd;
        }
    }
}
