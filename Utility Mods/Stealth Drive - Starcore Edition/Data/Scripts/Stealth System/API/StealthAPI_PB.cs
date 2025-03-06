using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;

namespace StealthSystem
{
    internal class StealthPbAPI
    {
        /// Returns true if drive status was toggled successfully.
        public bool ToggleStealth(Sandbox.ModAPI.Ingame.IMyTerminalBlock drive) => _toggleStealth?.Invoke(drive) ?? false;

        /// Returns status of drive. 0 = Ready, 1 = Active, 2 = Cooldown, 3 = Not enough power, 4 = Offline
        public int GetStatus(Sandbox.ModAPI.Ingame.IMyTerminalBlock drive) => _getStatus?.Invoke(drive) ?? 4;

        /// Returns remaining duration of stealth/cooldown.
        public int GetDuration(Sandbox.ModAPI.Ingame.IMyTerminalBlock drive) => _getDuration?.Invoke(drive) ?? 0;

        /// Retuns active stealth drive on grid if one exists, otherwise returns null.
        public Sandbox.ModAPI.Ingame.IMyTerminalBlock GetMainDrive(VRage.Game.ModAPI.Ingame.IMyCubeGrid grid) => _getMainDrive?.Invoke(grid);

        /// <param name="sinks">Collection to populate with heat sinks on grid.</param>
        public void GetHeatSinks(VRage.Game.ModAPI.Ingame.IMyCubeGrid grid, ICollection<Sandbox.ModAPI.Ingame.IMyTerminalBlock> sinks) => _getHeatSinks?.Invoke(grid, sinks);




        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> _toggleStealth;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int> _getStatus;
        private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int> _getDuration;
        private Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, Sandbox.ModAPI.Ingame.IMyTerminalBlock> _getMainDrive;
        private Action<VRage.Game.ModAPI.Ingame.IMyCubeGrid, ICollection<Sandbox.ModAPI.Ingame.IMyTerminalBlock>> _getHeatSinks;

        public bool Activate(Sandbox.ModAPI.Ingame.IMyTerminalBlock pbBlock)
        {
            var dict = pbBlock.GetProperty("StealthPbAPI")?.As<IReadOnlyDictionary<string, Delegate>>().GetValue(pbBlock);
            if (dict == null) throw new Exception("StealthPbAPI failed to activate");
            return ApiAssign(dict);
        }

        public bool ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            if (delegates == null)
                return false;

            AssignMethod(delegates, "ToggleStealth", ref _toggleStealth);
            AssignMethod(delegates, "GetStatus", ref _getStatus);
            AssignMethod(delegates, "GetDuration", ref _getDuration);
            AssignMethod(delegates, "GetMainDrive", ref _getMainDrive);
            AssignMethod(delegates, "GetHeatSinks", ref _getHeatSinks);
            return true;
        }

        private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
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
