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

namespace SC.GyroscopicGunsight
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class GyroscopicGunsightSession : MySessionComponentBase
    {
        public WcApi WcApi = new WcApi();

        private MyStringId _gunsightTexture = MyStringId.GetOrCompute("GyroGunsight");
        private Vector4 _gunsightColor = new Vector4(1, 1, 1, 1);

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
                DrawGunsight(CalculateDeflection(null, null));
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
        public Vector3D CalculateDeflection(IMyCubeBlock thisWeapon, IMyCubeGrid targetGrid)
        {
            // TODO

            Vector3D targetPos = targetGrid.GetPosition();
            MatrixD cameraMatrix = MatrixD.Identity; // pretend this is filled out
            Vector3D offsetVec = new Vector3D(0, 0, 0); // Your offsets here

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
