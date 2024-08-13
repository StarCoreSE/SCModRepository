using System;
using System.Collections.Generic;
using System.IO;
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

namespace Starcore.FieldGenerator
{
    public static class FieldGeneratorControls
    {
        const string IdPrefix = "FieldGenerator_";

        static bool Done = false;

        public static void DoOnce(IMyModContext context)
        {
            try
            {
                if (Done)
                    return;
                Done = true;


                CreateControls();
                CreateActions(context);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"[FieldGenerator] {e}");
            }
        }

        static bool IsVisible(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<FieldGenerator>() != null;
        }

        static bool CooldownEnabler(IMyTerminalBlock b)
        {
            var logic = GetLogic(b);
            if (logic != null)
            {
                return !logic.SiegeCooldownActive; ;
            }
            return false;
        }

        static void CreateControls()
        {
            #region Siege Mode Toggle
            var SiegeModeToggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCollector>(IdPrefix + "SiegeModeToggle");
            SiegeModeToggle.Title = MyStringId.GetOrCompute("Siege Mode");
            SiegeModeToggle.Tooltip = MyStringId.GetOrCompute("Toggle Siege Mode");
            SiegeModeToggle.OnText = MyStringId.GetOrCompute("On");
            SiegeModeToggle.OffText = MyStringId.GetOrCompute("Off");
            SiegeModeToggle.Visible = IsVisible;
            SiegeModeToggle.Enabled = CooldownEnabler; // Replace with Cooldown Enabler
            SiegeModeToggle.Getter = (b) => b.GameLogic.GetAs<FieldGenerator>().SiegeMode;
            SiegeModeToggle.Setter = (b, v) => b.GameLogic.GetAs<FieldGenerator>().SiegeMode = v;
            SiegeModeToggle.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(SiegeModeToggle);
            #endregion

            #region Field Power Slider
            var FieldPowerSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCollector>(IdPrefix + "FieldPowerSlider");
            FieldPowerSlider.Title = MyStringId.GetOrCompute("Integrity Field Power");
            FieldPowerSlider.Tooltip = MyStringId.GetOrCompute("Set Damage Absorption Percentage");
            FieldPowerSlider.SetLimits
            (
                (b) => GetMinLimit(b),
                (b) => GetMaxLimit(b)
            );
            FieldPowerSlider.Visible = IsVisible;
            FieldPowerSlider.Enabled = (b) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    return !logic.SiegeMode;
                }
                else
                    return true;
            };
            FieldPowerSlider.Writer = (b, w) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    float value = logic.FieldPower;
                    w.Append(Math.Round(value, 1, MidpointRounding.ToEven)).Append('%');
                }
            };
            FieldPowerSlider.Getter = (b) => b.GameLogic.GetAs<FieldGenerator>().FieldPower;
            FieldPowerSlider.Setter = (b, v) => b.GameLogic.GetAs<FieldGenerator>().FieldPower = (int)Math.Round(v, 1);
            FieldPowerSlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(FieldPowerSlider);
            #endregion
        }

        static void CreateActions(IMyModContext context)
        {
            #region Siege Mode Action
            var SiegeToggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "SiegeToggleAction");
            SiegeToggleAction.Name = new StringBuilder("Siege Mode");
            SiegeToggleAction.ValidForGroups = false;
            SiegeToggleAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            SiegeToggleAction.Action = (b) => 
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    logic.SiegeMode = !logic.SiegeMode;
                }
            };
            SiegeToggleAction.Writer = (b, sb) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    string boolState = logic.SiegeMode ? "Active" : "Inactive";
                    sb.Append(boolState);
                }
            };
            SiegeToggleAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            SiegeToggleAction.Enabled = CooldownEnabler;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(SiegeToggleAction);
            #endregion

            #region Increase Power Action
            var IncreasePowerAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "IncreasePowerAction");
            IncreasePowerAction.Name = new StringBuilder("Increase Field Power");
            IncreasePowerAction.ValidForGroups = false;
            IncreasePowerAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            IncreasePowerAction.Action = (b) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    logic.FieldPower += 2.5f;                
                }
            };
            IncreasePowerAction.Writer = (b, sb) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    sb.Append($"{logic.FieldPower}%");
                }
            };
            IncreasePowerAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            IncreasePowerAction.Enabled = (b) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    return !logic.SiegeMode;
                }
                else
                    return true;
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(IncreasePowerAction);
            #endregion

            #region Decrease Power Action
            var DecreasePowerAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "DecreasePowerAction");
            DecreasePowerAction.Name = new StringBuilder("Decrease Field Power");
            DecreasePowerAction.ValidForGroups = false;
            DecreasePowerAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            DecreasePowerAction.Action = (b) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                { 
                    logic.FieldPower -= 2.5f;
                }
            };
            DecreasePowerAction.Writer = (b, sb) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    sb.Append($"{logic.FieldPower}%");
                }
            };
            DecreasePowerAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            DecreasePowerAction.Enabled = (b) =>
            {
                var logic = GetLogic(b);
                if (logic != null)
                {
                    return !logic.SiegeMode;
                }
                else
                    return true;
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(DecreasePowerAction);
            #endregion
        }

        static FieldGenerator GetLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<FieldGenerator>();

        static float GetMinLimit(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                return logic.MinFieldPower;
            }
            return 0;
        }

        static float GetMaxLimit(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                return logic.MaxFieldPower;
            }
            return 0;
        }
    }
}
