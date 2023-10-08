using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;


namespace klime.FaceSmash
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class FaceSmashSession : MySessionComponentBase
    {
        public static FaceSmashSession instance;
        public HashSet<long> faceGrids = new HashSet<long>();
        public MyStringHash deformationType;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            instance = this;

            deformationType = MyStringHash.GetOrCompute("Deformation");
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);
        }

        public void AddBlock(IMyControlPanel block)
        {
            if (!faceGrids.Contains(block.CubeGrid.EntityId))
            {
                faceGrids.Add(block.CubeGrid.EntityId);
            }
        }

        public void RemoveBlock(IMyControlPanel block)
        {
            if (!faceGrids.Contains(block.CubeGrid.EntityId)) return;

            MyCubeGrid cGrid = block.CubeGrid as MyCubeGrid;
            if (cGrid.Physics == null || cGrid.MarkedForClose)
            {
                faceGrids.Remove(cGrid.EntityId);
                return;
            }

            bool shouldRemove = true;
            foreach (var fatBlock in cGrid.GetFatBlocks())
            {
                IMyControlPanel panel = fatBlock as IMyControlPanel;
                if (panel != null && panel.BlockDefinition.SubtypeId.Contains("FaceSmash"))
                {
                    shouldRemove = false;
                    break;
                }
            }

            if (shouldRemove)
            {
                faceGrids.Remove(cGrid.EntityId);
            }
        }

        private void DamageHandler(object target, ref MyDamageInformation info)
        {
            if (info.Type != deformationType) return;

            IMySlimBlock block = target as IMySlimBlock;
            if (block == null || block.CubeGrid == null || block.CubeGrid.WorldMatrix == null) return;

            if (faceGrids.Contains(block.CubeGrid.EntityId))
            {
                // Cancel damage
                info.Amount = 0;

                // Create bounding box for 100m around the grid
                BoundingBoxD boundingBox = new BoundingBoxD(
                    block.CubeGrid.WorldMatrix.Translation - new Vector3D(50, 50, 50),
                    block.CubeGrid.WorldMatrix.Translation + new Vector3D(50, 50, 50)
                );

                BoundingSphereD boundingSphere = new BoundingSphereD(block.CubeGrid.PositionComp.WorldAABB.Center, block.CubeGrid.WorldVolume.Radius);

                // Retrieve entities within bounding box
                List<IMyEntity> closeEntities = MyAPIGateway.Entities.GetEntitiesInAABB(ref boundingBox);

                List<IMyEntity> closeEntitiesSphere = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref boundingSphere);

                // Exclude the grid that the block is on
                closeEntitiesSphere.RemoveAll(e => e.EntityId == block.CubeGrid.EntityId);

                foreach (var grid in closeEntitiesSphere)
                {
                    if (grid == null || grid.Physics == null) continue;

                    // Zero the velocity of the grid
                  

                    // Apply impulse to push the grid away at 25m/s
                    Vector3D pushDirection = grid.PositionComp.WorldAABB.Center - block.CubeGrid.PositionComp.WorldAABB.Center;
                    pushDirection.Normalize();
                    Vector3D impulse = pushDirection * 2;
                    grid.Physics.LinearVelocityUnsafe = impulse;
                    grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, impulse, grid.WorldMatrix.Translation, null);
                }
            }
        }


        protected override void UnloadData()
        {
            instance = null;
        }
    }
}