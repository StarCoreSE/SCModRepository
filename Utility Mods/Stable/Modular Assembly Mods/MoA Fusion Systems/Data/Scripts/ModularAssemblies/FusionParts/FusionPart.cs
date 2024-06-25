using System;
using System.Collections.Generic;
using System.Text;
using FusionSystems.Communication;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;

namespace FusionSystems.FusionParts
{
    public abstract class FusionPart<T> : MyGameLogicComponent, IMyEventProxy
        where T : IMyCubeBlock
    {
        public static readonly Guid SettingsGUID = new Guid("36a45185-2e80-461c-9f1c-e2140a47a4df");

        /// <summary>
        ///     List of all types that have inited controls.
        /// </summary>
        private static readonly List<string> _haveControlsInited = new List<string>();

        internal readonly StringBuilder InfoText = new StringBuilder("Output: 0/0\nInput: 0/0\nEfficiency: N/A");
        internal T Block;

        internal float BufferPowerGeneration;

        internal long LastShutdown = 0;
        public float MaxPowerConsumption;

        internal S_FusionSystem MemberSystem;
        public MySync<bool, SyncDirection.BothWays> OverrideEnabled;
        public MySync<float, SyncDirection.BothWays> OverridePowerUsageSync;

        public float PowerConsumption;

        public MySync<float, SyncDirection.BothWays> PowerUsageSync;
        internal FusionPartSettings Settings = new FusionPartSettings();
        internal static ModularDefinitionApi ModularApi => ModularDefinition.ModularApi;

        /// <summary>
        ///     Block subtypes allowed.
        /// </summary>
        internal abstract string BlockSubtype { get; }

        /// <summary>
        ///     Human-readable name for this part type.
        /// </summary>
        internal abstract string ReadableName { get; }

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
                    // Only allow value to be set if 2 seconds of power is stored
                    if (!value || logic.MemberSystem?.PowerStored > MemberSystem?.PowerConsumption * 60)
                        logic.OverrideEnabled.Value = value;
                };

                boostPowerToggle.OnText = MyStringId.GetOrCompute("On");
                boostPowerToggle.OffText = MyStringId.GetOrCompute("Off");

                boostPowerToggle.Visible = block => block.BlockDefinition.SubtypeName == BlockSubtype;
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
                powerUsageSlider.SetLimits(0.01f, 0.99f);
                powerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionPart<T>>()?.PowerUsageSync.Value ?? 0;
                powerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionPart<T>>().PowerUsageSync.Value = value;

                powerUsageSlider.Writer = (block, builder) =>
                    builder?.Append(Math.Round(block.GameLogic.GetAs<FusionPart<T>>()?.PowerUsageSync.Value * 100 ?? 0))
                        .Append('%');

                powerUsageSlider.Visible = block => block.BlockDefinition.SubtypeName == BlockSubtype;
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
                boostPowerUsageSlider.SetLimits(0.01f, 4.0f);
                boostPowerUsageSlider.Getter = block =>
                    block.GameLogic.GetAs<FusionPart<T>>()?.OverridePowerUsageSync.Value ?? 0;
                boostPowerUsageSlider.Setter = (block, value) =>
                    block.GameLogic.GetAs<FusionPart<T>>().OverridePowerUsageSync.Value = value;

                boostPowerUsageSlider.Writer = (block, builder) =>
                    builder?.Append(
                            Math.Round(block.GameLogic.GetAs<FusionPart<T>>()?.OverridePowerUsageSync.Value * 100 ?? 0))
                        .Append('%');

                boostPowerUsageSlider.Visible = block => block.BlockDefinition.SubtypeName == BlockSubtype;
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
                    // Only allow value to be set if 2 seconds of power is stored
                    if (logic.OverrideEnabled.Value ||
                        logic.MemberSystem?.PowerStored > MemberSystem?.PowerConsumption * 60)
                        logic.OverrideEnabled.Value = !logic.OverrideEnabled.Value;
                };
                boostPowerAction.Writer = (b, sb) =>
                {
                    var logic = b?.GameLogic?.GetAs<FusionPart<T>>();
                    if (logic != null) sb.Append(logic.OverrideEnabled.Value ? "OVR   On" : "OVR  Off");
                };
                boostPowerAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                boostPowerAction.Enabled = block => block.BlockDefinition.SubtypeName == BlockSubtype;
                MyAPIGateway.TerminalControls.AddAction<T>(boostPowerAction);
            }

            MyAPIGateway.TerminalControls.CustomControlGetter += AssignDetailedInfoGetter;

            _haveControlsInited.Add(ReadableName);
        }

        private void AssignDetailedInfoGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block?.BlockDefinition.SubtypeName != BlockSubtype)
                return;
            block.RefreshCustomInfo();
            block.SetDetailedInfoDirty();
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            stringBuilder.Insert(0, InfoText.ToString());
        }

        public abstract void UpdatePower(float PowerGeneration, float OutputPerFusionPower);

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
            Settings.PowerUsage = PowerUsageSync.Value;
            PowerUsageSync.ValueChanged += value =>
                Settings.PowerUsage = value.Value;

            Settings.OverridePowerUsage = OverridePowerUsageSync.Value;
            OverridePowerUsageSync.ValueChanged += value =>
                Settings.OverridePowerUsage = value.Value;
            SaveSettings();

            if (!_haveControlsInited.Contains(ReadableName))
                CreateControls();

            ((IMyTerminalBlock)Block).AppendingCustomInfo += AppendingCustomInfo;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        #endregion

        #region Settings

        internal void SaveSettings()
        {
            if (Block == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; Test log 1");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException(
                    $"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; Test log 2");

            if (Block.Storage == null)
                Block.Storage = new MyModStorageComponent();

            Block.Storage.SetValue(SettingsGUID,
                Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
        }

        internal virtual void LoadDefaultSettings()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Settings.PowerUsage = 0.5f;
            Settings.OverridePowerUsage = 1.5f;

            PowerUsageSync.Value = Settings.PowerUsage;
            OverridePowerUsageSync.Value = Settings.OverridePowerUsage;
        }

        internal virtual bool LoadSettings()
        {
            if (Block.Storage == null)
            {
                LoadDefaultSettings();
                return false;
            }

            string rawData;
            if (!Block.Storage.TryGetValue(SettingsGUID, out rawData))
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