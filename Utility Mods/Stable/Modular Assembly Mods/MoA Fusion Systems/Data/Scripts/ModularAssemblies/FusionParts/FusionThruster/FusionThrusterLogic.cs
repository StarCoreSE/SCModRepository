using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace StarCore.FusionSystems.FusionParts.FusionThruster
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "Caster_FocusLens")]
    public class FusionThrusterLogic : FusionPart<IMyThrust>
    {
        private int _bufferBlockCount = 1;
        private float _bufferThrustOutput;


        internal override string BlockSubtype => "Caster_FocusLens";
        internal override string ReadableName => "Thruster";

        public override void UpdatePower(float powerGeneration, float newtonsPerFusionPower, int numberThrusters)
        {
            BufferPowerGeneration = powerGeneration;
            _bufferBlockCount = numberThrusters;

            var consumptionMultiplier =
                OverrideEnabled.Value
                    ? OverridePowerUsageSync
                    : PowerUsageSync.Value; // This is ugly, let's make it better.
            consumptionMultiplier /= numberThrusters;

            // Power generation consumed (per second)
            var powerConsumption = powerGeneration * 60 * consumptionMultiplier;

            var efficiencyMultiplier = 1 / (0.489f + consumptionMultiplier);

            // Power generated (per second)
            var thrustOutput = efficiencyMultiplier * powerConsumption * newtonsPerFusionPower;
            _bufferThrustOutput = thrustOutput;
            MaxPowerConsumption = powerConsumption / 60;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {Math.Round(thrustOutput, 1)}/{Math.Round(powerGeneration * 60 * newtonsPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(powerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(efficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            if (!IsShutdown)
                SyncMultipliers.ThrusterOutput(Block, _bufferThrustOutput);
        }

        public void SetPowerBoost(bool value)
        {
            if (OverrideEnabled.Value == value)
                return;

            OverrideEnabled.Value = value;
            UpdatePower(BufferPowerGeneration, SFusionSystem.NewtonsPerFusionPower, _bufferBlockCount);
        }

        #region Base Methods

        public override void Init(MyObjectBuilder_EntityBase definition)
        {
            base.Init(definition);
            Block = (IMyThrust)Entity;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // Trigger power update is only needed when OverrideEnabled is false
            PowerUsageSync.ValueChanged += value =>
            {
                if (!OverrideEnabled.Value)
                    UpdatePower(BufferPowerGeneration, SFusionSystem.NewtonsPerFusionPower, _bufferBlockCount);
            };

            // Trigger power update is only needed when OverrideEnabled is true
            OverridePowerUsageSync.ValueChanged += value =>
            {
                if (OverrideEnabled.Value)
                    UpdatePower(BufferPowerGeneration, SFusionSystem.NewtonsPerFusionPower, _bufferBlockCount);
            };

            // Trigger power update if boostEnabled is changed
            OverrideEnabled.ValueChanged += value =>
                UpdatePower(BufferPowerGeneration, SFusionSystem.NewtonsPerFusionPower, _bufferBlockCount);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            var storagePct = MemberSystem?.PowerStored / MemberSystem?.MaxPowerStored ?? 0;

            if (storagePct <= 0.05f)
            {
                if (Block.ThrustMultiplier <= 0.01)
                    return;
                SyncMultipliers.ThrusterOutput(Block, 0);
                PowerConsumption = 0;
                LastShutdown = DateTime.Now.Ticks + 4 * TimeSpan.TicksPerSecond;
                IsShutdown = true;
                return;
            }

            // If boost is unsustainable, disable it.
            // If power draw exceeds power available, disable self until available.
            if ((OverrideEnabled.Value && MemberSystem?.PowerStored <= MemberSystem?.PowerConsumption * 30) ||
                !Block.IsWorking)
            {
                SetPowerBoost(false);
                PowerConsumption = 0;
                SyncMultipliers.ThrusterOutput(Block, 0);
            }
            else if (storagePct > 0.1f && DateTime.Now.Ticks > LastShutdown)
            {
                SyncMultipliers.ThrusterOutput(Block, _bufferThrustOutput);
                PowerConsumption = MaxPowerConsumption * (Block.CurrentThrustPercentage / 100f);
                IsShutdown = false;
            }
        }

        #endregion
    }
}