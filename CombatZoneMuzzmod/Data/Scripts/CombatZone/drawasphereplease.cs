using System;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;

namespace invalid.drawthefuckingsphereplease
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class SphereDrawing : MySessionComponentBase
    {
        public override void UpdateAfterSimulation()
        {
            // executed every tick, 60 times a second, after physics simulation and only if game is not paused.

            // Define the sphere center
            Vector3D sphereCenter = new Vector3D(0, 0, 0);

            // Define the sphere color as red (ARGB format)
            Color sphereColor = new Color(255, 0, 0, 128); // A=255, R=0, G=0, B=128

            // Define the sphere radius as 10m
            float sphereRadius = 10.0f;

            // Define the sphere transformation matrix
            MatrixD worldMatrix = MatrixD.CreateWorld(sphereCenter);

            // Draw the transparent sphere
            MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, sphereRadius, ref sphereColor, MySimpleObjectRasterizer.SolidAndWireframe, 16);

            //MyAPIGateway.Utilities.ShowNotification("HELLO???");
        }

        protected override void UnloadData()
        {

        }
    }
}