using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using System.Collections.Generic;

namespace StealthSystem
{
    public partial class StealthSession
    {
        private void OnEntityCreate(MyEntity entity)
        {
            try
            {
                if (!Inited) lock (InitObj) Init();

                var planet = entity as MyPlanet;
                if (planet != null)
                    PlanetTemp.TryAdd(planet, byte.MaxValue); //More keen jank workarounds

                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    (grid as MyCubeGrid).AddedToScene += AddToStart => _startGrids.Add(grid);
                    return;
                }

                var upgrade = entity as IMyUpgradeModule;
                if (upgrade != null)
                {
                    var subtype = upgrade.BlockDefinition.SubtypeName;
                    if (Enforced && !DriveDefinitions.ContainsKey(subtype) && !SinkDefinitions.ContainsKey(subtype))
                        return;

                    (upgrade as MyCubeBlock).AddedToScene += AddToStart => _startBlocks.Add(upgrade);
                }

                if (!PbApiInited && IsServer && entity is IMyProgrammableBlock)
                {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() => API.PbInit());
                    PbApiInited = true;
                }
            }
            catch (Exception ex)
            {
                Logs.WriteLine($"Exception in EntityCreate: {entity.GetType()} - {ex}");
            }

        }

        private void OnGridClose(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;

            if (GridMap.ContainsKey(grid))
            {
                var comp = GridMap[grid];
                GridMap.Remove(grid);
                GridList.Remove(comp);

                comp.Clean();
                _gridCompPool.Push(comp);
            }
            else Logs.WriteLine("OnGridClose() - grid not in map!!!");
        }

        private void OnCloseAll()
        {
            try
            {
                var list = new List<IMyGridGroupData>(GridGroupMap.Keys);
                foreach (var value in list)
                    GridGroupsOnOnGridGroupDestroyed(value);

                MyAPIGateway.GridGroups.OnGridGroupDestroyed -= GridGroupsOnOnGridGroupDestroyed;
                MyAPIGateway.GridGroups.OnGridGroupCreated -= GridGroupsOnOnGridGroupCreated;

                GridGroupMap.Clear();
            }
            catch (Exception ex)
            {
                Logs.WriteLine($"Exception in CloseAll: {ex}");
            }

        }

        private void GridGroupsOnOnGridGroupCreated(IMyGridGroupData groupData)
        {
            if (groupData.LinkType != GridLinkTypeEnum.Physical)
                return;

            var map = _groupMapPool.Count > 0 ? _groupMapPool.Pop() : new GroupMap();
            map.Init(groupData, this);

            //groupData.OnReleased += map.OnReleased;
            groupData.OnGridAdded += map.OnGridAdded;
            groupData.OnGridRemoved += map.OnGridRemoved;
            GridGroupMap[groupData] = map;
        }

        private void GridGroupsOnOnGridGroupDestroyed(IMyGridGroupData groupData)
        {
            if (groupData.LinkType != GridLinkTypeEnum.Physical)
                return;

            GroupMap map;
            if (GridGroupMap.TryGetValue(groupData, out map))
            {
                //groupData.OnReleased -= map.OnReleased;
                groupData.OnGridAdded -= map.OnGridAdded;
                groupData.OnGridRemoved -= map.OnGridRemoved;

                GridGroupMap.Remove(groupData);
                map.Clean();
                _groupMapPool.Push(map);
            }
            else
                Logs.WriteLine($"GridGroupsOnOnGridGroupDestroyed could not find map");
        }

        private void PlayerConnected(long id)
        {
            try
            {
                MyAPIGateway.Multiplayer.Players.GetPlayers(null, myPlayer => FindPlayer(myPlayer, id));
            }
            catch (Exception ex) { Logs.WriteLine($"Exception in PlayerConnected: {ex}"); }
        }

        private bool FindPlayer(IMyPlayer player, long id)
        {
            if (player.IdentityId == id)
            {
                var packet = new SettingsPacket { EntityId = 0, Settings = ConfigSettings.Config, Type = PacketType.Settings };
                SendPacketToClient(packet, player.SteamUserId);
            }
            return false;
        }

        internal void AfterDamageApplied(object target, MyDamageInformation info)
        {
            if (!DisableWeapons && RevealOnDamage) //Reveal grid on dealing damage
            {
                var ent = MyEntities.GetEntityById(info.AttackerId);
                if (!(ent is MyCubeBlock)) return;

                var attackingGrid = (ent as IMyCubeBlock).CubeGrid;
                if (attackingGrid == null) return;

                if (!StealthedGrids.Contains(attackingGrid))
                    return;

                GridComp gridCompA;
                if (!GridMap.TryGetValue(attackingGrid, out gridCompA))
                {
                    Logs.WriteLine("Attacking grid not mapped in damage handler");
                    return;
                }

                gridCompA.Revealed = true;
            }

            if (!TrackDamage) return;

            if (info.AttackerId == 0 || !(target is IMySlimBlock))
                return;

            var targetGrid = (target as IMySlimBlock).CubeGrid;

            if (targetGrid == null || !StealthedGrids.Contains(targetGrid)) return;

            GridComp gridComp;
            if (!GridMap.TryGetValue(targetGrid, out gridComp))
            {
                Logs.WriteLine("Grid not mapped in damage handler");
                return;
            }

            gridComp.DamageTaken += (int)info.Amount;
        }
    }
}
