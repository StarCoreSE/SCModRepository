using Sandbox.ModAPI;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.Utils;

namespace DefenseShields.Support
{
    using System;
    using System.Collections.Generic;
    using Sandbox.Game.Entities;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.Voxels;
    using VRageMath;

    internal class CustomCollision
    {
        public static bool FutureIntersect(DefenseShields ds, MyEntity ent, MatrixD detectMatrix, MatrixD detectMatrixInv)
        {
            var entVel = ent.Physics.LinearVelocity;
            var entCenter = ent.PositionComp.WorldVolume.Center;
            var velStepSize = entVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 1;
            var futureCenter = entCenter + velStepSize;
            var testDir = Vector3D.Normalize(entCenter - futureCenter);
            var ray = new RayD(entCenter, -testDir);
            var ellipsoid = IntersectEllipsoid(ref ds.DetectMatrixOutsideInv, ds.DetectionMatrix, ref ray);
            var intersect = ellipsoid == null && PointInShield(entCenter, detectMatrixInv) || ellipsoid <= velStepSize.Length();
            return intersect;
        }

        public static Vector3D PastCenter(DefenseShields ds, MyEntity ent, MatrixD detectMatrix, MatrixD detectMatrixInv, int steps)
        {
            var entVel = -ent.Physics.LinearVelocity;
            var entCenter = ent.PositionComp.WorldVolume.Center;
            var velStepSize = entVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * steps;
            var pastCenter = entCenter + velStepSize;
            return pastCenter;
        }

        /*
        public static Vector3D? MissileIntersect(DefenseShields ds, MyEntity missile, MatrixD detectMatrix, MatrixD detectMatrixInv)
        {
            var missileVel = missile.Physics.LinearVelocity;
            var velStepSize = missileVel * (MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 2);
            var missileCenter = missile.PositionComp.WorldVolume.Center;
            var inflatedSphere = new BoundingSphereD(missileCenter, velStepSize.Length());
            var wDir = detectMatrix.Translation - inflatedSphere.Center;
            var wLen = wDir.Length();
            var wTest = inflatedSphere.Center + wDir / wLen * Math.Min(wLen, inflatedSphere.Radius);
            var intersect = Vector3D.Transform(wTest, detectMatrixInv).LengthSquared() <= 1;
            Vector3D? hitPos = null;

            if (intersect)
            {
                const float gameSecond = MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 60;
                var line = new LineD(missileCenter + -missileVel * gameSecond, missileCenter + missileVel * gameSecond);
                var obbIntersect = ds.SOriBBoxD.Intersects(ref line);
                if (obbIntersect.HasValue)
                {
                    var testDir = line.From - line.To;
                    testDir.Normalize();
                    hitPos = line.From + testDir * -obbIntersect.Value;
                }
            }
            return hitPos;
        }
        */

        public static bool MissileNoIntersect(MyEntity missile, MatrixD detectMatrix, MatrixD detectMatrixInv, IMySlimBlock block)
        {
            var missileVel = missile.Physics.LinearVelocity;
            var missileCenter = missile.PositionComp.WorldVolume.Center;
            var leaving = Vector3D.Transform(missileCenter + (-missileVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 2), detectMatrixInv).LengthSquared() <= 1;
            return leaving;
        }

