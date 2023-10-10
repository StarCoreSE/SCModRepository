using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Sandbox.Definitions;
using VRage;
using VRage.Game;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.ObjectBuilders;

namespace MIG.Shared.SE {

    public static class NAPI {
        private const int FREEZE_FLAG = 4;
        public static bool isFrozen(this IMyEntity grid) { return ((int)grid.Flags | FREEZE_FLAG) == (int)grid.Flags; }
        public static void setFrozen(this IMyEntity grid) { grid.Flags = grid.Flags | (EntityFlags)FREEZE_FLAG; }
        public static void setUnFrozen(this IMyEntity e) { e.Flags &= ~(EntityFlags)FREEZE_FLAG; }



        public static Vector3D GetWorldPosition (this IMySlimBlock block) {
            var box = new BoundingBoxD ();
            block.GetWorldBoundingBox(out box);
            return box.Center;
        }

        [Obsolete("Use IsDedicated instead")]
        public static bool isTorchServer (this IMySession session) {
            return MyAPIGateway.Utilities.IsDedicated;
        }

        public static bool IsDedicated (this IMySession session) {
            return MyAPIGateway.Utilities.IsDedicated;
        }

        public static string SubtypeName (this IMyCubeBlock block) {
            return block.SlimBlock.BlockDefinition.Id.SubtypeName;
        }

        public static T Definition<T>(this MyObjectBuilder_CubeBlock ob) where T : MyCubeBlockDefinition
        {
            return (T)MyDefinitionManager.Static.GetCubeBlockDefinition(ob);
        }

        public static MyCubeBlockDefinition Definition(this MyObjectBuilder_CubeBlock ob)
        {
            return MyDefinitionManager.Static.GetCubeBlockDefinition(ob);
        }

        public static string GetHumanName(this MyDefinitionId id)
        {
            return id.ToString().Replace("MyObjectBuilder_", "");
        }

        public static string GetHumanInfo(this Dictionary<MyDefinitionId, double> kv)
        {
            var sb = new StringBuilder();
            foreach (var kv2 in kv)
            {
                sb.Append($" {kv2.Value}x{kv2.Key.GetHumanName()} ");
            }
            return sb.ToString();
        }


        public static long BuiltBy(this IMyCubeBlock block) {
            return block.SlimBlock.BuiltBy;
        }

        public static bool IsSameFactionLeader(this IMyCubeBlock block)
        {
            var builtBy = block.BuiltBy();
            var f = builtBy.PlayerFaction();
            var pl = MyAPIGateway.Session.LocalHumanPlayer;
            var my = pl.GetFaction();
            if (f != my) return false;
            if (my == null) return pl.IdentityId == builtBy;
            return my.IsLeader(pl.IdentityId) || my.IsFounder(pl.IdentityId);
        }

        public static T As<T>(this long entityId) {
            IMyEntity entity;
            if (!MyAPIGateway.Entities.TryGetEntityById(entityId, out entity)) return default(T);
            if (entity is T) { return (T) entity; } else { return default(T); }
        }

        public static T GetAs <T> (this IMyEntity entity) where T : MyGameLogicComponent {
            var logic = entity.GameLogic;
            var t = logic as T;
            if (t != null) return t;
            var cmp = logic as MyCompositeGameLogicComponent;
            return cmp?.GetAs<T>();
        }

        public static void FindFatBlocks<T>(this IMyCubeGrid grid, List<T> blocks, Func<IMyCubeBlock, bool> filter) {
            var gg = grid as MyCubeGrid;
            var ff = gg.GetFatBlocks();
            foreach (var x in ff) {
                var fat = (IMyCubeBlock) x;
                if (filter(fat)) { blocks.Add((T) fat); }
            }
        }

        public static void OverFatBlocks(this IMyCubeGrid grid, Action<IMyCubeBlock> action) {
            var gg = grid as MyCubeGrid;
            var ff = gg.GetFatBlocks();
            foreach (var x in ff) {
                var fat = (IMyCubeBlock) x;
                action(fat);
            }
        }

        public static List<IMyCubeGrid> GetConnectedGrids(this IMyCubeGrid grid, GridLinkTypeEnum with, List<IMyCubeGrid> list = null, bool clear = false) {
            if (list == null) list = new List<IMyCubeGrid>();
            if (clear) list.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, with, list);
            return list;
        }

