using System;
using System.Collections.Generic;
using System.Linq;
using TLB.ShareTrack.ShipTracking;
using VRage.Game.ModAPI;

namespace TLB.ShareTrack.TrackerApi
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
                ["GetGridPoints"] = new Func<IMyCubeGrid, double>(GetGridPoints),
                ["TrackGrid"] = new Action<IMyCubeGrid, bool>(TrackGrid),
                ["UnTrackGrid"] = new Action<IMyCubeGrid, bool>(UnTrackGrid),
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

        private double GetGridPoints(IMyCubeGrid grid)
        {
            if (TrackingManager.I == null || !TrackingManager.I.TrackedGrids.ContainsKey(grid))
                return -1;

            return TrackingManager.I.TrackedGrids[grid].BattlePoints;
        }

        private void TrackGrid(IMyCubeGrid grid, bool share)
        {
            TrackingManager.I?.TrackGrid(grid, share);
        }

        private void UnTrackGrid(IMyCubeGrid grid, bool share)
        {
            TrackingManager.I?.UntrackGrid(grid, share);
        }
    }
}