        public static float? IntersectEllipsoid(ref MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, ref RayD ray)
        {
            var normSphere = new BoundingSphereD(Vector3D.Zero, 1f);

            Vector3D tPos;
            Vector3D.Transform(ref ray.Position, ref ellipsoidMatrixInv, out tPos);

            Vector3D tDir;
            Vector3D.TransformNormal(ref ray.Direction, ref ellipsoidMatrixInv, out tDir);

            Vector3D tNormDir;
            Vector3D.Normalize(ref tDir, out tNormDir);

            var localRay = new RayD(tPos, tNormDir);

            var normalizedDistance = normSphere.Intersects(localRay);

            if (normalizedDistance == null || normalizedDistance.Value <= 0) 
                return (float?) normalizedDistance;
            
            var localHitPos = tPos + (tNormDir * normalizedDistance.Value);

            Vector3D worldHitPos;
            Vector3D.Transform(ref localHitPos, ref ellipsoidMatrix, out worldHitPos);

            double distance;
            Vector3D.Distance(ref worldHitPos, ref ray.Position, out distance);
            var isNaN = double.IsNaN(distance);

            return (float?)(isNaN ? (double?)null : distance);
        }
        /*
        public static bool IntersectEllipsoidEllipsoid(Vector3D e1Center, Vector3D e1Size, Quaternion e1Rot, Vector3D e2Center, Vector3D e2Size, Quaternion e2Rot, out Vector3D collisionPoint)
        {
            collisionPoint = Vector3D.Zero;

            // Create transform matrices for the ellipsoids
            MatrixD transform1 = MatrixD.CreateFromTransformScale(e1Rot, e1Center, Vector3D.One);
            MatrixD transform2 = MatrixD.CreateFromTransformScale(e2Rot, e2Center, Vector3D.One);

            // Transform the ellipsoid centers and radii into a common space
            MatrixD transform1Inv = MatrixD.Invert(transform1);
            MatrixD transform2Inv = MatrixD.Invert(transform2);
            var center1 = MultiplyPoint(e2Center, transform1Inv);
            var center2 = MultiplyPoint(e1Center, transform2Inv);
            var radii1 = MultiplyVector(e2Size, transform1Inv);
            var radii2 = MultiplyVector(e1Size, transform2Inv);

            // Calculate the distance between the transformed ellipsoid centers
            Vector3D centerDistance = center1 - center2;
            double distance = centerDistance.Length();
            // Calculate the sum of the transformed ellipsoid radii
            Vector3D radiiSum = radii1 + radii2;

            if (distance <= radiiSum.X && distance <= radiiSum.Y && distance <= radiiSum.Z)
            {
                Log.Line($"collision");
                // The ellipsoids intersect, so find the surface collision points
                Vector3D surfacePoint1 = center1 + centerDistance.Normalize() * radii1;
                Vector3D surfacePoint2 = center2 - centerDistance.Normalize() * radii2;

                // Calculate the collision distance
                double collisionDistance = (radiiSum - centerDistance).Length() / 2;

                // Set the collision point to the midpoint between the surface collision points, minus the collision distance
                collisionPoint = (surfacePoint1 + surfacePoint2) / 2 - centerDistance.Normalize() * collisionDistance;
                // Transform the collision point back into world space
                collisionPoint = Vector3D.Transform(collisionPoint, transform1);
                //collisionPoint = transform1.MultiplyPoint(collisionPoint);
                return true;
            }
            else
            {
                return false;
            }
        }
        */
        public static bool IntersectEllipsoidEllipsoid(Vector3D e1Center, Vector3D e1Size, Quaternion e1Rot, Vector3D e2Center, Vector3D e2Size, Quaternion e2Rot, out Vector3D collisionPoint)
        {
            // Move ellipsoids to the origin
            Vector3D relativeCenter = e2Center - e1Center;
            e1Center = Vector3D.Zero;
            e2Center = relativeCenter;

            // Apply rotation
            MatrixD e1RotMatrix = MatrixD.CreateFromQuaternion(e1Rot);
            MatrixD e2RotMatrix = MatrixD.CreateFromQuaternion(e2Rot);
            MatrixD e1RotMatrixInverse = MatrixD.Invert(e1RotMatrix);

            // Scale ellipsoids to spheres
            MatrixD e1ScaleMatrix = MatrixD.CreateScale(e1Size);
            MatrixD e2ScaleMatrix = MatrixD.CreateScale(e2Size);
            MatrixD e1ScaleMatrixInverse = MatrixD.Invert(e1ScaleMatrix);

            // Transform e2Center and e2RotMatrix to e1's coordinate system
            e2Center = Vector3D.Transform(e2Center, e1RotMatrixInverse * e1ScaleMatrixInverse * e2ScaleMatrix);
            e2RotMatrix = e1RotMatrixInverse * e2RotMatrix;

            Vector3D localCollisionPoint;
            // Check intersection between a sphere and an ellipsoid
            bool intersection = IntersectSphereEllipsoid(e1Center, 1.0, e2Center, e2Size, e2RotMatrix, out localCollisionPoint);

            // Transform the collision point back to the original coordinate system
            collisionPoint = Vector3D.Transform(localCollisionPoint, e1ScaleMatrix * e1RotMatrix);

            return intersection;
        }

        private static bool IntersectSphereEllipsoid(Vector3D sphereCenter, double sphereRadius, Vector3D ellipsoidCenter, Vector3D ellipsoidSize, MatrixD ellipsoidRotMatrix, out Vector3D collisionPoint)
        {
            // Move the sphere to the origin
            Vector3D relativeCenter = ellipsoidCenter - sphereCenter;
            sphereCenter = Vector3D.Zero;
            ellipsoidCenter = relativeCenter;

            // Transform the ellipsoid center point to the ellipsoid's local coordinate system
            Vector3D localEllipsoidCenter = Vector3D.Transform(ellipsoidCenter, ellipsoidRotMatrix);

            // Get the closest point on the ellipsoid to the sphere center
            Vector3D closestPoint = GetClosestPointOnEllipsoid(localEllipsoidCenter, ellipsoidSize);

            // Calculate the squared distance between the closest point and the sphere center
            double squaredDistance = (closestPoint - localEllipsoidCenter).LengthSquared();

            // Check if the squared distance is less than or equal to the squared sphere radius
            bool intersection = squaredDistance <= sphereRadius * sphereRadius;

            // Assign the collision point
            collisionPoint = closestPoint;

            return intersection;
        }
        private static Vector3D GetClosestPointOnEllipsoid(Vector3D point, Vector3D ellipsoidSize)
        {
            Vector3D scaledPoint = new Vector3D(point.X / ellipsoidSize.X, point.Y / ellipsoidSize.Y, point.Z / ellipsoidSize.Z);
            double length = scaledPoint.Length();

            // If the point is inside the ellipsoid, return the point itself
            if (length <= 1.0)
            {
                return point;
            }

            // If the point is outside the ellipsoid, return the closest point on the ellipsoid surface
            Vector3D unitScaledPoint = scaledPoint / length;
            Vector3D closestPoint = new Vector3D(unitScaledPoint.X * ellipsoidSize.X, unitScaledPoint.Y * ellipsoidSize.Y, unitScaledPoint.Z * ellipsoidSize.Z);
            return closestPoint;
        }

        // Transforms a position by this matrix, with a perspective divide. (generic)
        public static Vector3D MultiplyPoint(Vector3D point, MatrixD matrix)
        {
            Vector3D res;
            double w;
            res.X = matrix.M11 * point.X + matrix.M21 * point.Y + matrix.M31 * point.Z + matrix.M41;
            res.Y = matrix.M12 * point.X + matrix.M22 * point.Y + matrix.M32 * point.Z + matrix.M42;
            res.Z = matrix.M13 * point.X + matrix.M23 * point.Y + matrix.M33 * point.Z + matrix.M43;
            w = matrix.M14 * point.X + matrix.M24 * point.Y + matrix.M34 * point.Z + matrix.M44;

            w = 1F / w;
            res.X *= w;
            res.Y *= w;
            res.Z *= w;
            return res;
        }

