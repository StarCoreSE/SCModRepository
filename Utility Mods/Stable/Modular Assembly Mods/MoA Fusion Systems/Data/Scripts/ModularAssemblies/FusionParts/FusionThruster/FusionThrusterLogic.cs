using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI.Network;
using VRage.Network;
using VRage.Sync;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.FusionParts.FusionThruster
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "Caster_FocusLens")]
    public class FusionThrusterLogic : MyGameLogicComponent, IMyEventProxy
    {
        private static bool _haveControlsInited;

        private readonly StringBuilder InfoText = new StringBuilder("Output: 0/0\nInput: 0/0\nEfficiency: N/A");

        public IMyThrust Block;

        private float BufferPowerGeneration;
        private float BufferThrustOutput;
        public float MaxPowerConsumption;
        internal S_FusionSystem MemberSystem;
        public float PowerConsumption;

        public MySync<float, SyncDirection.BothWays> PowerUsageSync;
        public MySync<float, SyncDirection.BothWays> OverridePowerUsageSync;
        public MySync<bool, SyncDirection.BothWays> OverrideEnabled;
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public void UpdateThrust(float PowerGeneration, float NewtonsPerFusionPower)
        {
            BufferPowerGeneration = PowerGeneration;

            var consumptionMultiplier = OverrideEnabled.Value ? OverridePowerUsageSync : PowerUsageSync.Value; // This is ugly, let's make it better.
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

        private void CreateControls()
        {
            {
                var boostPowerToggle =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyThrust>(
                        "FusionSystems.ThrustBoostPowerToggle");
                boostPowerToggle.Title = MyStringId.GetOrCompute("Override Fusion Power");
                boostPowerToggle.Tooltip =
                    MyStringId.GetOrCompute("Toggles Power Override - a temporary override on Fusion Power draw.");
                boostPowerToggle.Getter = block =>
                    block.GameLogic.GetAs<FusionThrusterLogic>()?.OverrideEnabled.Value ?? false;
                boostPowerToggle.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionThrusterLogic>().OverrideEnabled.Value = value;

                boostPowerToggle.OnText = MyStringId.GetOrCompute("On");
                boostPowerToggle.OffText = MyStringId.GetOrCompute("Off");

                boostPowerToggle.Visible = block => block.BlockDefinition.SubtypeName == "Caster_FocusLens";
                boostPowerToggle.SupportsMultipleBlocks = true;
                boostPowerToggle.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<IMyThrust>(boostPowerToggle);
            }
            {
                var powerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyThrust>(
                        "FusionSystems.ThrustPowerUsage");
                powerUsageSlider.Title = MyStringId.GetOrCompute("Fusion Power Usage");
                powerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute("Fusion Power generation this Thruster should use.");
                powerUsageSlider.SetLimits(0.01f, 0.99f);
                powerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionThrusterLogic>()?.PowerUsageSync.Value ?? 0;
                powerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionThrusterLogic>().PowerUsageSync.Value = value;

                powerUsageSlider.Writer = (block, builder) =>
                    builder.Append(Math.Round(block.GameLogic.GetAs<FusionThrusterLogic>().PowerUsageSync.Value * 100))
                        .Append('%');

                powerUsageSlider.Visible = block => block.BlockDefinition.SubtypeName == "Caster_FocusLens";
                powerUsageSlider.SupportsMultipleBlocks = true;
                powerUsageSlider.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<IMyThrust>(powerUsageSlider);
            }
            {
                var boostPowerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyThrust>(
                        "FusionSystems.ThrustBoostPowerUsage");
                boostPowerUsageSlider.Title = MyStringId.GetOrCompute("Override Power Usage");
                boostPowerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute("Fusion Power generation this Thruster should use when Override is enabled.");
                boostPowerUsageSlider.SetLimits(0.01f, 4.0f);
                boostPowerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionThrusterLogic>()?.OverridePowerUsageSync.Value ?? 0;
                boostPowerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionThrusterLogic>().OverridePowerUsageSync.Value = value;

                boostPowerUsageSlider.Writer = (block, builder) =>
                    builder.Append(Math.Round(block.GameLogic.GetAs<FusionThrusterLogic>().OverridePowerUsageSync.Value * 100))
                        .Append('%');

                boostPowerUsageSlider.Visible = block => block.BlockDefinition.SubtypeName == "Caster_FocusLens";
                boostPowerUsageSlider.SupportsMultipleBlocks = true;
                boostPowerUsageSlider.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<IMyThrust>(boostPowerUsageSlider);
            }

            MyAPIGateway.TerminalControls.CustomControlGetter += AssignDetailedInfoGetter;

            _haveControlsInited = true;
        }

        private void AssignDetailedInfoGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block.BlockDefinition.SubtypeName != "Caster_FocusLens")
                return;
            block.RefreshCustomInfo();
            block.SetDetailedInfoDirty();
        }

        public void SetPowerBoost(bool value)
        {
            if (OverrideEnabled.Value == value)
                return;

            OverrideEnabled.Value = value;
            UpdateThrust(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
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
                    UpdateThrust(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
            };

            // Trigger power update is only needed when OverrideEnabled is true
            OverridePowerUsageSync.ValueChanged += value =>
            {
                if (OverrideEnabled.Value)
                    UpdateThrust(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
            };

            // Trigger power update if boostEnabled is changed
            OverrideEnabled.ValueChanged += value =>
                UpdateThrust(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            if (!_haveControlsInited)
                CreateControls();

            ((IMyTerminalBlock)Block).AppendingCustomInfo += FusionThrusterLogic_AppendingCustomInfo;

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
                SyncMultipliers.ThrusterOutput(Block, 0);
            }
            else
            {
                SyncMultipliers.ThrusterOutput(Block, BufferThrustOutput);
                PowerConsumption = MaxPowerConsumption * (Block.CurrentThrustPercentage / 100f);
            }
        }

        private void FusionThrusterLogic_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            stringBuilder.Insert(0, InfoText.ToString());
        }

        #endregion
    }
}
