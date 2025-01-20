﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace DefenseShields
{
    public class ShieldApi
    {
        private const long Channel = 1365616918;

        private Action<long> _addAtacker;
        private bool _apiInit;
        private Func<IMyTerminalBlock, IMyEntity, bool, bool> _entityBypass;
        private Func<IMyTerminalBlock, float> _getCharge;
        private Func<IMyTerminalBlock, float> _getChargeRate;
        private Func<Vector3D, IMyTerminalBlock> _getClosestShield;
        private Func<IMyTerminalBlock, Vector3D, Vector3D?> _getClosestShieldPoint;
        private Func<IMyTerminalBlock, Vector3D, double> _getDistanceToShield;
        private Func<IMyTerminalBlock, Vector3D, bool, MyTuple<bool, int, int, float, float>> _getFaceInfo;

        private Func<IMyTerminalBlock, Vector3D, bool, MyTuple<bool, int, int, float, float, float>>
            _getFaceInfoAndPenChance;

        private Func<MyEntity, MyTuple<bool, Vector3I>> _getFacesFast;
        private Action<MyEntity, ICollection<MyTuple<long, float, uint>>> _getLastAttackers;
        private Func<IMyTerminalBlock, float> _getMaxCharge;
        private Func<IMyTerminalBlock, float> _getMaxHpCap;
        private Func<MyEntity, MyTuple<bool, bool, float, float>> _getModulationInfo;
        private Func<IMyTerminalBlock, float> _getPowerCap;
        private Func<IMyTerminalBlock, float> _getPowerUsed;
        private Func<IMyEntity, IMyTerminalBlock> _getShieldBlock;
        private Func<IMyTerminalBlock, int> _getShieldHeat;
        private Func<MyEntity, MyTuple<bool, bool, float, float, float, int>> _getShieldInfo;
        private Func<IMyTerminalBlock, float> _getShieldPercent;
        private Func<IMyCubeGrid, bool> _gridHasShield;
        private Func<IMyCubeGrid, bool> _gridShieldOnline;
        private Func<IMyTerminalBlock, int> _hpToChargeRatio;

        private Func<List<MyEntity>, RayD, bool, bool, long, float, MyTuple<bool, float>>
            _intersectEntToShieldFast; // fast check of entities for shield

        private Func<IMySlimBlock, bool> _isBlockProtected;
        private Func<IMyTerminalBlock, bool> _isFortified;

        private bool _isRegistered;
        private Func<IMyTerminalBlock, bool> _isShieldBlock;
        private Func<IMyTerminalBlock, bool> _isShieldUp;

        private Func<IMyTerminalBlock, LineD, long, float, bool, bool, Vector3D?>
            _lineAttackShield; // negative damage values heal

        private Func<IMyTerminalBlock, LineD, Vector3D?> _lineIntersectShield;
        private Func<IMyEntity, bool, IMyTerminalBlock> _matchEntToShieldFast;

        private Func<MyEntity, bool, MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>,
            MyTuple<MatrixD, MatrixD>, MyTuple<bool, bool, float, float>>?> _matchEntToShieldFastDetails;

        private Func<MyEntity, bool, MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>,
            MyTuple<MatrixD, MatrixD>>?> _matchEntToShieldFastExt;

        private Action<IMyTerminalBlock> _overLoad;

        private Func<IMyTerminalBlock, Vector3D, long, float, bool, bool, bool, bool>
            _pointAttackShield; // negative damage values heal

        private Func<IMyTerminalBlock, Vector3D, long, float, float, bool, bool, bool, float?>
            _pointAttackShieldCon; // negative damage values heal, conditional secondary damage

        private Func<IMyTerminalBlock, Vector3D, long, float, bool, bool, bool, float?>
            _pointAttackShieldExt; // negative damage values heal

        private Func<IMyTerminalBlock, Vector3D, long, float, float, bool, bool, bool, float, float?>
            _pointAttackShieldHeat; // negative damage values heal, conditional secondary damage

        private Func<IMyTerminalBlock, Vector3D, bool> _pointInShield;
        private Func<IMyEntity, bool> _protectedByShield;

        private Func<IMyTerminalBlock, RayD, long, float, bool, bool, Vector3D?>
            _rayAttackShield; // negative damage values heal

        private Func<IMyTerminalBlock, RayD, Vector3D?> _rayIntersectShield;
        private Action<IMyTerminalBlock, float> _setCharge;
        private Action<IMyTerminalBlock, int> _setShieldHeat;
        private Func<IMyTerminalBlock, string> _shieldStatus;

        public bool IsReady { get; private set; }
        public bool Compromised { get; private set; }

        private void HandleMessage(object o)
        {
            if (_apiInit) return;
            var dict = o as IReadOnlyDictionary<string, Delegate>;
            var message = o as string;

            if (message != null && message == "Compromised")
                Compromised = true;

            if (dict == null || dict is ImmutableDictionary<string, Delegate>)
                return;

            var builder = ImmutableDictionary.CreateBuilder<string, Delegate>();
            foreach (var pair in dict)
                builder.Add(pair.Key, pair.Value);

            MyAPIGateway.Utilities.SendModMessage(Channel, builder.ToImmutable());

            ApiLoad(dict);
            IsReady = true;
        }

        public bool Load()
        {
            if (!_isRegistered)
            {
                _isRegistered = true;
                MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);
            }

            if (!IsReady)
                MyAPIGateway.Utilities.SendModMessage(Channel, "ApiEndpointRequest");
            return IsReady;
        }

        public void Unload()
        {
            if (_isRegistered)
            {
                _isRegistered = false;
                MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);
            }

            IsReady = false;
        }

        public void ApiLoad(IReadOnlyDictionary<string, Delegate> delegates)
        {
            _apiInit = true;
            _rayAttackShield =
                (Func<IMyTerminalBlock, RayD, long, float, bool, bool, Vector3D?>)delegates["RayAttackShield"];
            _lineAttackShield =
                (Func<IMyTerminalBlock, LineD, long, float, bool, bool, Vector3D?>)delegates["LineAttackShield"];
            _intersectEntToShieldFast =
                (Func<List<MyEntity>, RayD, bool, bool, long, float, MyTuple<bool, float>>)delegates[
                    "IntersectEntToShieldFast"];
            _pointAttackShield =
                (Func<IMyTerminalBlock, Vector3D, long, float, bool, bool, bool, bool>)delegates["PointAttackShield"];
            _pointAttackShieldExt =
                (Func<IMyTerminalBlock, Vector3D, long, float, bool, bool, bool, float?>)delegates[
                    "PointAttackShieldExt"];
            _pointAttackShieldCon =
                (Func<IMyTerminalBlock, Vector3D, long, float, float, bool, bool, bool, float?>)delegates[
                    "PointAttackShieldCon"];
            _pointAttackShieldHeat =
                (Func<IMyTerminalBlock, Vector3D, long, float, float, bool, bool, bool, float, float?>)delegates[
                    "PointAttackShieldHeat"];
            _setShieldHeat = (Action<IMyTerminalBlock, int>)delegates["SetShieldHeat"];
            _overLoad = (Action<IMyTerminalBlock>)delegates["OverLoadShield"];
            _setCharge = (Action<IMyTerminalBlock, float>)delegates["SetCharge"];
            _rayIntersectShield = (Func<IMyTerminalBlock, RayD, Vector3D?>)delegates["RayIntersectShield"];
            _lineIntersectShield = (Func<IMyTerminalBlock, LineD, Vector3D?>)delegates["LineIntersectShield"];
            _pointInShield = (Func<IMyTerminalBlock, Vector3D, bool>)delegates["PointInShield"];
            _getShieldPercent = (Func<IMyTerminalBlock, float>)delegates["GetShieldPercent"];
            _getShieldHeat = (Func<IMyTerminalBlock, int>)delegates["GetShieldHeat"];
            _getChargeRate = (Func<IMyTerminalBlock, float>)delegates["GetChargeRate"];
            _hpToChargeRatio = (Func<IMyTerminalBlock, int>)delegates["HpToChargeRatio"];
            _getMaxCharge = (Func<IMyTerminalBlock, float>)delegates["GetMaxCharge"];
            _getCharge = (Func<IMyTerminalBlock, float>)delegates["GetCharge"];
            _getPowerUsed = (Func<IMyTerminalBlock, float>)delegates["GetPowerUsed"];
            _getPowerCap = (Func<IMyTerminalBlock, float>)delegates["GetPowerCap"];
            _getMaxHpCap = (Func<IMyTerminalBlock, float>)delegates["GetMaxHpCap"];
            _isShieldUp = (Func<IMyTerminalBlock, bool>)delegates["IsShieldUp"];
            _shieldStatus = (Func<IMyTerminalBlock, string>)delegates["ShieldStatus"];
            _entityBypass = (Func<IMyTerminalBlock, IMyEntity, bool, bool>)delegates["EntityBypass"];
            _gridHasShield = (Func<IMyCubeGrid, bool>)delegates["GridHasShield"];
            _gridShieldOnline = (Func<IMyCubeGrid, bool>)delegates["GridShieldOnline"];
            _protectedByShield = (Func<IMyEntity, bool>)delegates["ProtectedByShield"];
            _getShieldBlock = (Func<IMyEntity, IMyTerminalBlock>)delegates["GetShieldBlock"];
            _matchEntToShieldFast = (Func<IMyEntity, bool, IMyTerminalBlock>)delegates["MatchEntToShieldFast"];
            _matchEntToShieldFastExt =
                (Func<MyEntity, bool, MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>,
                    MyTuple<MatrixD, MatrixD>>?>)delegates["MatchEntToShieldFastExt"];
            _matchEntToShieldFastDetails =
                (Func<MyEntity, bool, MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>,
                    MyTuple<MatrixD, MatrixD>, MyTuple<bool, bool, float, float>>?>)delegates[
                    "MatchEntToShieldFastDetails"];
            _isShieldBlock = (Func<IMyTerminalBlock, bool>)delegates["IsShieldBlock"];
            _getClosestShield = (Func<Vector3D, IMyTerminalBlock>)delegates["GetClosestShield"];
            _getDistanceToShield = (Func<IMyTerminalBlock, Vector3D, double>)delegates["GetDistanceToShield"];
            _getClosestShieldPoint = (Func<IMyTerminalBlock, Vector3D, Vector3D?>)delegates["GetClosestShieldPoint"];
            _getShieldInfo = (Func<MyEntity, MyTuple<bool, bool, float, float, float, int>>)delegates["GetShieldInfo"];
            _getModulationInfo = (Func<MyEntity, MyTuple<bool, bool, float, float>>)delegates["GetModulationInfo"];
            _getFaceInfo =
                (Func<IMyTerminalBlock, Vector3D, bool, MyTuple<bool, int, int, float, float>>)delegates["GetFaceInfo"];
            _getFaceInfoAndPenChance =
                (Func<IMyTerminalBlock, Vector3D, bool, MyTuple<bool, int, int, float, float, float>>)delegates[
                    "GetFaceInfoAndPenChance"];
            _addAtacker = (Action<long>)delegates["AddAttacker"];
            _isBlockProtected = (Func<IMySlimBlock, bool>)delegates["IsBlockProtected"];
            _getFacesFast = (Func<MyEntity, MyTuple<bool, Vector3I>>)delegates["GetFacesFast"];
            _getLastAttackers =
                (Action<MyEntity, ICollection<MyTuple<long, float, uint>>>)delegates["GetLastAttackers"];
            _isFortified = (Func<IMyTerminalBlock, bool>)delegates["IsFortified"];
        }

        public Vector3D? RayAttackShield(IMyTerminalBlock block, RayD ray, long attackerId, float damage, bool energy,
            bool drawParticle)
        {
            return _rayAttackShield?.Invoke(block, ray, attackerId, damage, energy, drawParticle) ?? null;
        }

        public Vector3D? LineAttackShield(IMyTerminalBlock block, LineD line, long attackerId, float damage,
            bool energy, bool drawParticle)
        {
            return _lineAttackShield?.Invoke(block, line, attackerId, damage, energy, drawParticle) ?? null;
        }

        public MyTuple<bool, float> IntersectEntToShieldFast(List<MyEntity> entities, RayD ray, bool onlyIfOnline,
            bool enemyOnly, long requesterId, float maxRange)
        {
            return _intersectEntToShieldFast?.Invoke(entities, ray, onlyIfOnline, enemyOnly, requesterId, maxRange) ??
                   new MyTuple<bool, float>(false, float.MaxValue);
        }

        public bool PointAttackShield(IMyTerminalBlock block, Vector3D pos, long attackerId, float damage, bool energy,
            bool drawParticle, bool posMustBeInside = false)
        {
            return _pointAttackShield?.Invoke(block, pos, attackerId, damage, energy, drawParticle, posMustBeInside) ??
                   false;
        }

        public float? PointAttackShieldExt(IMyTerminalBlock block, Vector3D pos, long attackerId, float damage,
            bool energy, bool drawParticle, bool posMustBeInside = false)
        {
            return _pointAttackShieldExt?.Invoke(block, pos, attackerId, damage, energy, drawParticle,
                posMustBeInside) ?? null;
        }

        public float? PointAttackShieldCon(IMyTerminalBlock block, Vector3D pos, long attackerId, float damage,
            float optionalDamage, bool energy, bool drawParticle, bool posMustBeInside = false)
        {
            return _pointAttackShieldCon?.Invoke(block, pos, attackerId, damage, optionalDamage, energy, drawParticle,
                posMustBeInside) ?? null;
        }

        public float? PointAttackShieldHeat(IMyTerminalBlock block, Vector3D pos, long attackerId, float damage,
            float optionalDamage, bool energy, bool drawParticle, bool posMustBeInside = false, float heatScaler = 1)
        {
            return _pointAttackShieldHeat?.Invoke(block, pos, attackerId, damage, optionalDamage, energy, drawParticle,
                posMustBeInside, heatScaler) ?? null;
        }

        public void SetShieldHeat(IMyTerminalBlock block, int value)
        {
            _setShieldHeat?.Invoke(block, value);
        }

        public void OverLoadShield(IMyTerminalBlock block)
        {
            _overLoad?.Invoke(block);
        }

        public void SetCharge(IMyTerminalBlock block, float value)
        {
            _setCharge.Invoke(block, value);
        }

        public Vector3D? RayIntersectShield(IMyTerminalBlock block, RayD ray)
        {
            return _rayIntersectShield?.Invoke(block, ray) ?? null;
        }

        public Vector3D? LineIntersectShield(IMyTerminalBlock block, LineD line)
        {
            return _lineIntersectShield?.Invoke(block, line) ?? null;
        }

        public bool PointInShield(IMyTerminalBlock block, Vector3D pos)
        {
            return _pointInShield?.Invoke(block, pos) ?? false;
        }

        public float GetShieldPercent(IMyTerminalBlock block)
        {
            return _getShieldPercent?.Invoke(block) ?? -1;
        }

        public int GetShieldHeat(IMyTerminalBlock block)
        {
            return _getShieldHeat?.Invoke(block) ?? -1;
        }

        public float GetChargeRate(IMyTerminalBlock block)
        {
            return _getChargeRate?.Invoke(block) ?? -1;
        }

        public float HpToChargeRatio(IMyTerminalBlock block)
        {
            return _hpToChargeRatio?.Invoke(block) ?? -1;
        }

        public float GetMaxCharge(IMyTerminalBlock block)
        {
            return _getMaxCharge?.Invoke(block) ?? -1;
        }

        public float GetCharge(IMyTerminalBlock block)
        {
            return _getCharge?.Invoke(block) ?? -1;
        }

        public float GetPowerUsed(IMyTerminalBlock block)
        {
            return _getPowerUsed?.Invoke(block) ?? -1;
        }

        public float GetPowerCap(IMyTerminalBlock block)
        {
            return _getPowerCap?.Invoke(block) ?? -1;
        }

        public float GetMaxHpCap(IMyTerminalBlock block)
        {
            return _getMaxHpCap?.Invoke(block) ?? -1;
        }

        public bool IsShieldUp(IMyTerminalBlock block)
        {
            return _isShieldUp?.Invoke(block) ?? false;
        }

        public string ShieldStatus(IMyTerminalBlock block)
        {
            return _shieldStatus?.Invoke(block) ?? string.Empty;
        }

        public bool EntityBypass(IMyTerminalBlock block, IMyEntity entity, bool remove = false)
        {
            return _entityBypass?.Invoke(block, entity, remove) ?? false;
        }

        public bool GridHasShield(IMyCubeGrid grid)
        {
            return _gridHasShield?.Invoke(grid) ?? false;
        }

        public bool GridShieldOnline(IMyCubeGrid grid)
        {
            return _gridShieldOnline?.Invoke(grid) ?? false;
        }

        public bool ProtectedByShield(IMyEntity entity)
        {
            return _protectedByShield?.Invoke(entity) ?? false;
        }

        public IMyTerminalBlock GetShieldBlock(IMyEntity entity)
        {
            return _getShieldBlock?.Invoke(entity) ?? null;
        }

        public IMyTerminalBlock MatchEntToShieldFast(IMyEntity entity, bool onlyIfOnline)
        {
            return _matchEntToShieldFast?.Invoke(entity, onlyIfOnline) ?? null;
        }

        public MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>, MyTuple<MatrixD, MatrixD>>?
            MatchEntToShieldFastExt(MyEntity entity, bool onlyIfOnline)
        {
            return _matchEntToShieldFastExt?.Invoke(entity, onlyIfOnline) ?? null;
        }

        public MyTuple<IMyTerminalBlock, MyTuple<bool, bool, float, float, float, int>, MyTuple<MatrixD, MatrixD>,
            MyTuple<bool, bool, float, float>>? MatchEntToShieldFastDetails(MyEntity entity, bool onlyIfOnline)
        {
            return _matchEntToShieldFastDetails?.Invoke(entity, onlyIfOnline) ?? null;
        }

        public bool IsShieldBlock(IMyTerminalBlock block)
        {
            return _isShieldBlock?.Invoke(block) ?? false;
        }

        public IMyTerminalBlock GetClosestShield(Vector3D pos)
        {
            return _getClosestShield?.Invoke(pos) ?? null;
        }

        public double GetDistanceToShield(IMyTerminalBlock block, Vector3D pos)
        {
            return _getDistanceToShield?.Invoke(block, pos) ?? -1;
        }

        public Vector3D? GetClosestShieldPoint(IMyTerminalBlock block, Vector3D pos)
        {
            return _getClosestShieldPoint?.Invoke(block, pos) ?? null;
        }

        public MyTuple<bool, bool, float, float, float, int> GetShieldInfo(MyEntity entity)
        {
            return _getShieldInfo?.Invoke(entity) ?? new MyTuple<bool, bool, float, float, float, int>();
        }

        public MyTuple<bool, bool, float, float> GetModulationInfo(MyEntity entity)
        {
            return _getModulationInfo?.Invoke(entity) ?? new MyTuple<bool, bool, float, float>();
        }

        public MyTuple<bool, int, int, float, float> GetFaceInfo(IMyTerminalBlock block, Vector3D pos,
            bool posMustBeInside = false)
        {
            return _getFaceInfo?.Invoke(block, pos, posMustBeInside) ?? new MyTuple<bool, int, int, float, float>();
        }

        public MyTuple<bool, int, int, float, float, float> TAPI_GetFaceInfoAndPenChance(IMyTerminalBlock block,
            Vector3D pos, bool posMustBeInside = false)
        {
            return _getFaceInfoAndPenChance?.Invoke(block, pos, posMustBeInside) ??
                   new MyTuple<bool, int, int, float, float, float>();
        }

        public void AddAttacker(long attacker)
        {
            _addAtacker?.Invoke(attacker);
        }

        public bool IsBlockProtected(IMySlimBlock block)
        {
            return _isBlockProtected?.Invoke(block) ?? false;
        }

        public MyTuple<bool, Vector3I> GetFacesFast(MyEntity entity)
        {
            return _getFacesFast?.Invoke(entity) ?? new MyTuple<bool, Vector3I>();
        }

        public void GetLastAttackers(MyEntity entity, ICollection<MyTuple<long, float, uint>> collection)
        {
            _getLastAttackers?.Invoke(entity, collection);
        }

        public bool IsFortified(IMyTerminalBlock block)
        {
            return _isFortified?.Invoke(block) ?? false;
        }
    }
}