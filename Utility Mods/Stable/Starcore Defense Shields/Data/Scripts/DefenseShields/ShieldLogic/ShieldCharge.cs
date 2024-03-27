using VRageMath;
using System;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using VRage;
using VRage.Collections;
using VRage.Utils;
using static VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNode;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        private bool PowerOnline()
        {
            UpdateGridPower();

            if (!_shieldPowered) return false;

            CalculatePowerCharge();

            if (!WarmedUp) return true;

            if (_isServer && _shieldConsumptionRate.Equals(0f) && DsState.State.Charge.Equals(0.01f))
                return false;

            _power = _shieldMaxChargeRate > 0 ? _shieldConsumptionRate + _shieldMaintaintPower : 0f;

            if (_power < ShieldCurrentPower && (_power - _shieldMaxChargeRate) >= 0.0001f) 
                _sink.Update();
            else if (_count == 28 && (ShieldCurrentPower <= 0 || Math.Abs(_power - ShieldCurrentPower) >= 0.0001f)) 
                _sink.Update();

            if (ChargeMgr.Absorb > 0) {

                _damageReadOut += ChargeMgr.Absorb;
                ChargeMgr.NormalAverage.UpdateAverage(ChargeMgr.EnergyDamage, ChargeMgr.KineticDamage, !AggregateModulation);
                ChargeMgr.AverageNormDamage = ChargeMgr.NormalAverage.GetDifferenceRatio();
                ChargeMgr.LastDamageTick = _tick;

                EffectsCleanTick = _tick;
                ChargeMgr.SetCharge((ChargeMgr.Absorb * ShieldChargeMgr.ConvToWatts), ShieldChargeMgr.ChargeMode.Discharge);
            }
            else if (ChargeMgr.Absorb < 0) 
                ChargeMgr.SetCharge((ChargeMgr.Absorb * -1) * ShieldChargeMgr.ConvToWatts, ShieldChargeMgr.ChargeMode.Charge);

            if (_isServer && DsState.State.Charge < 0) {

                ChargeMgr.SetCharge(0, ShieldChargeMgr.ChargeMode.Zero);
                if (!_empOverLoad) _overLoadLoop = 0;
                else _empOverLoadLoop = 0;
            }

            if (_tick - ChargeMgr.LastDamageTick > 600)
                ChargeMgr.ClearDamageTypeInfo();
            
            ChargeMgr.Absorb = 0f;
            ChargeMgr.KineticDamage = 0;
            ChargeMgr.EnergyDamage = 0;
            return DsState.State.Charge > 0;
        }

        private void CalculatePowerCharge()
        {
            var heat = DsState.State.Heat;
            var nerfScaler = DsState.State.NerfScaler <= 0 ? 1 : DsState.State.NerfScaler;
            var hpsEfficiency = Session.Enforced.HpsEfficiency;
            var baseScaler = Session.Enforced.BaseScaler;
            var maintenanceCost = Session.Enforced.MaintenanceCost;
            var fortify = DsSet.Settings.FortifyShield && DsState.State.Enhancer;
            var shieldTypeRatio = _shieldTypeRatio / 100f;
            var shieldMaintainPercent = maintenanceCost / 100;
            _shieldCapped = DsState.State.CapModifier < 1;

            if (ShieldMode == ShieldType.Station && DsState.State.Enhancer)
                hpsEfficiency *= 3.5f;
            else if (fortify) {
                var fortMod = heat <= 0 ? 3f : heat == 1 ? 2f : heat == 2 ? 1.5f : 1.25f;
                hpsEfficiency *= fortMod;
            }

            var bufferMaxScaler = ((baseScaler * shieldTypeRatio) / _sizeScaler) * (2 - nerfScaler);

            var maxHp = ShieldMaxPower * bufferMaxScaler;
            var reducedMaxHp = maxHp - (maxHp * DsState.State.MaxHpReductionScaler);
            ShieldMaxHp = reducedMaxHp;
            var bonus = 0f;

            if (DsState.State.CapModifier < 1) {
                var diff = 1 - DsState.State.CapModifier;
                if (ShieldMode == ShieldType.Station) {
                    bonus = 1 - (diff / 2) / 2;
                }
                else if (fortify)
                    bonus = (diff / 2);

                bonus *= Session.Enforced.PowerScaler;
            }

            var maxHpScaler = DsState.State.CapModifier + bonus;
            shieldMaintainPercent = shieldMaintainPercent * DsState.State.EnhancerPowerMulti * (DsState.State.ShieldPercent * ConvToDec);
            
            if (DsState.State.Lowered) 
                shieldMaintainPercent *= 0.25f;

            _shieldMaintaintPower = ShieldMaxPower * maxHpScaler * shieldMaintainPercent;

            ShieldChargeBase = maxHp * maxHpScaler;
            ShieldMaxCharge = ShieldMaxHp * maxHpScaler;
            var powerForShield = PowerNeeded(hpsEfficiency);

            if (!WarmedUp) return;

            var overCharged = DsState.State.Charge > ShieldMaxCharge;
            if (overCharged && ++_overChargeCount >= 120) {
                ChargeMgr.SetCharge(ShieldMaxCharge, ShieldChargeMgr.ChargeMode.Set);
                _overChargeCount = 0;
            }
            else if (!overCharged)
                _overChargeCount = 0;

            if (_isServer) {

                var powerLost = powerForShield <= 0 || _powerNeeded > ShieldMaxPower || MyUtils.IsZero(ShieldMaxPower - _powerNeeded);
                var serverNoPower = DsState.State.NoPower;

                if (powerLost && _pLossTimer++ > 60 || serverNoPower) {

                    if (PowerLoss(powerLost, serverNoPower)) {
                        _powerFail = true;
                        return;
                    }
                }
                else {

                    _pLossTimer = 0;

                    if (_capacitorLoop != 0 && _tick - _capacitorTick > CapacitorStableCount) 
                        _capacitorLoop = 0;

                    _powerFail = false;
                }
            }

            if (heat != 0) 
                UpdateHeatRate();
            else 
                _expChargeReduction = 0;
            if (_count == 29 && DsState.State.Charge < ShieldMaxCharge) {
                ChargeMgr.SetCharge(ShieldChargeRate, ShieldChargeMgr.ChargeMode.Charge);
            }
            else if (DsState.State.Charge.Equals(ShieldMaxCharge))
            {
                
                ShieldChargeRate = 0f;
                _shieldConsumptionRate = 0f;
            }


            if (_isServer) {

                if (DsState.State.Charge < ShieldMaxCharge) {
                    DsState.State.ShieldPercent = DsState.State.Charge / ShieldMaxCharge * 100;
                }
                else if (DsState.State.Charge < ShieldMaxCharge * 0.1) {
                    DsState.State.ShieldPercent = 0f;
                }
                else {
                    DsState.State.ShieldPercent = 100f;
                }
            }
        }

        private float PowerNeeded(float hpsEfficiency)
        {
            var cleanPower = ShieldAvailablePower + ShieldCurrentPower;
            _otherPower = ShieldMaxPower - cleanPower;
            var powerForShield = (cleanPower * 0.9f) - _shieldMaintaintPower;
            var rawMaxChargeRate = powerForShield > 0 ? powerForShield : 0f;
            _shieldMaxChargeRate = rawMaxChargeRate;
            _shieldPeakRate = (_shieldMaxChargeRate * hpsEfficiency);

            if (DsState.State.Charge + _shieldPeakRate < ShieldMaxCharge) {
                ShieldChargeRate = _shieldPeakRate;
                _shieldConsumptionRate = _shieldMaxChargeRate;
            }
            else {

                if (_shieldPeakRate > 0) {

                    var remaining = MathHelper.Clamp(ShieldMaxCharge - DsState.State.Charge, 0, ShieldMaxCharge);
                    var remainingScaled = remaining / _shieldPeakRate;
                    _shieldConsumptionRate = remainingScaled * _shieldMaxChargeRate;
                    ShieldChargeRate = _shieldPeakRate * remainingScaled;
                }
                else {
                    _shieldConsumptionRate = 0;
                    ShieldChargeRate = 0;
                }
            }

            _powerNeeded = _shieldMaintaintPower + _shieldConsumptionRate + _otherPower;

            return powerForShield;
        }

        private bool PowerLoss(bool powerLost, bool serverNoPower)
        {
            if (powerLost) {

                if (!DsState.State.Online) {

                    ChargeMgr.SetCharge(0.01f, ShieldChargeMgr.ChargeMode.Set);
                    ShieldChargeRate = 0f;
                    _shieldConsumptionRate = 0f;
                    return true;
                }

                _capacitorTick = _tick;
                _capacitorLoop++;

                if (_capacitorLoop > CapacitorDrainCount) {

                    if (_isServer && !DsState.State.NoPower) {

                        DsState.State.NoPower = true;
                        _sendMessage = true;
                        ShieldChangeState();
                    }

                    var shieldLoss = ShieldMaxCharge * 0.0016667f;
                    ChargeMgr.SetCharge(shieldLoss, ShieldChargeMgr.ChargeMode.Discharge);
                    if (DsState.State.Charge < 0.01f)
                        ChargeMgr.SetCharge(0.01f, ShieldChargeMgr.ChargeMode.Set);

                    if (_isServer) {
                        if (DsState.State.Charge < ShieldMaxCharge) 
                            DsState.State.ShieldPercent = DsState.State.Charge / ShieldMaxCharge * 100;
                        else if (DsState.State.Charge < ShieldMaxCharge * 0.1) 
                            DsState.State.ShieldPercent = 0f;
                        else 
                            DsState.State.ShieldPercent = 100f;
                    }

                    ShieldChargeRate = 0f;
                    _shieldConsumptionRate = 0f;
                    return true;
                }
            }

            if (serverNoPower) {

                _powerNoticeLoop++;
                if (_powerNoticeLoop >= PowerNoticeCount) {

                    DsState.State.NoPower = false;
                    _powerNoticeLoop = 0;
                    ShieldChangeState();
                }
            }
            return false;
        }


        private void UpdateGridPower()
        {
            GridAvailablePower = 0;
            GridMaxPower = 0;
            GridCurrentPower = 0;
            _batteryCurrentInput = 0;

            if (MyResourceDist == null || MyResourceDist.SourcesEnabled == MyMultipleEnabledEnum.NoObjects || _checkResourceDist) {
                ResetDistributor();
                if (MyResourceDist == null) 
                    return;
            }


            GridMaxPower = MyResourceDist.MaxAvailableResourceByType(GId, MyGrid);
            GridCurrentPower = MyResourceDist.TotalRequiredInputByType(GId, MyGrid);
            if (!DsSet.Settings.UseBatteries) CalculateBatteryInput();

            GridAvailablePower = GridMaxPower - GridCurrentPower;

            if (!DsSet.Settings.UseBatteries)
            {
                GridCurrentPower += _batteryCurrentInput;
                GridAvailablePower -= _batteryCurrentInput;
            }

            var powerScale = Session.Instance.GameLoaded ? DsSet.Settings.PowerScale : 0;
            var reserveScaler = ReserveScaler[powerScale];
            var userPowerCap = DsSet.Settings.PowerWatts * reserveScaler;
            var shieldMax = GridMaxPower > userPowerCap && reserveScaler > 0 ? userPowerCap : GridMaxPower;
            
            if (shieldMax >= ShieldMaxPower  || shieldMax <= 0 || _maxPowerTick++ > 100)
            {
                _maxPowerTick = 0;
                ShieldMaxPower = shieldMax;
            }

            ShieldAvailablePower = shieldMax - GridCurrentPower;

            _shieldPowered = ShieldMaxPower > 0;
        }

        private void CalculateBatteryInput()
        {
            foreach (var sub in ShieldComp.LinkedGrids.Keys)
            {
                ConcurrentCachingList<MyBatteryBlock> batteries;
                if (Session.Instance.GridBatteryMap.TryGetValue(sub, out batteries))
                {
                    for (int i = 0; i < batteries.Count; i++)
                    {

                        var battery = batteries[i];
                        if (!battery.IsWorking) continue;
                        var currentInput = battery.CurrentInput;

                        if (currentInput > 0)
                            _batteryCurrentInput += currentInput;

                    }
                }
            }
        }
    }
}