        public static Vector3D MultiplyVector(Vector3D vector, MatrixD matrix)
        {
            Vector3D res;
            res.X = matrix.M11 * vector.X + matrix.M21 * vector.Y + matrix.M31 * vector.Z;
            res.Y = matrix.M12 * vector.X + matrix.M22 * vector.Y + matrix.M32 * vector.Z;
            res.Z = matrix.M13 * vector.X + matrix.M23 * vector.Y + matrix.M33 * vector.Z;
            return res;
        }

        public static bool IntersectEllipsoidObb(ref MatrixD ellipsoidMatrixInv, ref Vector3D obbCenter, ref Vector3D obbHalfExtent, ref Vector3D shieldHalfExtet, ref Quaternion dividedQuat)
        {
            var normSphere = new BoundingSphereD(Vector3D.Zero, 1f);
            var transObbCenter = Vector3D.Transform(obbCenter, ref ellipsoidMatrixInv);
            var squishedSize = obbHalfExtent / shieldHalfExtet;
            var newObb = new MyOrientedBoundingBoxD(transObbCenter, squishedSize, dividedQuat);

            var intersected = newObb.Intersects(ref normSphere);

            normSphere.Center += obbCenter + (Vector3D.Forward * 50);
            newObb.Center += obbCenter + (Vector3D.Forward * 50);
            DsDebugDraw.DrawSphere(normSphere, Color.Blue);
            DsDebugDraw.DrawOBB(newObb, Color.Red);
            return intersected;
        }


        public static bool IntersectEllipsoidObb(MatrixD ellipsoidMatrixInv, MyOrientedBoundingBoxD obb, MyOrientedBoundingBoxD SOriBBoxD)
        {
            var normSphere = new BoundingSphereD(Vector3D.Zero, 1f);
            DsDebugDraw.DrawSphere(normSphere, Color.Blue);
            var newObb = new MyOrientedBoundingBoxD(
                Vector3D.Transform(obb.Center, ellipsoidMatrixInv),
                obb.HalfExtent / SOriBBoxD.HalfExtent, //Vector3D.Normalize(Vector3D.Transform(obb.HalfExtent, ellipsoidMatrixInv)), 
                Quaternion.Divide(obb.Orientation, SOriBBoxD.Orientation));

            DsDebugDraw.DrawOBB(newObb, Color.Red);
            var intersected = newObb.Intersects(ref normSphere);

            return intersected;
        }

        public static bool IntersectEllipsoidBox(MatrixD ellipsoidMatrixInv, BoundingBoxD box)
        {
            var normSphere = new BoundingSphereD(Vector3D.Zero, 1f);

            box.TransformSlow(ref ellipsoidMatrixInv);
            var intersected = box.Intersects(ref normSphere);

            return intersected;
        }

        public static Vector3D ClosestObbPointToPos(MyOrientedBoundingBoxD obb, Vector3D point)
        {
            var center = obb.Center;
            var directionVector = point - center;
            var halfExtents = obb.HalfExtent;
            var m = MatrixD.CreateFromQuaternion(obb.Orientation);
            m.Translation = obb.Center;
            var xAxis = m.GetDirectionVector(Base6Directions.Direction.Right);
            var yAxis = m.GetDirectionVector(Base6Directions.Direction.Up);
            var zAxis = m.GetDirectionVector(Base6Directions.Direction.Forward);

            var distanceX = Vector3D.Dot(directionVector, xAxis);
            if (distanceX > halfExtents.X) distanceX = halfExtents.X;
            else if (distanceX < -halfExtents.X) distanceX = -halfExtents.X;

            var distanceY = Vector3D.Dot(directionVector, yAxis);
            if (distanceY > halfExtents.Y) distanceY = halfExtents.Y;
            else if (distanceY < -halfExtents.Y) distanceY = -halfExtents.Y;

            var distanceZ = Vector3D.Dot(directionVector, zAxis);
            if (distanceZ > halfExtents.Z) distanceZ = halfExtents.Z;
            else if (distanceZ < -halfExtents.Z) distanceZ = -halfExtents.Z;

            return center + distanceX * xAxis + distanceY * yAxis + distanceZ * zAxis;
        }

        public static Vector3D ClosestEllipsoidPointToPos(ref MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, ref Vector3D point)
        {
            var ePos = Vector3D.Transform(point, ref ellipsoidMatrixInv);
            Vector3D closestLPos;
            Vector3D.Normalize(ref ePos, out closestLPos);

            return Vector3D.Transform(closestLPos, ref ellipsoidMatrix);
        }

        public static double EllipsoidDistanceToPos(ref MatrixD ellipsoidMatrixInv, ref MatrixD ellipsoidMatrix, ref Vector3D point)
        {
            var ePos = Vector3D.Transform(point, ref ellipsoidMatrixInv);
            Vector3D closestLPos;
            Vector3D.Normalize(ref ePos, out closestLPos);
            var closestWPos = Vector3D.Transform(closestLPos, ref ellipsoidMatrix);
            double distToPoint;
            Vector3D.Distance(ref closestWPos, ref point, out distToPoint);
            if (ePos.LengthSquared() < 1) distToPoint *= -1;

            return distToPoint;
        }

        public static double EllipsoidDistanceToPos(ref MatrixD ellipsoidMatrixInv, ref MatrixD ellipsoidMatrix, ref Vector3D point, out Vector3D closestWPos)
        {
            var ePos = Vector3D.Transform(point, ref ellipsoidMatrixInv);
            Vector3D closestLPos;
            Vector3D.Normalize(ref ePos, out closestLPos);
            closestWPos = Vector3D.Transform(closestLPos, ref ellipsoidMatrix);
            double distToPoint;
            Vector3D.Distance(ref closestWPos, ref point, out distToPoint);
            if (ePos.LengthSquared() < 1) distToPoint *= -1;

            return distToPoint;
        }

