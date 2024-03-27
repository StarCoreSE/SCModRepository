using System;
using VRageMath;

namespace GjkShapes
{
    public struct Sphere : IConvex
    {
        public Sphere(Vector3D center, double radius)
        {
            Center = center;
            Radius = radius;
        }

        public Vector3D Center { get; }
        public readonly double Radius;
        
        public Vector3D FarthestInDirection(Vector3D dir)
        {
            dir.Normalize();
            return Center + dir * Radius;
        }
    }
}