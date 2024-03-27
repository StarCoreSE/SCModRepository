using VRageMath;

namespace DefenseShields
{
    static class CollisionExtensions
    {
        public static double SignedDistanceTo(this PlaneD plane, Vector3D point)
        {
            return plane.DotCoordinate(point);
        }

        public static double Dot(this Vector3D vector1, Vector3D vector2)
        {
            return Vector3D.Dot(vector1, vector2);
        }

        public static bool IsParallelTo(this Vector3D vector, PlaneD plane)
        {
            return plane.DotNormal(vector) == 0.0d;
        }

        public static bool IsFrontFacingTo(this PlaneD plane, Vector3D direction)
        {
            return plane.DotNormal(direction) <= 0.0d;
        }

        public static bool EmbeddedIn(this BoundingSphereD sphere, PlaneD plane)
        {
            return plane.Intersects(sphere) == PlaneIntersectionType.Intersecting;
        }

        public static Vector3D Normalized(this Vector3D vector)
        {
            Vector3D normalizedVector = vector;
            normalizedVector.Normalize();
            return normalizedVector;
        }

        public static PlaneD Plane(Vector3D origin, Vector3D normal)
        {
            return new PlaneD(normal, -normal.Dot(origin));
        }

        public static Vector3D ProjectOn(this Vector3D vector, PlaneD plane)
        {
            return vector - plane.SignedDistanceTo(vector) * plane.Normal;
        }
    }
}
