using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using ShipPoints.HeartNetworking;
using ShipPoints.HeartNetworking.Custom;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace ShipPoints.ShipTracking
{
    internal class TrackingManager
    {
        public static TrackingManager I;

        private static readonly string[] AutoTrackSubtypes =
        {
            "LargeFlightMovement",
            "RivalAIRemoteControlLarge"
        };

        private readonly HashSet<long> _queuedGridTracks = new HashSet<long>();

        public HashSet<IMyCubeGrid> AllGrids = new HashSet<IMyCubeGrid>();
        public Dictionary<IMyCubeGrid, ShipTracker> TrackedGrids = new Dictionary<IMyCubeGrid, ShipTracker>();

        private TrackingManager()
        {
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            foreach (var entity in entities)
                OnEntityAdd(entity);
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
        }

        private void Update()
        {
        }

        private void Unload()
        {
            AllGrids.Clear();
            foreach (var tracker in TrackedGrids.Values)
                tracker.DisposeHud();
            TrackedGrids.Clear();

            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid?.Physics == null)
                return;

            AllGrids.Add(grid);
            grid.GetBlocks(null, block =>
            {
                CheckAutotrack(block);
                return false;
            });

            if (_queuedGridTracks.Contains(grid.EntityId))
            {
                _queuedGridTracks.Remove(grid.EntityId);
                var tracker = new ShipTracker(grid);
                TrackedGrids.Add(grid, tracker);
            }
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid) || entity.Physics == null)
                return;
            var grid = (IMyCubeGrid)entity;

            AllGrids.Remove(grid);

            if (TrackedGrids.ContainsKey(grid))
            {
                TrackedGrids[grid].DisposeHud();
                TrackedGrids.Remove(grid);
            }

            _queuedGridTracks.Remove(grid.EntityId);
        }

        private long[] GetGridIds()
        {
            var gridIds = new List<long>();
            foreach (var grid in TrackedGrids.Keys) gridIds.Add(grid.EntityId);
            return gridIds.ToArray();
        }

        private void CheckAutotrack(IMySlimBlock block)
        {
            if (block.FatBlock == null ||
                !AutoTrackSubtypes.Contains(block.BlockDefinition.Id.SubtypeName))
                return;
            TrackGrid(block.CubeGrid, false);
        }

        #region Public Methods

        public static void Init()
        {
            I = new TrackingManager();
        }

        public static void UpdateAfterSimulation()
        {
            I?.Update();
        }

        public static void Close()
        {
            I?.Unload();
            I = null;
        }

        public void BulkTrackGrids(long[] gridIds)
        {
            Log.Info($"Receive bulk track request with {gridIds.Length} items!");
            var gridIds_List = new List<long>(gridIds);
            foreach (var grid in TrackedGrids.Keys.ToArray())
            {
                if (gridIds.Contains(grid.EntityId))
                {
                    gridIds_List.Remove(grid.EntityId);
                    continue;
                }

                UntrackGrid(grid, false);
            }

            foreach (var gridId in gridIds_List) TrackGrid(gridId, false);
        }

        public void TrackGrid(IMyCubeGrid grid, bool share = true)
        {
            if (!(((MyCubeGrid)grid)?.DestructibleBlocks ?? false) ||
                TrackedGrids.ContainsKey(grid)) // Ignore invulnerable and already tracked grids
                return;

            // Don't allow tracking grids that are already tracked in the group.
            var allAttachedGrids = new List<IMyCubeGrid>();
            grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(allAttachedGrids);
            foreach (var attachedGrid in allAttachedGrids)
                if (TrackedGrids.ContainsKey(attachedGrid))
                    return;

            var tracker = new ShipTracker(grid);
            TrackedGrids.Add(grid, tracker);

            if (!share)
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                ServerDoSync();
            }
            else
            {
                var packet = new TrackingSyncPacket(grid.EntityId, true);
                HeartNetwork.I.SendToServer(packet);
            }
        }

        public void TrackGrid(long gridId, bool share = true)
        {
            var grid = MyAPIGateway.Entities.GetEntityById(gridId) as IMyCubeGrid;
            if (grid == null)
            {
                _queuedGridTracks.Add(gridId);
                return;
            }

            TrackGrid(grid, share);
        }

        public void UntrackGrid(IMyCubeGrid grid, bool share = true)
        {
            // Untrack all grids in group.
            var allAttachedGrids = new List<IMyCubeGrid>();
            grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(allAttachedGrids);
            foreach (var attachedGrid in allAttachedGrids.Where(attachedGrid => TrackedGrids.ContainsKey(attachedGrid)))
            {
                TrackedGrids[attachedGrid].DisposeHud();
                TrackedGrids.Remove(attachedGrid);
            }

            if (!share)
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                ServerDoSync();
            }
            else
            {
                var packet = new TrackingSyncPacket(grid.EntityId, false);
                HeartNetwork.I.SendToServer(packet);
            }
        }

        public void UntrackGrid(long gridId, bool share = true)
        {
            var grid = MyAPIGateway.Entities.GetEntityById(gridId) as IMyCubeGrid;
            _queuedGridTracks.Remove(gridId);
            if (grid != null)
                UntrackGrid(grid, share);
        }

        public bool IsGridTracked(IMyCubeGrid grid)
        {
            var allAttachedGrids = new List<IMyCubeGrid>();
            grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(allAttachedGrids);
            foreach (var attachedGrid in allAttachedGrids.Where(attachedGrid => TrackedGrids.ContainsKey(attachedGrid)))
                return true;
            return false;
        }

        public void ServerDoSync()
        {
            var packet = new TrackingSyncPacket(GetGridIds());
            HeartNetwork.I.SendToEveryone(packet);
        }

        #endregion
    }
}