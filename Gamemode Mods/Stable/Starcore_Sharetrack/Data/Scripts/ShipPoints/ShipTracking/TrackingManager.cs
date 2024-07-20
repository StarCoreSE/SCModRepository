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
        private static readonly string[] AutoTrackSubtypes = { "LargeFlightMovement", "RivalAIRemoteControlLarge" };
        private readonly HashSet<long> _queuedGridTracks = new HashSet<long>();
        public HashSet<IMyCubeGrid> AllGrids = new HashSet<IMyCubeGrid>();
        public Dictionary<IMyCubeGrid, ShipTracker> TrackedGrids = new Dictionary<IMyCubeGrid, ShipTracker>();

        #region Public Actions

        public Action<IMyCubeGrid, bool> OnShipTracked;
        public Action<IMyCubeGrid, bool> OnShipAliveChanged;

        #endregion

        private bool _isTracking = false;

        private TrackingManager()
        {
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
        }

        public void StartTracking()
        {
            Log.Info("Starting grid tracking");
            _isTracking = true;
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            foreach (var entity in entities)
                ProcessEntity(entity);
        }

        private void ProcessEntity(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid?.Physics == null) return;

            AllGrids.Add(grid);
            grid.GetBlocks(null, block =>
            {
                CheckAutotrack(block);
                return false;
            });

            if (_queuedGridTracks.Contains(grid.EntityId))
            {
                _queuedGridTracks.Remove(grid.EntityId);
                if (!TrackedGrids.ContainsKey(grid))
                {
                    TrackGrid(grid, false);
                }
            }
        }

        private void Update()
        {
            if (!_isTracking) return;

            foreach (var tracker in TrackedGrids.Values)
            {
                tracker.UpdateAfterSim();
            }
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
            if (!_isTracking) return;
            ProcessEntity(entity);
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
            foreach (var grid in TrackedGrids.Keys)
                gridIds.Add(grid.EntityId);
            return gridIds.ToArray();
        }

        private void CheckAutotrack(IMySlimBlock block)
        {
            if (block == null)
            {
                Log.Error("CheckAutotrack called with null block");
                return;
            }
            if (block.FatBlock == null || !AutoTrackSubtypes.Contains(block.BlockDefinition.Id.SubtypeName))
                return;

            if (block.CubeGrid == null)
            {
                Log.Error($"Block {block.BlockDefinition.Id.SubtypeName} has null CubeGrid");
                return;
            }

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
            if (!_isTracking)
            {
                Log.Info($"Queuing bulk track request with {gridIds.Length} items!");
                _queuedGridTracks.UnionWith(gridIds);
                return;
            }

            Log.Info($"Processing bulk track request with {gridIds.Length} items!");
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
            foreach (var gridId in gridIds_List)
                TrackGrid(gridId, false);
        }

        public void TrackGrid(IMyCubeGrid grid, bool share = true)
        {
            if (!_isTracking)
            {
                _queuedGridTracks.Add(grid.EntityId);
                return;
            }

            if (grid == null)
            {
                Log.Error("TrackGrid called with null grid");
                return;
            }

            if (!(((MyCubeGrid)grid)?.DestructibleBlocks ?? false) || TrackedGrids.ContainsKey(grid))
                return;

            // Don't allow tracking grids that are already tracked in the group.
            var allAttachedGrids = new List<IMyCubeGrid>();
            var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Physical);
            if (gridGroup != null)
            {
                gridGroup.GetGrids(allAttachedGrids);
            }
            else
            {
                Log.Error($"Grid {grid.DisplayName} has no physical grid group");
                allAttachedGrids.Add(grid);
            }

            foreach (var attachedGrid in allAttachedGrids)
                if (TrackedGrids.ContainsKey(attachedGrid))
                    return;

            try
            {
                var tracker = new ShipTracker(grid);
                TrackedGrids[grid] = tracker;
                // Automatically added to tracked grid list
                Log.Info($"TrackGrid Tracked grid {grid.DisplayName}. Visible: true");
                OnShipTracked?.Invoke(grid, true);
            }
            catch (Exception ex)
            {
                Log.Error($"Error creating ShipTracker for grid {grid.DisplayName}: {ex}");
                return;
            }

            if (!share) return;
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
            if (!_isTracking)
            {
                _queuedGridTracks.Add(gridId);
                return;
            }

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
                OnShipTracked?.Invoke(attachedGrid, false);
            }

            if (!share) return;
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

        public long[] GetQueuedGridTracks()
        {
            return _queuedGridTracks.ToArray();
        }

        #endregion
    }
}
