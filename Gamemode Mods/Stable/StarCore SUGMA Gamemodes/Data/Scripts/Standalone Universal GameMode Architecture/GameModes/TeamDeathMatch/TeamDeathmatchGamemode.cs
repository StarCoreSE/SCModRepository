using Sandbox.ModAPI;
using SC.SUGMA.API;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameModes.TeamDeathMatch
{
    internal class TeamDeathmatchGamemode : ComponentBase
    {
        private ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;

        private readonly string _pointTrackerId;

        public TeamDeathmatchGamemode(string pointTrackerId)
        {
            _pointTrackerId = pointTrackerId;
        }

        public override void Init(string id)
        {
            base.Init(id);

            ShareTrackApi.RegisterOnTrack(OnTracked);
            ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);
        }

        public override void Close()
        {
            ShareTrackApi.UnregisterOnTrack(OnTracked);
            ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);
        }

        public override void UpdateTick()
        {
            
        }

        private void OnTracked(IMyCubeGrid grid, bool isTracked)
        {
            MyAPIGateway.Utilities.ShowNotification($"T {grid.DisplayName}: {isTracked}");
        }

        private void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            MyAPIGateway.Utilities.ShowNotification($"A {grid.DisplayName}: {isAlive}");
        }
    }
}
