using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace StarCore.ShareTrack.TrackerApi
{
    // Aristeas? Reuse code? NEVER...
    public class ShareTrackApi
    {
        /// <summary>
        ///     The expected API version.
        /// </summary>
        public const int ApiVersion = 3;

        /// <summary>
        ///     Triggered whenever the API is ready - added to by the constructor or manually.
        /// </summary>
        public Action OnReady;

        /// <summary>
        ///     The currently loaded ShareTrack version.
        ///     <remarks>
        ///         Not the API version; see <see cref="ApiVersion" />
        ///     </remarks>
        /// </summary>
        public int FrameworkVersion { get; private set; } = -1;

        /// <summary>
        ///     Displays whether endpoints are loaded and the API is ready for use.
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        ///     Call this to initialize the ShareTrackApi.<br />
        ///     <remarks>
        ///         API methods will be unusable until the endpoints are populated. Check <see cref="IsReady" /> or utilize
        ///         <see cref="OnReady" /> for safety.
        ///     </remarks>
        /// </summary>
        /// <param name="modContext"></param>
        /// <param name="onLoad">Method to be triggered when the API is ready.</param>
        /// <exception cref="Exception"></exception>
        public void Init(IMyModContext modContext, Action onLoad = null)
        {
            if (_isRegistered)
                throw new Exception($"{GetType().Name}.Load() should not be called multiple times!");

            _modContext = modContext;
            OnReady = onLoad;
            _isRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(ApiChannel, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(ApiChannel, "ApiEndpointRequest");
            MyLog.Default.WriteLineAndConsole(
                $"{_modContext.ModName}: ShareTrackAPI listening for API methods...");
        }

        /// <summary>
        ///     Call this to unload the ShareTrackApi; i.e. in case of instantiating a new API or for freeing up resources.
        ///     <remarks>
        ///         This method will also be called automatically when ShareTrack is
        ///         closed.
        ///     </remarks>
        /// </summary>
        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiChannel, HandleMessage);

            if (_apiInit)
                ApiAssign(); // Clear API methods if the API is currently inited.

            _isRegistered = false;
            _apiInit = false;
            IsReady = false;
            OnReady = null;
            MyLog.Default.WriteLineAndConsole($"{_modContext.ModName}: ShareTrackAPI unloaded.");
        }

        // These sections are what the user can actually see when referencing the API, and can be used freely. //
        // Note the null checks. //

        #region API Methods

        public IMyCubeGrid[] GetTrackedGrids()
        {
            return _getTrackedGrids?.Invoke();
        }

        public bool IsGridAlive(IMyCubeGrid grid)
        {
            return _isGridAlive?.Invoke(grid) ?? false;
        }

        public void RegisterOnTrack(Action<IMyCubeGrid, bool> action)
        {
            _registerOnTrack?.Invoke(action);
        }

        public void UnregisterOnTrack(Action<IMyCubeGrid, bool> action)
        {
            _unRegisterOnTrack?.Invoke(action);
        }

        public void RegisterOnAliveChanged(Action<IMyCubeGrid, bool> action)
        {
            _registerOnAliveChanged?.Invoke(action);
        }

        public void UnregisterOnAliveChanged(Action<IMyCubeGrid, bool> action)
        {
            _unregisterOnAliveChanged?.Invoke(action);
        }

        public bool AreTrackedGridsLoaded()
        {
            return _areTrackedGridsLoaded?.Invoke() ?? false;
        }

        public int GetGridPoints(IMyCubeGrid grid)
        {
            return _getGridPoints?.Invoke(grid) ?? -1;
        }

        public void TrackGrid(IMyCubeGrid grid, bool share = true)
        {
            _trackGrid?.Invoke(grid, share);
        }

        public void UnTrackGrid(IMyCubeGrid grid, bool share = true)
        {
            _unTrackGrid?.Invoke(grid, share);
        }

        public void SetAutotrack(bool value)
        {
            _setAutotrack?.Invoke(value);
        }

        #endregion


        // This section lists all the delegates that will be assigned and utilized below. //

        #region Delegates

        // Global methods
        private Func<IMyCubeGrid[]> _getTrackedGrids;
        private Func<IMyCubeGrid, bool> _isGridAlive;
        private Action<Action<IMyCubeGrid, bool>> _registerOnTrack;
        private Action<Action<IMyCubeGrid, bool>> _unRegisterOnTrack;
        private Action<Action<IMyCubeGrid, bool>> _registerOnAliveChanged;
        private Action<Action<IMyCubeGrid, bool>> _unregisterOnAliveChanged;
        private Func<bool> _areTrackedGridsLoaded;
        private Func<IMyCubeGrid, int> _getGridPoints;
        private Action<IMyCubeGrid, bool> _trackGrid;
        private Action<IMyCubeGrid, bool> _unTrackGrid;
        private Action<bool> _setAutotrack;

        #endregion


        // This section is the 'guts' of the API; it assigns out all the API endpoints internally and registers with the main mod. //

        #region API Initialization

        private bool _isRegistered;
        private bool _apiInit;
        private const long ApiChannel = 3033234540;
        private IReadOnlyDictionary<string, Delegate> _methodMap;
        private IMyModContext _modContext;

        /// <summary>
        ///     Assigns all API methods. Internal function, avoid editing.
        /// </summary>
        /// <returns></returns>
        public bool ApiAssign()
        {
            _apiInit = _methodMap != null;

            // Global methods
            SetApiMethod("GetTrackedGrids", ref _getTrackedGrids);
            SetApiMethod("IsGridAlive", ref _isGridAlive);
            SetApiMethod("RegisterOnTrack", ref _registerOnTrack);
            SetApiMethod("UnregisterOnTrack", ref _unRegisterOnTrack);
            SetApiMethod("RegisterOnAliveChanged", ref _registerOnAliveChanged);
            SetApiMethod("UnregisterOnAliveChanged", ref _unregisterOnAliveChanged);
            SetApiMethod("AreTrackedGridsLoaded", ref _areTrackedGridsLoaded);
            SetApiMethod("GetGridPoints", ref _getGridPoints);
            SetApiMethod("TrackGrid", ref _trackGrid);
            SetApiMethod("UnTrackGrid", ref _unTrackGrid);
            SetApiMethod("SetAutotrack", ref _setAutotrack);

            // Unload data if told to, otherwise notify that the API is ready.
            if (_methodMap == null)
            {
                UnloadData();
                return false;
            }

            _methodMap = null;
            OnReady?.Invoke();
            return true;
        }

        /// <summary>
        ///     Assigns a single API endpoint.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Shared endpoint name; matches with the mod.</param>
        /// <param name="method">Method to assign.</param>
        /// <exception cref="Exception"></exception>
        private void SetApiMethod<T>(string name, ref T method) where T : class
        {
            if (_methodMap == null)
            {
                method = null;
                return;
            }

            if (!_methodMap.ContainsKey(name))
                throw new Exception("Method Map does not contain method " + name);
            var del = _methodMap[name];
            if (del.GetType() != typeof(T))
                throw new Exception(
                    $"Method {name} type mismatch! [MapMethod: {del.GetType().Name} | ApiMethod: {typeof(T).Name}]");
            method = _methodMap[name] as T;
        }

        /// <summary>
        ///     Triggered whenever the API receives a message from the mod.
        /// </summary>
        /// <param name="obj"></param>
        private void HandleMessage(object obj)
        {
            try
            {
                if (_apiInit || obj is string ||
                    obj == null) // the "ApiEndpointRequest" message will also be received here, we're ignoring that
                    return;

                var tuple = (MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>)obj;
                var receivedVersion = tuple.Item1;
                var dict = tuple.Item2;

                if (dict == null)
                {
                    MyLog.Default.WriteLineAndConsole(
                        $"{_modContext.ModName}: ShareTrackAPI ERR: Received null dictionary!");
                    return;
                }

                if (receivedVersion.Y != ApiVersion)
                    Log.Info(
                        $"Expected API version ({ApiVersion}) differs from received API version {receivedVersion}; errors may occur.");

                _methodMap = dict;

                if (!ApiAssign()) // If we're unassigning the API, don't notify when ready
                    return;

                FrameworkVersion = receivedVersion.X;
                IsReady = true;
                Log.Info($"ShareTrackApi v{ApiVersion} loaded!");
            }
            catch (Exception ex)
            {
                // We really really want to notify the player if something goes wrong here.
                MyLog.Default.WriteLineAndConsole($"{_modContext.ModName}: Exception in ShareTrackAPI! " + ex);
                MyAPIGateway.Utilities.ShowMessage(_modContext.ModName, "Exception in ShareTrackAPI!\n" + ex);
            }
        }

        #endregion
    }
}
