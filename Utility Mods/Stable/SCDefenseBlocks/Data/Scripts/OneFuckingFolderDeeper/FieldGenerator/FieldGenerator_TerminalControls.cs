using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Starcore.FieldGenerator {
    public static class FieldGeneratorControls {
        const string IdPrefix = "FieldGenerator_";
        static bool Done = false;
        private static DateTime lastSliderUpdate = DateTime.MinValue;
        private static float lastSliderValue = 0f;

        public static void DoOnce(IMyModContext context) {
            try {
                if (Done)
                    return;
                Done = true;

                CreateControls();
                CreateActions(context);
            }
            catch (Exception e) {
                MyLog.Default.WriteLine($"[FieldGenerator] {e}");
            }
        }

        static bool IsVisible(IMyTerminalBlock b) {
            return b?.GameLogic?.GetAs<FieldGenerator>() != null;
        }

        static bool CooldownEnabler(IMyTerminalBlock b) {
            var logic = GetLogic(b);
            if (logic != null) {
                return !logic.Settings.SiegeCooldownActive;
            }
            return false;
        }

        static void CreateControls() {
            var SiegeModeToggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCollector>(IdPrefix + "SiegeModeToggle");
            SiegeModeToggle.Title = MyStringId.GetOrCompute("Siege Mode");
            SiegeModeToggle.Tooltip = MyStringId.GetOrCompute("Toggle Siege Mode");
            SiegeModeToggle.OnText = MyStringId.GetOrCompute("On");
            SiegeModeToggle.OffText = MyStringId.GetOrCompute("Off");
            SiegeModeToggle.Visible = IsVisible;
            SiegeModeToggle.Enabled = CooldownEnabler;
            SiegeModeToggle.Getter = (b) => b.GameLogic.GetAs<FieldGenerator>()?.Settings.SiegeMode ?? false;
            SiegeModeToggle.Setter = (b, v) => {
                var logic = b.GameLogic.GetAs<FieldGenerator>();
                if (logic != null) {
                    logic.Settings.SiegeMode = v;
                    logic.SaveSettings();
                }
            };
            SiegeModeToggle.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(SiegeModeToggle);

            var FieldPowerSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCollector>(IdPrefix + "FieldPowerSlider");
            FieldPowerSlider.Title = MyStringId.GetOrCompute("Integrity Field Power");
            FieldPowerSlider.Tooltip = MyStringId.GetOrCompute("Set Damage Absorption Percentage");
            FieldPowerSlider.SetLimits((b) => {
                return 0f;
            }, (b) => {
                var logic = GetLogic(b);
                return logic?.Settings.MaxFieldPower ?? 100f;
            });
            FieldPowerSlider.Visible = IsVisible;
            FieldPowerSlider.Enabled = (b) => {
                var logic = GetLogic(b);
                return logic != null && !logic.Settings.SiegeMode;
            };
            FieldPowerSlider.Writer = (b, w) => {
                var logic = GetLogic(b);
                if (logic != null) {
                    w.Append(Math.Round(logic.Settings.FieldPower, 1, MidpointRounding.ToEven))
                      .Append('%')
                      .Append(" / ")
                      .Append(Math.Round(logic.Settings.MaxFieldPower, 1))
                      .Append('%');
                }
            };
            FieldPowerSlider.Getter = (b) => {
                var logic = b.GameLogic.GetAs<FieldGenerator>();
                if (logic != null) {
                    if ((DateTime.Now - lastSliderUpdate).TotalMilliseconds < 100) {
                        return lastSliderValue;
                    }
                    return logic.Settings.FieldPower;
                }
                return 0f;
            };
            FieldPowerSlider.Setter = (b, v) => {
                var logic = b.GameLogic.GetAs<FieldGenerator>();
                if (logic != null) {
                    lastSliderUpdate = DateTime.Now;
                    lastSliderValue = v;

                    float newValue = MathHelper.Clamp(
                        (float)Math.Round(v, 1),
                        0f,
                        logic.Settings.MaxFieldPower
                    );

                    if (Math.Abs(logic.Settings.FieldPower - newValue) > 0.01f) {
                        logic.Settings.FieldPower = newValue;
                        logic.SaveSettings();

                        if (logic.Sink != null) {
                            logic.Sink.Update();
                        }
                    }
                }
            };
            FieldPowerSlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(FieldPowerSlider);
        }

        static void CreateActions(IMyModContext context) {
            var SiegeToggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "SiegeToggleAction");
            SiegeToggleAction.Name = new StringBuilder("Siege Mode");
            SiegeToggleAction.ValidForGroups = false;
            SiegeToggleAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            SiegeToggleAction.Action = (b) => {
                var logic = GetLogic(b);
                if (logic != null) {
                    logic.Settings.SiegeMode = !logic.Settings.SiegeMode;
                    logic.SaveSettings();
                }
            };
            SiegeToggleAction.Writer = (b, sb) => {
                var logic = GetLogic(b);
                if (logic != null) {
                    string boolState = logic.Settings.SiegeMode ? "Active" : "Inactive";
                    sb.Append(boolState);
                }
            };
            SiegeToggleAction.InvalidToolbarTypes = new List<MyToolbarType>() {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            SiegeToggleAction.Enabled = CooldownEnabler;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(SiegeToggleAction);

            var IncreasePowerAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "IncreasePowerAction");
            IncreasePowerAction.Name = new StringBuilder("Increase Field Power");
            IncreasePowerAction.ValidForGroups = false;
            IncreasePowerAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            IncreasePowerAction.Action = (b) => {
                var logic = GetLogic(b);
                if (logic != null) {
                    float newValue = Math.Min(logic.Settings.FieldPower + 2.5f, logic.Settings.MaxFieldPower);
                    if (Math.Abs(logic.Settings.FieldPower - newValue) > 0.01f) {
                        logic.Settings.FieldPower = newValue;
                        logic.SaveSettings();
                    }
                }
            };
            IncreasePowerAction.Writer = (b, sb) => {
                var logic = GetLogic(b);
                if (logic != null)
                    sb.Append($"{logic.Settings.FieldPower:F1}% / {logic.Settings.MaxFieldPower:F1}%");
            };
            IncreasePowerAction.InvalidToolbarTypes = new List<MyToolbarType>() {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            IncreasePowerAction.Enabled = (b) => {
                var logic = GetLogic(b);
                return logic != null && !logic.Settings.SiegeMode;
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(IncreasePowerAction);

            var DecreasePowerAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "DecreasePowerAction");
            DecreasePowerAction.Name = new StringBuilder("Decrease Field Power");
            DecreasePowerAction.ValidForGroups = false;
            DecreasePowerAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            DecreasePowerAction.Action = (b) => {
                var logic = GetLogic(b);
                if (logic != null) {
                    float newValue = Math.Max(0f, logic.Settings.FieldPower - 2.5f);
                    if (Math.Abs(logic.Settings.FieldPower - newValue) > 0.01f) {
                        logic.Settings.FieldPower = newValue;
                        logic.SaveSettings();
                    }
                }
            };
            DecreasePowerAction.Writer = (b, sb) => {
                var logic = GetLogic(b);
                if (logic != null)
                    sb.Append($"{logic.Settings.FieldPower:F1}% / {logic.Settings.MaxFieldPower:F1}%");
            };
            DecreasePowerAction.InvalidToolbarTypes = new List<MyToolbarType>() {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            DecreasePowerAction.Enabled = (b) => {
                var logic = GetLogic(b);
                return logic != null && !logic.Settings.SiegeMode;
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(DecreasePowerAction);
        }

        static FieldGenerator GetLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<FieldGenerator>();
    }
}