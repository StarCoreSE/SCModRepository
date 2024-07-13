using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace StarCore.FusionSystems.FusionParts.FusionThruster
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "Caster_FocusLens")]
    public class FusionThrusterLogic : FusionPart<IMyThrust>
    {
        private int BufferBlockCount;
        private float BufferThrustOutput;


        internal override string BlockSubtype => "Caster_FocusLens";
        internal override string ReadableName => "Thruster";

        public override void UpdatePower(float PowerGeneration, float NewtonsPerFusionPower, int numberThrusters)
        {
            BufferPowerGeneration = PowerGeneration;
            BufferBlockCount = numberThrusters;

            var consumptionMultiplier =
                OverrideEnabled.Value
                    ? OverridePowerUsageSync
                    : PowerUsageSync.Value; // This is ugly, let's make it better.

            // Power generation consumed (per second)
            var powerConsumption = PowerGeneration * 60 * consumptionMultiplier / numberThrusters;

            var efficiencyMultiplier = 1 / (0.669f + consumptionMultiplier);

            // Power generated (per second)
            var thrustOutput = efficiencyMultiplier * powerConsumption * NewtonsPerFusionPower;
            BufferThrustOutput = thrustOutput;
            MaxPowerConsumption = powerConsumption / 60;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {Math.Round(thrustOutput, 1)}/{Math.Round(PowerGeneration * 60 * NewtonsPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(PowerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(efficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            SyncMultipliers.ThrusterOutput(Block, BufferThrustOutput);
        }

        public void SetPowerBoost(bool value)
        {
            if (OverrideEnabled.Value == value)
                return;

            OverrideEnabled.Value = value;
            UpdatePower(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower, BufferBlockCount);
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
                    UpdatePower(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower, BufferBlockCount);
            };

            // Trigger power update is only needed when OverrideEnabled is true
            OverridePowerUsageSync.ValueChanged += value =>
            {
                if (OverrideEnabled.Value)
                    UpdatePower(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower, BufferBlockCount);
            };

            // Trigger power update if boostEnabled is changed
            OverrideEnabled.ValueChanged += value =>
                UpdatePower(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower, BufferBlockCount);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            var storagePct = MemberSystem?.PowerStored / MemberSystem?.MaxPowerStored ?? 0;

            if (storagePct <= 0.05f)
            {
                if (Block.ThrustMultiplier == 0)
                    return;
                SyncMultipliers.ThrusterOutput(Block, 0);
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
                SyncMultipliers.ThrusterOutput(Block, 0);
            }
            else if (storagePct > 0.1f && DateTime.Now.Ticks > LastShutdown)
            {
                SyncMultipliers.ThrusterOutput(Block, BufferThrustOutput);
                PowerConsumption = MaxPowerConsumption * (Block.CurrentThrustPercentage / 100f);
            }
        }

        #endregion
    }
}