using VRageMath;

namespace DefenseShields
{
    static class SphereTriangleCollision
    {
        public static CollisionResult CollidesWith(this BoundingSphereD sphere, Triangle triangle, Vector3D velocity)
        {
            CollisionResult collisionResult = CollisionResult.NoCollision;
            /*if (!triangle.Plane.IsFrontFacingTo(velocity.Normalized()))
            {
                return CollisionResult.NoCollision;
            }*/

            if (velocity.IsParallelTo(triangle.Plane))
            {
                if (!sphere.EmbeddedIn(triangle.Plane))
                {
                    return CollisionResult.NoCollision;
                }
            }
            else
            {
                #region CollisionInsideTriangle
                //Some precalculated variables we'll be using
                double signedDistToTrianglePlane = triangle.Plane.SignedDistanceTo(sphere.Center);
                double normalDotVelocity = triangle.Plane.DotNormal(velocity);

                //parameters representing relative points along the velocity vector,
                //therefor ranging from 0 (current position)
                //to 1 (position after current velocity has been applied)
                //Calculate intersection values along the velocity vector
                double t0 = (-sphere.Radius - signedDistToTrianglePlane) / normalDotVelocity;
                double t1 = (sphere.Radius - signedDistToTrianglePlane) / normalDotVelocity;
                //Make sure t0 < t1
                MathExtensions.MinMax(ref t0, ref t1);
                //t1 is "less than or equal to" to prevent from being stuck when touching an object
                if (t0 > 1.0d || t1 <= 0.0d)
                {
                    //If t0-t1 is outside the range (0-1) there is no collision along the velocity vector
                    return CollisionResult.NoCollision;
                }
                //Clamps t0 and t1 to the range (0-1), as we only care about the current frame
                //in which the values represent relative points along the current velocity vector.
                t0 = MathHelper.Clamp(t0, 0, 1);
                t1 = MathHelper.Clamp(t1, 0, 1);

                Vector3D planeIntersectionPoint =
                    sphere.Center - triangle.Plane.Normal * sphere.Radius + velocity * t0;

                //If the intersection point is in the triangle, we have a collision.
                //Since a collision with the "inside" of the triangle takes place before
                //any collision with its vertices or edges, this is the closest collision,
                //so we can safely return
                if (triangle.ContainsPoint(planeIntersectionPoint))
                {
                    collisionResult.FoundCollision = true;
                    collisionResult.IntersectionTime = t0;
                    collisionResult.IntersectionPoint = planeIntersectionPoint;
                    return collisionResult;
                }
                #endregion
            }

            foreach (Vector3D vertex in triangle.Vertices)
            {
                CollisionResult vertexCollisionResult = sphere.CollidesWith(vertex, velocity);
                if (vertexCollisionResult.FoundCollision &&
                            vertexCollisionResult.IntersectionTime < collisionResult.IntersectionTime)
                {
                    collisionResult = vertexCollisionResult;
                }
            }

            foreach (Edge edge in triangle.Edges)
            {
                CollisionResult edgeCollisionResult = sphere.CollidesWith(edge, velocity);
                if (edgeCollisionResult.FoundCollision &&
                            edgeCollisionResult.IntersectionTime < collisionResult.IntersectionTime)
                {
                    collisionResult = edgeCollisionResult;
                }
            }

            return collisionResult;
        }
    }
}
