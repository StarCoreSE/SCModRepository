﻿using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Epstein_Fusion_DS.FusionParts.FusionThruster
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, 
        "Caster_FocusLens"
        )]
    public class FusionThrusterLogic : FusionPart<IMyThrust>
    {
        private float _bufferThrustOutput;


        protected override string[] BlockSubtypes => new[]
        {
            "Caster_FocusLens",
        };

        protected override string ReadableName => "Thruster";
        protected override float ProductionPerFusionPower => SFusionSystem.NewtonsPerFusionPower;

        protected override Func<IMyTerminalBlock, float> MinOverrideLimit =>
            b => 1;

        protected override Func<IMyTerminalBlock, float> MaxOverrideLimit =>
            b => DriveSettings[b.BlockDefinition.SubtypeId].MaxOverclock;

        internal static readonly Dictionary<string, DriveSetting> DriveSettings = new Dictionary<string, DriveSetting>
        {
            ["Caster_FocusLens"] = new DriveSetting(1.00f, 1.5f, 144000000),
        };

        public override void UpdatePower(float powerGeneration, int numberThrusters)
        {
            BufferPowerGeneration = powerGeneration;
            bufferBlockCount = numberThrusters;

            var overrideModifier = (OverrideEnabled ? OverridePowerUsageSync : PowerUsageSync).Value;

            var thrustOutput = Block.CurrentThrust;
            var maxThrustOutput = DriveSettings[Block.BlockDefinition.SubtypeId].BaseThrust * overrideModifier;
            // This formula is very dumb but it just about does the trick. Approaches 200% efficiency at zero usage, and 0% at 2x usage.
            var efficiencyMultiplier = DriveSettings[Block.BlockDefinition.SubtypeId].EfficiencyModifier
                                       *
                                       (2 - 1/(DriveSettings[Block.BlockDefinition.SubtypeId].BaseThrust/maxThrustOutput));

            // Power generation consumed (per second)
            var powerConsumption = thrustOutput / ProductionPerFusionPower / efficiencyMultiplier;

            // Power generated (per second)
            //var thrustOutput = efficiencyMultiplier * powerConsumption * newtonsPerFusionPower;
            _bufferThrustOutput = maxThrustOutput;
            MaxPowerConsumption = powerConsumption / 60;

            InfoText.Clear();
            InfoText.AppendLine(
                $"\nOutput: {thrustOutput/1000000:F1}/{maxThrustOutput/1000000:F1} MN");
            InfoText.AppendLine($"Input: {powerConsumption:F1}/{powerGeneration * 60:F1}");
            InfoText.AppendLine($"Efficiency: {efficiencyMultiplier * 100:F1}%");

            // Convert back into power per tick
            if (!IsShutdown)
                SyncMultipliers.ThrusterOutput(Block, _bufferThrustOutput);
        }

        #region Base Methods

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            var storagePct = (MemberSystem?.PowerStored / MemberSystem?.MaxPowerStored) ?? 0;
            if (float.IsNaN(storagePct) || float.IsInfinity(storagePct))
                storagePct = 0;

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

        public struct DriveSetting
        {
            /// <summary>
            /// Efficiency modifier for converting fusing rate into power.
            /// </summary>
            public float EfficiencyModifier;

            /// <summary>
            /// Maximum overclock, in percent.
            /// </summary>
            public float MaxOverclock;

            /// <summary>
            /// Default thrust output.
            /// </summary>
            public float BaseThrust;

            public DriveSetting(float efficiencyModifier, float maxOverclock, float baseThrust)
            {
                EfficiencyModifier = efficiencyModifier;
                MaxOverclock = maxOverclock;
                BaseThrust = baseThrust;
            }
        }
    }
}