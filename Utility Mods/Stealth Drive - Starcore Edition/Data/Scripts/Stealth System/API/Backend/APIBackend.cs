using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;
using IMyCubeGrid = VRage.Game.ModAPI.Ingame.IMyCubeGrid;

namespace StealthSystem
{
    internal class APIBackend
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;
        internal Dictionary<string, Delegate> PbApiMethods;

        private readonly StealthSession _session;

        internal APIBackend(StealthSession session)
        {
            _session = session;

            ModApiMethods = new Dictionary<string, Delegate>
            {
                ["ToggleStealth"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, bool, bool>(ToggleStealth),
                ["GetStatus"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int>(GetStatus),
                ["GetDuration"] = new Func<Sandbox.ModAPI.IMyTerminalBlock, int>(GetDuration),
                ["GetMainDrive"] = new Func<VRage.Game.ModAPI.IMyCubeGrid, Sandbox.ModAPI.IMyTerminalBlock>(GetMainDrive),
                ["GetHeatSinks"] = new Action<VRage.Game.ModAPI.IMyCubeGrid, ICollection<Sandbox.ModAPI.IMyTerminalBlock>>(GetHeatSinks),
            };
        }


        internal void PbInit()
        {
            PbApiMethods = new Dictionary<string, Delegate>
            {
                ["ToggleStealth"] = new Func<IMyTerminalBlock, bool>(ToggleStealthPB),
                ["GetStatus"] = new Func<IMyTerminalBlock, int>(GetStatus),
                ["GetDuration"] = new Func<IMyTerminalBlock, int>(GetDuration),
                ["GetMainDrive"] = new Func<IMyCubeGrid, IMyTerminalBlock>(GetMainDrive),
                ["GetHeatSinks"] = new Action<IMyCubeGrid, ICollection<IMyTerminalBlock>>(GetHeatSinksPB),
            };
            var pb = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyDictionary<string, Delegate>, Sandbox.ModAPI.IMyTerminalBlock>("StealthPbAPI");
            pb.Getter = b => PbApiMethods;
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(pb);
            _session.PbApiInited = true;
        }

        private bool ToggleStealth(IMyTerminalBlock block, bool force)
        {
            DriveComp comp;
            if (!_session.DriveMap.TryGetValue(block.EntityId, out comp))
                return false;

            return comp.ToggleStealth(force);
        }

        private bool ToggleStealthPB(IMyTerminalBlock block)
        {
            return ToggleStealth(block, false);
        }

        private int GetStatus(IMyTerminalBlock block)
        {
            DriveComp comp;
            if (!_session.DriveMap.TryGetValue(block.EntityId, out comp))
                return 4;

            var status = !comp.Online ? 4 : !comp.SufficientPower ? 3 : comp.CoolingDown ? 2 : comp.StealthActive ? 1 : 0;
            return status;
        }

        private int GetDuration(IMyTerminalBlock block)
        {
            DriveComp comp;
            if (!_session.DriveMap.TryGetValue(block.EntityId, out comp))
                return 0;

            var duration = comp.StealthActive ? comp.TotalTime - comp.TimeElapsed : comp.CoolingDown ? comp.TimeElapsed : comp.MaxDuration;
            return duration;
        }

        private Sandbox.ModAPI.IMyTerminalBlock GetMainDrive(IMyCubeGrid grid)
        {
            GridComp comp;
            if (!_session.GridMap.TryGetValue(grid as VRage.Game.ModAPI.IMyCubeGrid, out comp))
                return null;

            return comp.MasterComp?.Block;
        }

        private void GetHeatSinksPB(IMyCubeGrid grid, ICollection<IMyTerminalBlock> blocks)
        {
            GridComp comp;
            if (_session.GridMap.TryGetValue(grid as VRage.Game.ModAPI.IMyCubeGrid, out comp))
            {
                for (int i = 0; i < comp.HeatComps.Count; i++)
                    blocks.Add(comp.HeatComps[i].Block);
            }

            return;
        }

        private void GetHeatSinks(VRage.Game.ModAPI.IMyCubeGrid grid, ICollection<Sandbox.ModAPI.IMyTerminalBlock> blocks)
        {
            GridComp comp;
            if (_session.GridMap.TryGetValue(grid, out comp))
            {
                for (int i = 0; i < comp.HeatComps.Count; i++)
                    blocks.Add(comp.HeatComps[i].Block);
            }

            return;
        }

    }
}
