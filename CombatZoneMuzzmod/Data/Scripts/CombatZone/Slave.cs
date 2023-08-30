using System;
using System.Collections.Generic;
using System.IO;
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
        private const double CombatRadius = 10000;
        private const double CombatNearEdge = CombatRadius - 1;
        private const string SphereModel = "\\Models\\Cubes\\OuterShield.mwm";
        private readonly BoundingSphereD _combatNearSphere = new BoundingSphereD(Vector3D.Zero, CombatNearEdge);
        private BoundingSphereD _combatMinSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius);
        private BoundingSphereD _combatMaxSphere = new BoundingSphereD(Vector3D.Zero, CombatRadius + 22500);
        private int _count;
        private int _fastStart;
        private readonly List
        <MyEntity>
        _managedEntities = new List
        <MyEntity>
        (1000);
        private MyEntity _sphereEntity;
        private const double ViewDistSqr = 81000000;
        public override void LoadData()
        {
        }
        protected override void UnloadData()
        {
        }
        public override void BeforeStart()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                _sphereEntity = GetSphereEntity();
            }
        }
        public override void UpdateBeforeSimulation()
        {
            _count++;
            if (_count - _fastStart < 300 || _count % 100 == 0)
            {
                RefreshVisualState();
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
        private bool ShouldProcessEntity(MyEntity ent)
        {
            if (ent.Physics == null ||
            ent.Physics.IsPhantom ||
            ent.IsPreview ||
            ent.MarkedForClose ||
            !ent.InScene)
            {
                return false;
            }
            var grid = ent as MyCubeGrid;
            var player = ent as IMyCharacter;
            if (grid == null && player == null)
            {
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
        private void RefreshVisualState()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                var cameraPos = MyAPIGateway.Session.Camera.Position;
                double distToCenter = 0;
                Vector3D.DistanceSquared(ref cameraPos, ref Vector3D.Zero, out distToCenter);
                double distFromCenterSqr = distToCenter;
                if (distFromCenterSqr >= ViewDistSqr)
                {
                    if (distFromCenterSqr >= 175562500)
                    {
                        _sphereEntity.InScene = false;
                        _sphereEntity.Render.RemoveRenderObjects();
                        return;
                    }
                    if (distFromCenterSqr < 81000000)
                    {
                        var dist = Vector3D.Distance(cameraPos, Vector3D.Zero);
                        var range = 9000f - dist;
                        var p = (float)Math.Round(range / 1500f, 2);
                        if (!MyUtils.IsEqual(p, _sphereEntity.Render.Transparency))
                        {
                            _sphereEntity.Render.UpdateRenderObject(false);
                            _sphereEntity.Render.Transparency = p;
                            _sphereEntity.Render.UpdateRenderObject(true);
                        }
                    }
                    if (!_sphereEntity.InScene)
                    {
                        _sphereEntity.InScene = true;
                        _sphereEntity.Render.UpdateRenderObject(true, false);
                    }
                }
                else if (_sphereEntity.InScene)
                {
                    _sphereEntity.InScene = false;
                    _sphereEntity.Render.RemoveRenderObjects();
                }
            }
        }
        private MyEntity GetSphereEntity()
        {
            var ent = new MyEntity();
            var model = $"{ModContext.ModPath}{SphereModel}";
            ent.Init(null, model, null, null);
            ent.Render.CastShadows = false;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent);
            var matrix = MatrixD.CreateScale(CombatRadius + 101);
            ent.PositionComp.SetWorldMatrix(ref matrix, null, false, false, false);
            ent.InScene = true;
            ent.Render.UpdateRenderObject(true, false);
            return ent;
        }
    }
}