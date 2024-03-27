using VRageMath;

namespace DefenseShields
{
    static class SpherePointCollision
    {
        public static CollisionResult CollidesWith(this BoundingSphereD sphere, Vector3D point, Vector3D velocity)
        {
            CollisionResult collisionResult = CollisionResult.NoCollision;

            /* The collision with a vertex comes down to a quadratic equation
             * with a maximum of two possible points of collision.           */
            double a = velocity.LengthSquared();
            double b = 2.0d * velocity.Dot(sphere.Center - point);
            double c = (point - sphere.Center).LengthSquared() - sphere.Radius * sphere.Radius;
            Vector2D? result = MathExtensions.SolveQuadricEquation(a, b, c);

            if (result.HasValue)
            {
                Vector2D solutions = result.GetValueOrDefault();
                //Not "bigger or equals to" to prevent being stuck when touching an object
                if (solutions.X > 0.0d)
                {
                    if (solutions.X <= 1.0d)
                    {
                        collisionResult.FoundCollision = true;
                        collisionResult.IntersectionTime = solutions.X;
                        collisionResult.IntersectionPoint = point;
                    }
                }
                else if (solutions.Y > 0.0d && solutions.Y <= 1.0d)
                {
                    collisionResult.FoundCollision = true;
                    collisionResult.IntersectionTime = solutions.Y;
                    collisionResult.IntersectionPoint = point;
                }
            }
            return collisionResult;
        }
    }
}
