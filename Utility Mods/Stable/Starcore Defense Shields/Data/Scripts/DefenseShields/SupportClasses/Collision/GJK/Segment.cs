using VRageMath;

namespace GjkShapes
{
    public struct LineSegment : IConvex
    {
        public readonly Vector3D A, B;

        public LineSegment(Vector3D a, Vector3D b)
        {
            A = a;
            B = b;
        }

        public Vector3D Center => (A + B) / 2;
        public Vector3D FarthestInDirection(Vector3D dir)
        {
            return dir.Dot(A) > dir.Dot(B) ? A : B;
        }
    }
}