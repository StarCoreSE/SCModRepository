using VRageMath;

namespace GjkShapes
{
    public struct ConvexTranslate<TBacking> : IConvex where TBacking : struct, IConvex
    {
        public Vector3D Translate;
        public TBacking Backing;

        public ConvexTranslate(TBacking shape, Vector3D translate)
        {
            Backing = shape;
            Translate = translate;
        }

        public Vector3D Center => Backing.Center + Translate;
        public Vector3D FarthestInDirection(Vector3D dir)
        {
            return Backing.FarthestInDirection(dir) + Translate;
        }
    }

    public static class ConvexTranslateHelper
    {
        public static ConvexTranslate<TBacking> Translated<TBacking>(this TBacking shape, Vector3D translate) where TBacking : struct, IConvex
        {
            return new ConvexTranslate<TBacking>(shape, translate);
        }
    }
}