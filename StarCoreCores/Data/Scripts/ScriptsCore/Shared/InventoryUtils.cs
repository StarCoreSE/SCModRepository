using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using Sandbox.Game;
using VRageMath;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using static VRage.Game.ModAPI.Ingame.MyInventoryItemExtension;

//using VRage.Game.ModAPI.Ingame;

namespace ServerMod {
    static class InventoryUtils {

        // получает все предметы в инвентарях
        public static void GetInventoryItems(IMyCubeBlock block, Dictionary<MyPhysicalItemDefinition, double> dictionary, string type = "", string subtypeId = "", bool NamesByClient = false, bool IgnoreGarage = false) {
            if (block == null || !block.HasInventory) return;
            if (IgnoreGarage && block.SubtypeName().Contains("Garage")) return;

            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inventory = (MyInventory) block.GetInventory(i);
                var items = inventory.GetItems();

                foreach (var item in items)
                {
                    var _item = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.GetDefinitionId());

                    if ((string.IsNullOrWhiteSpace(type) || _item.Id.TypeId.ToString() == type) &&
                        _item.Id.SubtypeName.Contains(subtypeId))
                    {
                        double count = item.Amount.RawValue / 1000000d;

                        if (dictionary.ContainsKey(_item)) dictionary[_item] += count;
                        else dictionary.Add(_item, count);
                    }
                }
            }
        }

        public static void GetInventoryItems(IMyCubeBlock block, Dictionary<MyPhysicalItemDefinition, double> dictionary, string type = "", string subtypeId = "", bool IgnoreGarage = false) {
            if (block == null || !block.HasInventory) return;
            if (IgnoreGarage && block.SubtypeName().Contains("Garage")) return;

            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inventory = (MyInventory) block.GetInventory(i);
                var items = inventory.GetItems();

                foreach (var item in items)
                {
                    var _item = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.GetDefinitionId());

                    if ((string.IsNullOrWhiteSpace(type) || _item.Id.TypeId.ToString() == type) &&
                        _item.Id.SubtypeName.Contains(subtypeId))
                    {
                        double count = item.Amount.RawValue / 1000000d;

                        if (dictionary.ContainsKey(_item)) dictionary[_item] += count;
                        else dictionary.Add(_item, count);
                    }
                }
            }
        }

        // получает текущее компоненты в блоках
        public static void GetComponents(this IMySlimBlock block, IDictionary<MyPhysicalItemDefinition, double> dictionary) {
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;

            foreach (var component in components)
            {
                var name = component.DeconstructItem;
                int count = component.Count;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }

            var missingComponents = new Dictionary<MyPhysicalItemDefinition, double>();
            block.GetMissingComponentsItemsDefinitions(missingComponents);

            foreach (var component in missingComponents)
            {
                var item = component.Key;
                var count = component.Value;

                if (dictionary.ContainsKey(item)) dictionary[item] -= count;
                else dictionary.Add(item, count);
            }
        }
        public static void GetMissingComponentsItemsDefinitions(this IMySlimBlock block, IDictionary<MyPhysicalItemDefinition, double> dictionary) {

            var missingComponents = new Dictionary<string, int>();
            block.GetMissingComponents(missingComponents);

            foreach (var component in missingComponents) {
                var item = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(MyDefinitionId.Parse("MyObjectBuilder_Component/" + component.Key));
                int count = component.Value;

                if (dictionary.ContainsKey(item)) dictionary[item] -= count;
                else dictionary.Add(item, count);
            }
        }

        // получает все компоненты в блоках
        public static void GetTotalComponents(this IMySlimBlock block, Dictionary<MyPhysicalItemDefinition, double> dictionary) {
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;

            foreach (var component in components)
            {
                var name = component.DeconstructItem;

                int count = component.Count;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }
        }

        public static void GetComponentsTranslation(this IMySlimBlock block, Dictionary<string, string> dictionary)
        {
            var components = (block.BlockDefinition as MyCubeBlockDefinition).Components;

            foreach (var component in components)
            {
                var SubtypeName = component.Definition.Id.SubtypeName;
                var TextName = component.Definition.DisplayNameText;

                if (!dictionary.ContainsKey(SubtypeName)) dictionary.Add(SubtypeName, TextName);
            }

        }


        // получает объем компонентов в блоках
        public static float GetComponentsVolume(this IMySlimBlock block)

        {
            float total = 0f;
            foreach (var component in (block.BlockDefinition as MyCubeBlockDefinition).Components)
            {
                total += component.Definition.Volume;
            }
            return total;
        }



        // получает недостающие компоненты
        // IMySlimBlock.GetConstructionStockpileItemAmount() работает (работал) некорректно,
        // поэтому поиск через IMySlimBlock.GetMissingComponents()
        public static void GetMissingComponents(this IMySlimBlock block, Dictionary<string, double> dictionary, bool NamesByClient = false)
        {
            var missingComponents = new Dictionary<string, int>();
            block.GetMissingComponents(missingComponents);

            foreach (var component in missingComponents) {
                string name = component.Key;
                int count = component.Value;

                if (dictionary.ContainsKey(name)) dictionary[name] += count;
                else dictionary.Add(name, count);
            }
        }



        public static void GetAllCargosInRange (ref BoundingSphereD sphere2, List<MyEntity> cargos, string blockName, bool allowAssemblers = false, Func<IMyTerminalBlock, bool> filter = null, Func<IMyTerminalBlock, IMyTerminalBlock, int> sort = null, int maxTake = Int32.MaxValue) {
            var data = MyEntities.GetTopMostEntitiesInSphere(ref sphere2);
            var ships = new HashSet<MyCubeGrid>();
            foreach (var x in data) {
                var g = x as MyCubeGrid;
                if (g != null) {
                    ships.Add(g);
                }
            }
            data.Clear();

            var allBlocks = new List<IMyTerminalBlock>();
            var enters = new List<IMyTerminalBlock>();
            var sphere3 = sphere2;

            foreach (var x in ships) {
                x?.OverFatBlocks((b) => {
                    if (!(b is IMyCargoContainer) && !(b is IMyShipConnector) && !(allowAssemblers && b is IMyAssembler)) return;
                    var term = b as IMyTerminalBlock;
                    if (!term.IsFunctional) return;
                    if (term.CustomName.Contains(blockName)) return;
                    if (!term.HasLocalPlayerAccess()) return;
                    if (sphere3.Contains(term.WorldMatrix.Translation) == ContainmentType.Contains && !IsSeparated(term)) {
                        var add = true;
                        foreach (var e in enters) {
                            if (AreConnected(e, term)) {
                                add = false;
                                break;
                            }
                        }
                        if (add) enters.Add(term);
                    }

                    if (filter == null || filter.Invoke(term)) {
                        allBlocks.Add(term);
                    }
                });
            }

            if (sort != null) {
                allBlocks.Sort((a,b)=>sort.Invoke(a, b));
            }

            foreach (var x in allBlocks) {
                if (IsSeparated(x)) {
                    cargos.Add (x as MyEntity);
                    if (cargos.Count >= maxTake) return;
                } else {
                    foreach (var y in enters) {
                        if (AreConnected (x, y)) {
                            cargos.Add (x as MyEntity);
                            if (cargos.Count >= maxTake) return;
                            break;
                        }
                    }
                }
            }

        }

        public static void GetAllCargosInRangeSimple(ref BoundingSphereD sphere2, List<IMyCubeBlock> cargos, Func<IMyTerminalBlock, bool> filter = null)
        {
            var data = MyEntities.GetTopMostEntitiesInSphere(ref sphere2);
            var ships = new HashSet<MyCubeGrid>();
            foreach (var x in data)
            {
                var g = x as MyCubeGrid;
                if (g != null)
                {
                    ships.Add(g);
                }
            }
            data.Clear();

            var allBlocks = new List<IMyTerminalBlock>();
            var sphere3 = sphere2;

            foreach (var x in ships){
                x?.OverFatBlocks((b) => {
                    if (!(b is IMyCargoContainer) && !(b is IMyShipConnector)) return;
                    var term = b as IMyTerminalBlock;
                    if (!term.IsFunctional) return;
                    if (!term.HasLocalPlayerAccess()) return;
                    if (sphere3.Contains(term.WorldMatrix.Translation) != ContainmentType.Contains) return;

                    if (filter == null || filter.Invoke(term)) {
                        cargos.Add(b);
                    }
                });
            }

        }

        private static bool IsSeparated(IMyTerminalBlock a) {
            var sn = a.SlimBlock.BlockDefinition.Id.SubtypeName;
            if (sn.StartsWith("Freight")) return true;
            if (sn.Equals("LargeBlockLockerRoom") || sn.Equals("LargeBlockLockerRoomCorner") || sn.Equals("LargeBlockLockers")) return true;
            return false;
        }

        private static bool AreConnected (IMyTerminalBlock a, IMyTerminalBlock b) {
            //if (a.CubeGrid != b.CubeGrid) return false;
            if (a==b) return true;

            for (var x=0; x < a.InventoryCount; x++) {
                for (var y=0; y < b.InventoryCount; y++) {
                    if (a.GetInventory (x).IsConnectedTo (b.GetInventory(y))) {
                        return true;
                    }
                }
            }

            return false;
        }


        public static string CustomName (this IMyInventory x) {
            var term = (x.Owner as IMyTerminalBlock);
            if (term == null) return x.Owner.DisplayName;
            else return term.CustomName;
        }

        public static MyFixedPoint calculateCargoMass (this IMyCubeGrid grid) {
            MyFixedPoint mass = 0;
            grid.FindBlocks(x => {

                var fat = x.FatBlock;
                if (fat == null || !fat.HasInventory) return false;
                var i = fat.GetInventory();
                if (i == null) return false;

                mass += i.CurrentMass;
                return false;
            });

            return mass;
        }




        public static int GetItemIndexByID (this IMyInventory inv, uint itemId) {
            for (var index=0; index<inv.ItemCount; index++) {
                if (inv.GetItemAt (index).Value.ItemId == itemId) {
                    return index;
                }
            }
            return -1;
        }

        public static void MoveAllItemsFrom(this IMyInventory inventory, IMyInventory from, Func<VRage.Game.ModAPI.Ingame.MyInventoryItem, MyFixedPoint?> p = null, bool alwaysAtEnd = false) {
            for (var x=from.ItemCount-1; x>=0; x--) {
                var t = from.GetItemAt(x);
                MyFixedPoint? amount = p!= null ? p.Invoke(t.Value) : null;
                if (amount == null || amount > 0) {
                    from.TransferItemTo (inventory, x, checkConnection:false, amount:amount, targetItemIndex: (alwaysAtEnd ? (int?)inventory.ItemCount : null));
                }
            }
        }


        public static void PullRequest (this IMyInventory target, List<IMyInventory> from, Dictionary<MyDefinitionId, MyFixedPoint> what, bool alwaysAtEnd = false) {
            foreach (var x in from) {
                if (what.Count == 0) break;

                MoveAllItemsFrom (target, x, (i) => {
                    if (what.ContainsKey (i.Type)) {
                        var need = what[i.Type];
                        var have = i.Amount;


                        if (need > have) {
                            what[i.Type] = need - have;
                            return null;
                        } else {
                            what.Remove(i.Type);
                            return need;
                        }
                    }
                    return -1;
                }, alwaysAtEnd:alwaysAtEnd);
            }
        }

        public static void PushRequest (this IMyInventory target, List<IMyInventory> from, Dictionary<MyDefinitionId, MyFixedPoint> what, bool alwaysAtEnd = false) {
            foreach (var x in from) {
                if (what.Count == 0) break;

                MoveAllItemsFrom (x, target, (i) => {
                    if (what.ContainsKey (i.Type)) {
                        var need = what[i.Type];
                        var have = i.Amount;


                        if (need > have) {
                            what[i.Type] = need - have;
                            return null;
                        } else {
                            what.Remove(i.Type);
                            return need;
                        }
                    }
                    return -1;
                }, alwaysAtEnd:alwaysAtEnd);
            }
        }

        public static double CanProduce (this MyBlueprintDefinitionBase bp, Dictionary<MyDefinitionId, MyFixedPoint> items) {
            double can_produce_times = Double.MaxValue;
            foreach (var pre in bp.Prerequisites) {
                var have = items.GetOr (pre.Id, MyFixedPoint.Zero);
                var times = (double)have / (double)pre.Amount;
                if (times < can_produce_times) {
                    can_produce_times = times;
                    if (can_produce_times == 0) { return 0; }
                }
            }

            return can_produce_times;
        }


        public static bool ParseHumanDefinition (string type, string subtype, out MyDefinitionId id)
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
        public static string GetHumanName(MyDefinitionId id)
        {
            return id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "/" + id.SubtypeName;
        }
    }
}
