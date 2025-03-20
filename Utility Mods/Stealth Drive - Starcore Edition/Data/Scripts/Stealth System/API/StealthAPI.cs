using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace StealthSystem
{
    internal class StealthAPI
    {
        /// Returns true if drive status was toggled successfully.
        /// <param name="force">Ignore power requirements and overheat.</param>
        public bool ToggleStealth(IMyTerminalBlock drive, bool force) => _toggleStealth?.Invoke(drive, force) ?? false;

        /// Returns status of drive. 0 = Ready, 1 = Active, 2 = Cooldown, 3 = Not enough power, 4 = Offline
        public int GetStatus(IMyTerminalBlock drive) => _getStatus?.Invoke(drive) ?? 4;

        /// Returns remaining duration of stealth/cooldown.
        public int GetDuration(IMyTerminalBlock drive) => _getDuration?.Invoke(drive) ?? 0;

        /// Retuns active stealth drive on grid if one exists, otherwise returns null.
        public IMyTerminalBlock GetMainDrive(IMyCubeGrid grid) => _getMainDrive?.Invoke(grid);

        /// <param name="sinks">Collection to populate with heat sinks on grid.</param>
        public void GetHeatSinks(IMyCubeGrid grid, ICollection<IMyTerminalBlock> sinks) => _getHeatSinks?.Invoke(grid, sinks);



        private const long CHANNEL = 2172757427;
        private bool _isRegistered;
        private bool _apiInit;
        private Action _readyCallback;

        private Func<IMyTerminalBlock, bool, bool> _toggleStealth;
        private Func<IMyTerminalBlock, int> _getStatus;
        private Func<IMyTerminalBlock, int> _getDuration;
        private Func<IMyCubeGrid, IMyTerminalBlock> _getMainDrive;
        private Action<IMyCubeGrid, ICollection<IMyTerminalBlock>> _getHeatSinks;

        public bool IsReady { get; private set; }


        /// <summary>
        /// Ask CoreSystems to send the API methods.
        /// <para>Throws an exception if it gets called more than once per session without <see cref="Unload"/>.</para>
        /// </summary>
        /// <param name="readyCallback">Method to be called when CoreSystems replies.</param>
        public void Load(Action readyCallback = null)
        {
            if (_isRegistered)
                throw new Exception($"{GetType().Name}.Load() should not be called multiple times!");

            _readyCallback = readyCallback;
            _isRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(CHANNEL, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(CHANNEL, "ApiEndpointRequest");
        }

        public void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(CHANNEL, HandleMessage);

            ApiAssign(null);

            _isRegistered = false;
            _apiInit = false;
            IsReady = false;
        }

        private void HandleMessage(object obj)
        {
            if (_apiInit || obj is string
            ) // the sent "ApiEndpointRequest" will also be received here, explicitly ignoring that
                return;

            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
                return;

            ApiAssign(dict);

            IsReady = true;
            _readyCallback?.Invoke();
        }

        public void ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            _apiInit = (delegates != null);
            /// base methods
            AssignMethod(delegates, "ToggleStealth", ref _toggleStealth);
            AssignMethod(delegates, "GetStatus", ref _getStatus);
            AssignMethod(delegates, "GetDuration", ref _getDuration);
            AssignMethod(delegates, "GetMainDrive", ref _getMainDrive);
            AssignMethod(delegates, "GetHeatSinks", ref _getHeatSinks);
        }

        private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field)
            where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;

            if (field == null)
                throw new Exception(
                    $"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }

    }
    
}
