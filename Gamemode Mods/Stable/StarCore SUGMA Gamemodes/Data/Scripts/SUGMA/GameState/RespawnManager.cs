using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.Utilities;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameState
{
    /// <summary>
    /// Controls serverside respawn mechanics.
    /// </summary>
    public class RespawnManager : ComponentBase
    {
        private static ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;

        public double RespawnTimeSeconds;
        private readonly Dictionary<IMyCubeGrid, MyObjectBuilder_CubeGrid> _respawnBuffer = new Dictionary<IMyCubeGrid, MyObjectBuilder_CubeGrid>();
        private readonly Queue<MyTuple<int, IMyCubeGrid, IMyCubeGrid, MyObjectBuilder_CubeGrid>> _respawnTimeBuffer = new Queue<MyTuple<int, IMyCubeGrid, IMyCubeGrid, MyObjectBuilder_CubeGrid>>();

        public RespawnManager(double respawnTimeSeconds = 0)
        {
            RespawnTimeSeconds = respawnTimeSeconds;
        }

        #region Public Methods

        public override void Init(string id)
        {
            base.Init(id);

            if (!MyAPIGateway.Session.IsServer)
                return;

            foreach (var grid in ShareTrackApi.GetTrackedGrids())
            {
                var refObjectBuilder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder();
                refObjectBuilder.CreatePhysics = true;
                MyEntities.RemapObjectBuilder(refObjectBuilder);
                _respawnBuffer[grid] = refObjectBuilder;
            }
            ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);
        }

        private int _ticks = 0;
        public override void UpdateTick()
        {
            if (!MyAPIGateway.Session.IsServer)
            {
                return;
            }

            _ticks++;
            while (_respawnTimeBuffer.Count > 0 && _respawnTimeBuffer.Peek().Item1 <= _ticks)
            {
                var tuple = _respawnTimeBuffer.Dequeue();
                ShareTrackApi.UnTrackGrid(tuple.Item3);
                ActivateGrid(tuple.Item2);
                _respawnBuffer[tuple.Item2] = tuple.Item4;
            }
        }

        #endregion

        private void ActivateGrid(IMyCubeGrid newGrid)
        {
            Vector3D spawnPos = SUtils.GetFactionSpawns().GetValueOrDefault(newGrid.GetFaction(), null)?.GetPosition() ??
                                Vector3D.One * 1500;
            spawnPos -= spawnPos.Normalized() * 250;

            MatrixD newGridMatrix = MatrixD.CreateWorld(spawnPos, -spawnPos.Normalized(), Vector3D.Up);
            newGrid.PositionComp.SetWorldMatrix(ref newGridMatrix);

            newGrid.Physics.Activate();
            ((MyCubeGrid)newGrid).DestructibleBlocks = true;

            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                ShareTrackApi.TrackGrid(newGrid);
                newGrid.Physics.SetSpeeds(Vector3.Zero, Vector3.Zero);
            });

            if (newGrid.GetOwner() != null/* && !(newGrid.GetOwner().Controller?.ControlledEntity?.Entity is IMyCubeGrid && newGrid.GetOwner().Controller.ControlledEntity.Entity != grid)*/)
            {
                var newControl = (MyCockpit) ((MyCubeGrid)newGrid).MainCockpit ?? (MyCockpit) newGrid.GetFatBlocks<IMyCockpit>().FirstOrDefault();

                if (newControl != null)
                    MyVisualScriptLogicProvider.CockpitInsertPilot(newControl.Name, false, newGrid.BigOwners[0]);
            }

            //var refObjectBuilder = (MyObjectBuilder_CubeGrid)newGrid.GetObjectBuilder();
            //refObjectBuilder.CreatePhysics = true;
            //MyEntities.RemapObjectBuilder(refObjectBuilder);
            //_respawnBuffer[newGrid] = refObjectBuilder;

            Log.Info("Respawned grid " + newGrid.DisplayName);
        }

        private void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            if (isAlive || !_respawnBuffer.ContainsKey(grid))
                return;

            IMyCubeGrid newGrid = (IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderParallel(_respawnBuffer[grid], false, SetupBufferGrid);

            foreach (var powerProducer in grid.GetFatBlocks<IMyPowerProducer>())
                powerProducer.Close();

            _respawnTimeBuffer.Enqueue(new MyTuple<int, IMyCubeGrid, IMyCubeGrid, MyObjectBuilder_CubeGrid>(_ticks + (int)(60 * RespawnTimeSeconds), newGrid, grid, _respawnBuffer[grid]));
            _respawnBuffer.Remove(grid);
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
