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

namespace StarCore.RepairModule
{
    public static class RepairModuleControls
    {
        const string IdPrefix = "RepairModule_";

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
                MyLog.Default.WriteLine($"[RepairModule] {e}");
            }
        }

        static bool IsVisible(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<RepairModule>() != null;
        }

        static void CreateControls()
        {
            #region Priority Dropdown
            var PriorityDropdown = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyCollector>(IdPrefix + "PriorityDropdown");
            PriorityDropdown.Title = MyStringId.GetOrCompute("Repair Prioity");
            PriorityDropdown.Tooltip = MyStringId.GetOrCompute("Select a Subsystem Group to Prioritize");
            PriorityDropdown.SupportsMultipleBlocks = true;
            PriorityDropdown.Visible = IsVisible;
            PriorityDropdown.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<RepairModule>();
                if (logic != null)
                {
                    return logic.SubsystemPriority;
                }
                else
                {
                    return 0;
                }

            };
            PriorityDropdown.Setter = SetPriority_Control;
            PriorityDropdown.ComboBoxContent = (list) =>
            {
                list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Any") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Offense") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Power") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 3, Value = MyStringId.GetOrCompute("Thrust") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 4, Value = MyStringId.GetOrCompute("Steering") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 5, Value = MyStringId.GetOrCompute("Utility") });
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(PriorityDropdown);
            #endregion

            #region Priority Checkbox
            var PriorityCheckbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyCollector>(IdPrefix + "PriorityCheckbox");
            PriorityCheckbox.Title = MyStringId.GetOrCompute("Priority Only");
            PriorityCheckbox.Tooltip = MyStringId.GetOrCompute("Toggle - Only Repair Priority Blocks");
            PriorityCheckbox.SupportsMultipleBlocks = true;
            PriorityCheckbox.Visible = IsVisible;
            PriorityCheckbox.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<RepairModule>();
                if (logic != null)
                {
                    return logic.PriorityOnly;
                }
                else
                {
                    return false;
                }
            };
            PriorityCheckbox.Setter = SetPriorityToggle_Action;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(PriorityCheckbox);
            #endregion

            #region Ignore Armor Checkbox
            var IgnoreArmorCheckbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyCollector>(IdPrefix + "IgnoreArmorCheckbox");
            IgnoreArmorCheckbox.Title = MyStringId.GetOrCompute("Ignore Armor");
            IgnoreArmorCheckbox.Tooltip = MyStringId.GetOrCompute("Toggle - Only Repair Non-Armor Blocks");
            IgnoreArmorCheckbox.SupportsMultipleBlocks = true;
            IgnoreArmorCheckbox.Visible = IsVisible;
            IgnoreArmorCheckbox.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<RepairModule>();
                if (logic != null)
                {
                    return logic.IgnoreArmor;
                }
                else
                {
                    return false;
                }
            };
            IgnoreArmorCheckbox.Setter = SetIgnoreArmorToggle_Action;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(IgnoreArmorCheckbox);
            #endregion          
        }

        static void CreateActions(IMyModContext context)
        {
            #region Priority Dropdown Action
            var PriorityDropdownAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "PriorityDropdownAction");
            PriorityDropdownAction.Name = new StringBuilder("Cycle Priority");
            PriorityDropdownAction.ValidForGroups = true;
            PriorityDropdownAction.Icon = @"Textures\GUI\Icons\Actions\SubsystemTargeting_Cycle.dds";
            PriorityDropdownAction.Action = SetPriority_Action;
            PriorityDropdownAction.Writer = WritePriority_Action;
            PriorityDropdownAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            PriorityDropdownAction.Enabled = IsVisible;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(PriorityDropdownAction);
            #endregion

            #region Priority Checkbox Action
            var PriorityCheckboxAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "PriorityCheckboxAction");
            PriorityCheckboxAction.Name = new StringBuilder("Toggle Priority Only");
            PriorityCheckboxAction.ValidForGroups = true;
            PriorityCheckboxAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            PriorityCheckboxAction.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<RepairModule>();
                if (logic != null)
                {
                    logic.PriorityOnly = !logic.PriorityOnly;
                }
            };
            PriorityCheckboxAction.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<RepairModule>();
                if (logic != null)
                {
                    string boolState = logic.PriorityOnly ? "True" : "False";
                    sb.Append(boolState);
                }
            };
            PriorityCheckboxAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            PriorityCheckboxAction.Enabled = IsVisible;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(PriorityCheckboxAction);
            #endregion

            #region Ignore Armor Checkbox Action
            var IgnoreArmorCheckboxAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(IdPrefix + "IgnoreArmorCheckboxAction");
            IgnoreArmorCheckboxAction.Name = new StringBuilder("Toggle Ignore Armor");
            IgnoreArmorCheckboxAction.ValidForGroups = true;
            IgnoreArmorCheckboxAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            IgnoreArmorCheckboxAction.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<RepairModule>();
                if (logic != null)
                {
                    logic.IgnoreArmor = !logic.IgnoreArmor;
                }
            };
            IgnoreArmorCheckboxAction.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<RepairModule>();
                if (logic != null)
                {
                    string boolState = logic.IgnoreArmor ? "True" : "False";
                    sb.Append(boolState);
                }
            };
            IgnoreArmorCheckboxAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            IgnoreArmorCheckboxAction.Enabled = IsVisible;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(IgnoreArmorCheckboxAction);
            #endregion
        }

        static RepairModule GetLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<RepairModule>();

        static void SetPriority_Control(IMyTerminalBlock block, long key)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                logic.SubsystemPriority = key;
            }
        }

        static void SetPriority_Action(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                if (logic.SubsystemPriority < 5)
                {
                    logic.SubsystemPriority++;
                }
                else
                {
                    logic.SubsystemPriority = 0;
                }
            }          
        }

        static void WritePriority_Action(IMyTerminalBlock block, StringBuilder sb)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                switch (logic.SubsystemPriority)
                {
                    case 0:
                        sb.Append("Any");
                        break;
                    case 1:
                        sb.Append("Offense");
                        break;
                    case 2:
                        sb.Append("Power");
                        break;
                    case 3:
                        sb.Append("Thrust");
                        break;
                    case 4:
                        sb.Append("Steering");
                        break;
                    case 5:
                        sb.Append("Utility");
                        break;
                }
            }           
        }

        static void SetPriorityToggle_Action(IMyTerminalBlock block, bool v)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                logic.PriorityOnly = v;
            }               
        }

        static void SetIgnoreArmorToggle_Action(IMyTerminalBlock block, bool v)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                logic.IgnoreArmor = v;
            }               
        }
    }
}
