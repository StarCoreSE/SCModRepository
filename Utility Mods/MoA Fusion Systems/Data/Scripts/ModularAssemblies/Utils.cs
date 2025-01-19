using VRageMath;

namespace Epstein_Fusion_DS
{
    public static class Utils
    {
        // TODO make this less inefficient.
        public static Matrix RotateMatrixAroundPoint(Matrix matrix, Vector3D point, Vector3D axis, double angleRadians)
        {
            matrix.Translation -= point;
            Matrix rotation = MatrixD.CreateFromAxisAngle(axis, angleRadians);

            Matrix transformedMatrix =  matrix * rotation;
            transformedMatrix.Translation += point;

            return transformedMatrix;
        }
    }
}
