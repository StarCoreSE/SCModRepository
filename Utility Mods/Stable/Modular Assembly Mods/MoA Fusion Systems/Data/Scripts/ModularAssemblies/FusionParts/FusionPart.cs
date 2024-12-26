using System;
using System.Collections.Generic;
using System.Text;
using Epstein_Fusion_DS.Communication;
using Epstein_Fusion_DS.HeatParts;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;

namespace Epstein_Fusion_DS.FusionParts
{
    public abstract class FusionPart<T> : MyGameLogicComponent, IMyEventProxy
        where T : IMyCubeBlock
    {
        public static readonly Guid SettingsGuid = new Guid("36a45185-2e80-461c-9f1c-e2140a47a4df");

        /// <summary>
        ///     List of all types that have inited controls.
        /// </summary>
        private static readonly List<string> HaveControlsInited = new List<string>();

        internal readonly StringBuilder InfoText = new StringBuilder("Output: 0/0\nInput: 0/0\nEfficiency: N/A");
        internal T Block;

        internal float BufferPowerGeneration;

        internal long LastShutdown = 0;
        public float MaxPowerConsumption;

        internal SFusionSystem MemberSystem;
        public MySync<bool, SyncDirection.BothWays> OverrideEnabled;
        public MySync<float, SyncDirection.BothWays> OverridePowerUsageSync;

        public bool IsShutdown = false;
        public float PowerConsumption;

        public MySync<float, SyncDirection.BothWays> PowerUsageSync;
        internal FusionPartSettings Settings;
        internal static ModularDefinitionApi ModularApi => Epstein_Fusion_DS.ModularDefinition.ModularApi;

        /// <summary>
        ///     Block subtypes allowed.
        /// </summary>
        internal abstract string[] BlockSubtypes { get; }

        /// <summary>
        ///     Human-readable name for this part type.
        /// </summary>
        internal abstract string ReadableName { get; }

        internal virtual Func<IMyTerminalBlock, float> MinOverrideLimit { get; } = b => 0.01f;
        internal virtual Func<IMyTerminalBlock, float> MaxOverrideLimit { get; } = b => 4;

        #region Controls

        private void CreateControls()
        {
            /* TERMINAL */
            {
                var boostPowerToggle =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(
                        $"FusionSystems.{ReadableName}BoostPowerToggle");
                boostPowerToggle.Title = MyStringId.GetOrCompute("Override Fusion Power");
                boostPowerToggle.Tooltip =
                    MyStringId.GetOrCompute("Toggles Power Override - a temporary override on Fusion Power draw.");
                boostPowerToggle.Getter = block =>
                    block.GameLogic.GetAs<FusionPart<T>>()?.OverrideEnabled.Value ?? false;
                boostPowerToggle.Setter = (block, value) =>
                {
                    var logic = block.GameLogic.GetAs<FusionPart<T>>();
                    // Only allow value to be set if 1 second of power is stored
                    if (!value || logic.MemberSystem?.PowerStored > -logic.MemberSystem?.PowerGeneration * 60)
                        logic.OverrideEnabled.Value = value;
                };

                boostPowerToggle.OnText = MyStringId.GetOrCompute("On");
                boostPowerToggle.OffText = MyStringId.GetOrCompute("Off");

                boostPowerToggle.Visible = block => BlockSubtypes.Contains(block.BlockDefinition.SubtypeName);
                boostPowerToggle.SupportsMultipleBlocks = true;
                boostPowerToggle.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<T>(boostPowerToggle);
            }
            {
                var powerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(
                        $"FusionSystems.{ReadableName}PowerUsage");
                powerUsageSlider.Title = MyStringId.GetOrCompute("Fusion Power Usage");
                powerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute($"Fusion Power generation this {ReadableName} should use.");
                powerUsageSlider.SetLimits(0.005f, 0.995f);
                powerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionPart<T>>()?.PowerUsageSync.Value ?? 0;
                powerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionPart<T>>().PowerUsageSync.Value = value;

                powerUsageSlider.Writer = (block, builder) =>
                    builder?.Append(Math.Round(block.GameLogic.GetAs<FusionPart<T>>()?.PowerUsageSync.Value * 100 ?? 0))
                        .Append('%');

                powerUsageSlider.Visible = block => BlockSubtypes.Contains(block.BlockDefinition.SubtypeName);
                powerUsageSlider.SupportsMultipleBlocks = true;
                powerUsageSlider.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<T>(powerUsageSlider);
            }
            {
                var boostPowerUsageSlider =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(
                        $"FusionSystems.{ReadableName}BoostPowerUsage");
                boostPowerUsageSlider.Title = MyStringId.GetOrCompute("Override Power Usage");
                boostPowerUsageSlider.Tooltip =
                    MyStringId.GetOrCompute(
                        $"Fusion Power generation this {ReadableName} should use when Override is enabled.");
                boostPowerUsageSlider.SetLimits(MinOverrideLimit, MaxOverrideLimit);
                boostPowerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionPart<T>>()?.OverridePowerUsageSync.Value ?? 0;
                boostPowerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionPart<T>>().OverridePowerUsageSync.Value = value;

                boostPowerUsageSlider.Writer = (block, builder) =>
                    builder?.Append(
                            Math.Round(block.GameLogic.GetAs<FusionPart<T>>()?.OverridePowerUsageSync.Value * 100 ?? 0))
                        .Append('%');

                boostPowerUsageSlider.Visible = block => BlockSubtypes.Contains(block.BlockDefinition.SubtypeName);
                boostPowerUsageSlider.SupportsMultipleBlocks = true;
                boostPowerUsageSlider.Enabled = block => true;

                MyAPIGateway.TerminalControls.AddControl<T>(boostPowerUsageSlider);
            }

            /* ACTIONS */
            {
                var boostPowerAction =
                    MyAPIGateway.TerminalControls.CreateAction<T>($"FusionSystems.{ReadableName}BoostPowerAction");
                boostPowerAction.Name = new StringBuilder("Override Fusion Power");
                boostPowerAction.Action = block =>
                {
                    var logic = block.GameLogic.GetAs<FusionPart<T>>();
                    // Only allow value to be set if 1 second of power is stored
                    if (logic.OverrideEnabled.Value ||
                        logic.MemberSystem?.PowerStored > -logic.MemberSystem?.PowerGeneration * 60)
                        logic.OverrideEnabled.Value = !logic.OverrideEnabled.Value;
                };
                boostPowerAction.Writer = (b, sb) =>
                {
                    var logic = b?.GameLogic?.GetAs<FusionPart<T>>();
                    if (logic != null) sb.Append(logic.OverrideEnabled.Value ? "OVR   On" : "OVR  Off");
                };
                boostPowerAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                boostPowerAction.Enabled = block => BlockSubtypes.Contains(block.BlockDefinition.SubtypeName);
                MyAPIGateway.TerminalControls.AddAction<T>(boostPowerAction);
            }

            MyAPIGateway.TerminalControls.CustomControlGetter += AssignDetailedInfoGetter;

            HaveControlsInited.Add(ReadableName);
        }

        private void AssignDetailedInfoGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (!BlockSubtypes.Contains(block.BlockDefinition.SubtypeName))
                return;
            block.RefreshCustomInfo();
            block.SetDetailedInfoDirty();
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            stringBuilder.Insert(0, InfoText.ToString());
        }

