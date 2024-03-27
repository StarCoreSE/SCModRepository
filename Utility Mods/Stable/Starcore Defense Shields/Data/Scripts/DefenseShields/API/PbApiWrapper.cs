using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace DefenseShields
{
    internal class PbApiWrapper
    {
        private Sandbox.ModAPI.Ingame.IMyTerminalBlock _block;

        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, RayD, Vector3D?> _rayIntersectShield;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, LineD, Vector3D?> _lineIntersectShield;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, bool> _pointInShield;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getShieldPercent;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int> _getShieldHeat;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getChargeRate;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int> _hpToChargeRatio;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getMaxCharge;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getCharge;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getPowerUsed;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getPowerCap;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getMaxHpCap;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> _isShieldUp;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, string> _shieldStatus;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, VRage.Game.ModAPI.Ingame.IMyEntity, bool, bool> _entityBypass;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool, bool> _entityBypassPb;
        // Fields below do not require SetActiveShield to be defined first.
        private readonly Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool> _gridHasShield;
        private readonly Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool> _gridShieldOnline;
        private readonly Func<VRage.Game.ModAPI.Ingame.IMyEntity, bool> _protectedByShield;
        private readonly Func<VRage.Game.ModAPI.Ingame.IMyEntity, Sandbox.ModAPI.Ingame.IMyTerminalBlock> _getShieldBlock;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> _isShieldBlock;
        private readonly Func<Vector3D, Sandbox.ModAPI.Ingame.IMyTerminalBlock> _getClosestShield;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, double> _getDistanceToShield;
        private readonly Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, Vector3D?> _getClosestShieldPoint;

        public void SetActiveShield(Sandbox.ModAPI.Ingame.IMyTerminalBlock block) => _block = block; // AutoSet to TapiFrontend(block) if shield exists on grid.

        public PbApiWrapper(Sandbox.ModAPI.Ingame.IMyTerminalBlock block)
        {
            _block = block;
            var delegates = _block.GetProperty("DefenseSystemsPbAPI")?.As<IReadOnlyDictionary<string, Delegate>>().GetValue(_block);
            if (delegates == null) return;

            _rayIntersectShield = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, RayD, Vector3D?>)delegates["RayIntersectShield"];
            _lineIntersectShield = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, LineD, Vector3D?>)delegates["LineIntersectShield"];
            _pointInShield = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, bool>)delegates["PointInShield"];
            _getShieldPercent = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>)delegates["GetShieldPercent"];
            _getShieldHeat = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int>)delegates["GetShieldHeat"];
            _getChargeRate = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>)delegates["GetChargeRate"];
            _hpToChargeRatio = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int>)delegates["HpToChargeRatio"];
            _getMaxCharge = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>)delegates["GetMaxCharge"];
            _getCharge = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>)delegates["GetCharge"];
            _getPowerUsed = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>)delegates["GetPowerUsed"];
            _getPowerCap = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>)delegates["GetPowerCap"];
            _getMaxHpCap = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float>)delegates["GetMaxHpCap"];
            _isShieldUp = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>)delegates["IsShieldUp"];
            _shieldStatus = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, string>)delegates["ShieldStatus"];
            _entityBypass = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, VRage.Game.ModAPI.Ingame.IMyEntity, bool, bool>)delegates["EntityBypass"];
            _entityBypassPb = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool, bool>)delegates["EntityBypassPb"];
            _gridHasShield = (Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>)delegates["GridHasShield"];
            _gridShieldOnline = (Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>)delegates["GridShieldOnline"];
            _protectedByShield = (Func<VRage.Game.ModAPI.Ingame.IMyEntity, bool>)delegates["ProtectedByShield"];
            _getShieldBlock = (Func<VRage.Game.ModAPI.Ingame.IMyEntity, Sandbox.ModAPI.Ingame.IMyTerminalBlock>)delegates["GetShieldBlock"];
            _isShieldBlock = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool>)delegates["IsShieldBlock"];
            _getClosestShield = (Func<Vector3D, Sandbox.ModAPI.Ingame.IMyTerminalBlock>)delegates["GetClosestShield"];
            _getDistanceToShield = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, double>)delegates["GetDistanceToShield"];
            _getClosestShieldPoint = (Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, Vector3D, Vector3D?>)delegates["GetClosestShieldPoint"];

            if (!IsShieldBlock()) _block = GetShieldBlock(_block.CubeGrid) ?? _block;
        }
        public Vector3D? RayIntersectShield(RayD ray) => _rayIntersectShield?.Invoke(_block, ray) ?? null;
        public Vector3D? LineIntersectShield(LineD line) => _lineIntersectShield?.Invoke(_block, line) ?? null;
        public bool PointInShield(Vector3D pos) => _pointInShield?.Invoke(_block, pos) ?? false;
        public float GetShieldPercent() => _getShieldPercent?.Invoke(_block) ?? -1;
        public int GetShieldHeat() => _getShieldHeat?.Invoke(_block) ?? -1;
        public float GetChargeRate() => _getChargeRate?.Invoke(_block) ?? -1;
        public float HpToChargeRatio() => _hpToChargeRatio?.Invoke(_block) ?? -1;
        public float GetMaxCharge() => _getMaxCharge?.Invoke(_block) ?? -1;
        public float GetCharge() => _getCharge?.Invoke(_block) ?? -1;
        public float GetPowerUsed() => _getPowerUsed?.Invoke(_block) ?? -1;
        public float GetPowerCap() => _getPowerCap?.Invoke(_block) ?? -1;
        public float GetMaxHpCap() => _getMaxHpCap?.Invoke(_block) ?? -1;
        public bool IsShieldUp() => _isShieldUp?.Invoke(_block) ?? false;
        public string ShieldStatus() => _shieldStatus?.Invoke(_block) ?? string.Empty;
        public bool EntityBypass(VRage.Game.ModAPI.Ingame.IMyEntity entity, bool remove = false) => _entityBypass?.Invoke(_block, entity, remove) ?? false;
        public bool EntityBypassPb(long entity, bool remove = false) => _entityBypassPb?.Invoke(_block, entity, remove) ?? false;
        public bool GridHasShield(VRage.Game.ModAPI.Ingame.IMyCubeGrid grid) => _gridHasShield?.Invoke(grid) ?? false;
        public bool GridShieldOnline(VRage.Game.ModAPI.Ingame.IMyCubeGrid grid) => _gridShieldOnline?.Invoke(grid) ?? false;
        public bool ProtectedByShield(VRage.Game.ModAPI.Ingame.IMyEntity entity) => _protectedByShield?.Invoke(entity) ?? false;
        public Sandbox.ModAPI.Ingame.IMyTerminalBlock GetShieldBlock(VRage.Game.ModAPI.Ingame.IMyEntity entity) => _getShieldBlock?.Invoke(entity) ?? null;
        public bool IsShieldBlock() => _isShieldBlock?.Invoke(_block) ?? false;
        public Sandbox.ModAPI.Ingame.IMyTerminalBlock GetClosestShield(Vector3D pos) => _getClosestShield?.Invoke(pos) ?? null;
        public double GetDistanceToShield(Vector3D pos) => _getDistanceToShield?.Invoke(_block, pos) ?? -1;
        public Vector3D? GetClosestShieldPoint(Vector3D pos) => _getClosestShieldPoint?.Invoke(_block, pos) ?? null;
    }
}
