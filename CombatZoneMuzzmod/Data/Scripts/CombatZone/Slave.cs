using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
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
        private const double CombatRadius = 10000;
        private const double CombatNearEdge = CombatRadius - 1;
        private const string SphereModel = "\\Models\\Cubes\\BounceZoneAlt.mwm";
        private readonly BoundingSphereD _combatNearSphere = new BoundingSphereD(Vector3D.Zero, CombatNearEdge);
        private BoundingSphereD _combatMinSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius);
        private BoundingSphereD _combatMaxSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius + 20000);

        private int _count;
        private int _fastStart;
        private readonly List <MyEntity> _managedEntities = new List<MyEntity>(1000);
        private MyEntity _sphereEntity;
        
        public override void LoadData()
        {
        }
        protected override void UnloadData()
        {
        }
        public override void BeforeStart()
        {
          // if (!MyAPIGateway.Utilities.IsDedicated)
          // {
          //     _sphereEntity = GetSphereEntity();
          // }
        }
        public override void UpdateBeforeSimulation()
        {
            _count++;
            if (_count - _fastStart < 300 || _count % 100 == 0)
            {
                //RefreshVisualState();
                _managedEntities.Clear();
                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref _combatMaxSphere, _managedEntities, MyEntityQueryType.Dynamic);
                MyAPIGateway.Parallel.For(0, _managedEntities.Count, i =>
                {
                    try
                    {
                        var ent = _managedEntities[i];
                        if (!ShouldProcessEntity(ent))
                        {
                            return;
                        }
                        ApplyForcesToEntity(ent);
                    }
                    catch (Exception e)
                    {
                        MyLog.Default.WriteLine($"An exception occurred while processing entity {i}: {e.Message}");
                    }
                });
            }
        }
        private readonly HashSet<Type> _skipEntityTypes = new HashSet<Type> { /* Add pre-ignored Types Here */ };

        private bool ShouldProcessEntity(MyEntity ent)
        {
            Type entType = ent.GetType();

            // Fast check against cache
            if (_skipEntityTypes.Contains(entType) || ent.MarkedForClose || ent.IsPreview || ent.Physics == null || ent.Physics.IsPhantom || !ent.InScene)
            {
                return false;
            }

            var grid = ent as MyCubeGrid;
            var player = ent as IMyCharacter;
            if (grid == null && player == null)
            {
                // Cache this type for future fast checks
                _skipEntityTypes.Add(entType);
                return false;
            }

            var entVolume = ent.PositionComp.WorldVolume;
            return entVolume.Contains(_combatNearSphere) != ContainmentType.Contains;
        }
        private void ApplyForcesToEntity(MyEntity ent)
        {
            var grid = ent as MyCubeGrid;
            var pos = grid?.Physics?.CenterOfMassWorld ?? ent.PositionComp.WorldVolume.Center;
            var dir = Vector3D.Zero - pos;
            Vector3D dirNorm;
            Vector3D.Normalize(ref dir, out dirNorm);
            var force = dirNorm * ((grid?.Physics?.Mass ?? ent.Physics.Mass) *
            MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref pos, ref _combatMinSphere));
            ent.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, pos, Vector3.Zero);
            _fastStart = _count;
        }
       // private int frameCounter = 0;
       // private float lastCalculatedP = -1.0f;  // Cache the last calculated transparency

     // private void RefreshVisualState()
     // {
     //     frameCounter++;
     //     if (frameCounter % 10 != 0)  // Update only every 10 frames
     //     {
     //         return;
     //     }
     //
     //     if (!MyAPIGateway.Utilities.IsDedicated)
     //     {
     //         var cameraPos = MyAPIGateway.Session.Camera.WorldMatrix.Translation;
     //         double distToCenter;
     //         Vector3D.Distance(ref cameraPos, ref Vector3D.Zero, out distToCenter);
     //
     //         if (distToCenter > 20000)
     //         {
     //             if (_sphereEntity.InScene)
     //             {
     //                 _sphereEntity.InScene = false;
     //                 _sphereEntity.Render.RemoveRenderObjects();
     //             }
     //             return;  // Early exit
     //         }
     //
     //         if (distToCenter <= 4000)
     //         {
     //             if (_sphereEntity.InScene)
     //             {
     //                 _sphereEntity.InScene = false;
     //                 _sphereEntity.Render.RemoveRenderObjects();
     //             }
     //             return;  // Early exit
     //         }
     //
     //         if (distToCenter >= 4000 && distToCenter <= CombatRadius + 1000)
     //         {
     //             // Calculate inverted transparency based on camera distance
     //             var p = 1.0f - (float)((distToCenter - 4000) / (CombatRadius - 4000));
     //
     //             // Only update if the transparency value has changed significantly
     //             if (Math.Abs(lastCalculatedP - p) > 0.01)
     //             {
     //                // MyAPIGateway.Utilities.ShowNotification($"Transparency: {p}", 16, MyFontEnum.Red);
     //                 _sphereEntity.Render.UpdateRenderObject(false);
     //                 _sphereEntity.Render.Transparency = p;
     //                 _sphereEntity.Render.UpdateRenderObject(true);
     //
     //                 lastCalculatedP = p;  // Cache the last calculated transparency
     //             }
     //         }
     //
     //         if (!_sphereEntity.InScene)
     //         {
     //             _sphereEntity.InScene = true;
     //             _sphereEntity.Render.UpdateRenderObject(true, false);
     //         }
     //     }
     // }
      // private MyEntity GetSphereEntity()
      // {
      //     var ent = new MyEntity();
      //     var model = $"{ModContext.ModPath}{SphereModel}";
      //     ent.Init(null, model, null, null);
      //     ent.Render.CastShadows = false;
      //     ent.IsPreview = true;
      //     ent.Save = false;
      //     ent.SyncFlag = false;
      //     ent.NeedsWorldMatrix = false;
      //     ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
      //     MyEntities.Add(ent);
      //     var matrix = MatrixD.CreateScale(CombatRadius + 101);
      //     ent.PositionComp.SetWorldMatrix(ref matrix, null, false, false, false);
      //     ent.InScene = true;
      //     ent.Render.UpdateRenderObject(true, false);
      //     return ent;
      // }
    }
}
