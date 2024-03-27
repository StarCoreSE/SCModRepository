using VRageMath;

namespace DefenseShields
{
    class CollisionResult
    {
        bool foundCollision;
        double intersectionTime;
        Vector3D intersectionPoint;

        public bool FoundCollision { get { return foundCollision; } set { foundCollision = value; } }
        public double IntersectionTime { get { return intersectionTime; } set { intersectionTime = value; } }
        public Vector3D IntersectionPoint { get { return intersectionPoint; } set { intersectionPoint = value; } }

        public static CollisionResult NoCollision
        {
            get { return new CollisionResult(false, 2.0d, Vector3D.Zero); }
        }

        public CollisionResult(bool foundCollision, double intersectionTime, Vector3D intersectionPoint)
        {
            this.intersectionTime = intersectionTime;
            this.foundCollision = foundCollision;
            this.intersectionPoint = intersectionPoint;
        }
    }
}
