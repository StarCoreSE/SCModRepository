using VRageMath;

namespace DefenseShields
{
    public class BoundingEllipsoid
    {
        private Vector3D originalCenter;
        private Vector3D originalRadius;

        private Vector3D center;
        private Vector3D radius;

        private MatrixD world;

        public Vector3D Center { get { return center; } set { center = value; } }
        public Vector3D Radius { get { return radius; } set { radius = value; } }
        public MatrixD World { get { return world; } }

        public BoundingEllipsoid(Vector3D center, Vector3D radius)
        {
            this.originalCenter = this.center = center;
            this.originalRadius = this.radius = radius;
        }

        public BoundingEllipsoid(BoundingSphereD sphere)
        {
            this.originalCenter = center = sphere.Center;
            this.originalRadius = radius = new Vector3D(sphere.Radius);
        }

        public void Transform(MatrixD matrix)
        {
            world = matrix;
            center = Vector3D.Transform(originalCenter, matrix);
            radius = Vector3D.TransformNormal(originalRadius, matrix);
        }

        public override string ToString()
        {
            return "Center: " + center + " Radius: " + radius;
        }
    }
}
