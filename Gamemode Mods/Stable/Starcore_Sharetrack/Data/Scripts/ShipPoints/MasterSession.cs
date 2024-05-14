using System;
using ShipPoints.Commands;
using ShipPoints.HeartNetworking;
using ShipPoints.ShipTracking;
using VRage.Game.Components;

namespace ShipPoints
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class MasterSession : MySessionComponentBase
    {
        public const ushort ComId = 42511;
        public const string Keyword = "/debug";
        public const string DisplayName = "Debug";
        public static MasterSession I;

        private readonly PointCheck _pointCheck = new PointCheck();

        public override void LoadData()
        {
            I = this;

            try
            {
                HeartNetwork.I = new HeartNetwork();
                HeartNetwork.I.LoadData(42521);
                CommandHandler.Init();
                _pointCheck.Init();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void UnloadData()
        {
            try
            {
                _pointCheck.Close();
                TrackingManager.Close();
                CommandHandler.Close();
                HeartNetwork.I.UnloadData();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            I = null;
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                HeartNetwork.I.Update();
                TrackingManager.UpdateAfterSimulation();
                _pointCheck.UpdateAfterSimulation();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public override void Draw()
        {
            try
            {
                _pointCheck.Draw();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public override void HandleInput()
        {
            try
            {
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}