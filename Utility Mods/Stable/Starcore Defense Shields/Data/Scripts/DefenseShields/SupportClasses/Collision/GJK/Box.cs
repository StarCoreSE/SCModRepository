using VRageMath;

namespace GjkShapes
{
    public struct Box : IConvex
    {
        public readonly Vector3D Min, Max;

        public Box(Vector3D min, Vector3D max)
        {
            Min = Vector3D.Min(min, max);
            Max = Vector3D.Max(min, max);
        }

        public Vector3D Center => (Min + Max) / 2;
        public Vector3D FarthestInDirection(Vector3D dir)
        {
            return new Vector3D
            {
                X = dir.X < 0 ? Min.X : Max.X,
                Y = dir.Y < 0 ? Min.Y : Max.Y,
                Z = dir.Z < 0 ? Min.Z : Max.Z
            };
        }
    }
}