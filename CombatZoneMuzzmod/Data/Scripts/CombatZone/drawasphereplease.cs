using System;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Utils;

namespace YourModNamespace
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class BoxDrawing : MySessionComponentBase
    {
        private const int numberOfBoxes = 10; // Number of boxes in the line

        public override void UpdateAfterSimulation()
        {
            // Get player's position
            Vector3D playerPosition = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

            // Define the color of the boxes as red (ARGB format)
            Color boxColor = new Color(255, 0, 0, 128);

            // Calculate directional vector from player to origin (0,0,0)
            Vector3D directionToOrigin = Vector3D.Normalize(Vector3D.Zero - playerPosition);

            // Calculate the distance from the player to the origin
            double distanceToOrigin = playerPosition.Length();

            // Calculate the spacing between boxes based on distance (adjust this as needed)
            float boxSpacing = Math.Max((float)(distanceToOrigin / numberOfBoxes), 10f); // Minimum spacing of 10 meters

            // Calculate the position for the first box to be 10m away in the direction of (0,0,0) from the player
            Vector3D boxPosition = playerPosition + (directionToOrigin * 10);

            // Define the half-extents of the box (assuming a 5m x 5m x 5m box)
            Vector3 halfExtents = new Vector3(2.5f, 2.5f, 2.5f);

            // Calculate the rotation matrix to align the boxes with the direction to the origin
            MatrixD rotationMatrix = MatrixD.CreateWorld(Vector3D.Zero, directionToOrigin, Vector3D.Up);

            // Calculate the position for the blue wall
            Vector3D blueWallPosition = Vector3D.Zero + (directionToOrigin * 1000);

            // Check if the player is more than 1000m away from the origin
            if (distanceToOrigin > 1000)
            {
                // Create a "wall" of blue boxes behind the player
                Color wallColor = new Color(0, 0, 255, 128);

                // Create BoundingBoxD for the blue wall
                BoundingBoxD wallBox = new BoundingBoxD(-halfExtents, halfExtents);

                // Calculate the size of the blue wall
                float wallSize = numberOfBoxes * boxSpacing;

                // Transform the blue wall by the player's position and rotation
                wallBox = wallBox.TransformFast(rotationMatrix);
                wallBox.Translate(blueWallPosition);

                // Draw the blue wall using MySimpleObjectDraw.DrawTransparentBox
                MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref wallBox, ref wallColor, MySimpleObjectRasterizer.Solid, 1, 0.1f, MyStringId.GetOrCompute("Square"));
            }

            // Loop to draw multiple red boxes in a line
            for (int i = 0; i < numberOfBoxes; i++)
            {
                // Create BoundingBoxD for the red box
                BoundingBoxD boundingBox = new BoundingBoxD(-halfExtents, halfExtents);

                // Transform the red box by the player's position and rotation
                boundingBox = boundingBox.TransformFast(rotationMatrix);
                boundingBox.Translate(boxPosition);

                // Draw the red box using MySimpleObjectDraw.DrawTransparentBox
                MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref boundingBox, ref boxColor, MySimpleObjectRasterizer.Solid, 1, 0.1f, MyStringId.GetOrCompute("Square"));

                // Move the red box position along the line
                boxPosition += directionToOrigin * boxSpacing;
            }
        }

        protected override void UnloadData()
        {

        }
    }
}
