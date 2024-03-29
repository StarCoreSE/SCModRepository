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
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public void UpdateThrust(float PowerGeneration, float NewtonsPerFusionPower)
        {
            BufferPowerGeneration = PowerGeneration;

            var consumptionMultiplier = PowerUsageSync.Value; // This is ugly, let's make it better.
            var efficiencyMultiplier = 1 / (0.5f + consumptionMultiplier);

            // Power generation consumed (per second)
            var powerConsumption = PowerGeneration * 60 * consumptionMultiplier;
            // Power generated (per second)
            var thrustOutput = efficiencyMultiplier * powerConsumption * NewtonsPerFusionPower;
            BufferThrustOutput = thrustOutput;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {Math.Round(thrustOutput, 1)}/{Math.Round(PowerGeneration * 60 * NewtonsPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(PowerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(efficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            SyncMultipliers.ThrusterOutput(Block, BufferThrustOutput);
            MaxPowerConsumption = powerConsumption / 60;
        }

        private void CreateControls()
        {
            {
                var powerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyThrust>(
                        "FusionSystems.ThrusterPowerUsage");
                powerUsageSlider.Title = MyStringId.GetOrCompute("Fusion Power Usage");
                powerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute("Fusion Power generation this thruster should use.");
                powerUsageSlider.SetLimits(0, 2);
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
                UpdateThrust(BufferPowerGeneration, S_FusionSystem.NewtonsPerFusionPower);
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

            if (MemberSystem?.PowerStored <= PowerConsumption || !Block.IsWorking)
            {
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
