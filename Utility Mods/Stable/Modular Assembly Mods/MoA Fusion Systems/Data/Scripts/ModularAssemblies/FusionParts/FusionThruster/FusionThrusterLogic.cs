using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.FusionParts.FusionThruster
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "Caster_FocusLens")]
    public class FusionThrusterLogic : FusionPart<IMyThrust>
    {
        private float BufferThrustOutput;


        internal override string BlockSubtype => "Caster_FocusLens";
        internal override string ReadableName => "Thruster";

        public void UpdateThrust(float PowerGeneration, float NewtonsPerFusionPower)
        {
            BufferPowerGeneration = PowerGeneration;

            var consumptionMultiplier =
                OverrideEnabled.Value
                    ? OverridePowerUsageSync
                    : PowerUsageSync.Value; // This is ugly, let's make it better.
            var efficiencyMultiplier = 1 / (0.5f + consumptionMultiplier);

            // Power generation consumed (per second)
            var powerConsumption = PowerGeneration * 60 * consumptionMultiplier;
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
            UpdateThrust(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower);
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
                    UpdateThrust(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower);
            };

            // Trigger power update is only needed when OverrideEnabled is true
            OverridePowerUsageSync.ValueChanged += value =>
            {
                if (OverrideEnabled.Value)
                    UpdateThrust(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower);
            };

            // Trigger power update if boostEnabled is changed
            OverrideEnabled.ValueChanged += value =>
                UpdateThrust(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // If boost is unsustainable, disable it.
            // If power draw exceeds power available, disable self until available.
            if ((OverrideEnabled.Value && MemberSystem?.PowerStored <= PowerConsumption * 120) || !Block.IsWorking)
            {
                SetPowerBoost(false);
                PowerConsumption = 0;
                SyncMultipliers.ThrusterOutput(Block, 0);
            }
            else
            {
                SyncMultipliers.ThrusterOutput(Block, BufferThrustOutput);
                PowerConsumption = MaxPowerConsumption * (Block.CurrentThrustPercentage / 100f);
            }
        }

        #endregion
    }
}