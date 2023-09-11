using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using Digi;
using MIG.Shared.CSharp;
using Sandbox.ModAPI.Weapons;

namespace MIG.Shared.SE {
    static class Relations {
        public static Dictionary<IMyPlayer, int> GetOnlinePlayers (this IMyFaction faction, Dictionary<IMyPlayer, int> set,  List<IMyPlayer> pl = null) {
            if (pl == null) {
                pl = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers (pl, null);
            }

            foreach (IMyPlayer x in pl) {
                foreach (var y in faction.Members) {
                    if (y.Key == x.IdentityId) {
                        set.Set (x, y.Value.IsFounder ? 2 : y.Value.IsLeader ? 1 : 0);
                    }
                }
            }

            return set;
        }



        public static long FindBot(String name) {
            var y = MyAPIGateway.Multiplayer.Players;
            var pl = new List<IMyIdentity>();
            y.GetAllIdentites(pl, x =>{ return x.DisplayName.Equals(name); });
            if (pl.Count == 1) {
                return pl[0].IdentityId;
            } else return 0L;
        }

        public static bool IsFriend(this MyDamageInformation damage, IMySlimBlock block) {
            return damage.GetRelation (block) == 1;
        }

        public static int GetRelation(this MyDamageInformation damage, IMySlimBlock block) {
            var dealer = GetDamageDealer (damage);
            if (dealer == 0) return 0;

            if (block.OwnerId != 0) return block.CubeGrid.GetRelation(dealer);
            else return block.GetRelationToBuilder(dealer);
        }

        public static bool IsByHandGrinder (this MyDamageInformation damage) {
            var attacker = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
            var hnd = attacker as IMyEngineerToolBase;
            if (hnd != null) return hnd.DefinitionId.SubtypeName.Contains("Grinder");
            var hnd2 = attacker as IMyHandheldGunObject<MyDeviceBase>;
            if (hnd2 != null) return hnd2.DefinitionId.SubtypeName.Contains("Grinder");

            return false;
        }

        public static long GetDamageDealer (this MyDamageInformation damage) {
            var attacker = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
            var hnd = attacker as IMyEngineerToolBase;
            if (hnd != null) return hnd.GetToolOwner();
            var hnd2 = attacker as IMyHandheldGunObject<MyDeviceBase>;
            if (hnd2 != null) return hnd2.GetToolOwner();
            var pl = attacker as IMyCharacter;
            if (pl != null)  return Other.FindPlayerByCharacterId(pl.EntityId);
            var cb = attacker as IMySlimBlock;
            if (cb != null) return cb.OwnerId != 0 ? cb.OwnerId : cb.BuiltBy;
            var cb2 = attacker as IMyCubeBlock;
            if (cb2 != null) return cb2.OwnerId != 0 ? cb2.OwnerId : cb2.BuiltBy();

            return 0;
        }
        
        

        public static void OwnGrid(this IMyCubeGrid y, long transferTo, MyOwnershipShareModeEnum shareOptions) {
            y.ChangeGridOwnership(transferTo, shareOptions);
        }

        public static void OwnBlocks(this IMyCubeGrid y, long transferTo, MyOwnershipShareModeEnum shareOptions, Func<IMySlimBlock, bool> apply) { //MyOwnershipShareModeEnum.None
            var blocks = new List<IMySlimBlock>();
            y.GetBlocks(blocks);

            Log.Info("OwnBlocks:" + y.DisplayName + " : " + blocks.Count);

            foreach (var b in blocks) {
                var fat = b.FatBlock;
                if (fat != null) {
                    var own = apply(b);
                    if (own) {
                        if (fat is MyCubeBlock) {
                            (fat as MyCubeBlock).ChangeOwner(transferTo, shareOptions);
                            Log.Info("Change ownership:" + blocks.Count + " " + b.OwnerId + " " + " " + b.GetType().Name + " " + fat.GetType().Name + " " + (b is MyCubeBlock));
                        }
                    }
                }
            }
        }

        public static void OwnBlock(this IMySlimBlock b, long transferTo, MyOwnershipShareModeEnum shareOptions) {
            var fat = b.FatBlock;
            if (fat != null && fat is MyCubeBlock) {
                (fat as MyCubeBlock).ChangeOwner(transferTo, shareOptions);
            }
        }
    }
}
