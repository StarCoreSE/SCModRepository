using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Scripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 999999)]
    public class Session : MySessionComponentBase
    {
        private const double CombatRadius = 7500;  // Changed to 15km diameter
        private const double ViewDistSqr = 16000000;  // 4km from border squared
        private readonly BoundingSphereD _combatMaxSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius);
        private List<MyEntity> _managedEntities = new List<MyEntity>(1000);
        private MyEntity _sphereEntity;

        public override void BeforeStart()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                _sphereEntity = GetSphereEntity();
            }
        }

        public override void UpdateBeforeSimulation()
        {
            RefreshVisualState();

            _managedEntities.Clear();
            var combatMaxSphere = _combatMaxSphere;  // Create a mutable copy
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref combatMaxSphere, _managedEntities, MyEntityQueryType.Dynamic);

            MyAPIGateway.Parallel.For(0, _managedEntities.Count, i =>
            {
                var ent = _managedEntities[i];
                if (ShouldProcessEntity(ent))
                {
                    ApplyForcesToEntity(ent);
                }
            });
        }

        private static bool ShouldProcessEntity(MyEntity ent)
        {
            return !(ent.Physics == null || ent.Physics.IsPhantom || ent.IsPreview || ent.MarkedForClose || !ent.InScene);
        }

        private static void ApplyForcesToEntity(MyEntity ent)
        {
            var pos = ent.PositionComp.WorldVolume.Center;
            var dir = Vector3D.Zero - pos;
            Vector3D dirNorm;
            Vector3D.Normalize(ref dir, out dirNorm);
            var force = dirNorm * (ent.Physics.Mass);
            ent.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, pos, Vector3.Zero);
        }

        private void RefreshVisualState()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                var cameraPos = MyAPIGateway.Session.Camera.Position;
                double distToCenter;
                Vector3D.DistanceSquared(ref cameraPos, ref Vector3D.Zero, out distToCenter);

                if (distToCenter >= ViewDistSqr)
                {
                   
                    _sphereEntity.Render.UpdateRenderObject(true, false);
                }
                else 
                {
                    
                    _sphereEntity.Render.UpdateRenderObject(false, false);
                }
            }
        }

        private MyEntity GetSphereEntity()
        {
            var ent = new MyEntity();
            var model = $"{ModContext.ModPath}\\Models\\Cubes\\OuterShield.mwm";
            ent.Init(null, model, null, null);
            ent.Render.CastShadows = false;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Render.FadeIn = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent);
            var matrix = MatrixD.CreateScale(CombatRadius + 101);
            ent.PositionComp.SetWorldMatrix(ref matrix, null, false, false, false);
            return ent;
        }
    }
}