        public static void ClosestPointPlanePoint(ref PlaneD plane, ref Vector3D point, out Vector3D result)
        {
            double result1;
            Vector3D.Dot(ref plane.Normal, ref point, out result1);
            double num = result1 - plane.D;
            result = point - num * plane.Normal;
        }

        public static bool RayIntersectsTriangle(Vector3D rayOrigin, Vector3D rayVector, Vector3D v0, Vector3D v1, Vector3D v2, Vector3D outIntersectionPoint)
        {
            const double Epsilon = 0.0000001;
            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = rayVector.Cross(edge2);
            var a = edge1.Dot(h);
            if (a > -Epsilon && a < Epsilon) return false;

            var f = 1 / a;
            var s = rayOrigin - v0;
            var u = f * s.Dot(h);
            if (u < 0.0 || u > 1.0) return false;

            var q = s.Cross(edge1);
            var v = f * rayVector.Dot(q);
            if (v < 0.0 || u + v > 1.0) return false;
            
            var t = f * edge2.Dot(q);
            if (t > Epsilon) 
            {
                // outIntersectionPoint = rayOrigin + rayVector * t;
                return true;
            }
            return false;
        }

        public static void ShieldX2PointsInside(Vector3D[] shield1Verts, MatrixD shield1MatrixInv, Vector3D[] shield2Verts, MatrixD shield2MatrixInv, List<Vector3D> insidePoints)
        {
            for (int i = 0; i < 642; i++) if (Vector3D.Transform(shield1Verts[i], shield2MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield1Verts[i]); 
            for (int i = 0; i < 642; i++) if (Vector3D.Transform(shield2Verts[i], shield1MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield2Verts[i]);
        }

        public static void ClientShieldX2PointsInside(Vector3D[] shield1Verts, MatrixD shield1MatrixInv, Vector3D[] shield2Verts, MatrixD shield2MatrixInv, List<Vector3D> insidePoints)
        {
            for (int i = 0; i < 162; i++) if (Vector3D.Transform(shield1Verts[i], shield2MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield1Verts[i]);
            for (int i = 0; i < 162; i++) if (Vector3D.Transform(shield2Verts[i], shield1MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield2Verts[i]);
        }

        public static bool VoxelContact(Vector3D[] physicsVerts, MyVoxelBase voxelBase)
        {
            try
            {
                if (voxelBase.RootVoxel.MarkedForClose || voxelBase.RootVoxel.Storage.Closed) return false;
                var planet = voxelBase as MyPlanet;
                var map = voxelBase as MyVoxelMap;

                if (planet != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - planet.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        planet.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit) return true;
                    }
                }
                else if (map != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - map.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        map.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit) return true;
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in VoxelContact: {ex}"); }

            return false;
        }

        public static Vector3D? VoxelEllipsoidCheck(IMyCubeGrid shieldGrid, Vector3D[] physicsVerts, MyVoxelBase voxelBase)
        {
            var collisionAvg = Vector3D.Zero;
            try
            {
                if (voxelBase.RootVoxel.MarkedForClose || voxelBase.RootVoxel.Storage.Closed) return null;
                var planet = voxelBase as MyPlanet;
                var map = voxelBase as MyVoxelMap;

                var collision = Vector3D.Zero;
                var collisionCnt = 0;
                
                if (planet != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - planet.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        planet.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit)
                        {
                            collision += from;
                            collisionCnt++;
                        }
                    }
                }
                else if (map != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - map.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        map.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit)
                        {
                            collision += from;
                            collisionCnt++;
                        }
                    }
                }

                if (collisionCnt == 0) return null;
                collisionAvg = collision / collisionCnt;
            }
            catch (Exception ex) { Log.Line($"Exception in VoxelCollisionSphere: {ex}"); }

            return collisionAvg;
        }

        internal static Vector3D? PointsInsideVoxel(DefenseShields shield, Vector3D[] physicsVerts, MyVoxelBase voxel)
        {
            var voxelMatrix = voxel.PositionComp.WorldMatrixInvScaled;
            var vecMax = new Vector3I(int.MaxValue);
            var vecMin = new Vector3I(int.MinValue);

            var collision = Vector3D.Zero;
            var collisionCnt = 0;
            for (int index = 0; index < physicsVerts.Length; ++index)
            {
                var point = physicsVerts[index];
                Vector3D result;
                Vector3D.Transform(ref point, ref voxelMatrix, out result);
                var r = result + (Vector3D)(voxel.Size / 2);
                var v1 = Vector3D.Floor(r);
                Vector3D.Fract(ref r, out r);
                var v2 = v1 + voxel.StorageMin;
                var v3 = v2 + 1;
                if (v2 != vecMax && v3 != vecMin)
                {
                    shield.TmpStorage.Resize(v2, v3);
                    voxel.Storage.ReadRange(shield.TmpStorage, MyStorageDataTypeFlags.Content, 0, v2, v3);
                    vecMax = v2;
                    vecMin = v3;
                }
                var num1 = shield.TmpStorage.Content(0, 0, 0);
                var num2 = shield.TmpStorage.Content(1, 0, 0);
                var num3 = shield.TmpStorage.Content(0, 1, 0);
                var num4 = shield.TmpStorage.Content(1, 1, 0);
                var num5 = shield.TmpStorage.Content(0, 0, 1);
                var num6 = shield.TmpStorage.Content(1, 0, 1);
                var num7 = shield.TmpStorage.Content(0, 1, 1);
                var num8 = shield.TmpStorage.Content(1, 1, 1);
                var num9 = num1 + (num2 - num1) * r.X;
                var num10 = num3 + (num4 - num3) * r.X;
                var num11 = num5 + (num6 - num5) * r.X;
                var num12 = num7 + (num8 - num7) * r.X;
                var num13 = num9 + (num10 - num9) * r.Y;
                var num14 = num11 + (num12 - num11) * r.Y;
                if (num13 + (num14 - num13) * r.Z >= sbyte.MaxValue)
                {
                    collision += point;
                    collisionCnt++;
                }
            }
            shield.TmpStorage.Clear(MyStorageDataTypeEnum.Content, byte.MaxValue);
            if (collisionCnt == 0) return null;
            collision /= collisionCnt;

            return collision;
        }

