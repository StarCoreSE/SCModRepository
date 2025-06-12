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
        private float _bufferReactorOutput;


        protected override string[] BlockSubtypes => new[] { "Caster_Reactor" };
        protected override string ReadableName => "Reactor";
        protected override float ProductionPerFusionPower => SFusionSystem.MegawattsPerFusionPower;


        public override void UpdatePower(float powerGeneration, int numberReactors)
        {
            BufferPowerGeneration = powerGeneration;
            bufferBlockCount = numberReactors;

            var reactorConsumptionMultiplier =
                OverrideEnabled.Value
                    ? OverridePowerUsageSync.Value
                    : PowerUsageSync.Value; // This is ugly, let's make it better.
            reactorConsumptionMultiplier /= numberReactors;

            // Power generation consumed (per second)
            var powerConsumption = powerGeneration * 60 * reactorConsumptionMultiplier;

            var reactorEfficiencyMultiplier = 1 / (0.489f + reactorConsumptionMultiplier);

            // Power generated (per second)
            var reactorOutput = reactorEfficiencyMultiplier * powerConsumption * ProductionPerFusionPower;

            _bufferReactorOutput = reactorOutput;
            MaxPowerConsumption = powerConsumption / 60;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {Math.Round(reactorOutput, 1)}/{Math.Round(powerGeneration * 60 * ProductionPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(powerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(reactorEfficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            SyncMultipliers.ReactorOutput(Block, _bufferReactorOutput);
        }

        #region Base Methods

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