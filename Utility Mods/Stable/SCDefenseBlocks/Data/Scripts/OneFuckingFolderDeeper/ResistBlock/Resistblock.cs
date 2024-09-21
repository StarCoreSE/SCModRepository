using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using System;
using VRage.Utils;
using VRageMath;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Game;
using VRage.Game;
using Sandbox.Game.EntityComponents;
using System.Text;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;

namespace Starcore.ResistBlock
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "FieldGen_Core")]
    public class ResistBlock : MyGameLogicComponent
    {
        private IMyCollector block;
        private ResistSettings Settings;
        public MySync<bool, SyncDirection.BothWays> IsActiveSync;
        public MySync<float, SyncDirection.BothWays> ResistanceStrengthSync;

        private IMyCubeGrid coreBlockGrid;

        private static bool m_controlsCreated = false;

        public readonly Guid ResistSettingsGUID = new Guid("160803f9-9800-4515-9619-e5385d5208fb");

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCollector)Entity;

            if (block == null || block.CubeGrid == null)
            {
                MyLog.Default.WriteLineAndConsole("Error: Block or CubeGrid is null during Init.");
                return;
            }

            coreBlockGrid = block.CubeGrid;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            CreateTerminalControls();

            if (block == null || block.CubeGrid?.Physics == null)
                return;

            Settings = new ResistSettings(this);

            IsActiveSync.ValueChanged += IsActive_ValueChanged;
            ResistanceStrengthSync.ValueChanged += ResistanceStrength_ValueChanged;

            LoadSettings();
            SaveSettings();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private void ResistanceStrength_ValueChanged(MySync<float, SyncDirection.BothWays> obj)
        {
            // Handle resistance strength change if needed
        }

        private void IsActive_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            if (IsActiveSync.Value)
                StartResistance();
            else
                StopResistance();
        }

        private void StartResistance()
        {
            // Apply grid-wide resistance based on the current slider value (capped at 50%)
            float resistanceStrength = Math.Min(Settings.ResistanceStrength, 0.5f);
            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(block.CubeGrid.Name, 1f - resistanceStrength); // Adjusting resistance
        }

        private void StopResistance()
        {
            // Reset the grid's damage modifier to normal when resistance is off
            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(block.CubeGrid.Name, 1.0f); // Reset to normal damage
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (Settings.IsActive)
            {
                ApplyResistance();
            }
        }

        private void ApplyResistance()
        {
            float resistanceStrength = Math.Min(Settings.ResistanceStrength, 0.5f); // Cap at 50% resistance
            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(block.CubeGrid.Name, 1f - resistanceStrength);
        }

        private static void CreateTerminalControls()
        {
            if (m_controlsCreated)
                return;

            m_controlsCreated = true;

            // Create Activate Switch
            var activateSwitch = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCollector>("ResistBlock_Terminal_Activate");
            activateSwitch.Title = MyStringId.GetOrCompute("Activate Resistance");
            activateSwitch.OnText = MyStringId.GetOrCompute("On");
            activateSwitch.OffText = MyStringId.GetOrCompute("Off");
            activateSwitch.Visible = CustomVisibleCondition;

            activateSwitch.Getter = (b) => (b.GameLogic.GetAs<ResistBlock>()?.Settings.IsActive) ?? false;
            activateSwitch.Setter = (b, v) =>
            {
                var resistBlock = b.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    resistBlock.Settings.IsActive = v;
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(activateSwitch);

            // Create Resistance Strength Slider (0 - 50% instead of 0 - 100%)
            var strengthSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCollector>("ResistBlock_Terminal_StrengthSlider");
            strengthSlider.Title = MyStringId.GetOrCompute("Resistance Strength");
            strengthSlider.Tooltip = MyStringId.GetOrCompute("Adjust the resistance strength.");
            strengthSlider.SetLimits(0, 0.5f); // Set limits from 0 to 50%
            strengthSlider.Visible = CustomVisibleCondition;

            strengthSlider.Writer = (b, sb) =>
            {
                sb.Append(Math.Round(b.GameLogic.GetAs<ResistBlock>()?.Settings.ResistanceStrength ?? 0, 2).ToString("F2"));
            };
            strengthSlider.Getter = (b) => (b.GameLogic.GetAs<ResistBlock>()?.Settings.ResistanceStrength) ?? 0;
            strengthSlider.Setter = (b, v) =>
            {
                var resistBlock = b.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    resistBlock.Settings.ResistanceStrength = v;
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(strengthSlider);

            // Adding an Action for Cockpits/Control Stations
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("ResistBlock_Action_Activate");
            action.Name = new StringBuilder("Toggle Resistance");
            action.Writer = (block, stringBuilder) =>
            {
                var resistBlock = block.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    stringBuilder.Append("Resistance: ").Append(resistBlock.Settings.IsActive ? "On" : "Off");
                }
            };
            action.Action = (block) =>
            {
                var resistBlock = block.GameLogic.GetAs<ResistBlock>();
                if (resistBlock != null)
                {
                    resistBlock.Settings.IsActive = !resistBlock.Settings.IsActive;
                }
            };
            action.ValidForGroups = true;
            action.Enabled = CustomVisibleCondition;

            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(action);
        }

        private static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<ResistBlock>() != null;
        }

        internal bool LoadSettings()
        {
            string rawData;
            if (block == null || block.Storage == null || !block.Storage.TryGetValue(ResistSettingsGUID, out rawData))
                return false;

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<ResistSettings>(Convert.FromBase64String(rawData));
                if (loadedSettings != null)
                {
                    Settings.ResistanceStrength = loadedSettings.ResistanceStrength;
                    Settings.IsActive = loadedSettings.IsActive;
                    return true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Failed to load resist settings: {e}");
            }

            return false;
        }

        internal void SaveSettings()
        {
            try
            {
                if (block == null || Settings == null)
                    return;

                if (block.Storage == null)
                    block.Storage = new MyModStorageComponent();

                string rawData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings));
                block.Storage[ResistSettingsGUID] = rawData;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Failed to save resist settings: {e}");
            }
        }

        public override bool IsSerialized()
        {
            try
            {
                if (block == null || Settings == null)
                {
                    MyLog.Default.WriteLineAndConsole("Error in IsSerialized: block or Settings is null.");
                    return false;
                }

                SaveSettings();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Exception in IsSerialized: {e}");
            }

            return base.IsSerialized();
        }

        public override void Close()
        {
            if (IsActiveSync != null)
            {
                IsActiveSync.ValueChanged -= IsActive_ValueChanged;
            }

            base.Close();
        }
    }
}
