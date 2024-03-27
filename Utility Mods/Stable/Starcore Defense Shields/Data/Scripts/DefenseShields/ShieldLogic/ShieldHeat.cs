using System;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DefenseShields
{
    using Support;

    public partial class DefenseShields
    {

        private void UpdateHeatRate()
        {
            var heat = DsState.State.Heat;
            heat /= 10;

            if (heat >= 10) ShieldChargeRate = 0;
            else
            {
                _expChargeReduction = ExpChargeReductions[heat];
                ShieldChargeRate /= _expChargeReduction;
            }
        }

        private void StepDamageState()
        {
            if (_isServer) Heating();

            if (_tick30)
            {
                _runningDamage = _dpsAvg.Add((int)_damageReadOut);
                _runningHeal = _hpsAvg.Add((int)(ShieldChargeRate * ConvToHp));
                _damageReadOut = 0;

            }
        }

        private void Heating()
        {
            if (ChargeMgr.AbsorbHeat > 0)
                _lastHeatTick = _tick;

            var oldMaxHpScaler = DsState.State.MaxHpReductionScaler;
            var heatSinkActive = DsSet.Settings.SinkHeatCount > HeatSinkCount;


            if ((heatSinkActive || _sinkCount != 0) && oldMaxHpScaler <= 0.90)
                DecreaseHeatLevel();
            else if (!heatSinkActive && oldMaxHpScaler > 0 && _currentHeatStep == 0 && _tick - _lastHeatTick >= 1200)
                IncreaseMaxHealth();

            HeatTick();

            var hp = ShieldMaxCharge * ConvToHp;
            var oldHeat = DsState.State.Heat;

            var heatScale = (ShieldMode == ShieldType.Station || DsSet.Settings.FortifyShield) && DsState.State.Enhancer ? Session.Enforced.HeatScaler * 2.75f : Session.Enforced.HeatScaler * 1f;
            var thresholdAmount = heatScale * _heatScaleHp;
            var nextThreshold = hp * thresholdAmount * (_currentHeatStep + 1);
            
            var scaledOverHeat = OverHeat / _heatScaleTime;
            var scaledHeatingSteps = HeatingStep / _heatScaleTime;

            var lastStep = _currentHeatStep == 10;
            var overloadStep = _heatCycle == scaledOverHeat && lastStep;

            var nextCycle = _heatCycle == (_currentHeatStep * scaledHeatingSteps) + scaledOverHeat;

            var fastCoolDown = (heatSinkActive || DsState.State.Overload) && (_heatCycle % 200 == 0);
            var pastThreshold = ChargeMgr.AbsorbHeat > nextThreshold;
            var venting = lastStep && pastThreshold;
            var leftCritical = lastStep && _tick >= _heatVentingTick;

            var backTwoCycles = ((_currentHeatStep - 2) * scaledHeatingSteps) + scaledOverHeat + 1;

            if (overloadStep)
                Overload(hp, thresholdAmount, nextThreshold);
            else if (fastCoolDown)
                FastCoolDown(hp, thresholdAmount, scaledHeatingSteps, scaledOverHeat, backTwoCycles);
            else if (nextCycle && !lastStep)
                NextCycleAction(hp, nextThreshold, thresholdAmount, pastThreshold, scaledHeatingSteps, scaledOverHeat, backTwoCycles);
            else if (venting)
                Venting(nextThreshold);
            else if (leftCritical)
                NoLongerCritical(backTwoCycles, thresholdAmount, nextThreshold, hp);

            var fallbackTime = _heatCycle > (HeatingStep * 10) + OverHeat && _tick >= _heatVentingTick;
            if (fallbackTime)
                FallBack();

            if (oldHeat != DsState.State.Heat || !MyUtils.IsEqual(oldMaxHpScaler, DsState.State.MaxHpReductionScaler)) 
                ShieldChangeState();
        }

        private void HeatTick()
        {
            var ewarProt = DsState.State.EwarProtection && ShieldMode != ShieldType.Station;

            if (_tick30 && ChargeMgr.AbsorbHeat > 0 && _heatCycle == -1)
                _heatCycle = 0;
            else if (_heatCycle > -1)
                _heatCycle++;

            if (ewarProt && _heatCycle == 0) {
                _heatScaleHp = 0.1f;
                _heatScaleTime = 5;
            }
            else if (!ewarProt && _heatCycle == 0) {
                _heatScaleHp = 1f;
                _heatScaleTime = 1;
            }
        }
        
        private void NextCycleAction(float hp, float nextThreshold, float thresholdAmount, bool pastThreshold, int scaledHeatingSteps, int scaledOverHeat, int backTwoCycles)
        {
            var currentThreshold = hp * thresholdAmount * _currentHeatStep;
            var metThreshold = ChargeMgr.AbsorbHeat > currentThreshold;
            var underThreshold = !pastThreshold && !metThreshold;
            var backOneCycles = ((_currentHeatStep - 1) * scaledHeatingSteps) + scaledOverHeat + 1;

            if (_heatScaleTime == 5)
            {
                if (ChargeMgr.AbsorbHeat > 0)
                {
                    _fallbackCycle = 1;
                    ChargeMgr.AbsorbHeat = 0;
                }
                else _fallbackCycle++;
            }

            if (pastThreshold)
            {
                PastThreshold(hp, nextThreshold, thresholdAmount);
            }
            else if (metThreshold)
            {
                MetThreshold(backOneCycles, hp, nextThreshold, thresholdAmount);
            }
            else _heatCycle = backOneCycles;

            if (_fallbackCycle == FallBackStep || underThreshold)
            {
                DropHeat(backTwoCycles, currentThreshold);
            }
        }

        public void FastCoolDown(float hp, float thresholdAmount, int scaledHeatingSteps, int scaledOverHeat, int backTwoCycles)
        {
            var currentThreshold = hp * thresholdAmount * _currentHeatStep;
            var backOneCycles = ((_currentHeatStep - 1) * scaledHeatingSteps) + scaledOverHeat + 1;
            _heatCycle = backOneCycles;

            DropHeat(backTwoCycles, currentThreshold);
        }

        public void DecreaseHeatLevel()
        {
            ChargeMgr.AbsorbHeat = 0;
            var end = _tick30 && _sinkCount++ >= SinkCountTime;
            if (!end)
                return;

            _sinkCount = 0;
            HeatSinkCount = DsSet.Settings.SinkHeatCount;
            var hpLoss = DsSet.Settings.FortifyShield ? 0.2 : 0.1;
            DsState.State.MaxHpReductionScaler = (float)MathHelper.Clamp(Math.Round(DsState.State.MaxHpReductionScaler + hpLoss, 2), 0.10, 0.90);
        }

        public void IncreaseMaxHealth()
        {
            if (DsState.State.ShieldPercent >= 100)
                DsState.State.MaxHpReductionScaler = (float) MathHelper.Clamp(Math.Round(DsState.State.MaxHpReductionScaler - 0.10, 2), 0, 0.80);
        }

        private void Overload(float hp, float thresholdAmount, float nextThreshold)
        {
            var overload = ChargeMgr.AbsorbHeat > hp * thresholdAmount * 2;
            if (overload)
            {
                if (Session.Enforced.Debug == 3) Log.Line($"overh - stage:{_currentHeatStep + 1} - cycle:{_heatCycle} - resetCycle:xxxx - heat:{ChargeMgr.AbsorbHeat} - threshold:{hp * thresholdAmount * 2}[{hp / hp * thresholdAmount * (_currentHeatStep + 1)}] - nThreshold:{hp * thresholdAmount * (_currentHeatStep + 2)} - ShieldId [{Shield.EntityId}]");
                _currentHeatStep = 1;
                DsState.State.Heat = _currentHeatStep * 10;
                ChargeMgr.AbsorbHeat = 0;
            }
            else
            {
                if (Session.Enforced.Debug == 3) Log.Line($"under - stage:{_currentHeatStep} - cycle:{_heatCycle} - resetCycle:[-1] - heat:{ChargeMgr.AbsorbHeat} - threshold:{nextThreshold} - ShieldId [{Shield.EntityId}]");
                DsState.State.Heat = 0;
                _currentHeatStep = 0;
                _heatCycle = -1;
                ChargeMgr.AbsorbHeat = 0;
            }
        }

        private void Venting(float nextThreshold)
        {
            if (Session.Enforced.Debug == 4) Log.Line($"mainc - stage:{_currentHeatStep} - cycle:{_heatCycle} - resetCycle:xxxx - heat:{ChargeMgr.AbsorbHeat} - threshold:{nextThreshold} - ShieldId [{Shield.EntityId}]");
            _heatVentingTick = _tick + CoolingStep;
            ChargeMgr.AbsorbHeat = 0;
        }

        private void NoLongerCritical(int backTwoCycles, float thresholdAmount, float nextThreshold, float hp)
        {
            if (_currentHeatStep >= 10) _currentHeatStep--;
            if (Session.Enforced.Debug == 4) Log.Line($"leftc - stage:{_currentHeatStep} - cycle:{_heatCycle} - resetCycle:{backTwoCycles} - heat:{ChargeMgr.AbsorbHeat} - threshold:{nextThreshold}[{hp / hp * thresholdAmount * (_currentHeatStep + 1)}] - nThreshold:{hp * thresholdAmount * (_currentHeatStep + 2)} - ShieldId [{Shield.EntityId}]");
            DsState.State.Heat = _currentHeatStep * 10;
            _heatCycle = backTwoCycles;
            _heatVentingTick = uint.MaxValue;
            ChargeMgr.AbsorbHeat = 0;
        }

        private void DropHeat(int backTwoCycles, float currentThreshold)
        {
            if (_currentHeatStep == 0)
            {
                DsState.State.Heat = 0;
                _currentHeatStep = 0;
                if (Session.Enforced.Debug == 4) Log.Line($"nohea - stage:{_currentHeatStep} - cycle:{_heatCycle} - resetCycle:[-1] - heat:{ChargeMgr.AbsorbHeat} - ShieldId [{Shield.EntityId}]");
                _heatCycle = -1;
                ChargeMgr.AbsorbHeat = 0;
                _fallbackCycle = 0;
            }
            else
            {
                if (Session.Enforced.Debug == 4) Log.Line($"decto - stage:{_currentHeatStep - 1} - cycle:{_heatCycle} - resetCycle:{backTwoCycles} - heat:{ChargeMgr.AbsorbHeat} - threshold:{currentThreshold} - ShieldId [{Shield.EntityId}]");
                _currentHeatStep--;
                DsState.State.Heat = _currentHeatStep * 10;
                _heatCycle = backTwoCycles;
                ChargeMgr.AbsorbHeat = 0;
                _fallbackCycle = 0;
            }
        }

        private void MetThreshold(int backOneCycles, float hp, float nextThreshold, float thresholdAmount)
        {
            if (Session.Enforced.Debug == 4) Log.Line($"uncha - stage:{_currentHeatStep} - cycle:{_heatCycle} - resetCycle:{backOneCycles} - heat:{ChargeMgr.AbsorbHeat} - threshold:{nextThreshold} - nThreshold:{hp * thresholdAmount * (_currentHeatStep + 2)} - ShieldId [{Shield.EntityId}]");
            DsState.State.Heat = _currentHeatStep * 10;
            _heatCycle = backOneCycles;
            ChargeMgr.AbsorbHeat = 0;
        }

        private void PastThreshold(float hp, float nextThreshold, float thresholdAmount)
        {
            if (Session.Enforced.Debug == 4) Log.Line($"incre - stage:{_currentHeatStep + 1} - cycle:{_heatCycle} - resetCycle:xxxx - heat:{ChargeMgr.AbsorbHeat} - threshold:{nextThreshold}[{hp / hp * thresholdAmount * (_currentHeatStep + 1)}] - nThreshold:{hp * thresholdAmount * (_currentHeatStep + 2)} - ShieldId [{Shield.EntityId}]");
            _currentHeatStep++;
            DsState.State.Heat = _currentHeatStep * 10;
            ChargeMgr.AbsorbHeat = 0;
            if (_currentHeatStep == 10) _heatVentingTick = _tick + CoolingStep;
        }

        private void FallBack()
        {
            if (Session.Enforced.Debug == 4) Log.Line($"HeatCycle over limit, resetting: heatCycle:{_heatCycle} - fallCycle:{_fallbackCycle}");
            _heatCycle = -1;
            _fallbackCycle = 0;
        }

    }
}