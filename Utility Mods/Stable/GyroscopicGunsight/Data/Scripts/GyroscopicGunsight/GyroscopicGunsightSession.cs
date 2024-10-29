using System;
using Sandbox.Graphics.GUI;
using System.Drawing;
using Sandbox.ModAPI;
using SC.GyroscopicGunsight.API.CoreSystems;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Render.Scene;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Game.ModAPI.Ingame;

namespace SC.GyroscopicGunsight
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class GyroscopicGunsightSession : MySessionComponentBase
    {
        public WcApi WcApi = new WcApi();

        private MyStringId _gunsightTexture = MyStringId.GetOrCompute("GyroGunsight");
        private Vector4 _gunsightColor = new Vector4(1, 1, 1, 1);
        Vector3D currentPos;
        Vector3D vectorToTarget;
        public double distanceToTarget;
        Vector3D prevPosition;
        Vector3D velocity;
        public Vector3D targetPos = Vector3D.Zero;
        public Vector3D targetVelocity = Vector3D.Zero;

        public double deflectionX;
        public double deflectionY;
        public double xRate;
        public double yRate;
        public double muzzleVelocity = 2000;
        public double range;

        /// <summary>
        /// Distance from the camera to the billboard.
        /// Ideally, this should be as low as possible.
        /// </summary>
        private const float NearDistance = 4f;
        /// <summary>
        /// Size in meters of the billboard.
        /// Seems to have a lower cap of 0.05?
        /// </summary>
        private const float SightSize = 0.1f;

        public override void LoadData()
        {
            WcApi.Load();
        }

        private float i = 0;
        public override void Draw()
        {
            if (MyAPIGateway.Utilities.IsDedicated || !(WcApi?.IsReady ?? false))
                return;

            try
            {
                // You'll need to find the weapon and target yourself, those aren't too terribly bad though
                // The exact contents of the CalculateDeflection method aren't important to how this is set up, do whatever you need to
                Vector3D dynamicPosition = CalculateDeflection(null, null);
                DrawGunsight(dynamicPosition);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        /// <summary>
        /// Squid's fancy leading math
        /// </summary>
        /// <param name="thisWeapon"></param>
        /// <param name="targetGrid"></param>
        /// <returns></returns>
        /// 

        public Vector3 GetShipAngularVelocity(IMyEntity ship)
        {

            var physics = ship?.Physics;
            if (physics == null) return Vector3.Zero;


        }
        public Vector3D CalculateDeflection(VRage.Game.ModAPI.IMyCubeBlock thisWeapon, VRage.Game.ModAPI.IMyCubeGrid targetGrid)
        {
            if (thisWeapon == null || targetGrid == null)
            {
                MyLog.Default.WriteLine("CalculateDeflection error: thisWeapon or targetGrid is null");
                return Vector3D.Zero; // Return a default value to avoid breaking
            }

            xRate = GetShipAngularVelocity(thisWeapon.CubeGrid).X;
            yRate = GetShipAngularVelocity(thisWeapon.CubeGrid).Y;
            Vector3D targetPos = targetGrid.GetPosition();
            Vector3D myPos = thisWeapon.CubeGrid.GetPosition();


            range = Vector3.Distance(myPos, targetPos);

            deflectionX = (range / muzzleVelocity) * xRate;
            deflectionY = (range / muzzleVelocity) * yRate;


            MatrixD cameraMatrix = MatrixD.Identity; // pretend this is filled out


            Vector3D offsetVec = new Vector3D(deflectionX, deflectionY, 0); // Full trailing reticle

            return targetPos + Vector3D.Transform(offsetVec, cameraMatrix.GetOrientation());
        }

        /// <summary>
        /// Aristeas's unfancy texture math
        /// </summary>
        /// <param name="Position"></param>
        public void DrawGunsight(Vector3D Position)
        {
            try
            {
                var camera = MyAPIGateway.Session.Camera;
                Vector3D offsetPosition = (Position - camera.Position).Normalized() * NearDistance + camera.Position;

                MySimpleObjectDraw.DrawLine(offsetPosition + camera.WorldMatrix.Left * SightSize, offsetPosition + camera.WorldMatrix.Right * SightSize, _gunsightTexture, ref _gunsightColor, SightSize);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }
    }
}
