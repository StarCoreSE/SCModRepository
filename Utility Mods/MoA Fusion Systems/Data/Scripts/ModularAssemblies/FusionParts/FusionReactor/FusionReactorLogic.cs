using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Epstein_Fusion_DS.FusionParts.FusionReactor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "Caster_Reactor")]
    public class FusionReactorLogic : FusionPart<IMyReactor>
    {
        private int _bufferBlockCount = 1;
        private float _bufferReactorOutput;


        internal override string[] BlockSubtypes => new[] { "Caster_Reactor" };
        internal override string ReadableName => "Reactor";


        public override void UpdatePower(float powerGeneration, float megawattsPerFusionPower, int numberReactors)
        {
            BufferPowerGeneration = powerGeneration;
            _bufferBlockCount = numberReactors;

            var reactorConsumptionMultiplier =
                OverrideEnabled.Value
                    ? OverridePowerUsageSync.Value
                    : PowerUsageSync.Value; // This is ugly, let's make it better.
            reactorConsumptionMultiplier /= numberReactors;

            // Power generation consumed (per second)
            var powerConsumption = powerGeneration * 60 * reactorConsumptionMultiplier;

            var reactorEfficiencyMultiplier = 1 / (0.489f + reactorConsumptionMultiplier);

            // Power generated (per second)
            var reactorOutput = reactorEfficiencyMultiplier * powerConsumption * megawattsPerFusionPower;

            _bufferReactorOutput = reactorOutput;
            MaxPowerConsumption = powerConsumption / 60;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {Math.Round(reactorOutput, 1)}/{Math.Round(powerGeneration * 60 * megawattsPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(powerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(reactorEfficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            SyncMultipliers.ReactorOutput(Block, _bufferReactorOutput);
        }

        public void SetPowerBoost(bool value)
        {
            if (OverrideEnabled.Value == value)
                return;

            OverrideEnabled.Value = value;
            UpdatePower(BufferPowerGeneration, SFusionSystem.MegawattsPerFusionPower, _bufferBlockCount);
        }

        #region Base Methods

        public override void Init(MyObjectBuilder_EntityBase definition)
        {
            base.Init(definition);
            Block = (IMyReactor)Entity;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // Trigger power update is only needed when OverrideEnabled is false
            PowerUsageSync.ValueChanged += value =>
            {
                if (!OverrideEnabled.Value)
                    UpdatePower(BufferPowerGeneration, SFusionSystem.MegawattsPerFusionPower, _bufferBlockCount);
            };

            // Trigger power update is only needed when OverrideEnabled is true
            OverridePowerUsageSync.ValueChanged += value =>
            {
                if (OverrideEnabled.Value)
                    UpdatePower(BufferPowerGeneration, SFusionSystem.MegawattsPerFusionPower, _bufferBlockCount);
            };

            // Trigger power update if boostEnabled is changed
            OverrideEnabled.ValueChanged += value =>
                UpdatePower(BufferPowerGeneration, SFusionSystem.MegawattsPerFusionPower, _bufferBlockCount);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            var storagePct = (MemberSystem?.PowerStored / MemberSystem?.MaxPowerStored) ?? 0;
            if (float.IsNaN(storagePct) || float.IsInfinity(storagePct))
                storagePct = 0;

            if (storagePct <= 0)
            {
                if (Block.MaxOutput == 0)
                    return;
                SyncMultipliers.ReactorOutput(Block, 0);
                PowerConsumption = 0;
                LastShutdown = DateTime.Now.Ticks + 4 * TimeSpan.TicksPerSecond;
                return;
            }

            // If boost is unsustainable, disable it.
            // If power draw exceeds power available, disable self until available.
            if ((OverrideEnabled.Value && MemberSystem?.PowerStored <= MemberSystem?.PowerConsumption * 30) ||
                !Block.IsWorking)
            {
                SetPowerBoost(false);
                PowerConsumption = 0;
                SyncMultipliers.ReactorOutput(Block, 0);
            }
            else if (storagePct > 0.025f && DateTime.Now.Ticks > LastShutdown)
            {
                SyncMultipliers.ReactorOutput(Block, _bufferReactorOutput);
                PowerConsumption = MaxPowerConsumption * Block.CurrentOutputRatio;
            }
        }

        #endregion
    }
}