        public static MyVoxelBase AabbInsideVoxel(MatrixD worldMatrix, BoundingBoxD localAabb)
        {
            BoundingBoxD box = localAabb.TransformFast(ref worldMatrix);
            List<MyVoxelBase> result = new List<MyVoxelBase>();
            MyGamePruningStructure.GetAllVoxelMapsInBox(ref box, result);
            foreach (MyVoxelBase voxelMap in result)
            {
                if (voxelMap.IsAnyAabbCornerInside(ref worldMatrix, localAabb)) return voxelMap;
            }
            return null;
        }

        public static Vector3D? BlockIntersect(IMySlimBlock block, bool cubeExists, ref MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, ref Vector3D shieldHalfExtet, ref Quaternion dividedQuat)
        {
            Vector3D halfExtents;
            Vector3D center;

            if (cubeExists) {
                halfExtents = block.FatBlock.LocalAABB.HalfExtents;
                center = block.FatBlock.WorldAABB.Center;
            }
            else {
                Vector3 halfExt;
                block.ComputeScaledHalfExtents(out halfExt);
                halfExtents = halfExt;
                block.ComputeWorldCenter(out center);
            }

            var normSphere = new BoundingSphereD(Vector3D.Zero, 1f);
            var transObbCenter = Vector3D.Transform(center, ref ellipsoidMatrixInv);
            var squishedSize = halfExtents / shieldHalfExtet;
            var newObb = new MyOrientedBoundingBoxD(transObbCenter, squishedSize, dividedQuat);

            if (!newObb.Intersects(ref normSphere))
                return null;

            return ClosestEllipsoidPointToPos(ref ellipsoidMatrixInv, ellipsoidMatrix, ref center);
        }

