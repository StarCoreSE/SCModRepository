using VRageMath;

namespace GjkShapes
{
    public struct ConvexHull : IConvex
    {
        private readonly Vector3D[] _pts;

        public ConvexHull(Vector3D[] pts)
        {
            _pts = pts;
            var tmp = Vector3D.Zero;
            foreach (var k in pts)
                tmp += k;
            Center = tmp / pts.Length;
        }
        public Vector3D Center { get; }
        public Vector3D FarthestInDirection(Vector3D dir)
        {
            var best = Vector3D.Zero;
            var bestDot = double.NegativeInfinity;
            double dotTmp;
            for (var i = 0; i < _pts.Length; i++)
            {
                Vector3D.Dot(ref _pts[i], ref dir, out dotTmp);
                if (dotTmp > bestDot)
                {
                    best = _pts[i];
                    bestDot = dotTmp;
                }
            }

            return best;
        }
    }
}