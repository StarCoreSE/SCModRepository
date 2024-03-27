using VRageMath;

namespace GjkShapes
{
    public struct ConvexTransform<TBacking> : IConvex where TBacking : struct, IConvex
    {
        public MatrixD Transform;
        public MatrixD TransformInv;
        public TBacking Backing;

        public ConvexTransform(TBacking shape, MatrixD transform, MatrixD? transformInv = null)
        {
            Backing = shape;
            Transform = transform;
            if (transformInv.HasValue)
                TransformInv = transformInv.Value;
            else
                MatrixD.Invert(ref transform, out TransformInv);
        }

        public Vector3D Center => Vector3D.Transform(Backing.Center, ref Transform);

        public Vector3D FarthestInDirection(Vector3D dir)
        {
            return Vector3D.Transform(Backing.FarthestInDirection(Vector3D.TransformNormal(dir, ref TransformInv)), ref Transform);
        }
    }

    public static class ConvexTransformHelper
    {
        public static ConvexTransform<TBacking> Transformed<TBacking>(this TBacking shape, MatrixD transform, MatrixD? transformInv = null) where TBacking : struct, IConvex
        {
            return new ConvexTransform<TBacking>(shape, transform, transformInv);
        }
    }
}