        public abstract void UpdatePower(float powerGeneration, float outputPerFusionPower, int numberParts);

        #endregion

        #region Base Methods

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            Block = (T)Entity;

            if (Block.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            LoadSettings();
            PowerUsageSync.Value = Settings.PowerUsage;
            PowerUsageSync.ValueChanged += value =>
                Settings.PowerUsage = value.Value;

            OverridePowerUsageSync.Value = Settings.OverridePowerUsage;
            OverridePowerUsageSync.ValueChanged += value =>
                Settings.OverridePowerUsage = value.Value;
            SaveSettings();

            if (!HaveControlsInited.Contains(ReadableName))
                CreateControls();

            ((IMyTerminalBlock)Block).AppendingCustomInfo += AppendingCustomInfo;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            try
            {
                if (!MyAPIGateway.Session.IsServer)
                    return;

                float heatLevel = HeatManager.I.GetGridHeatLevel(Block.CubeGrid);
                if (heatLevel > 0.8f)
                {
                    // 10h^8
                    float damagePerTick = 100 * heatLevel * heatLevel * heatLevel * heatLevel * heatLevel * heatLevel * heatLevel * heatLevel;
                    Block.SlimBlock.DoDamage(damagePerTick, MyDamageType.Temperature, true);
                }
            }
            catch (Exception ex)
            {
                ModularApi.Log(ex.ToString());
            }
        }

        #endregion

        #region Settings

        internal void SaveSettings()
        {
            if (Block == null || Settings == null)
                return; // called too soon or after it was already closed, ignore

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException(
                    $"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; Test log 2");

            if (Block.Storage == null)
                Block.Storage = new MyModStorageComponent();

            Block.Storage.SetValue(SettingsGuid,
                Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
        }

        internal virtual void LoadDefaultSettings()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Settings.PowerUsage = 1.0f;
            Settings.OverridePowerUsage = 1.5f;

            PowerUsageSync.Value = Settings.PowerUsage;
            OverridePowerUsageSync.Value = Settings.OverridePowerUsage;
        }

        internal virtual bool LoadSettings()
        {
            if (Settings == null)
                Settings = new FusionPartSettings();

            if (Block.Storage == null)
            {
                LoadDefaultSettings();
                return false;
            }

            string rawData;
            if (!Block.Storage.TryGetValue(SettingsGuid, out rawData))
            {
                LoadDefaultSettings();
                return false;
            }

            try
            {
                var loadedSettings =
                    MyAPIGateway.Utilities.SerializeFromBinary<FusionPartSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.PowerUsage = loadedSettings.PowerUsage;
                    Settings.OverridePowerUsage = loadedSettings.OverridePowerUsage;

                    PowerUsageSync.Value = loadedSettings.PowerUsage;
                    OverridePowerUsageSync.Value = loadedSettings.OverridePowerUsage;

                    return true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Exception in loading FusionPart settings: " + e);
                MyAPIGateway.Utilities.ShowMessage("Fusion Systems", "Exception in loading FusionPart settings: " + e);
            }

            return false;
        }

        public override bool IsSerialized()
        {
            if (Block.CubeGrid?.Physics == null)
                return base.IsSerialized();

            try
            {
                SaveSettings();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Exception in loading FusionPart settings: " + e);
                MyAPIGateway.Utilities.ShowMessage("Fusion Systems", "Exception in loading FusionPart settings: " + e);
            }

            return base.IsSerialized();
        }

        #endregion
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    internal class FusionPartSettings
    {
        [ProtoMember(2)] public float OverridePowerUsage;

        [ProtoMember(1)] public float PowerUsage;
        // Don't need to save Override because it would be instantly reset.
    }
}
