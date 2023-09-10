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
        private const int NumberOfBoxes = 10;
        private static readonly Color BoxColor = new Color(255, 0, 0, 128);
        private static readonly Color BlueBoxColor = new Color(0, 0, 255, 128);
        private static readonly Color LineColor = Color.Green;
        private static readonly int[] DistanceMultipliers = { 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500 };

        public override void UpdateAfterSimulation()
        {
            Vector3D? playerPosition = GetPlayerPosition();
            if (!playerPosition.HasValue)
            {
                return;  // If playerPosition is null, exit the method
            }

            Vector3D directionToOrigin = CalculateDirectionToOrigin(playerPosition.Value);
            double distanceToOrigin = playerPosition.Value.Length();
            float boxSpacing = CalculateBoxSpacing(distanceToOrigin);
            MatrixD rotationMatrix = CalculateRotationMatrix(directionToOrigin);

            if (rotationMatrix == null)
            {
                return; // If rotationMatrix is null, exit the method
            }

            DrawBlueBoxes(distanceToOrigin, directionToOrigin, rotationMatrix);
            // DrawRedBoxes(playerPosition.Value, directionToOrigin, rotationMatrix.Value, boxSpacing);
            DrawGreenLine(distanceToOrigin, playerPosition.Value, directionToOrigin);
        }


        private Vector3D GetPlayerPosition() => MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

        private Vector3D CalculateDirectionToOrigin(Vector3D playerPosition) => Vector3D.Normalize(Vector3D.Zero - playerPosition);

        private float CalculateBoxSpacing(double distanceToOrigin) => Math.Max((float)(distanceToOrigin / NumberOfBoxes), 10f);

        private MatrixD CalculateRotationMatrix(Vector3D directionToOrigin) => MatrixD.CreateWorld(Vector3D.Zero, directionToOrigin, Vector3D.Up);

        // Modify this existing method
        private void DrawBlueBoxes(double distanceToOrigin, Vector3D directionToOrigin, MatrixD rotationMatrix)
        {
            Vector3 halfExtents = new Vector3(2.5f, 2.5f, 2.5f);
            BoundingBoxD blueBox = new BoundingBoxD(-halfExtents, halfExtents);
            Color specialLineColor = new Color(255, 0, 0, 128); // Red color for the special perpendicular line

            // Separate distance check for the special 7500 unit line
            if (distanceToOrigin >= 6000 && distanceToOrigin < 7500)
            {
                Vector3D specialWallPosition = Vector3D.Zero - (directionToOrigin * 7500);
                DrawBox(blueBox, specialWallPosition, BlueBoxColor, rotationMatrix);
                DrawPerpendicularLine(specialWallPosition, rotationMatrix, 500, specialLineColor);
            }

            // General distance check for other boxes
            if (distanceToOrigin <= 7000) return;

            foreach (int multiplier in DistanceMultipliers)
            {
                if (distanceToOrigin <= multiplier) continue;

                Vector3D blueWallPosition = Vector3D.Zero - (directionToOrigin * multiplier);
                DrawBox(blueBox, blueWallPosition, BlueBoxColor, rotationMatrix);

                if (multiplier == 7500)
                {
                    continue; // Already drawn above
                }
                else
                {
                    DrawPerpendicularLine(blueWallPosition, rotationMatrix, 50);
                }
            }
        }



        private void DrawRedBoxes(Vector3D playerPosition, Vector3D directionToOrigin, MatrixD rotationMatrix, float boxSpacing)
        {
            Vector3 halfExtents = new Vector3(2.5f, 2.5f, 2.5f);
            BoundingBoxD redBox = new BoundingBoxD(-halfExtents, halfExtents);
            Vector3D boxPosition = playerPosition + (directionToOrigin * 10);

            for (int i = 0; i < NumberOfBoxes; i++)
            {
                DrawBox(redBox, boxPosition, BoxColor, rotationMatrix);
                boxPosition += directionToOrigin * boxSpacing;
            }
        }

        private void DrawBox(BoundingBoxD box, Vector3D position, Color color, MatrixD rotationMatrix)
        {
            BoundingBoxD transformedBox = box.TransformFast(rotationMatrix);
            transformedBox.Translate(position);
            MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref transformedBox, ref color, MySimpleObjectRasterizer.Solid, 1, 0.1f, MyStringId.GetOrCompute("Square"));
        }

        private void DrawGreenLine(double distanceToOrigin, Vector3D playerPosition, Vector3D directionToOrigin)
        {
            
            if (distanceToOrigin <= 7000) return;

            Vector4 lineColorVector4 = LineColor.ToVector4();
            Vector3D lineEndPoint = playerPosition + (directionToOrigin * 200);
            MySimpleObjectDraw.DrawLine(playerPosition, lineEndPoint, MyStringId.GetOrCompute("Square"), ref lineColorVector4, 1f);


        }


        private void DrawPerpendicularLine(Vector3D boxPosition, MatrixD rotationMatrix, double lineLength = 50, Color? color = null)
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
