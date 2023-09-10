using System;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Render.Scene;
using VRage.Utils;

namespace invalid.drawthefuckingsphereplease
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class SphereDrawing : MySessionComponentBase
    {
        public override void UpdateAfterSimulation()
        {
            // Get player's position
            Vector3D playerPosition = MyAPIGateway.Session.Player.GetPosition();

            // Define the color of the box as red (ARGB format)
            Color boxColor = new Color(255, 0, 0, 128);

            // Calculate directional vector from player to origin (0,0,0)
            Vector3D directionToOrigin = Vector3D.Normalize(Vector3D.Zero - playerPosition);

            // Calculate the position for the box at 100m distance *toward* (0,0,0) from the player
            Vector3D boxCenter = playerPosition + (directionToOrigin * -100);

            // Define the half-extents of the box
            Vector3 halfExtents = new Vector3(2.5f);

            // Create BoundingBoxD for the box
            BoundingBoxD boundingBox = new BoundingBoxD(boxCenter - halfExtents, boxCenter + halfExtents);

            // Create transformation matrix for the box
            MatrixD boxWorldMatrix = MatrixD.CreateWorld(boxCenter);

            // Draw the box
            MySimpleObjectDraw.DrawTransparentBox(ref boxWorldMatrix, ref boundingBox, ref boxColor, MySimpleObjectRasterizer.Solid, 1, 0.1f, MyStringId.GetOrCompute("Square"));
        }

        protected override void UnloadData()
        {

        }
    }
}
