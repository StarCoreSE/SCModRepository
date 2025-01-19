using Havok;
using Sandbox.ModAPI;
using SC.SUGMA.Utilities;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameState
{
    public class DisconnectHandler : ComponentBase
    {
        private const bool FreezeAllGrids = false; //use this if you want to stop every grid instead of just the player who disconnects
        private const bool TeleportToSpawn = false;
        private const bool ReportProblem = true;

        public static DisconnectHandler I { get; private set; }

        private bool _active;
        private readonly HashSet<IMyPlayer> _trackedPlayers = new HashSet<IMyPlayer>();
        private readonly HashSet<IMyPlayer> _currentPlayers = new HashSet<IMyPlayer>();
        private readonly Dictionary<ulong, Sandbox.Game.Entities.MyCubeGrid> _missingPlayers = new Dictionary<ulong, Sandbox.Game.Entities.MyCubeGrid>();
        private readonly Dictionary<long, Vector3D> _spawnPositions = new Dictionary<long, Vector3D>();
        private readonly HashSet<Sandbox.Game.Entities.MyCubeGrid> _frozenGrids = new HashSet<Sandbox.Game.Entities.MyCubeGrid>();
        private readonly Dictionary<IMyPlayer, Action<IMyControllableEntity, IMyControllableEntity>> _playerEvent = new Dictionary<IMyPlayer, Action<IMyControllableEntity, IMyControllableEntity>>();
        private int _tick;

        private static readonly Dictionary<IMyPlayer, IMyControllableEntity> controlledEntityCache = new Dictionary<IMyPlayer, IMyControllableEntity>();

        public override void Init(string id)
        {
            base.Init(id);
            I = this;
        }

        public override void UpdateTick()
        {
            if (_tick++ % 10 != 0)
                return;

            MyAPIGateway.Players.GetPlayers(null, CollectPlayers);
            _trackedPlayers.RemoveWhere(RemoveMissingPlayers);
            _currentPlayers.Clear();
        }

        public override void Close()
        {
            I = null;
            _trackedPlayers.Clear();
            controlledEntityCache.Clear();
            foreach (var kvp in _playerEvent)
            {
                kvp.Key.Controller.ControlledEntityChanged -= kvp.Value;
            }
            _playerEvent.Clear();
            if (_active)
                Deactivate();
        }

        public void Activate()
        {
            _active = true;
            foreach (var player in _trackedPlayers)
            {
                var cockpit = player.Controller.ControlledEntity as IMyCockpit;
                if (cockpit == null)
                    continue;

                _spawnPositions[cockpit.CubeGrid.EntityId] = cockpit.CubeGrid.WorldMatrix.Translation;
            }
        }

        public void Deactivate()
        {
            _active = false;
            UnfreezeGrids();
            _frozenGrids.Clear();
            _spawnPositions.Clear();
        }

        public void ResolveProblem() //all players connected
        {
            if (ReportProblem)
                SUtils.ResolvedProblem();
            UnfreezeGrids();
        }

        public void UnfreezeGrids(bool unfreezeAll = false)
        {
            if (FreezeAllGrids || unfreezeAll)
            {
                foreach (var grid in _frozenGrids)
                {
                    grid.Immune = false;
                    grid.OnConvertToDynamic();
                }
                _frozenGrids.Clear();
            }
            else
            {
                foreach (var grid in _missingPlayers.Values)
                {
                    var group = grid.GetGridGroup(GridLinkTypeEnum.Physical);
                    foreach (var subgrid in group.GetGrids(new List<IMyCubeGrid>()))
                    {
                        ((Sandbox.Game.Entities.MyCubeGrid)subgrid).Immune = false;
                        ((Sandbox.Game.Entities.MyCubeGrid)subgrid).OnConvertToDynamic();
                    }
                }
            }
            _missingPlayers.Clear();
        }

        private bool CollectPlayers(IMyPlayer player)
        {
            _currentPlayers.Add(player);
            if (_trackedPlayers.Add(player))
            {
                PlayerJoined(player);
            }
            return false;
        }

        private bool RemoveMissingPlayers(IMyPlayer player)
        {
            if (_currentPlayers.Contains(player))
                return false;

            PlayerLeft(player);

            return true;
        }

        public bool FreezeGrids(IMyEntity entity)
        {
            var grid = entity as Sandbox.Game.Entities.MyCubeGrid;
            if (grid == null || !_frozenGrids.Add(grid) || grid.Physics == null)
                return false;

            grid.Immune = true;
            var physics = ((IMyEntity)grid).Physics;
            physics.ClearSpeed();
            grid.ConvertToStatic();
            return false;
        }

        private void PlayerJoined(IMyPlayer player)
        {
            //memory leaks are bad
            Action<IMyControllableEntity, IMyControllableEntity> action = (old, current) => ControlledEntityChanged(player, old, current);
            player.Controller.ControlledEntityChanged += action;
            _playerEvent[player] = action;

            if (!_active)
                return;
            Sandbox.Game.Entities.MyCubeGrid grid;
            if (_missingPlayers.TryGetValue(player.SteamUserId, out grid))
            {
                _missingPlayers.Remove(player.SteamUserId);
                if (_missingPlayers.Count == 0)
                    ResolveProblem();

                if (!FreezeAllGrids)
                {
                    var group = grid.GetGridGroup(GridLinkTypeEnum.Physical);
                    foreach (var subgrid in group.GetGrids(new List<IMyCubeGrid>()))
                    {
                        ((Sandbox.Game.Entities.MyCubeGrid)subgrid).Immune = false;
                        ((Sandbox.Game.Entities.MyCubeGrid)subgrid).OnConvertToDynamic();
                    }
                }
            }
        }

        private void PlayerLeft(IMyPlayer player)
        {
            //memory leaks are bad
            Action<IMyControllableEntity, IMyControllableEntity> action;
            if (_playerEvent.TryGetValue(player, out action))
            {
                player.Controller.ControlledEntityChanged -= action;
                _playerEvent.Remove(player);
            }

            if (!_active)
                return;

            //it should always be in controlledEntityCache
            var cockpit = (controlledEntityCache.GetValueOrDefault(player) ?? player.Controller.ControlledEntity) as IMyCockpit;
            controlledEntityCache.Remove(player);
            if (cockpit == null)
                return; //ignore players who aren't controlling grids

            var grid = (Sandbox.Game.Entities.MyCubeGrid)cockpit.CubeGrid;

            _missingPlayers[player.SteamUserId] = grid;

            //handle the disconnection
            if (ReportProblem && MyAPIGateway.Multiplayer?.IsServer == true)
            {
                //automatic "/sc problem {Player} has disconnected!"?
                SUtils.ReportProblem($"{player.DisplayName} has disconnected! ('/sc missing' to override)");
            }

            if (TeleportToSpawn)
            {
                //teleporting their grid back to their spawn?
                Vector3D spawnPosition;
                if (_spawnPositions.TryGetValue(grid.EntityId, out spawnPosition))
                {
                    var wm = grid.WorldMatrix;
                    wm.Translation = spawnPosition;
                    grid.Teleport(wm);
                }
            }

            //making their grid physicsless and invulnerable?
            if (!FreezeAllGrids)
            {
                var group = grid.GetGridGroup(GridLinkTypeEnum.Physical);

                foreach (var subgrid in group.GetGrids(new List<IMyCubeGrid>()))
                {
                    ((Sandbox.Game.Entities.MyCubeGrid)subgrid).Immune = true;
                    var physics = subgrid.Physics;
                    if (physics != null)
                    {
                        physics.ClearSpeed();
                        ((Sandbox.Game.Entities.MyCubeGrid)subgrid).ConvertToStatic();
                    }
                }


            }

            if (FreezeAllGrids)
            {
                //Freezing all grids in the world, setting them invulnerable and physicsless?
                MyAPIGateway.Entities.GetEntities(null, FreezeGrids);
            }
        }
        private static void ControlledEntityChanged(IMyPlayer player, IMyControllableEntity old, IMyControllableEntity current)
        {
            if (current != null)
                return;

            controlledEntityCache[player] = old;
        }
    }
}
