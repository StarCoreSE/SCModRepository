using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using StarCore.ShareTrack.HeartNetworking;
using StarCore.ShareTrack.HeartNetworking.Custom;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace StarCore.ShareTrack.ShipTracking
{
    internal class TrackingManager
    {
        public static TrackingManager I;
        private static readonly string[] AutoTrackSubtypes = { "LargeFlightMovement", "RivalAIRemoteControlLarge" };
        public bool EnableAutotrack = true;

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

            if (_queuedGridTracks.Contains(grid.EntityId) || CheckAutotrack(grid))
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

        private bool CheckAutotrack(IMyCubeGrid grid)
        {
            if (!EnableAutotrack /*|| !MasterSession.Config.AutoTrack*/)
                return false;

            foreach (var block in grid.GetFatBlocks<IMyCubeBlock>())
            {
                if (!(block is IMyCockpit) && !AutoTrackSubtypes.Contains(block.BlockDefinition.SubtypeName))
                    continue;
                //TransferGridOwnership(grid);
                return true;
            }

            return false;
        }

        private static readonly Dictionary<string, ulong> ShipNamesToLeaderIds = new Dictionary<string, ulong>
        {
            // ASQ - Demonfox
            ["ASQ Fulma Natrii"] = 76561198006916493,
            ["Birdsy is Hungy"] = 76561198006916493,

            // HAC - Spartan
            ["HAC Asesino"] = 76561198084063571,
            ["H.A.C. - Charybdis"] = 76561198084063571,
            ["Imanis' Shadow Mk_7"] = 76561198084063571,
            ["H.A.C. - IONA"] = 76561198084063571,

            // ICE - RyO
            ["ICE Gorilla Destroyer Death Star"] = 76561198133050445,
            ["ICE TIE Abel"] = 76561198133050445,
            ["ICE TIE Cain X"] = 76561198133050445,
            ["ICE Wraith Star Destroyer X"] = 76561198133050445,

            // MCE - Max
            ["GT4 - MCE Beekeeper"] = 76561198256358015,
            ["GT4 30 Ramstick ARRAY"] = 76561198256358015,
            ["MCE Sonnenblume GT4 Fusion"] = 76561198256358015,
            ["[MCE] Dancing in Starlight GT4"] = 76561198274566684,

            // RKD - Anomaly
            ["Fire Hawk MK3 v7"] = 76561198049738491,
            ["Hyperion Class Battlecruiser"] = 76561198049738491,
            ["[RKD] Subcritical Photon"] = 76561198049738491,

            // TEC - Darth411
            ["TEC Challenger T48G-2 [BANSHEE] GT4"] = 76561198013723549,
            ["TEC Coyote Mk4 GT4"] = 76561198013723549,
            [" TEC Heavy Frigate - GT4"] = 76561198013723549,
            ["TEC Stolen Identity (GT4)"] = 76561198013723549,

            // UOD - Bryce_Craft
            ["UOD Doomsday GT 4"] = 76561198330424595,
            ["UOD Eternity GT4"] = 76561198330424595,
            ["UOD Serenity GT 4"] = 76561198330424595,
            ["UOD Valhalla GT4"] = 76561198330424595,
        };

        /// <summary>
        /// Temporary autotransfer method for Grand Tournament 4.
        /// </summary>
        /// <param name="grid"></param>
        private void TransferGridOwnership(IMyCubeGrid grid)
        {
            if (!MyAPIGateway.Session.IsServer || !ShipNamesToLeaderIds.ContainsKey(grid.DisplayName.Replace("\"", "")))
                return;

            long playerId = MyAPIGateway.Players.TryGetIdentityId(ShipNamesToLeaderIds[grid.DisplayName.Replace("\"", "")]);

            List<IMyCubeGrid> attachedGrids = new List<IMyCubeGrid>();
            grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(attachedGrids);

            foreach (var aGrid in attachedGrids)
                aGrid.ChangeGridOwnership(playerId, MyOwnershipShareModeEnum.Faction);
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

            if (((MyCubeGrid)grid).IsStatic || TrackedGrids.ContainsKey(grid))
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

        public ShipTracker TryGetTracker(IMyCubeGrid grid)
        {
            var allAttachedGrids = new List<IMyCubeGrid>();
            grid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(allAttachedGrids);
            foreach (var attachedGrid in allAttachedGrids.Where(attachedGrid => TrackedGrids.ContainsKey(attachedGrid)))
                return TrackedGrids[attachedGrid];
            return null;
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
