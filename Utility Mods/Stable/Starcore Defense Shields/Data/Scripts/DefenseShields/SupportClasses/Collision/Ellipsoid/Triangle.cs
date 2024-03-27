using System.Collections.Generic;
using VRageMath;

namespace DefenseShields
{
    public class Triangle
    {
        private Vector3D[] vertices;
        private PlaneD plane;

        public Vector3D Vertex1 { get { return vertices[0]; } }
        public Vector3D Vertex2 { get { return vertices[1]; } }
        public Vector3D Vertex3 { get { return vertices[2]; } }
        public Vector3D[] Vertices { get { return vertices; } }
        public PlaneD Plane { get { return plane; } }
        public IEnumerable<Edge> Edges
        {
            get
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    yield return new Edge(vertices[i], vertices[(i + 1) % vertices.Length]);
                }
            }
        }


        public Triangle(Vector3D vertex1, Vector3D vertex2, Vector3D vertex3)
        {
            vertices = new Vector3D[3] { vertex1, vertex2, vertex3 };

            plane = new PlaneD(vertices[0], vertices[1], vertices[2]);
        }

        public bool ContainsPoint(Vector3 point)
        {
            // Compute vectors        
            Vector3D v0 = vertices[2] - vertices[0];
            Vector3D v1 = vertices[1] - vertices[0];
            Vector3D v2 = point - vertices[0];

            // Compute dot products
            double dot00 = v0.Dot(v0);
            double dot01 = v0.Dot(v1);
            double dot02 = v0.Dot(v2);
            double dot11 = v1.Dot(v1);
            double dot12 = v1.Dot(v2);

            // Compute barycentric coordinates
            double invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            double u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            double v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle
            return (u > 0) && (v > 0) && (u + v < 1);
        }

        public static Triangle operator /(Triangle triangle, Vector3D vector)
        {
            return new Triangle(triangle.Vertices[0] / vector,
                triangle.Vertices[1] / vector,
                triangle.Vertices[2] / vector);
        }

        public Triangle Transform(MatrixD matrix)
        {
            return new Triangle(Vector3D.Transform(vertices[0], matrix),
                Vector3D.Transform(vertices[1], matrix),
                Vector3D.Transform(vertices[2], matrix));
        }

        public Triangle TransformNormal(MatrixD matrix)
        {
            return new Triangle(Vector3D.TransformNormal(vertices[0], matrix),
                Vector3D.TransformNormal(vertices[1], matrix),
                Vector3D.TransformNormal(vertices[2], matrix));
        }
    }
}
