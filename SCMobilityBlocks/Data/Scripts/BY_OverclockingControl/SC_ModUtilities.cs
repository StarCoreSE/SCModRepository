using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace BuYanMod.Utils
{
    public static partial class Utils
    {
        public static class LoadSaveDatas
        {
            public static string Load(IMyTerminalBlock Me, Guid GUID)
            {
                if (Me.Storage == null || !Me.Storage.ContainsKey(GUID))
                {
                    return "";
                }
                return Me.Storage[GUID];
            }

            public static void Save(IMyTerminalBlock ThisGrid, string context, Guid GUID)
            {
                if (ThisGrid.Storage == null)
                    ThisGrid.Storage = new MyModStorageComponent();
                if (!ThisGrid.Storage.ContainsKey(GUID))
                {
                    ThisGrid.Storage.Add(GUID, context);
                }
                else
                {
                    ThisGrid.Storage[GUID] = context;
                }
            }

            public static bool TryAddComponent(IMyTerminalBlock block, Guid GUID)
            {
                if (NullCheck.IsNull(block))
                    return false;
                if (block.Storage == null)
                    block.Storage = new MyModStorageComponent();
                if (block.Storage == null)
                    return false;
                if (!block.Storage.ContainsKey(GUID))
                    block.Storage.Add(GUID, "");
                return block.Storage.ContainsKey(GUID);
            }

            public static string Init(IMyCubeGrid ThisGrid, Guid GUID, string context_default = "")
            {
                if (NullCheck.IsNull(ThisGrid))
                    return "";
                if (ThisGrid.Storage == null)
                    ThisGrid.Storage = new MyModStorageComponent();
                if (!ThisGrid.Storage.ContainsKey(GUID))
                {
                    ThisGrid.Storage.Add(GUID, context_default);
                }
                return ThisGrid.Storage[GUID];
            }

            public static string Init(IMyTerminalBlock blcok, Guid GUID, string context_default = "")
            {
                if (NullCheck.IsNull(blcok))
                    return "";
                if (blcok.Storage == null)
                    blcok.Storage = new MyModStorageComponent();
                if (!blcok.Storage.ContainsKey(GUID))
                {
                    blcok.Storage.Add(GUID, context_default);
                }
                return blcok.Storage[GUID];
            }

            public static string Load(IMyCubeGrid ThisGrid, Guid GUID)
            {
                if (NullCheck.IsNull(ThisGrid))
                    return "";
                if (ThisGrid.Storage == null || !ThisGrid.Storage.ContainsKey(GUID))
                {
                    return "";
                }
                return ThisGrid.Storage[GUID];
            }

            public static void Save(IMyCubeGrid ThisGrid, string context, Guid GUID)
            {
                if (NullCheck.IsNull(ThisGrid))
                    return;
                if (ThisGrid.Storage == null)
                    ThisGrid.Storage = new MyModStorageComponent();
                if (!ThisGrid.Storage.ContainsKey(GUID))
                {
                    ThisGrid.Storage.Add(GUID, context);
                }
                else
                {
                    ThisGrid.Storage[GUID] = context;
                }
            }

            public static string ByteToString(byte[] array)
            {
                if (array == null || array.Length < 1)
                    return "";
                string array_str = "";
                for (int i = 0; i < array.Length; i++)
                    array_str += array[i].ToString("X2");
                return array_str;
            }

            public static byte[] StringToByte(string array_str)
            {
                if (array_str == null || array_str.Length < 2 || array_str.Length % 2 != 0)
                    return null;
                var length = array_str.Length / 2;
                byte[] array = new byte[length];
                for (int i = 0; i < length; i++)
                    array[i] = Convert.ToByte(array_str.Substring(i * 2, 2), 16);
                return array;
            }
        }

        public static class NullCheck
        {
            public static bool IsNull(Vector3? Value) => Value == null || Value.Value == Vector3.Zero;

            public static bool IsNull(Vector3D? Value) => Value == null || Value.Value == Vector3D.Zero;

            public static bool IsNull<T>(T Ent) where T : IMyEntity => Ent == null || Ent.Closed || Ent.MarkedForClose;

            public static bool IsNullBlock<T>(T Block, IMyTerminalBlock RefBlock) where T : Sandbox.ModAPI.Ingame.IMyTerminalBlock =>
                Block == null || Block.Closed || RefBlock == null || RefBlock.Closed || RefBlock.MarkedForClose || (!RefBlock.IsSameConstructAs(Block));

            public static bool IsNullPowerProducer(IMyPowerProducer Block, IMyTerminalBlock RefBlock) =>
                Block == null || Block.Closed || Block.MarkedForClose || RefBlock == null || RefBlock.Closed || RefBlock.MarkedForClose || (!RefBlock.IsSameConstructAs(Block));

            public static bool IsNullCollection<T>(ICollection<T> Value, bool NoCheckEmpty = false)
            {
                if (Value == null)
                    return true;
                if (NoCheckEmpty)
                    return false;
                if (Value.Count < 1)
                    return true;
                return false;
            }

            public static bool IsNullCollection<T>(IEnumerable<T> Value, bool NoCheckEmpty = false)
            {
                if (Value == null)
                    return true;
                if (NoCheckEmpty)
                    return false;
                if ((Value?.ToList()?.Count ?? 0) < 1)
                    return true;
                return false;
            }
        }

        public static class TerminalRevise
        {
            public enum SliderStyle { Lin, Log, DLog }

            public static void CreateSeparator<TBlock>(string IDPrefix, string controlID, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible)
            {
                IMyTerminalControlSeparator control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, TBlock>($"{IDPrefix}{controlID}");
                control.Visible = Visible;
                control.Enabled = Enabled;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }

            public static void CreateLabel<TBlock>(string IDPrefix, string controlID, string label, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible)
            {
                IMyTerminalControlLabel control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, TBlock>($"{IDPrefix}{controlID}");
                control.Label = MyStringId.GetOrCompute(label);
                control.Visible = Visible;
                control.Enabled = Enabled;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }

            public static void CreateButton<TBlock>(string IDPrefix, string actionIDPrefix, string controlID, string Title, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Action<IMyTerminalBlock> Action, bool haveAction = true)
            {
                IMyTerminalControlButton control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, TBlock>($"{IDPrefix}{controlID}");
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.Title = MyStringId.GetOrCompute(Title);
                control.Action = Action;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);

                if (haveAction)
                {
                    IMyTerminalAction action_trigger = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{actionIDPrefix}{controlID}.Trigger");
                    action_trigger.Name = new StringBuilder(Title);
                    action_trigger.Enabled = Enabled;
                    action_trigger.Writer = (b, t) =>
                    {
                        t.Clear();
                        t.Append($"{Title}");
                    };
                    action_trigger.Action = Action;
                    action_trigger.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(action_trigger);
                }
            }

            public static void CreateSwitch<TBlock>(string IDPrefix, string actionIDPrefix, string controlID, string Title, string Notes, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Func<IMyTerminalBlock, bool> Getter, Action<IMyTerminalBlock, bool> Setter, Action<IMyTerminalBlock> Toggle, bool haveToggle = true, bool haveOnOff = false)
            {
                IMyTerminalControlOnOffSwitch control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, TBlock>($"{IDPrefix}{controlID}");
                control.Title = MyStringId.GetOrCompute(Title);
                control.Tooltip = MyStringId.GetOrCompute(Notes);
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.Getter = Getter;
                control.Setter = Setter;
                control.OnText = MyStringId.GetOrCompute("On");
                control.OffText = MyStringId.GetOrCompute("Off");
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);

                if (haveToggle)
                {
                    IMyTerminalAction action_trigger = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{actionIDPrefix}{controlID}.Trigger");
                    action_trigger.Name = new StringBuilder(Title);
                    action_trigger.Enabled = Enabled;
                    action_trigger.Writer = (b, t) =>
                    {
                        t.Clear();
                        t.Append($"{Title} {(Getter(b) ? "On" : "Off")}");
                    };
                    action_trigger.Action = Toggle;
                    action_trigger.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(action_trigger);
                }

                if (haveOnOff)
                {
                    IMyTerminalAction action_on = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{actionIDPrefix}{controlID}.On");
                    action_on.Name = new StringBuilder(Title);
                    action_on.Enabled = Enabled;
                    action_on.Writer = (b, t) =>
                    {
                        t.Clear();
                        t.Append($"{Title} {(Getter(b) ? "On" : "Off")}");
                    };
                    action_on.Action = b => Setter(b, true);
                    action_on.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(action_on);

                    IMyTerminalAction action_off = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{actionIDPrefix}{controlID}.Off");
                    action_off.Name = new StringBuilder(Title);
                    action_off.Enabled = Enabled;
                    action_off.Writer = (b, t) =>
                    {
                        t.Clear();
                        t.Append($"{Title} {(Getter(b) ? "On" : "Off")}");
                    };
                    action_off.Action = b => Setter(b, false);
                    action_off.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(action_off);
                }
            }

            public static void CreateCheckBox<TBlock>(string IDPrefix, string actionIDPrefix, string controlID, string Title, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Func<IMyTerminalBlock, bool> Getter, Action<IMyTerminalBlock, bool> Setter, Action<IMyTerminalBlock> Toggle, bool haveToggle = true, bool haveOnOff = false)
            {
                IMyTerminalControlCheckbox control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, TBlock>($"{IDPrefix}{controlID}");
                control.Title = MyStringId.GetOrCompute(Title);
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.Getter = Getter;
                control.Setter = Setter;
                control.OnText = MyStringId.GetOrCompute("On");
                control.OffText = MyStringId.GetOrCompute("Off");
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);

                if (haveToggle)
                {
                    IMyTerminalAction action_trigger = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{actionIDPrefix}{controlID}.Trigger");
                    action_trigger.Name = new StringBuilder(Title);
                    action_trigger.Enabled = Enabled;
                    action_trigger.Writer = (b, t) =>
                    {
                        t.Clear();
                        t.Append($"{Title} {(Getter(b) ? "On" : "Off")}");
                    };
                    action_trigger.Action = Toggle;
                    action_trigger.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(action_trigger);
                }

                if (haveOnOff)
                {
                    IMyTerminalAction action_on = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{actionIDPrefix}{controlID}.On");
                    action_on.Name = new StringBuilder(Title);
                    action_on.Enabled = Enabled;
                    action_on.Writer = (b, t) =>
                    {
                        t.Clear();
                        t.Append($"{Title} {(Getter(b) ? "On" : "Off")}");
                    };
                    action_on.Action = b => Setter(b, true);
                    action_on.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(action_on);

                    IMyTerminalAction action_off = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{actionIDPrefix}{controlID}.Off");
                    action_off.Name = new StringBuilder(Title);
                    action_off.Enabled = Enabled;
                    action_off.Writer = (b, t) =>
                    {
                        t.Clear();
                        t.Append($"{Title} {(Getter(b) ? "On" : "Off")}");
                    };
                    action_off.Action = b => Setter(b, false);
                    action_off.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(action_off);
                }
            }

            // Create a slider control for terminal block UI
            public static void CreateSlider<TBlock>(string IDPrefix, string controlID, string Title, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible,
                Func<IMyTerminalBlock, float> Getter, Action<IMyTerminalBlock, float> Setter, Action<IMyTerminalBlock, StringBuilder> Writer, float Mini, float Max,
                float Median = 0, SliderStyle Modle = SliderStyle.Lin)
            {
                IMyTerminalControlSlider control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, TBlock>($"{IDPrefix}{controlID}");
                control.Title = MyStringId.GetOrCompute(Title);
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.Getter = Getter;
                control.Setter = Setter;
                control.Writer = Writer;

                switch (Modle)
                {
                    case SliderStyle.Log:
                        control.SetLogLimits(Mini, Max);
                        break;
                    case SliderStyle.DLog:
                        control.SetDualLogLimits(Mini, Max, Median);
                        break;
                    default:
                        control.SetLimits(Mini, Max);
                        break;
                }

                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }


            public static void CreateComboBox<TBlock>(string IDPrefix, string controlID, string Title, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Func<IMyTerminalBlock, long> Getter, Action<IMyTerminalBlock, long> Setter, Action<List<MyTerminalControlComboBoxItem>> ComboBoxContent)
            {
                IMyTerminalControlCombobox control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, TBlock>($"{IDPrefix}{controlID}");
                control.Title = MyStringId.GetOrCompute(Title);
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.ComboBoxContent = ComboBoxContent;
                control.Getter = Getter;
                control.Setter = Setter;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }

            public static void CreateColor<TBlock>(string IDPrefix, string controlID, string Title, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Func<IMyTerminalBlock, Color> Getter, Action<IMyTerminalBlock, Color> Setter)
            {
                IMyTerminalControlColor control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, TBlock>($"{IDPrefix}{controlID}");
                control.Title = MyStringId.GetOrCompute(Title);
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.Getter = Getter;
                control.Setter = Setter;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }

            public static void CreateTextBox<TBlock>(string IDPrefix, string controlID, string Title, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Func<IMyTerminalBlock, StringBuilder> Getter, Action<IMyTerminalBlock, StringBuilder> Setter)
            {
                IMyTerminalControlTextbox control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, TBlock>($"{IDPrefix}{controlID}");
                control.Title = MyStringId.GetOrCompute(Title);
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.Getter = Getter;
                control.Setter = Setter;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }

            public static void CreateListBox<TBlock>(string IDPrefix, string controlID, string Title, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>> ItemSelected, Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> ListContent, int VisibleRowsCount = 6, bool Multiselect = false)
            {
                IMyTerminalControlListbox control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, TBlock>($"{IDPrefix}{controlID}");
                control.Title = MyStringId.GetOrCompute(Title);
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.ItemSelected = ItemSelected;
                control.ListContent = ListContent;
                control.VisibleRowsCount = VisibleRowsCount;
                control.Multiselect = Multiselect;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }

            public static void RefreshBlockTerminal(IMyTerminalBlock Block)
            {
                Block.ShowInToolbarConfig = !Block.ShowInToolbarConfig;
                Block.ShowInToolbarConfig = !Block.ShowInToolbarConfig;
            }

            public static void CreateProperty<TBlock, TValue>(string IDPrefix, string controlID, Func<IMyTerminalBlock, bool> Enabled, Func<IMyTerminalBlock, bool> Visible, Func<IMyTerminalBlock, TValue> Getter, Action<IMyTerminalBlock, TValue> Setter)
            {
                var control = MyAPIGateway.TerminalControls.CreateProperty<TValue, TBlock>($"{IDPrefix}Property.{controlID}");
                control.Visible = Visible;
                control.Enabled = Enabled;
                control.Getter = Getter;
                control.Setter = Setter;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }

        }
    }
}