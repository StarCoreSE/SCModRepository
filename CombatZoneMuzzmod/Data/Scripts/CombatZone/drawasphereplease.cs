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

            // Define the color of the blue box
            Color blueBoxColor = new Color(0, 0, 255, 128);

            // Create BoundingBoxD for the blue box
            BoundingBoxD blueBox = new BoundingBoxD(-halfExtents, halfExtents);

            // Array to hold the multipliers for distances at which blue walls should appear
            int[] distanceMultipliers = { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 7500 };

            // Loop to place blue boxes at specific distances
            foreach (int multiplier in distanceMultipliers)
            {
                // Calculate the position for each blue wall
                Vector3D blueWallPosition = Vector3D.Zero + (-directionToOrigin * multiplier);

                // Check if the player is more than the specified distance away from the origin
                if (distanceToOrigin > multiplier)
                {
                    // Transform the blue wall box by the player's position and rotation
                    BoundingBoxD transformedBlueBox = blueBox.TransformFast(rotationMatrix);
                    transformedBlueBox.Translate(blueWallPosition);

                    // Draw the blue box using MySimpleObjectDraw.DrawTransparentBox
                    MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref transformedBlueBox, ref blueBoxColor, MySimpleObjectRasterizer.Solid, 1, 0.1f, MyStringId.GetOrCompute("Square"));
                }
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