        public static Vector3D? BlockIntersect(IMySlimBlock block, bool cubeExists, ref MyOrientedBoundingBoxD obb, ref MatrixD matrix, ref MatrixD matrixInv, ref Vector3D[] blockPoints, bool debug = false)
        {
            BoundingBoxD blockBox;
            Vector3D center;
            double radius;
            if (cubeExists)
            {
                blockBox = block.FatBlock.LocalAABB;
                center = block.FatBlock.WorldAABB.Center;
                radius = block.FatBlock.LocalAABB.HalfExtents.AbsMax();
            }
            else
            {
                Vector3 halfExt;
                block.ComputeScaledHalfExtents(out halfExt);
                blockBox = new BoundingBoxD(-halfExt, halfExt);
                block.ComputeWorldCenter(out center);
                radius = halfExt.AbsMax();
            }
            var worldSphere = new BoundingSphereD(center, radius);
            if (!obb.Intersects(ref worldSphere))
                return null;

            Vector3D hitPos;
            var distFromEllips = EllipsoidDistanceToPos(ref matrixInv, ref matrix, ref center, out hitPos);

            if (distFromEllips > radius)
                return null;
            var sphereCheck = block.Min + block.Max == Vector3I.Zero;
            if (sphereCheck)
                return hitPos;

            // 4 + 5 + 6 + 7 = Front
            // 0 + 1 + 2 + 3 = Back
            // 1 + 2 + 5 + 6 = Top
            // 0 + 3 + 4 + 7 = Bottom
            new MyOrientedBoundingBoxD(center, blockBox.HalfExtents, obb.Orientation).GetCorners(blockPoints, 0);
            blockPoints[8] = center;
            var point0 = blockPoints[0];
            if (Vector3.Transform(point0, matrixInv).LengthSquared() <= 1) return point0;
            var point1 = blockPoints[1];
            if (Vector3.Transform(point1, matrixInv).LengthSquared() <= 1) return point1;
            var point2 = blockPoints[2];
            if (Vector3.Transform(point2, matrixInv).LengthSquared() <= 1) return point2;
            var point3 = blockPoints[3];
            if (Vector3.Transform(point3, matrixInv).LengthSquared() <= 1) return point3;
            var point4 = blockPoints[4];
            if (Vector3.Transform(point4, matrixInv).LengthSquared() <= 1) return point4;
            var point5 = blockPoints[5];
            if (Vector3.Transform(point5, matrixInv).LengthSquared() <= 1) return point5;
            var point6 = blockPoints[6];
            if (Vector3.Transform(point6, matrixInv).LengthSquared() <= 1) return point6;
            var point7 = blockPoints[7];
            if (Vector3.Transform(point7, matrixInv).LengthSquared() <= 1) return point7;
            var point8 = blockPoints[8];
            if (Vector3.Transform(point8, matrixInv).LengthSquared() <= 1) return point8;

            var blockSize = (float)blockBox.HalfExtents.AbsMax() * 2;
            var testDir = Vector3D.Normalize(point0 - point1);
            var ray = new RayD(point0, -testDir);
            var intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point1) >= Vector3D.DistanceSquared(point1, point))
                {
                    //Log.Line($"ray0: {intersect} - {Vector3D.Distance(point1, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point3);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray1: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point4);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point4) >= Vector3D.DistanceSquared(point4, point))
                {
                    //Log.Line($"ray2: {intersect} - {Vector3D.Distance(point4, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point2);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point2) >= Vector3D.DistanceSquared(point2, point))
                {
                    //Log.Line($"ray3: {intersect} - {Vector3D.Distance(point2, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point5);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray4: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point3);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray5: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point6);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray6: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point3 - point7);
            ray = new RayD(point3, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point3, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray7: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point5);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray8: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point7);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray9: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point5 - point6);
            ray = new RayD(point5, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point5, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray10: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point6 - point7);
            ray = new RayD(point6, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point6, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray11: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }
            return null;
        }

        public static bool Intersecting(IMyCubeGrid breaching, IMyCubeGrid shield, Vector3D[] physicsVerts, Vector3D breachingPos)
        {
            var shieldPos = ClosestVertArray(physicsVerts, breachingPos);
            var gridVel = breaching.Physics.LinearVelocity;
            var gridCenter = breaching.PositionComp.WorldVolume.Center;
            var shieldVel = shield.Physics.LinearVelocity;
            var shieldCenter = shield.PositionComp.WorldVolume.Center;
            var gApproching = Vector3.Dot(gridVel, gridCenter - shieldPos) < 0;
            var sApproching = Vector3.Dot(shieldVel, shieldCenter - breachingPos) < 0;
            return gApproching || sApproching;
        }

        public static Vector3D ContactPointOutside(MyEntity breaching, MatrixD matrix)
        {
            var wVol = breaching.PositionComp.WorldVolume;
            var wDir = matrix.Translation - wVol.Center;
            var wLen = wDir.Length();
            var contactPoint = wVol.Center + (wDir / wLen * Math.Min(wLen, wVol.Radius));
            return contactPoint;
        }

        public static bool SphereTouchOutside(MyEntity breaching, MatrixD matrix, MatrixD detectMatrixInv)
        {
            var wVol = breaching.PositionComp.WorldVolume;
            var wDir = matrix.Translation - wVol.Center;
            var wLen = wDir.Length();
            var closestPointOnSphere = wVol.Center + (wDir / wLen * Math.Min(wLen, wVol.Radius + 1));

            var intersect = Vector3D.Transform(closestPointOnSphere, detectMatrixInv).LengthSquared() <= 1;
            return intersect;
        }

        public static bool PointInShield(Vector3D entCenter, MatrixD matrixInv)
        {
            return Vector3D.Transform(entCenter, matrixInv).LengthSquared() <= 1;
        }

        public static void ClosestCornerInShield(Vector3D[] gridCorners, MatrixD matrixInv, ref Vector3D cloestPoint)
        {
            var minValue1 = double.MaxValue;

            for (int i = 0; i < 8; i++)
            {
                var point = gridCorners[i];
                var pointInside = Vector3D.Transform(point, matrixInv).LengthSquared();
                if (!(pointInside <= 1) || !(pointInside < minValue1)) continue;
                minValue1 = pointInside;
                cloestPoint = point;
            }
        }

        public static int CornerOrCenterInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] corners, bool firstMatch = false)
        {
            var c = 0;
            if (Vector3D.Transform(ent.PositionComp.WorldAABB.Center, matrixInv).LengthSquared() <= 1) c++;
            if (firstMatch && c > 0) return c;

            ent.PositionComp.WorldAABB.GetCorners(corners);
            for (int i = 0; i < 8; i++)
            {
                if (Vector3D.Transform(corners[i], matrixInv).LengthSquared() <= 1) c++;
                if (firstMatch && c > 0) return c;
            }
            return c;
        }

        public static int EntCornersInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] entCorners)
        {
            var entAabb = ent.PositionComp.WorldAABB;
            entAabb.GetCorners(entCorners);

            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                var pointInside = Vector3D.Transform(entCorners[i], matrixInv).LengthSquared() <= 2;
                if (pointInside) c++;
            }
            return c;
        }

        public static int NotAllCornersInShield(MyCubeGrid grid, MatrixD matrixInv, Vector3D[] gridCorners)
        {
            var gridAabb = grid.PositionComp.WorldAABB;
            gridAabb.GetCorners(gridCorners);

            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                var pointInside = Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1;
                if (pointInside) c++;
                else if (c != 0) break;
            }
            return c;
        }

        public static bool AllAabbInShield(BoundingBoxD gridAabb, MatrixD matrixInv, Vector3D[] gridCorners = null)
        {
            if (gridCorners == null) gridCorners = new Vector3D[8];

            gridAabb.GetCorners(gridCorners);
            var c = 0;
            for (int i = 0; i < 8; i++)
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1) c++;
            return c == 8;
        }

        public static bool ObbCornersInShield(MyOrientedBoundingBoxD bOriBBoxD, MatrixD matrixInv, Vector3D[] gridCorners, bool anyCorner = false)
        {
            bOriBBoxD.GetCorners(gridCorners, 0);
            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1)
                {
                    if (anyCorner) return true;
                    c++;
                }
            }
            return c == 8;
        }

        public static int NewObbPointsInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] gridPoints = null)
        {
            if (gridPoints == null) gridPoints = new Vector3D[9];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.WorldMatrix);
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            obb.GetCorners(gridPoints, 0);
            gridPoints[8] = obb.Center;
            var c = 0;
            for (int i = 0; i < 9; i++)
                if (Vector3D.Transform(gridPoints[i], matrixInv).LengthSquared() <= 1) c++;
            return c;
        }

        public static int NewObbCornersInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] gridCorners = null)
        {
            if (gridCorners == null) gridCorners = new Vector3D[8];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.WorldMatrix);
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            obb.GetCorners(gridCorners, 0);
            var c = 0;
            for (int i = 0; i < 8; i++)
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1) c++;
            return c;
        }

        public static BoundingSphereD NewObbClosestTriCorners(MyEntity ent, Vector3D pos)
        {
            var entCorners = new Vector3D[8];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.PositionComp.GetOrientation());
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            var minValue = double.MaxValue;
            var minValue0 = double.MaxValue;
            var minValue1 = double.MaxValue;
            var minValue2 = double.MaxValue;
            var minValue3 = double.MaxValue;

            var minNum = -2;
            var minNum0 = -2;
            var minNum1 = -2;
            var minNum2 = -2;
            var minNum3 = -2;

            obb.GetCorners(entCorners, 0);
            for (int i = 0; i < entCorners.Length; i++)
            {
                var gridCorner = entCorners[i];
                var range = gridCorner - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue3)
                {
                    if (test < minValue)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = minValue1;
                        minNum2 = minNum1;
                        minValue1 = minValue0;
                        minNum1 = minNum0;
                        minValue0 = minValue;
                        minNum0 = minNum;
                        minValue = test;
                        minNum = i;
                    }
                    else if (test < minValue0)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = minValue1;
                        minNum2 = minNum1;
                        minValue1 = minValue0;
                        minNum1 = minNum0;
                        minValue0 = test;
                        minNum0 = i;
                    }
                    else if (test < minValue1)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = minValue1;
                        minNum2 = minNum1;
                        minValue1 = test;
                        minNum1 = i;
                    }
                    else if (test < minValue2)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = test;
                        minNum2 = i;
                    }
                    else
                    {
                        minValue3 = test;
                        minNum3 = i;
                    }
                }
            }
            var corner = entCorners[minNum];
            var corner0 = entCorners[minNum0];
            var corner1 = entCorners[minNum1];
            var corner2 = entCorners[minNum2];
            var corner3 = gridCenter;
            Vector3D[] closestCorners = { corner, corner0, corner3};

            var sphere = BoundingSphereD.CreateFromPoints(closestCorners);
            //var subObb = MyOrientedBoundingBoxD.CreateFromBoundingBox(box);
            return sphere;
        }

        public static bool NewAllObbCornersInShield(MyEntity ent, MatrixD matrixInv, bool anyCorner, Vector3D[] gridCorners = null)
        {
            if (gridCorners == null) gridCorners = new Vector3D[8];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.WorldMatrix);
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            obb.GetCorners(gridCorners, 0);
            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1)
                {
                    if (anyCorner) return true;
                    c++;
                }
            }
            return c == 8;
        }

        public static void IntersectSmallBox(int[] closestFace, Vector3D[] physicsVerts, BoundingBoxD bWorldAabb, List<Vector3D> intersections)
        {
            for (int i = 0; i < closestFace.Length; i += 3)
            {
                var v0 = physicsVerts[closestFace[i]];
                var v1 = physicsVerts[closestFace[i + 1]];
                var v2 = physicsVerts[closestFace[i + 2]];
                var test1 = bWorldAabb.IntersectsTriangle(v0, v1, v2);
                if (!test1) continue;
                intersections.Add(v0); 
                intersections.Add(v1);
                intersections.Add(v2);
            }
        }

        public static Vector3D ClosestVertArray(Vector3D[] physicsVerts, Vector3D pos, int limit = -1)
        {
            if (limit == -1) limit = physicsVerts.Length;
            var minValue1 = double.MaxValue;
            var closestVert = Vector3D.NegativeInfinity;
            for (int p = 0; p < limit; p++)
            {
                var vert = physicsVerts[p];
                var range = vert - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue1)
                {
                    minValue1 = test;
                    closestVert = vert;
                }
            }
            return closestVert;
        }

        public static Vector3D ClosestVertList(List<Vector3D> physicsVerts, Vector3D pos, int limit = -1)
        {
            if (limit == -1) limit = physicsVerts.Count;
            var minValue1 = double.MaxValue;
            var closestVert = Vector3D.NegativeInfinity;
            for (int p = 0; p < limit; p++)
            {
                var vert = physicsVerts[p];
                var range = vert - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue1)
                {
                    minValue1 = test;
                    closestVert = vert;
                }
            }
            return closestVert;
        }

        public static int ClosestVertNum(Vector3D[] physicsVerts, Vector3D pos)
        {
            var minValue1 = double.MaxValue;
            var closestVertNum = int.MaxValue;

            for (int p = 0; p < physicsVerts.Length; p++)
            {
                var vert = physicsVerts[p];
                var range = vert - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue1)
                {
                    minValue1 = test;
                    closestVertNum = p;
                }
            }
            return closestVertNum;
        }

        public static int GetClosestTri(Vector3D[] physicsOutside, Vector3D pos)
        {
            var triDist1 = double.MaxValue;
            var triNum = 0;

            for (int i = 0; i < physicsOutside.Length; i += 3)
            {
                var ov0 = physicsOutside[i];
                var ov1 = physicsOutside[i + 1];
                var ov2 = physicsOutside[i + 2];
                var otri = new Triangle3d(ov0, ov1, ov2);
                var odistTri = new DistPoint3Triangle3(pos, otri);
                odistTri.Update(pos, otri);

                var test = odistTri.GetSquared();
                if (test < triDist1)
                {
                    triDist1 = test;
                    triNum = i;
                }
            }
            return triNum;
        }


        public static bool EllipsoidIntersects(MatrixD matrixA, MatrixD matrixB)
        {
            Vector3D v = matrixB.Translation - matrixA.Translation;
            Vector3D w = Support(matrixA, matrixB, v);
            if (Vector3D.Dot(w, v) <= 0)
                return false;

            List<Vector3D> simplex = new List<Vector3D> { w };

            for (int i = 0; i < 100; ++i)
            {
                v = -w;
                w = Support(matrixA, matrixB, v);
                if (Vector3D.Dot(w, v) <= 0)
                    return false;

                simplex.Add(w);
                if (ContainsOrigin(simplex, out w))
                    return true;
            }

            return false;
        }

        private static Vector3D Support(MatrixD matrixA, MatrixD matrixB, Vector3D direction)
        {
            Vector3D supportA = Vector3D.TransformNormal(direction, MatrixD.Transpose(matrixA));
            Vector3D supportB = Vector3D.TransformNormal(direction, MatrixD.Transpose(matrixB));
            supportA.Normalize();
            supportB.Normalize();
            supportA *= GetEllipsoidRadius(matrixA);
            supportB *= GetEllipsoidRadius(matrixB);
            supportA = Vector3D.Transform(supportA, matrixA);
            supportB = Vector3D.Transform(supportB, matrixB);

            return supportA - supportB;
        }

        private static double GetEllipsoidRadius(MatrixD matrix)
        {
            Vector3D xAxis = matrix.Right;
            Vector3D yAxis = matrix.Up;
            Vector3D zAxis = matrix.Forward;
            double xRadius = xAxis.Length();
            double yRadius = yAxis.Length();
            double zRadius = zAxis.Length();

            return Math.Max(xRadius, Math.Max(yRadius, zRadius));
        }

        private static bool ContainsOrigin(List<Vector3D> simplex, out Vector3D direction)
        {
            direction = Vector3D.Zero;

            if (simplex.Count == 1)
            {
                direction = -simplex[0];
                return false;
            }
            else if (simplex.Count == 2)
            {
                Vector3D a = simplex[1];
                Vector3D b = simplex[0];
                Vector3D ab = b - a;
                direction = Vector3D.Cross(Vector3D.Cross(ab, -a), ab);
                if (direction.LengthSquared() <= 0)
                {
                    direction = Vector3D.Cross(Vector3D.Cross(ab, -a), Vector3D.Cross(ab, Vector3D.UnitX));
                    if (direction.LengthSquared() <= 0)
                        direction = Vector3D.Cross(Vector3D.Cross(ab, -a), Vector3D.UnitY);
                }

                direction.Normalize();
                return false;
            }
            else if (simplex.Count == 3)
            {
                Vector3D a = simplex[2];
                Vector3D b = simplex[1];
                Vector3D c = simplex[0];
                Vector3D ab = b - a;
                Vector3D ac = c - a;
                Vector3D bc = c - b;
                Vector3D abc = Vector3D.Cross(ab, ac);
                if (Vector3D.Dot(abc, -a) > 0)
                {
                    simplex.RemoveAt(1);
                    direction = Vector3D.Cross(Vector3D.Cross(ac, -a), ac);
                }
                else if (Vector3D.Dot(abc, -b) > 0)
                {
                    simplex.RemoveAt(0);
                    direction = Vector3D.Cross(Vector3D.Cross(bc, -b), bc);
                }
                else
                {
                    direction = abc;
                }

                direction.Normalize();
                return false;
            }
            else
            {
                return true;
            }
        }

        public static Vector3D? ObbIntersect(MyOrientedBoundingBoxD obb, MatrixD matrix, MatrixD matrixInv)
        {
            var corners = new Vector3D[9];
            // 4 + 5 + 6 + 7 = Front
            // 0 + 1 + 2 + 3 = Back
            // 1 + 2 + 5 + 6 = Top
            // 0 + 3 + 4 + 7 = Bottom
            obb.GetCorners(corners, 0);
            corners[8] = obb.Center;
            var point0 = corners[0];
            if (Vector3.Transform(point0, matrixInv).LengthSquared() <= 1) return point0;
            var point1 = corners[1];
            if (Vector3.Transform(point1, matrixInv).LengthSquared() <= 1) return point1;
            var point2 = corners[2];
            if (Vector3.Transform(point2, matrixInv).LengthSquared() <= 1) return point2;
            var point3 = corners[3];
            if (Vector3.Transform(point3, matrixInv).LengthSquared() <= 1) return point3;
            var point4 = corners[4];
            if (Vector3.Transform(point4, matrixInv).LengthSquared() <= 1) return point4;
            var point5 = corners[5];
            if (Vector3.Transform(point5, matrixInv).LengthSquared() <= 1) return point5;
            var point6 = corners[6];
            if (Vector3.Transform(point6, matrixInv).LengthSquared() <= 1) return point6;
            var point7 = corners[7];
            if (Vector3.Transform(point7, matrixInv).LengthSquared() <= 1) return point7;
            var point8 = corners[8];
            if (Vector3.Transform(point8, matrixInv).LengthSquared() <= 1) return point8;

            var blockSize = (float)obb.HalfExtent.AbsMax() * 2;
            var testDir = Vector3D.Normalize(point0 - point1);
            var ray = new RayD(point0, -testDir);
            var intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point1) >= Vector3D.DistanceSquared(point1, point))
                {
                    //Log.Line($"ray0: {intersect} - {Vector3D.Distance(point1, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point3);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray1: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point4);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point4) >= Vector3D.DistanceSquared(point4, point))
                {
                    //Log.Line($"ray2: {intersect} - {Vector3D.Distance(point4, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point2);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point2) >= Vector3D.DistanceSquared(point2, point))
                {
                    //Log.Line($"ray3: {intersect} - {Vector3D.Distance(point2, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point5);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray4: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point3);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray5: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point6);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray6: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point3 - point7);
            ray = new RayD(point3, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point3, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray7: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point5);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray8: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point7);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray9: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point5 - point6);
            ray = new RayD(point5, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point5, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray10: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point6 - point7);
            ray = new RayD(point6, -testDir);
            intersect = IntersectEllipsoid(ref matrixInv, matrix, ref ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point6, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray11: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }
            return null;
        }
    }
}
