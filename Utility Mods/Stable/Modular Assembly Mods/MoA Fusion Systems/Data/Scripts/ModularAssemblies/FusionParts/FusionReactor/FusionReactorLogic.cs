using Modular_Definitions.Data.Scripts.ModularAssemblies;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.ModularAssemblies;
using Scripts.ModularAssemblies.Communication;
using Scripts.ModularAssemblies.FusionParts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;

namespace Data.Scripts.ModularAssemblies.FusionParts.FusionReactor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "Caster_Reactor")]
    public class FusionReactorLogic : MyGameLogicComponent, IMyEventProxy
    {
        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;
        static bool HaveControlsInited = false;

        public IMyReactor Block;
        internal S_FusionSystem MemberSystem;

        public MySync<float, SyncDirection.BothWays> PowerUsageSync;
        public float PowerConsumption = 0;
        public float MaxPowerConsumption = 0;

        float BufferPowerGeneration = 0;

        StringBuilder InfoText = new StringBuilder("Output: 0/0\nInput: 0/0\nEfficiency: N/A");

        #region Base Methods

        public override void Init(MyObjectBuilder_EntityBase definition)
        {
            base.Init(definition);
            Block = (IMyReactor) Entity;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            PowerUsageSync.ValueChanged += (value) => UpdatePower(BufferPowerGeneration, S_FusionSystem.MegawattsPerFusionPower);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            if (!HaveControlsInited)
                CreateControls();

            ((IMyTerminalBlock)Block).AppendingCustomInfo += FusionReactorLogic_AppendingCustomInfo;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            PowerConsumption = MaxPowerConsumption * Block.CurrentOutputRatio;
        }

        private void FusionReactorLogic_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            stringBuilder.Insert(0, InfoText.ToString());
        }

        #endregion

        public void UpdatePower(float PowerGeneration, float MegawattsPerFusionPower)
        {
            BufferPowerGeneration = PowerGeneration;

            float reactorConsumptionMultiplier = PowerUsageSync.Value; // This is ugly, let's make it better.
            float reactorEfficiencyMultiplier = 1 / (0.5f + reactorConsumptionMultiplier);

            // Power generation consumed (per second)
            float powerConsumption = PowerGeneration * 60 * reactorConsumptionMultiplier;
            // Power generated (per second)
            float reactorOutput = reactorEfficiencyMultiplier * powerConsumption * MegawattsPerFusionPower;

            InfoText.Clear();
            InfoText.AppendLine($"\nOutput: {Math.Round(reactorOutput, 1)}/{Math.Round(PowerGeneration * 60 * MegawattsPerFusionPower, 1)}");
            InfoText.AppendLine($"Input: {Math.Round(powerConsumption, 1)}/{Math.Round(PowerGeneration * 60, 1)}");
            InfoText.AppendLine($"Efficiency: {Math.Round(reactorEfficiencyMultiplier * 100)}%");

            // Convert back into power per tick
            SyncMultipliers.ReactorOutput(Block, reactorOutput);
            MaxPowerConsumption = powerConsumption / 60;
        }

        void CreateControls()
        {
            {
                var reactorPowerUsageSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyReactor>("FusionSystems.ReactorPowerUsage");
                reactorPowerUsageSlider.Title = MyStringId.GetOrCompute("Fusion Power Usage");
                reactorPowerUsageSlider.Tooltip = MyStringId.GetOrCompute("Fusion Power generation this reactor should use.");
                reactorPowerUsageSlider.SetLimits(0, 2);
                reactorPowerUsageSlider.Getter = (block) => block.GameLogic.GetAs<FusionReactorLogic>()?.PowerUsageSync.Value ?? 0;
                reactorPowerUsageSlider.Setter = (block, value) => block.GameLogic.GetAs<FusionReactorLogic>().PowerUsageSync.Value = value;

                reactorPowerUsageSlider.Writer = (block, builder) => builder.Append(Math.Round(block.GameLogic.GetAs<FusionReactorLogic>().PowerUsageSync.Value * 100)).Append('%');

                reactorPowerUsageSlider.Visible = (block) => block.BlockDefinition.SubtypeName == "Caster_Reactor";
                reactorPowerUsageSlider.SupportsMultipleBlocks = true;
                reactorPowerUsageSlider.Enabled = (block) => true;

                MyAPIGateway.TerminalControls.AddControl<IMyReactor>(reactorPowerUsageSlider);
            }

            MyAPIGateway.TerminalControls.CustomControlGetter += AssignDetailedInfoGetter;

            HaveControlsInited = true;
        }

        private void AssignDetailedInfoGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block.BlockDefinition.SubtypeName != "Caster_Reactor")
                return;
            block.RefreshCustomInfo();
            block.SetDetailedInfoDirty();
        }
    }
}
