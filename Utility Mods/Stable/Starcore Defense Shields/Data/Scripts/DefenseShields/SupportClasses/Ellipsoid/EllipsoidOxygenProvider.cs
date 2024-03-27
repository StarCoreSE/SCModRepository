using VRage.Game.ModAPI;
using VRageMath;

namespace DefenseShields.Support
{
    class EllipsoidOxygenProvider : IMyOxygenProvider
    {
        public MatrixD O2Matrix;
        public double O2Level;

        public EllipsoidOxygenProvider(MatrixD matrix)
        {
            O2Matrix = matrix;
        }

        public void UpdateOxygenProvider(MatrixD matrix, double o2Level)
        {
            O2Matrix = matrix;
            O2Level = o2Level;
        }

        public float GetOxygenForPosition(Vector3D worldPoint)
        {
            var inShield = CustomCollision.PointInShield(worldPoint, O2Matrix);
            if (inShield)
            {
                return (float)O2Level;
            }
            return 0f;
        }

        public bool IsPositionInRange(Vector3D worldPoint)
        {
            return CustomCollision.PointInShield(worldPoint, O2Matrix);
        }
    }
}
