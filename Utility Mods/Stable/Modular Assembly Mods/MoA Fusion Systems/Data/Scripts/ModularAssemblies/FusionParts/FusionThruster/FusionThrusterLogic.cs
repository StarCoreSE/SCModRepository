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

        private float BufferThrustGeneration;
        public float MaxPowerConsumption;
        internal S_FusionSystem MemberSystem;
        public float PowerConsumption;

        public MySync<float, SyncDirection.BothWays> PowerUsageSync;
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public void UpdateThrust(float PowerGeneration, float NewtonsPerFusionPower)
        {
            BufferThrustGeneration = PowerGeneration;

            var reactorConsumptionMultiplier = PowerUsageSync.Value; // This is ugly, let's make it better.
            var reactorEfficiencyMultiplier = 1 / (0.5f + reactorConsumptionMultiplier);

            // Power generation consumed (per second)
            var powerConsumption = PowerGeneration * 60 * reactorConsumptionMultiplier;
            // Power generated (per second)
            var thrustOutput = reactorEfficiencyMultiplier * powerConsumption * NewtonsPerFusionPower;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {Math.Round(thrustOutput, 1)}/{Math.Round(PowerGeneration * 60 * NewtonsPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(PowerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(reactorEfficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            SyncMultipliers.ThrusterOutput(Block, thrustOutput);
            MaxPowerConsumption = powerConsumption / 60;
        }

        private void CreateControls()
        {
            {
                var reactorPowerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyReactor>(
                        "FusionSystems.ThrusterPowerUsage");
                reactorPowerUsageSlider.Title = MyStringId.GetOrCompute("Fusion Power Usage");
                reactorPowerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute("Fusion Power generation this thruster should use.");
                reactorPowerUsageSlider.SetLimits(0, 2);
                reactorPowerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionThrusterLogic>()?.PowerUsageSync.Value ?? 0;
                reactorPowerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionThrusterLogic>().PowerUsageSync.Value = value;

                reactorPowerUsageSlider.Writer = (block, builder) =>
                    builder.Append(Math.Round(block.GameLogic.GetAs<FusionThrusterLogic>().PowerUsageSync.Value * 100))
                        .Append('%');

                reactorPowerUsageSlider.Visible = block => block.BlockDefinition.SubtypeName == "Caster_FocusLens";
                reactorPowerUsageSlider.SupportsMultipleBlocks = true;
                reactorPowerUsageSlider.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<IMyThrust>(reactorPowerUsageSlider);
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

        #region Base Methods

        public override void Init(MyObjectBuilder_EntityBase definition)
        {
            base.Init(definition);
            Block = (IMyThrust)Entity;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            PowerUsageSync.ValueChanged += value =>
                UpdateThrust(BufferThrustGeneration, S_FusionSystem.MegawattsPerFusionPower);
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

            PowerConsumption = MaxPowerConsumption * Block.ThrustMultiplier;
        }

        private void FusionThrusterLogic_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            stringBuilder.Insert(0, InfoText.ToString());
        }

        #endregion
    }
}