        public static IMyFaction PlayerFaction(this long playerId) { return MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId); }

        public static IMyPlayer GetPlayer(this IMyCharacter character) { return MyAPIGateway.Players.GetPlayerControllingEntity(character); } //CAN BE NULL IF IN COCKPIT
        public static IMyPlayer GetPlayer(this IMyShipController cockpit) { return MyAPIGateway.Players.GetPlayerControllingEntity(cockpit); }
        public static IMyPlayer GetPlayer(this IMyIdentity Identity) {
            IMyPlayer player = null;
            MyAPIGateway.Players.GetPlayers(null, (x) => {
                if (x.IdentityId == Identity.IdentityId)
                {
                    player = x;
                }
                return false;
            });
            return player;
        }

        public static IMyIdentity GetIdentity(this long playerId)
        {
            IMyIdentity ident = null;
            MyAPIGateway.Players.GetAllIdentites(null, (x)=>
            {
                if (playerId == x.IdentityId)
                {
                    ident = x;
                }
                return false;
            });
            return ident;
        }

        public static void SetPower(this IMyCubeBlock cubeBlock, float power)
        {
            cubeBlock?.ResourceSink?.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, power);
            cubeBlock?.ResourceSink?.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, power);
        }

        public static float CurrentPowerInput(this IMySlimBlock block)
        {
            var fat = block.FatBlock;
            if (fat == null) return 0;
            return fat.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
        }

        public static float MaxRequiredPowerInput(this IMySlimBlock block)
        {
            var fat = block.FatBlock;
            if (fat == null) return 0;
            return fat.ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId);
        }

        public static float CurrentPowerInput(this IMyCubeBlock fat)
        {
            if (fat == null) return 0;
            return fat.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
        }

        public static float MaxRequiredPowerInput(this IMyCubeBlock fat)
        {
            if (fat == null) return 0;
            return fat.ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId);
        }

        public static bool IsOnline (this IMyPlayerCollection players, long identity)
        {
            bool contains = false;
            players.GetPlayers (null, (x)=>{
                if (x.IdentityId == identity)
                {
                    contains = true;
                }
                return false;
            });

            return contains;
        }

        public static bool IsControllingCockpit (this IMyShipController cockpit)
        {
            if (cockpit.IsMainCockpit)
            {
                return true;
            }
            else if (cockpit.ControllerInfo != null && cockpit.ControllerInfo.Controller != null && cockpit.ControllerInfo.Controller.ControlledEntity != null)
            {
                return true;
            }

            return false;
        }

        public static bool IsMainControlledCockpit(this IMyShipController cockpit)
        {
            return cockpit.ControllerInfo != null && cockpit.ControllerInfo.Controller != null && cockpit.ControllerInfo.Controller.ControlledEntity != null;
        }

        public static IMyCubeGrid GetMyControlledGrid(this IMySession session) {
            var cock = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyCockpit;
            if (cock == null) return null;
            return cock.CubeGrid;
        }

        public static IMyFaction Faction(this long factionId)
        {
            return MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
        }

		public static bool isBot (this IMyIdentity identity)
		{
			return MyAPIGateway.Players.TryGetSteamId(identity.IdentityId) == 0;
		}

        public static bool IsUserAdmin(this ulong SteamUserId)
        {
            return MyAPIGateway.Session.IsUserAdmin(SteamUserId);
        }

        public static MyPromoteLevel PromoteLevel (this ulong SteamUserId)
        {
            var PlayersList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(PlayersList);
            foreach (var Player in PlayersList.Where(Player => Player.SteamUserId == SteamUserId))
            {
                return Player.PromoteLevel;
            }
            return MyPromoteLevel.None;
        }

        public static IMyIdentity Identity (this ulong SteamUserId)
        {
            IMyIdentity identity = null;
            MyAPIGateway.Multiplayer.Players.GetAllIdentites(null, (x)=>
            {
                if (identity != null) return false;
                var st = MyAPIGateway.Multiplayer.Players.TryGetSteamId(x.IdentityId);
                if (st == SteamUserId)
                {
                    identity = x;
                }
                return false;
            });
            return identity;
        }

        public static long IdentityId (this ulong SteamUserId)
        {
            return SteamUserId.Identity()?.IdentityId ?? 0;
        }

        public static BoundingBoxD GetAABB (this List<IMyCubeGrid> grids)
        {
            var aabb1 = grids[0].PositionComp.WorldAABB;
            BoundingBoxD aabb = new BoundingBoxD(aabb1.Min, aabb1.Max);
            for (var x=1; x<grids.Count; x++)
            {
                aabb.Include (grids[x].PositionComp.WorldAABB);
            }
            return aabb;
        }



        public static bool IsSameFaction(this long playerId, long player2Id) {
            if (playerId == player2Id) return true;

            var f1 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            var f2 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player2Id);

            if (f1 == f2) { return f1 != null; }

            return false;
        }

        public static Vector3D GetWorldPosition(this IMyCubeBlock slim) { return slim.CubeGrid.GridIntegerToWorld(slim.Position); }

        public static BoundingBoxD GetWorldAABB(this IMyCubeBlock slim) {
            var cb = slim.CubeGrid as MyCubeGrid;
            return new BoundingBoxD(slim.Min * cb.GridSize - cb.GridSizeHalfVector, slim.Max * cb.GridSize + cb.GridSizeHalfVector).TransformFast(cb.PositionComp.WorldMatrix);
        }

        public static Vector3D GetLocalCoordinates(this IMyCubeGrid grid, Vector3 worldCoordinates)
        {
            var matInv = grid.PositionComp.WorldMatrixNormalizedInv;
            Vector3D result;
            Vector3D.Transform(ref worldCoordinates, ref matInv, out result);
            return result;
        }

        public static List<IMyEntity> GetEntitiesInSphere (this IMyEntities entities, Vector3D pos, double radius, Func<IMyEntity, bool> filter = null) {
            var sphere = new BoundingSphereD(pos, radius);
            var list = entities.GetEntitiesInSphere(ref sphere);
            if (filter != null) { list.RemoveAll((x)=>!filter(x)); }
            return list;
        }


        public static void SendMessageToOthersProto(this IMyMultiplayer multi, ushort id, object o, bool reliable = true) {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(o);
            multi.SendMessageToOthers(id, bytes, reliable);
        }

        // Calculates how much components are welded for block
        public static void GetRealComponentsCost(this IMySlimBlock block, Dictionary<string, int> dictionary, Dictionary<string, int> temp = null)
        {
            if (temp == null) temp = new Dictionary<string, int>();
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;
            foreach (var component in components)
            {
                string name = component.Definition.Id.SubtypeName;
                int count = component.Count;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }

            temp.Clear();
            block.GetMissingComponents(temp);

            foreach (var component in temp)
            {
                string name = component.Key;
                int count = component.Value;

                if (dictionary.ContainsKey(name)) dictionary[name] -= count;
                else dictionary.Add(name, count);
            }
        }

        public static bool ParseHumanDefinition(string type, string subtype, out MyDefinitionId id)
        {
            if (type == "i" || type == "I")
            {
                type = "Ingot";
            }
            else if (type == "o" || type == "O")
            {
                type = "Ore";
            }
            else if (type == "c" || type == "C")
            {
                type = "Component";
            }
            return MyDefinitionId.TryParse("MyObjectBuilder_" + type + "/" + subtype, out id);
        }

        public static ListReader<MyCubeBlock> GetFatBlocks (this IMyCubeGrid grid)
        {
           return ((MyCubeGrid)grid).GetFatBlocks();
        }
    }

    public static class RandomSugar {
        public static Vector3 NextVector(this Random random, float x, float y, float z)
        {
            x = x * (float)(2*random.NextDouble()-1d);
            y = y * (float)(2*random.NextDouble()-1d);
            z = z * (float)(2*random.NextDouble()-1d);
            return new Vector3(x, y, z);
        }

        public static double NextDouble(this Random random, double min, double max)
        {
            return min + (max - min) * random.NextDouble();
        }

        public static float NextFloat(this Random random, double min, double max)
        {
            return (float)(min + (max - min) * random.NextDouble());
        }

        public static T Next<T> (this Random random, IEnumerable<T> array)
        {
            if (array.Count() == 0) return default (T);
            return array.ElementAt(random.Next()%array.Count());
        }

        public static T NextWithChance<T>(this Random random, List<T> array, Func<T, float> func, bool returnLastAsDefault = false)
        {
            if (array.Count == 0) return default(T);
            for (int x=0; x<array.Count; x++)
            {
                var a = array[x];
                var ch = func(a);
                if (random.NextDouble() <= ch)
                {
                    return a;
                }
            }

            return returnLastAsDefault ? array[array.Count-1] : default (T);
        }
    }

    public static class Serialization {
        public static MyModStorageComponentBase GetOrCreateStorage(this IMyEntity entity) { return entity.Storage = entity.Storage ?? new MyModStorageComponent(); }
        public static bool HasStorage(this IMyEntity entity) { return entity.Storage != null; }

        public static T FromBase64Binary<T>(this string d)
        {
            var data = Convert.FromBase64String(d);
            return MyAPIGateway.Utilities.SerializeFromBinary<T>(data);
        }

        public static string ToBase64Binary<T>(this T data)
        {
            var s = MyAPIGateway.Utilities.SerializeToBinary<T>(data);
            return Convert.ToBase64String(s);
        }

        public static bool TryGetStorageData<T>(this IMyEntity entity, Guid guid, out T value, bool protoBuf = false)
        {
            if (entity.Storage == null)
            {
                value = default(T);
                return false;
            }
            else
            {
                var d = entity.GetStorageData(guid);
                if (d == null)
                {
                    value = default(T);
                    return false;
                }

                try
                {
                    value = protoBuf ? FromBase64Binary<T>(d) : MyAPIGateway.Utilities.SerializeFromXML<T>(d);
                    return true;
                }
                catch (Exception e)
                {
                    value = default(T);
                    return false;
                }
            }
        }

        public static string GetStorageData(this IMyEntity entity, Guid guid) {
            if (entity.Storage == null) return null;
            string data;
            if (entity.Storage.TryGetValue(guid, out data)) {
                return data;
            } else {
                return null;
            }
        }

        public static T GetStorageData<T>(this IMyEntity entity, Guid guid, bool protoBuf = false)
        {
            T data;
            if (TryGetStorageData(entity, guid, out data, protoBuf))
            {
                return data;
            }

            return default(T);
        }

        public static string GetAndSetStorageData(this IMyEntity entity, Guid guid, string newData) {
            var data = GetStorageData(entity, guid);
            SetStorageData(entity, guid, newData);
            return data;
        }

        public static void SetStorageData(this IMyEntity entity, Guid guid, String data) {
            if (entity.Storage == null && data == null) {
                return;
            }
            entity.GetOrCreateStorage().SetValue(guid, data);
        }

        public static void SetStorageData<T>(this IMyEntity entity, Guid guid, T data, bool protoBuf = false)
        {
            if (data == null)
            {
                SetStorageData(entity, guid, null);
                return;
            }

            var s = protoBuf ? ToBase64Binary(data) : MyAPIGateway.Utilities.SerializeToXML(data);
            SetStorageData(entity, guid, s);
        }
    }


    public static class Sharp {
        private static Regex R = new Regex("{([^}]*)}");
        public static string Replace(this string pattern, Func<string, string> replacer)
        {
            string s = pattern;
            while (true)
            {
                var news = R.Replace(s, match =>
                {
                    return replacer(match.Groups[1].Value);
                });

                if (news == s)
                {
                    return news;
                }
                else
                {
                    s = news;
                }
            }
        }
        
        public static int NextExcept(this Random r, HashSet<int> ignored, int max) {
            while (true) {
                var n = r.Next(max);
                if (!ignored.Contains(n)) { return n; }
            }
        }

        public static string[] ToStrings(this string s, params string[] by)
        {
           return s.Split(by, StringSplitOptions.RemoveEmptyEntries);
        }
        
        public static int[] ToInts(this string s)
        {
            if (string.IsNullOrEmpty(s)) return new int[0];
            var ints = s.Split(new string[] { ",", " "}, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<int>();

            int res;
            foreach (var f in ints)
            {
                if (int.TryParse(f, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
                {
                    list.Add(res);
                }
            }
            return list.ToArray();
        }
        
        public static long[] ToLongs(this string s)
        {
            if (string.IsNullOrEmpty(s)) return new long[0];
            var ints = s.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<long>();

            long res;
            foreach (var f in ints)
            {
                if (long.TryParse(f, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
                {
                    list.Add(res);
                }
            }
            return list.ToArray();
        }
        
        private static float[] toFloats(this string s)
        {
            if (string.IsNullOrEmpty(s)) return new float[0];
            var floats = s.Split(new string[]{",", " "}, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<float>();

            float res;
            foreach (var f in floats)
            {
                if (float.TryParse(f, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
                {
                    list.Add(res);
                }
            }
            return list.ToArray();
        }

        public static DateTime utcZero = new DateTime(1970, 1, 1);
        public static DateTime y2020 = new DateTime(2020, 1, 1);

        public static float Lerp2 (float Current, float Desired, float Speed)
        {
            if (Current < Desired)
            {
                Current += Speed;
                if (Current > Desired)
                {
                    Current = Desired;
                }
            }
            else
            {
                Current -= Speed;
                if (Current < Desired)
                {
                    Current = Desired;
                }
            }
            return Current;
        }

        public static float toRadian (this float v)
        {
            return (float)(v * Math.PI / 180d);
        }

        public static double toRadian(this double v)
        {
            return (v * Math.PI / 180d);
        }

        public static float toDegree (this float v)
        {
            return (float)(v / Math.PI * 180d);
        }

        public static double toDegree (this double v)
        {
            return (v / Math.PI * 180d);
        }


        public static K GetOr<T, K>(this IDictionary<T, K> dict, T t, K k) {
            if (dict.ContainsKey(t)) {
                return dict[t];
            } else {
                return k;
            }
        }

        public static K GetOrNew<T, K>(this IDictionary<T, K> dict, T t) where K : new() {
            if (!dict.ContainsKey(t))
            {
                var k = new K();
                dict[t] = k;
                return dict[t];
            }
            else
            {
                return dict[t];
            }
        }

        public static bool HasFlags(this int x, int f)
        {
            return (x | f) == x;
        }

        public static long msTimeStamp () {
            return (long)(DateTime.UtcNow.Subtract(utcZero)).TotalMilliseconds;
        }

        public static string fixZero (this double num) {
            return String.Format ("{0:N2}", num);
        }

        public static string fixZero(this float num)
        {
            return String.Format("{0:N2}", num);
        }

        public static T ElementAtOrLast<T>(this T[] array, int at)
        {
            var min = Math.Min(at, array.Length-1);
            return array[min];
        }

        public static T ElementAtOrLast<T>(this IList<T> array, int at)
        {
            var min = Math.Min(at, array.Count-1);
            return array[min];
        }

        public static void Add<K, V>(this IDictionary<K, V> dict1, KeyValuePair<K, V> kv)
        {
            dict1.Add(kv.Key, kv.Value);
        }

        
        public static void Mlt <T> (this IDictionary<T,float> dict, IDictionary<T,float> dict2, bool onlyIfContains = false) {
            foreach (var kv in dict2)
            {
                dict.Mlt(kv.Key, kv.Value, onlyIfContains);
            }
        }
        
        public static void Mlt <T> (this IDictionary<T,float> dict, T key, float value, bool onlyIfContains = false) {
            if (!dict.ContainsKey(key)) {
                if (!onlyIfContains)
                {
                    dict[key] = value;
                }
            } else {
                dict[key] = dict[key] * value;
            }
        }

        public static void Sum <T> (this IDictionary<T,double> dict, T key, double value) {
            if (!dict.ContainsKey(key)) {
                dict[key] = value;
            } else {
                dict[key] = dict[key] + value;
            }
        }

        public static void Sum <T> (this IDictionary<T,float> dict, T key, float value) {
            if (!dict.ContainsKey(key)) {
                dict[key] = value;
            } else {
                dict[key] = dict[key] + value;
            }
        }

        public static void Sum <T> (this IDictionary<T,MyFixedPoint> dict, T key, MyFixedPoint value) {
            if (!dict.ContainsKey(key)) {
                dict[key] = value;
            } else {
                dict[key] = dict[key] + value;
            }
        }

        public static void Sum <T> (this IDictionary<T,int> dict, T key, int value) {
            if (!dict.ContainsKey(key)) {
                dict[key] = value;
            } else {
                dict[key] = dict[key] + value;
            }
        }

        public static void Sum <T> (this IDictionary<T,float> dict, IDictionary<T,float> dict2) {
            foreach (var d in dict2)
            {
                dict.Sum(d.Key, d.Value);
            }
        }

        public static void SetValues<T, K> (this IDictionary<T, K> dict, IDictionary<T, K> value)
        {
            foreach (var y in value)
            {
                dict[y.Key] = y.Value;
            }
        }

        public static bool Remove<K, V>(this IDictionary<K, V> dict1, K key, V value)
        {
            if (dict1.ContainsKeyValue(key, value))
            {
                dict1.Remove(key);
                return true;
            }

            return false;
        }

        public static bool ContainsKeyValue<K, V>(this IDictionary<K, V> dict1, K key, V value)
        {
            V value2;
            if (dict1.TryGetValue(key, out value2))
            {
                return value.Equals(value2);
            }

            return false;
        }

        public static bool ContainsKeyValue<K, V>(this IDictionary<K, V> dict1, KeyValuePair<K,V> kv)
        {
            return ContainsKeyValue(dict1, kv.Key, kv.Value);
        }


        public static void Minus<T>(this IDictionary<T, MyFixedPoint> x, IDictionary<T, MyFixedPoint> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] -= other[i.Key];
                }
            }
        }

        public static void Plus<T>(this IDictionary<T, MyFixedPoint> x, IDictionary<T, MyFixedPoint> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] += other[i.Key];
                } else {
                    x.Add(i.Key, other[i.Key]);
                }
            }
        }

        public static void  Minus<T>(this IDictionary<T, int> x, IDictionary<T, int> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] -= other[i.Key];
                } else
                {
                    x[i.Key] = -other[i.Key];
                }
            }
        }

        
        
        public static void Minus<T>(this IDictionary<T, float> x, IDictionary<T, float> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] -= other[i.Key];
                }
                else
                {
                    x[i.Key] = -other[i.Key];
                }
            }
        }

        public static void Plus<T>(this IDictionary<T, int> x, IDictionary<T, int> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] += other[i.Key];
                } else {
                    x.Add(i.Key, other[i.Key]);
                }
            }
        }

        public static void Plus<T>(this IDictionary<T, float> x, IDictionary<T, float> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] += other[i.Key];
                } else {
                    x.Add(i.Key, other[i.Key]);
                }
            }
        }

        public static void PlusIfContains<T>(this IDictionary<T, float> x, IDictionary<T, float> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] += other[i.Key];
                }
            }
        }

        public static void MinusIfContains<T>(this IDictionary<T, float> x, IDictionary<T, float> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] -= other[i.Key];
                }
            }
        }

        public static void Plus<T>(this IDictionary<T, int> x,T otherKey, int otherValue) {
            if (x.ContainsKey(otherKey)) {
                x[otherKey] += otherValue;
            } else {
                x.Add(otherKey, otherValue);
            }
        }


        public static void Max<T>(this IDictionary<T, float> x, IDictionary<T, float> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] = Math.Max(x[i.Key], other[i.Key]);
                } else {
                    x[i.Key] = other[i.Key];
                }
            }
        }

        public static void MaxIfContains<T>(this IDictionary<T, float> x, IDictionary<T, float> other) {
            foreach (var i in other) {
                if (x.ContainsKey(i.Key)) {
                    x[i.Key] = Math.Max(x[i.Key], other[i.Key]);
                }
            }
        }

        public static bool ContainsOneOfKeys<T, K>(this IDictionary<T, K> x, ICollection<T> collection) {
            foreach (var t in collection)
            {
                if (x.ContainsKey(t))
                {
                    return true;
                }
            }

            return false;
        }
        
        public static void SumIfNotContains<K,V>(this Dictionary<K, V> values, Dictionary<K, V> values2)
        {
            foreach (var u in values2)
            {
                if (values.ContainsKey(u.Key))
                {
                    values[u.Key] = u.Value;
                }
            }
        }

        public static Dictionary<K, int> SumDuplicates<K>(this ICollection<K> values)
        {
            var dict = new Dictionary<K, int>();
            foreach (var u in values)
            {
                dict.Sum(u, 1);
            }

            return dict;
        }

        public static void RemoveDuplicates<K, V>(this IDictionary<K, V> dict1, IDictionary<K, V> dict2)
        {
            foreach (var kv in dict2)
            {
                dict1.Remove(kv.Key, kv.Value);
            }
        }
        
        public static void RemoveDuplicateKeys<K, V1, V2>(this IDictionary<K, V1> dict1, IDictionary<K, V2> dict2)
        {
            foreach (var kv in dict2)
            {
                if (dict1.ContainsKey(kv.Key))
                {
                    dict1.Remove(kv.Key);
                }
            }
        }

        public static D GetDuplicates<K, V, D>(this IDictionary<K, V> dict1, IDictionary<K, V> dict2) where D : Dictionary<K, V>, new()
        {
            var dict3 = new D();
            foreach (var kv in dict1)
            {
                if (dict2.ContainsKeyValue(kv))
                {
                    dict3.Add(kv);
                }
            }

            return dict3;
        }

        public static void RemoveDuplicatesBoth<K, V>(this Dictionary<K, V> dict1, Dictionary<K, V> dict2)
        {
            var list = new List<K>();
            foreach (var kv in dict2)
            {
                if (dict1.Remove(kv.Key, kv.Value))
                {
                    list.Add(kv.Key);
                }
            }

            foreach (var k in list)
            {
                dict2.Remove(k);
            }
        }
    }

    public static class Bytes {
        public static int Pack(this byte[] bytes, int pos, int what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            return 4;
        }

        public static int Pack(this byte[] bytes, int pos, byte what) {
            bytes[pos] = what;
            return 1;
        }

        public static int Pack(this byte[] bytes, int pos, uint what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            return 4;
        }
		public static int Pack(this byte[] bytes, int pos, float what)
		{
			var b1 = BitConverter.GetBytes(what);
			bytes[pos + 0] = b1[0];
			bytes[pos + 1] = b1[1];
			bytes[pos + 2] = b1[2];
			bytes[pos + 3] = b1[3];
			return 4;
		}

		public static int Pack(this byte[] bytes, int pos, long what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            bytes[pos + 4] = b1[4];
            bytes[pos + 5] = b1[5];
            bytes[pos + 6] = b1[6];
            bytes[pos + 7] = b1[7];
            return 8;
        }

        public static int Pack(this byte[] bytes, int pos, ulong what) {
            var b1 = BitConverter.GetBytes(what);
            bytes[pos + 0] = b1[0];
            bytes[pos + 1] = b1[1];
            bytes[pos + 2] = b1[2];
            bytes[pos + 3] = b1[3];
            bytes[pos + 4] = b1[4];
            bytes[pos + 5] = b1[5];
            bytes[pos + 6] = b1[6];
            bytes[pos + 7] = b1[7];
            return 8;
        }

        public static long Long(this byte[] bytes, int pos) { return BitConverter.ToInt64(bytes, pos); }
        public static ulong ULong(this byte[] bytes, int pos) { return BitConverter.ToUInt64(bytes, pos); }
        public static double Double(this byte[] bytes, int pos) { return BitConverter.ToDouble(bytes, pos); }
        public static int Int(this byte[] bytes, int pos) { return BitConverter.ToInt32(bytes, pos); }
        public static uint UInt(this byte[] bytes, int pos) { return BitConverter.ToUInt32(bytes, pos); }
        public static float Float(this byte[] bytes, int pos) { return BitConverter.ToSingle(bytes, pos); }
        public static short Short(this byte[] bytes, int pos) { return BitConverter.ToInt16(bytes, pos); }
        public static ushort UShort(this byte[] bytes, int pos) { return BitConverter.ToUInt16(bytes, pos); }
    }


    public static class NapiRelations
    {
        public const int MEMBERSHIP_NO_FACTION = -2;
        public const int MEMBERSHIP_NOT_MEMBER = -1;
        public const int MEMBERSHIP_APPLICANT = 0;
        public const int MEMBERSHIP_MEMBER = 1;
        public const int MEMBERSHIP_LEADER = 2;
        public const int MEMBERSHIP_FOUNDER = 3;

        public static IMyFaction GetBuilderFaction (this IMyCubeBlock block) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (block.BuiltBy());
        }

        public static IMyFaction GetFaction (this IMyPlayer pl) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (pl.IdentityId);
        }
        public static IMyFaction GetFaction (long pl) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (pl);
        }
        public static IMyFaction GetFaction (this IMyIdentity pl) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction (pl.IdentityId);
        }

        public static int GetRelation(this long u1, long u2) {
            return MyIDModule.GetRelationPlayerPlayer(u1, u2).AsNumber();
        }

        public static int GetRelationToBuilder(this IMySlimBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId, block.BuiltBy, MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        public static int GetRelationToOwnerOrBuilder(this IMySlimBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId,  block.OwnerId != 0 ? block.OwnerId : block.BuiltBy, MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        public static int GetRelationToOwnerOrBuilder(this IMyCubeBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId,  block.OwnerId != 0 ? block.OwnerId : block.BuiltBy(), MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        public static long GetOwnerOrBuilder (this IMySlimBlock block) {
            return block.OwnerId != 0 ? block.OwnerId : block.BuiltBy;
        }

        public static long GetOwnerOrBuilder (this IMyCubeBlock block) {
            return block.OwnerId != 0 ? block.OwnerId : block.BuiltBy();
        }

        public static int GetRelation(this IMyCubeGrid cubeGrid, long userId) {
            return GetUserRelation(cubeGrid, userId).AsNumber();
        }

        public static int AsNumber(this MyRelationsBetweenPlayerAndBlock relation) {
            if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare) {
                return 1;
            } else if (relation == MyRelationsBetweenPlayerAndBlock.Enemies) {
                return -1;
            } else return 0;
        }

        public static int AsNumber(this MyRelationsBetweenPlayers relation) {
            if (relation == MyRelationsBetweenPlayers.Self) return 2;
            if (relation == MyRelationsBetweenPlayers.Allies) return 1;
            if (relation == MyRelationsBetweenPlayers.Enemies) return -1;
            return 0;
        }

        public static bool IsEnemy(this IMyCubeGrid grid, long userId) {
            return grid.GetUserRelation(userId) == MyRelationsBetweenPlayerAndBlock.Enemies;
        }

        public static bool IsEnemy(this IMyCharacter u, long userId) {
            return MyIDModule.GetRelationPlayerBlock(u.EntityId, userId, MyOwnershipShareModeEnum.Faction) == MyRelationsBetweenPlayerAndBlock.Enemies;
        }

        public static MyRelationsBetweenPlayerAndBlock GetUserRelation(this IMyCubeGrid cubeGrid, long userId) {
            var enemies = false;
            var neutral = false;
            try {
                foreach (var key in cubeGrid.BigOwners) {

                    var owner = MyAPIGateway.Entities.GetEntityById(key);
                    //Log.Info("Owner:" + owner);

                    var relation = MyIDModule.GetRelationPlayerBlock(key, userId, MyOwnershipShareModeEnum.Faction);
                    if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare) {
                        return relation;
                    } else if (relation == MyRelationsBetweenPlayerAndBlock.Enemies) {
                        enemies = true;
                    } else if (relation == MyRelationsBetweenPlayerAndBlock.Neutral) {
                        neutral = true;
                    }
                }
            } catch {
                //The list BigOwners could change while iterating -> a silent catch
            }
            if (enemies) return MyRelationsBetweenPlayerAndBlock.Enemies;
            if (neutral) return MyRelationsBetweenPlayerAndBlock.Neutral;
            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }

        public static int GetRelation(this IMySlimBlock block, long userId) {
            return MyIDModule.GetRelationPlayerBlock(userId, block.OwnerId, MyOwnershipShareModeEnum.Faction).AsNumber();
        }

        public static bool IsOwnedByFactionLeader (this IMyCubeBlock block) {
            if (block.OwnerId == block.BuiltBy()) {
                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction (block.BuiltBy());
                if (faction != null) {
                    return faction.FounderId == block.BuiltBy();
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }

        public static bool IsFactionLeaderOrFounder (this long user) {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction (user);
            if (faction != null) {
                return faction.GetMemberShip (user) > 1;
            }
            return false;
        }

        public static int GetFactionMemberShip (this long user) {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction (user);
            if (faction != null) {
                return faction.GetMemberShip (user);
            }
            return MEMBERSHIP_NO_FACTION;
        }
        public static int GetMemberShip (this IMyFaction faction, long user) {
            if (faction.FounderId == user) return MEMBERSHIP_FOUNDER;
            foreach (var x in faction.Members) {
                if (x.Key == user) {
                    if (x.Value.IsLeader) return MEMBERSHIP_LEADER;
                    return 1;
                }
            }

            if (faction.JoinRequests.ContainsKey(user)) return MEMBERSHIP_APPLICANT;
            return MEMBERSHIP_NOT_MEMBER;
        }
    }

    public static class InventoryUt
    {
        public static Dictionary<MyDefinitionId, int> GetBlockPrice (this IMySlimBlock slim, Dictionary<MyDefinitionId, int> dict = null) {
            if (dict == null) dict = new Dictionary<MyDefinitionId, int>();

            var cmps = (slim.BlockDefinition as MyCubeBlockDefinition).Components;

            foreach (var xx in cmps) {
                var id = xx.Definition.Id;
                var c = xx.Count;
                if (dict.ContainsKey(id)) {
                    dict[id] += c;
                } else {
                    dict.Add (id, c);
                }
            }

            return dict;
        }

        public static Dictionary<MyDefinitionId, int> GetBlockLeftNeededComponents (this IMySlimBlock slim, Dictionary<MyDefinitionId, int> dict = null, Dictionary<MyDefinitionId, int> temp = null) {
            if (dict == null) dict = new Dictionary<MyDefinitionId, int>();

            var cmps = (slim.BlockDefinition as MyCubeBlockDefinition).Components;

            temp.Clear();
            foreach (var xx in cmps) {
                var id = xx.Definition.Id;
                var c = xx.Count;
                if (temp.ContainsKey(id)) {
                    temp[id] += c;
                } else {
                    temp.Add (id, c);
                }
            }

            foreach (var x in temp) {
                var id = x.Key;
                var has = slim.GetConstructionStockpileItemAmount (id);
                var need = x.Value;
                var left = need - has;
                if (left > 0) {
                    if (dict.ContainsKey(id)) {
                        dict[id] += left;
                    } else {
                        dict.Add (id, left);
                    }
                }
            }

            return dict;
        }

        public static MyFixedPoint GetLeftVolume(this IMyInventory inventory) {
            return inventory.MaxVolume-inventory.CurrentVolume;
        }

        public static double GetLeftVolumeInLiters(this IMyInventory inventory) {
            return ((double)inventory.GetLeftVolume())*1000d;
        }

        public static double GetFilledRatio(this IMyInventory inventory) {
            return (double)inventory.CurrentVolume / (double)inventory.MaxVolume;
        }

        public static Dictionary<MyDefinitionId, MyFixedPoint> CountItems(this IMyInventory inventory, Dictionary<MyDefinitionId, VRage.MyFixedPoint> d = null) {
            var items = inventory.GetItems();

            if (d == null) {
                d = new Dictionary<MyDefinitionId, MyFixedPoint>();
            }

            foreach (var x in items) {
                var id = x.Content.GetId();
                if (!d.ContainsKey(id)) {
                    d.Add(x.Content.GetId(), x.Amount);
                } else {
                    d[id] += x.Amount;
                }

            }

            return d;
        }

        /// <summary>
        /// Adds items to many invetories, modifies `items` param, leaving info about how much items wasn't spawned
        /// </summary>
        /// <param name="inventories"></param>
        /// <param name="items"></param>
        public static void AddItems(this List<IMyInventory> inventories, Dictionary<MyDefinitionId, double> items)
        {
            var keys = new List<MyDefinitionId>(items.Keys);
            var zero = (MyFixedPoint)0.0001;

            if (inventories.Count == 0 || keys.Count == 0) return;

            foreach (var y in keys)
            {
                foreach (var x in inventories)
                {
                    if (!items.ContainsKey(y)) continue;
                    if (!x.CanItemsBeAdded(zero, y)) continue;


                    var amount = items[y];
                    var am = ((MyInventoryBase)x).ComputeAmountThatFits(y);
                    if (am >= (MyFixedPoint)amount)
                    {
                        x.AddItem(y, amount);
                        items.Remove(y);
                        break;
                    }
                    else
                    {
                        x.AddItem(y, am);
                        items[y] = amount - (double)am;
                    }
                }
            }
        }

        public static Dictionary<MyDefinitionId, double> CountItemsD(this IMyInventory inventory, Dictionary<MyDefinitionId, double> d = null)
        {
            var items = inventory.GetItems();

            if (d == null)
            {
                d = new Dictionary<MyDefinitionId, double>();
            }

            foreach (var x in items)
            {
                var id = x.Content.GetId();
                if (!d.ContainsKey(id))
                {
                    d.Add(x.Content.GetId(), (double)x.Amount);
                }
                else
                {
                    d[id] += (double)x.Amount;
                }

            }

            return d;
        }

        public static void AddItem(this IMyInventory inv, MyDefinitionId id, double amount) {
            inv.AddItems((MyFixedPoint)amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
        }
        public static void AddItem(this IMyInventory inv, MyDefinitionId id, MyFixedPoint amount) {
            inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
        }

        public static bool RemoveAmount(this IMyInventory inv, Dictionary<MyDefinitionId, MyFixedPoint> toRemove)
        {
            if (toRemove == null || toRemove.Count == 0) return false;

            var items = inv.GetItems();
            var l = items.Count;
            var k = 0;

            for (var i = 0; i < l; i++)
            {
                var itm = items[i];
                var id = itm.Content.GetId();
                if (toRemove.ContainsKey(id))
                {
                    var am = toRemove[id];
                    if (itm.Amount <= am)
                    {
                        am -= itm.Amount;
                        toRemove[id] = am;
                        inv.RemoveItemsAt(i - k);
                        k++;
                    }
                    else
                    {
                        toRemove.Remove(id);
                        inv.RemoveItemAmount(itm, am);
                    }
                }
            }

            return toRemove.Count == 0;
        }

        public static bool RemoveAmount(this IMyInventory inv, Dictionary<MyDefinitionId, double> toRemove)
        {
            if (toRemove == null || toRemove.Count == 0) return false;

            var items = inv.GetItems();
            var l = items.Count;
            var k = 0;

            for (var i = 0; i < l; i++)
            {
                var itm = items[i];
                var id = itm.Content.GetId();
                if (toRemove.ContainsKey(id))
                {
                    var am = toRemove[id];
                    if ((double)itm.Amount <= am)
                    {
                        am -= (double)itm.Amount;
                        toRemove[id] = am;
                        inv.RemoveItemsAt(i - k);
                        k++;
                    }
                    else
                    {
                        toRemove.Remove(id);
                        inv.RemoveItemAmount(itm, (MyFixedPoint)am);
                    }
                }
            }

            return toRemove.Count == 0;
        }

        public static MyFixedPoint RemoveAmount (this IMyInventory inv, MyDefinitionId id, double amount) {
            if (amount <= 0) return (MyFixedPoint)amount;

            var items = inv.GetItems();
            var l = items.Count;
            var k = 0;
            var am = (MyFixedPoint)amount;
            for (var i = 0; i<l; i++) {
                var itm = items[i];
                if (itm.Content.GetId() == id) {
                    if (itm.Amount <= am) {
                        am -= itm.Amount;
                        inv.RemoveItemsAt(i-k);
                        k++;
                    } else {
                        inv.RemoveItemAmount(itm, am);
                        return 0;
                    }
                }
            }

            return am;
        }
    }
}