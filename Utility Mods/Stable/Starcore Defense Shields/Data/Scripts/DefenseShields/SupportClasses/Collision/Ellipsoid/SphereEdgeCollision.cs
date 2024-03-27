using VRageMath;

namespace DefenseShields
{
    static class SphereEdgeCollision
    {
        public static CollisionResult CollidesWith(this BoundingSphereD sphere, Edge edge, Vector3D velocity)
        {
            CollisionResult collisionResult = CollisionResult.NoCollision;

            Vector3D centerToVertex = edge.Vertex1 - sphere.Center;
            double edgeSquaredLength = edge.LengthSquared();
            double edgeDotVelocity = edge.Dot(velocity);
            double edgeDotCenterToVertex = edge.Dot(centerToVertex);

            //calculate the parameters for the equation
            double a = edgeSquaredLength * -velocity.LengthSquared() + edgeDotVelocity * edgeDotVelocity;
            double b = 2 * (edgeSquaredLength * velocity.Dot(centerToVertex) -
                           edgeDotVelocity * edgeDotCenterToVertex);
            double c = edgeSquaredLength * (sphere.Radius * sphere.Radius - centerToVertex.LengthSquared()) +
                edgeDotCenterToVertex * edgeDotCenterToVertex;
            Vector2D? result = MathExtensions.SolveQuadricEquation(a, b, c);
            if (result.HasValue)
            {
                Vector2D solutions = result.GetValueOrDefault();
                //Not "bigger or equals to" to prevent being stuck when touching an object
                if (solutions.X > 0.0d)
                {
                    if (solutions.X <= 1.0d)
                    {
                        double f = (edgeDotVelocity * solutions.X - edgeDotCenterToVertex) / edgeSquaredLength;
                        if (f.InRange(0.0d, 1.0d))
                        {
                            collisionResult.FoundCollision = true;
                            collisionResult.IntersectionTime = solutions.X;
                            collisionResult.IntersectionPoint = edge.Vertex1 + f * edge.Direction;
                        }
                    }
                }
                else if (solutions.Y > 0.0d && solutions.Y <= 1.0d)
                {
                    double f = (edgeDotVelocity * solutions.Y - edgeDotCenterToVertex) / edgeSquaredLength;
                    if (f.InRange(0.0d, 1.0d))
                    {
                        collisionResult.FoundCollision = true;
                        collisionResult.IntersectionTime = solutions.Y;
                        collisionResult.IntersectionPoint = edge.Vertex1 + f * edge.Direction;
                    }
                }
            }

            return collisionResult;
        }
    }
}
