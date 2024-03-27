using VRageMath;

namespace DefenseShields
{
    public class Edge
    {
        private Vector3D[] vertices;
        private Vector3D direction;

        public Vector3D Vertex1 { get { return vertices[0]; } }
        public Vector3D Vertex2 { get { return vertices[1]; } }
        public Vector3D Direction { get { return direction; } }

        public Edge(Vector3D vertex1, Vector3D vertex2)
        {
            vertices = new Vector3D[2];
            vertices[0] = vertex1;
            vertices[1] = vertex2;

            direction = vertices[1] - vertices[0];
        }

        public double LengthSquared()
        {
            return direction.LengthSquared();
        }

        public double Dot(Vector3D vector)
        {
            return direction.Dot(vector);
        }
    }
}
