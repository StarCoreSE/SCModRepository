using CameraInfoApi.Data.Scripts.CameraInfoApi;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace CameraInfoApi
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ClientMain : MySessionComponentBase
    {
        private IMyCubeGrid prevGrid = null;
        private int _ticks = 0;
        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            if (_ticks++ % 10 != 0)
                return;

            var clientGrid = (MyAPIGateway.Session.Player?.Controller?.ControlledEntity as IMyShipController)?.CubeGrid;
            if (clientGrid == null)
            {
                if (prevGrid != null)
                    MyAPIGateway.Multiplayer.SendMessageToServer(3621, MyAPIGateway.Utilities.SerializeToBinary(new CameraDataPacket()
                    {
                        Matrix = MatrixD.Identity,
                        FieldOfView = -1,
                        GridId = prevGrid.EntityId,
                    }));
                prevGrid = null;
                return;
            }

            prevGrid = clientGrid;
            MyAPIGateway.Multiplayer.SendMessageToServer(3621, MyAPIGateway.Utilities.SerializeToBinary(new CameraDataPacket()
            {
                Matrix = MyAPIGateway.Session.Camera.ViewMatrix,
                FieldOfView = MyAPIGateway.Session.Camera.FieldOfViewAngle,
                GridId = prevGrid.EntityId,
            }));
        }
    }
}
