using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    FusionParts.FusionReactor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "Caster_Reactor")]
    public class FusionReactorLogic : FusionPart<IMyReactor>
    {
        // TODO: Convert into class inheritance-based
        private static bool _haveControlsInited;

        private readonly StringBuilder InfoText = new StringBuilder("Output: 0/0\nInput: 0/0\nEfficiency: N/A");

        private float BufferPowerGeneration;
        private float BufferReactorOutput;
        public float MaxPowerConsumption;

        internal S_FusionSystem MemberSystem;
        public float PowerConsumption;

        public void UpdatePower(float PowerGeneration, float MegawattsPerFusionPower)
        {
            BufferPowerGeneration = PowerGeneration;

            var reactorConsumptionMultiplier = OverrideEnabled.Value ? OverridePowerUsageSync : PowerUsageSync.Value; // This is ugly, let's make it better.
            var reactorEfficiencyMultiplier = 1 / (0.5f + reactorConsumptionMultiplier);

            // Power generation consumed (per second)
            var powerConsumption = PowerGeneration * 60 * reactorConsumptionMultiplier;
            // Power generated (per second)
            var reactorOutput = reactorEfficiencyMultiplier * powerConsumption * MegawattsPerFusionPower;
            BufferReactorOutput = reactorOutput;
            MaxPowerConsumption = powerConsumption / 60;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {Math.Round(reactorOutput, 1)}/{Math.Round(PowerGeneration * 60 * MegawattsPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(PowerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(reactorEfficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            SyncMultipliers.ReactorOutput(Block, BufferReactorOutput);
        }

        private void CreateControls()
        {
            {
                var boostPowerToggle =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyReactor>(
                        "FusionSystems.ReactorBoostPowerToggle");
                boostPowerToggle.Title = MyStringId.GetOrCompute("Override Fusion Power");
                boostPowerToggle.Tooltip =
                    MyStringId.GetOrCompute("Toggles Power Override - a temporary override on Fusion Power draw.");
                boostPowerToggle.Getter = block =>
                    block.GameLogic.GetAs<FusionReactorLogic>()?.OverrideEnabled.Value ?? false;
                boostPowerToggle.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionReactorLogic>().OverrideEnabled.Value = value;

                boostPowerToggle.OnText = MyStringId.GetOrCompute("On");
                boostPowerToggle.OffText = MyStringId.GetOrCompute("Off");

                boostPowerToggle.Visible = block => block.BlockDefinition.SubtypeName == "Caster_Reactor";
                boostPowerToggle.SupportsMultipleBlocks = true;
                boostPowerToggle.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<IMyReactor>(boostPowerToggle);
            }
            {
                var powerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyReactor>(
                        "FusionSystems.ReactorPowerUsage");
                powerUsageSlider.Title = MyStringId.GetOrCompute("Fusion Power Usage");
                powerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute("Fusion Power generation this reactor should use.");
                powerUsageSlider.SetLimits(0.01f, 0.99f);
                powerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionReactorLogic>()?.PowerUsageSync.Value ?? 0;
                powerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionReactorLogic>().PowerUsageSync.Value = value;

                powerUsageSlider.Writer = (block, builder) =>
                    builder.Append(Math.Round(block.GameLogic.GetAs<FusionReactorLogic>().PowerUsageSync.Value * 100))
                        .Append('%');

                powerUsageSlider.Visible = block => block.BlockDefinition.SubtypeName == "Caster_Reactor";
                powerUsageSlider.SupportsMultipleBlocks = true;
                powerUsageSlider.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<IMyReactor>(powerUsageSlider);
            }
            {
                var boostPowerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyReactor>(
                        "FusionSystems.ReactorBoostPowerUsage");
                boostPowerUsageSlider.Title = MyStringId.GetOrCompute("Override Power Usage");
                boostPowerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute("Fusion Power generation this reactor should use when Override is enabled.");
                boostPowerUsageSlider.SetLimits(0.01f, 4.0f);
                boostPowerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionReactorLogic>()?.OverridePowerUsageSync.Value ?? 0;
                boostPowerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionReactorLogic>().OverridePowerUsageSync.Value = value;

                boostPowerUsageSlider.Writer = (block, builder) =>
                    builder.Append(Math.Round(block.GameLogic.GetAs<FusionReactorLogic>().OverridePowerUsageSync.Value * 100))
                        .Append('%');

                boostPowerUsageSlider.Visible = block => block.BlockDefinition.SubtypeName == "Caster_Reactor";
                boostPowerUsageSlider.SupportsMultipleBlocks = true;
                boostPowerUsageSlider.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<IMyReactor>(boostPowerUsageSlider);
            }

            MyAPIGateway.TerminalControls.CustomControlGetter += AssignDetailedInfoGetter;

            _haveControlsInited = true;
        }

        private void AssignDetailedInfoGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block.BlockDefinition.SubtypeName != "Caster_Reactor")
                return;
            block.RefreshCustomInfo();
            block.SetDetailedInfoDirty();
        }

        public void SetPowerBoost(bool value)
        {
            if (OverrideEnabled.Value == value)
                return;

            OverrideEnabled.Value = value;
            UpdatePower(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
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
                    UpdatePower(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
            };

            // Trigger power update is only needed when OverrideEnabled is true
            OverridePowerUsageSync.ValueChanged += value =>
            {
                if (OverrideEnabled.Value)
                    UpdatePower(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
            };

            // Trigger power update if boostEnabled is changed
            OverrideEnabled.ValueChanged += value =>
                UpdatePower(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            if (!_haveControlsInited)
                CreateControls();

            ((IMyTerminalBlock)Block).AppendingCustomInfo += FusionReactorLogic_AppendingCustomInfo;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // If boost is unsustainable, disable it.
            // If power draw exceeds power available, disable self until available.
            if (MemberSystem?.PowerStored <= PowerConsumption || !Block.IsWorking)
            {
                SetPowerBoost(false);
                PowerConsumption = 0;
                SyncMultipliers.ReactorOutput(Block, 0);
            }
            else
            {
                SyncMultipliers.ReactorOutput(Block, BufferReactorOutput);
                PowerConsumption = MaxPowerConsumption * Block.CurrentOutputRatio;
            }
        }

        private void FusionReactorLogic_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            stringBuilder.Insert(0, InfoText.ToString());
        }

        #endregion
    }
}