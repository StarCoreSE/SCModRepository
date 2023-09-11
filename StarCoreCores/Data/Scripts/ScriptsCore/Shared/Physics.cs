using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace MIG.Shared.SE {
    public class PhysicsHelper {
        public static bool areSameDirection (Vector3D a, Vector3D b) {
            a.Normalize();
            b.Normalize();
            var d = a - b;
            if (d.Length () < 0.05) {
                return true;
            }
            return false;
        }
        
        public static void Draw (Color c, Vector3D start, Vector3D vec, float thick = 0.05f, string material = "Square") {
            if (!MyAPIGateway.Session.isTorchServer()) {
                var n = (float)vec.Normalize();
                MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute(material), c, start, vec, n, thick);//, MyBillboard.BlendTypeEnum.Standard);//, -1, 100f);
            }
        }

        public static void DrawPoint(Color c, Vector3D start, float radius, float angle = 0, string material = "Square")
        {
            if (!MyAPIGateway.Session.isTorchServer())
            {
                MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute(material), c, start, radius, angle);//, MyBillboard.BlendTypeEnum.Standard);//, -1, 100f);
            }
        }

        public static void DrawLine(Color c, Vector3D start, Vector3D vec, float thick, MyStringId material)
        {
            var n = (float)vec.Normalize();
            MyTransparentGeometry.AddLineBillboard(material, c, start, vec, n, thick);
        }

        public static void DrawLineFromTo(Color c, Vector3D start, Vector3D end, float thick, MyStringId material)
        {
            var v = (end - start);
            var n = (float)v.Normalize();
            MyTransparentGeometry.AddLineBillboard(material, c, start, v, n, thick);
        }
        
        public static void DrawLineFromTo(Color c, Vector3D start, Vector3D end, float thick)
        {
            DrawLineFromTo (c, start, end, thick, MyStringId.GetOrCompute("Square"));
        }

        public static void DrawPoint(Color c, Vector3D start, float radius, float angle, MyStringId material)
        {
            MyTransparentGeometry.AddPointBillboard(material, c, start, radius, angle);
        }
    }
}