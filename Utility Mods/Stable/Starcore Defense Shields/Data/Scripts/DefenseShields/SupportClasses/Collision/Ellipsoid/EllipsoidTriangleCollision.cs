using VRageMath;

namespace DefenseShields
{
    static class EllipsoidTriangleCollision
    {
        public static CollisionResult CollidesWith(this BoundingEllipsoid ellipsoid, Triangle triangle, Vector3D velocity)
        {
            BoundingSphereD sphere = new BoundingSphereD(ellipsoid.Center / ellipsoid.Radius, 1d);
            Vector3D eVelocity = velocity / ellipsoid.Radius;
            Triangle eTriangle = triangle / ellipsoid.Radius;

            CollisionResult collisionResult = sphere.CollidesWith(eTriangle, eVelocity);
            collisionResult.IntersectionPoint *= ellipsoid.Radius;
            return collisionResult;
        }
    }
}
