using System;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Utils;
using IMyCockpit = Sandbox.ModAPI.Ingame.IMyCockpit;

namespace YourModNamespace
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class BoxDrawing : MySessionComponentBase
    {
        private const int NumberOfBoxes = 10;
        private static readonly Color BoxColor = new Color(255, 0, 0, 128);
        private static readonly Color BlueBoxColor = new Color(0, 0, 255, 128);
        private static readonly Color LineColor = Color.Green;
        private static readonly int[] DistanceMultipliers = { 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000, 9500, 10000 };
        private const int BoundaryRadius = 10000; // Define the boundary radius

        public override void UpdateAfterSimulation()
        {
            // Check if the player is in spectator mode
            bool isSpectator = MyAPIGateway.Session.IsCameraUserControlledSpectator;

            // If in spectator mode, don't draw anything
            if (isSpectator)
            {
                return;
            }

            Vector3D? playerPosition = PlayerPosition;
            if (!playerPosition.HasValue)
            {
                return;  // If playerPosition is null, exit the method
            }

            Vector3D directionToOrigin = CalculateDirectionToOrigin(playerPosition.Value);
            double distanceToOrigin = playerPosition.Value.Length();
            double remainingDistance = BoundaryRadius - distanceToOrigin; // Calculate remaining distance
            float boxSpacing = CalculateBoxSpacing(distanceToOrigin);
            MatrixD rotationMatrix = CalculateRotationMatrix(directionToOrigin);

            if (remainingDistance <= 1000) // Adjust this value as needed
            {
                ShowDistanceNotification(remainingDistance);
            }

            if (rotationMatrix == null)
            {
                return; // If rotationMatrix is null, exit the method
            }

            DrawBlueBoxes(distanceToOrigin, directionToOrigin, rotationMatrix);
            // DrawRedBoxes(playerPosition.Value, directionToOrigin, rotationMatrix.Value, boxSpacing);
            DrawGreenLine(distanceToOrigin, playerPosition.Value, directionToOrigin);
        }

        private void ShowDistanceNotification(double remainingDistance)
        {
            string notificationMessage = $"Out of Bounds in: {remainingDistance.ToString("F2")} meters";
            MyAPIGateway.Utilities.ShowNotification(notificationMessage, 10, MyFontEnum.Red);
        }

        private static Vector3D? PlayerPosition
        {
            get
            {
                var spectator = MyAPIGateway.Session.IsCameraUserControlledSpectator;
                var session = MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity;
                var cockpit = MyAPIGateway.Session.ControlledObject?.Entity as IMyCockpit;
                if (session == null || cockpit == null && !spectator)
                {
                    // MyLog.Default.WriteLine("Null object encountered in GetPlayerPosition, or not in cockpit");
                    return null;
                }
                return session.GetPosition();
            }
        }

        private static Vector3D CalculateDirectionToOrigin(Vector3D playerPosition) => Vector3D.Normalize(Vector3D.Zero - playerPosition);

        private static float CalculateBoxSpacing(double distanceToOrigin) => Math.Max((float)(distanceToOrigin / NumberOfBoxes), 10f);

        private static MatrixD CalculateRotationMatrix(Vector3D directionToOrigin) => MatrixD.CreateWorld(Vector3D.Zero, directionToOrigin, Vector3D.Up);


        private static void DrawBlueBoxes(double distanceToOrigin, Vector3D directionToOrigin, MatrixD rotationMatrix)
        {
            Vector3 halfExtents = new Vector3(2.5f, 2.5f, 2.5f);
            BoundingBoxD blueBox = new BoundingBoxD(-halfExtents, halfExtents);
            Color specialLineColor = CalculateSpecialLineColor(distanceToOrigin); // Calculate the special line color

            // Check if the player is within 10km. If so, skip drawing the special line.
            if (distanceToOrigin < 10000)
            {
                return;
            }

            // Draw the less transparent panel if the player is beyond 10km but within 12.5km.
            if (distanceToOrigin >= 10000 && distanceToOrigin < 12500)
            {
                Vector3D specialWallPosition = Vector3D.Zero - (directionToOrigin * 10000);
                DrawBox(blueBox, specialWallPosition, BlueBoxColor, rotationMatrix);
                DrawPerpendicularPlane(specialWallPosition, rotationMatrix, 500, 0.1, specialLineColor);
            }

            // General distance check for other boxes
            if (distanceToOrigin <= 9000) return;

            foreach (int multiplier in DistanceMultipliers)
            {
                if (distanceToOrigin <= multiplier) continue;

                Vector3D blueWallPosition = Vector3D.Zero - (directionToOrigin * multiplier);
                DrawBox(blueBox, blueWallPosition, BlueBoxColor, rotationMatrix);

                if (multiplier == 10000)
                {
                    continue; // Already drawn above
                }
                else
                {
                    DrawPerpendicularLine(blueWallPosition, rotationMatrix, 50);
                }
            }
        }


        private static Color CalculateSpecialLineColor(double distanceToOrigin)
        {
            byte alpha;

            if (distanceToOrigin >= 9000 && distanceToOrigin < 10000)
            {
                // Scale alpha from 0 (100% transparent) to 179 (about 30% transparent) as distance approaches 10km
                // This makes the plane go from invisible to slightly visible
                alpha = (byte)((distanceToOrigin - 9000) / 1000 * 179);
            }
            else if (distanceToOrigin >= 10000)
            {
                // Set alpha to 25 (about 90% transparent) beyond 10km
                // This makes it slightly visible from outside
                alpha = 1;
            }
            else
            {
                // If distance is less than 9000m, make it completely transparent
                alpha = 0; // Completely transparent
            }

            return new Color(255, 0, 0, alpha); // Red color with calculated alpha
        }







        private static void DrawPerpendicularPlane(Vector3D boxPosition, MatrixD rotationMatrix, double planeWidth = 500, double planeThickness = 0.1, Color? color = null)
        {
            Vector3D perpendicularDir = Vector3D.CalculatePerpendicularVector(rotationMatrix.Forward);
            Vector3D planeNormal = rotationMatrix.Forward;
            Vector3 halfExtents = new Vector3((float)planeWidth, (float)planeWidth, (float)planeThickness);
            BoundingBoxD planeBox = new BoundingBoxD(-halfExtents, halfExtents);

            // Create a variable for the plane matrix
            MatrixD planeMatrix = MatrixD.CreateWorld(boxPosition, planeNormal, perpendicularDir);

            // Create variables for other parameters
            Color planeColor = color ?? LineColor;

            // Draw the plane
            MySimpleObjectDraw.DrawTransparentBox(ref planeMatrix, ref planeBox, ref planeColor, MySimpleObjectRasterizer.Solid, 1, 0.1f, MyStringId.GetOrCompute("Square"));
        }

        private static void DrawBox(BoundingBoxD box, Vector3D position, Color color, MatrixD rotationMatrix)
        {
            BoundingBoxD transformedBox = box.TransformFast(rotationMatrix);
            transformedBox.Translate(position);
            MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref transformedBox, ref color, MySimpleObjectRasterizer.Solid, 1, 0.1f, MyStringId.GetOrCompute("Square"));
        }

        private static void DrawGreenLine(double distanceToOrigin, Vector3D playerPosition, Vector3D directionToOrigin)
        {
            if (distanceToOrigin <= 9000) return;

            Color lineColor = LineColor; // Default to Green
            if (distanceToOrigin > 10000)
            {
                lineColor = Color.Red;  // Change color to Red if the distance is greater than 7500
            }
            Vector4 lineColorVector4 = lineColor.ToVector4();

            Vector3D lineEndPoint = playerPosition + (directionToOrigin * 200);
            MySimpleObjectDraw.DrawLine(playerPosition, lineEndPoint, MyStringId.GetOrCompute("Square"), ref lineColorVector4, 1f);
        }



        private static void DrawPerpendicularLine(Vector3D boxPosition, MatrixD rotationMatrix, double lineLength = 50, Color? color = null)
        {

            Vector3D perpendicularDir = Vector3D.CalculatePerpendicularVector(rotationMatrix.Forward);
            Vector3D lineStart = boxPosition - (perpendicularDir * (lineLength / 2));
            Vector3D lineEnd = boxPosition + (perpendicularDir * (lineLength / 2));
            Vector4 lineColorVector4 = (color ?? LineColor).ToVector4();
            MySimpleObjectDraw.DrawLine(lineStart, lineEnd, MyStringId.GetOrCompute("Square"), ref lineColorVector4, 1f);
        }

        protected override void UnloadData() { }
    }
}
