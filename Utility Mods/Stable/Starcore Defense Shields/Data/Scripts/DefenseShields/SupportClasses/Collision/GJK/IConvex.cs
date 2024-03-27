using VRageMath;

namespace GjkShapes
{
    public interface IConvex
    {
        Vector3D Center { get; }
        
        // [Pure]
        Vector3D FarthestInDirection(Vector3D dir);
    }
}