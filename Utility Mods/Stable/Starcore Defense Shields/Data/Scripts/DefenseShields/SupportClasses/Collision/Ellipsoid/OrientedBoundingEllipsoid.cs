using VRageMath;

namespace CollisionDetection
{
    struct OrientedBoundingEllipsoid
    {
        private Vector3D center;
        private Vector3D radius;
        private Quaternion orientation;
        private MatrixD world;

        public Vector3D Center { get { return center; } set { center = value; } }
        public Vector3D Radius { get { return radius; } set { radius = value; } }
        public Quaternion Orientation { get { return orientation; } set { orientation = value; } }
        public MatrixD World { get { return world; } set { world = value; } }

        public OrientedBoundingEllipsoid(Vector3D center, Vector3D radius, Quaternion orientation)
        {
            this.center = center;
            this.radius = radius;
            this.orientation = orientation;
            world = MatrixD.Identity;
        }

        public OrientedBoundingEllipsoid(BoundingSphereD sphere)
            : this(sphere.Center, new Vector3D(sphere.Radius), Quaternion.Identity) { }
    }
}
