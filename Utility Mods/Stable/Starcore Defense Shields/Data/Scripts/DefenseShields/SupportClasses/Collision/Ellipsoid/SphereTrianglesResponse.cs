using VRageMath;

namespace DefenseShields
{
    public class SphereTrianglesResponse
    {
        private const int MAX_RECURSSION_DEPTH = 1;
        private static Vector3D gravity = new Vector3D(0, -0.08d, 0);
        public static bool gravityOn = false;

        public const double unitsPerMeter = 100.0d;
        public const double VeryCloseDistance = unitsPerMeter * .00005d;
        private static double radius;

        public static Vector3D CollideAndSlide(BoundingSphereD sphere, Vector3D velocity, Triangle[] triangles)
        {
            radius = sphere.Radius;
            sphere.Center /= radius;
            sphere.Radius = 1.0d;

            velocity = CollideWithTriangles(sphere, velocity / radius, triangles, 0);

            //Add gravity pool
            if (gravityOn)
            {
                sphere.Center += velocity;
                velocity += CollideWithTriangles(sphere, gravity / radius, triangles, 0);
            }

            return velocity * radius;
        }

        private static Vector3D CollideWithTriangles(BoundingSphereD sphere, Vector3D velocity,
                                                        Triangle[] triangles, int recurssionDepth)
        {
            if (recurssionDepth > MAX_RECURSSION_DEPTH)
            {
                return Vector3D.Zero;
            }

            CollisionResult collision = CollidesWith(sphere, triangles, velocity);

            if (!collision.FoundCollision)
            {
                return velocity;
            }

            Vector3D originalDestinationPoint = sphere.Center + velocity;

            double intersectionDistance = velocity.Length() * collision.IntersectionTime;
            //Only update if we aren't very close, and if so only move very close
            //if (intersectionDistance >= VeryCloseDistance)
            //{
            Vector3D normalizedVelocity = velocity.Normalized();

            velocity = (intersectionDistance - VeryCloseDistance) * normalizedVelocity;
            sphere.Center += velocity;

            //Fake the collision results to match the very close approximation
            collision.IntersectionPoint -= normalizedVelocity * VeryCloseDistance;
            //}

            PlaneD slidingPlane = CollisionExtensions.Plane(collision.IntersectionPoint, sphere.Center - collision.IntersectionPoint);
            Vector3D destinationPoint = originalDestinationPoint.ProjectOn(slidingPlane);

            Vector3D newVelocityVector = destinationPoint - collision.IntersectionPoint;
            if (newVelocityVector.Length() < VeryCloseDistance)
            {
                return velocity;
            }

            return velocity + CollideWithTriangles(sphere, newVelocityVector, triangles, recurssionDepth + 1);
        }

        private static CollisionResult CollidesWith(BoundingSphereD sphere, Triangle[] triangles, Vector3D velocity)
        {
            CollisionResult closestCollision = CollisionResult.NoCollision;
            foreach (Triangle triangle in triangles)
            {
                CollisionResult collision = sphere.CollidesWith(triangle / new Vector3D(radius), velocity);
                if (collision.IntersectionTime < closestCollision.IntersectionTime)
                {
                    closestCollision = collision;
                }
            }
            return closestCollision;
        }
    }
}
