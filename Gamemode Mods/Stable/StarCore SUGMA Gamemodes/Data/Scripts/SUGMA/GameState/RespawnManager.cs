using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.Utilities;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameState
{
    public class RespawnManager : ComponentBase
    {
        private static ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;


        private Dictionary<IMyCubeGrid, IMyCubeGrid> _respawnBuffer = new Dictionary<IMyCubeGrid, IMyCubeGrid>();

        #region Public Methods

        public override void Init(string id)
        {
            base.Init(id);

            if (!MyAPIGateway.Session.IsServer)
                return;

            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                _respawnBuffer[grid] = GenerateGridCopy(grid, SetupBufferGrid);
            }
            ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);
        }

        public override void UpdateTick()
        {
            
        }

        #endregion

        private void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            if (isAlive || !_respawnBuffer.ContainsKey(grid))
                return;

            IMyCubeGrid newGrid = _respawnBuffer[grid];
            _respawnBuffer.Remove(grid);

            Vector3D spawnPos = SUtils.GetFactionSpawns().GetValueOrDefault(grid.GetFaction(), null)?.GetPosition() ??
                                Vector3D.One * 1500;
            spawnPos -= spawnPos.Normalized() * 250;

            MatrixD newGridMatrix = MatrixD.CreateWorld(spawnPos, -spawnPos.Normalized(), Vector3D.Up);
            newGrid.PositionComp.SetWorldMatrix(ref newGridMatrix);

            newGrid.Physics.Activate();
            ((MyCubeGrid)newGrid).DestructibleBlocks = true;

            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                ShareTrackApi.TrackGrid(newGrid);
                ShareTrackApi.UnTrackGrid(grid);
                newGrid.Physics.SetSpeeds(Vector3.Zero, Vector3.Zero);
            });

            Log.Info($"\nOwnerNull: {newGrid.GetOwner()}\nOwnerControlledEntityIsGrid: {newGrid.GetOwner().Controller?.ControlledEntity?.Entity is IMyCubeGrid}");
            if (newGrid.GetOwner() != null && !(newGrid.GetOwner().Controller?.ControlledEntity?.Entity is IMyCubeGrid && newGrid.GetOwner().Controller.ControlledEntity.Entity != grid))
            {
                var newControl = (MyCockpit) ((MyCubeGrid)newGrid).MainCockpit ?? (MyCockpit) newGrid.GetFatBlocks<IMyCockpit>().FirstOrDefault();

                if (newControl != null)
                    MyVisualScriptLogicProvider.CockpitInsertPilot(newControl.Name, false);
                    //newGrid.GetOwner().Controller.TakeControl(newControl);
            }

            Log.Info("Respawned grid " + newGrid.DisplayName);
        }

        private IMyCubeGrid GenerateGridCopy(IMyCubeGrid refGrid, Action<IMyEntity> onCompletion = null)
        {
            Log.Info("Try copying grid " + refGrid.DisplayName);
            var refObjectBuilder = (MyObjectBuilder_CubeGrid) refGrid.GetObjectBuilder();
            refObjectBuilder.CreatePhysics = true;
            MyEntities.RemapObjectBuilder(refObjectBuilder);
            return (IMyCubeGrid) MyAPIGateway.Entities.CreateFromObjectBuilderParallel(refObjectBuilder, false, onCompletion);
        }

        private void SetupBufferGrid(IMyEntity gridEnt)
        {
            try
            {
                IMyCubeGrid grid = gridEnt as IMyCubeGrid;
                if (grid == null)
                    return;

                MyAPIGateway.Entities.AddEntity(grid);
                ((MyCubeGrid)grid).DestructibleBlocks = false;
                grid.Physics.Deactivate();
                grid.SetPosition(Vector3D.Zero);

                Log.Info("Succeed copying grid " + grid.DisplayName);
                //grid.SetPosition(Vector3D.Zero);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(RespawnManager));
            }
        }
    }
}
