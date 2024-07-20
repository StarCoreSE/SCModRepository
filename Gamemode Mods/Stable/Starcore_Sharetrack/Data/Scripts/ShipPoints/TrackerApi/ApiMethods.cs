using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShipPoints.ShipTracking;
using VRage.Game.ModAPI;

namespace ShipPoints.TrackerApi
{
    internal class ApiMethods
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal ApiMethods()
        {
            ModApiMethods = new Dictionary<string, Delegate>
            {
                ["GetTrackedGrids"] = new Func<IMyCubeGrid[]>(GetTrackedGrids),
                ["IsGridAlive"] = new Func<IMyCubeGrid, bool>(IsGridAlive),
                ["RegisterOnTrack"] = new Action<Action<IMyCubeGrid, bool>>(RegisterOnTrack),
                ["UnregisterOnTrack"] = new Action<Action<IMyCubeGrid, bool>>(UnregisterOnTrack),
                ["RegisterOnAliveChanged"] = new Action<Action<IMyCubeGrid, bool>>(RegisterOnAliveChanged),
                ["UnregisterOnAliveChanged"] = new Action<Action<IMyCubeGrid, bool>>(UnregisterOnAliveChanged),
                ["AreTrackedGridsLoaded"] = new Func<bool>(AreTrackedGridsLoaded),
            };
        }

        private IMyCubeGrid[] GetTrackedGrids()
        {
            return TrackingManager.I?.TrackedGrids.Keys.ToArray();
        }

        private bool IsGridAlive(IMyCubeGrid grid)
        {
            return TrackingManager.I?.TrackedGrids.GetValueOrDefault(grid, null)?.IsFunctional ?? false;
        }

        private void RegisterOnTrack(Action<IMyCubeGrid, bool> action)
        {
            if (TrackingManager.I != null)
                TrackingManager.I.OnShipTracked += action;
        }

        private void UnregisterOnTrack(Action<IMyCubeGrid, bool> action)
        {
            if (TrackingManager.I != null)
                TrackingManager.I.OnShipTracked -= action;
        }

        private void RegisterOnAliveChanged(Action<IMyCubeGrid, bool> action)
        {
            if (TrackingManager.I != null)
                TrackingManager.I.OnShipAliveChanged += action;
        }

        private void UnregisterOnAliveChanged(Action<IMyCubeGrid, bool> action)
        {
            if (TrackingManager.I != null)
                TrackingManager.I.OnShipAliveChanged -= action;
        }

        private bool AreTrackedGridsLoaded()
        {
            if (TrackingManager.I == null)
                return false;

            return TrackingManager.I.GetQueuedGridTracks().Length == 0;
        }
    }
}
