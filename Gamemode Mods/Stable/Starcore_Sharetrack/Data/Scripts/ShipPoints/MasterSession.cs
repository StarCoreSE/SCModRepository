using System;
using StarCore.ShareTrack.HeartNetworking;
using StarCore.ShareTrack.ShipTracking;
using StarCore.ShareTrack.TrackerApi;
using VRage.Game.Components;
using VRageMath;

namespace StarCore.ShareTrack
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class MasterSession : MySessionComponentBase
    {
        /// <summary>
        /// API version, Mod version
        /// </summary>
        public static readonly Vector2I ModVersion = new Vector2I(3, 2);

        public static MasterSession I;

        private readonly AllGridsList _allGridsList = new AllGridsList();
        private ApiProvider _apiProvider;
        public int Ticks { get; private set; } = 0;

        public override void LoadData()
        {
            I = this;

            try
            {
                HeartNetwork.I = new HeartNetwork();
                HeartNetwork.I.LoadData(42521);
                _allGridsList.Init();
                _apiProvider = new ApiProvider();
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
                _apiProvider.Unload();
                _allGridsList.Close();
                TrackingManager.Close();
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
                Ticks++;
                HeartNetwork.I.Update();
                TrackingManager.UpdateAfterSimulation();
                _allGridsList.UpdateAfterSimulation();
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
                _allGridsList.Draw();
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