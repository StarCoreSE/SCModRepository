using System.Text;

namespace DefenseShields.Control
{
    using System;
    using System.Collections.Generic;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using VRage.ModAPI;
    using VRage.Utils;
    using VRageMath;

    public static class TerminalHelpers
    {
        internal static IMyTerminalControlOnOffSwitch AddOnOff<T>(T block, string name, string title, string tooltip, string onText, string offText, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, T>(name);
            var d = GetDefaultEnabled();

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.OnText = MyStringId.GetOrCompute(onText);
            c.OffText = MyStringId.GetOrCompute(offText);
            c.Enabled = enabledGetter ?? d;
            c.Visible = visibleGetter ?? d;
            c.Getter = getter;
            c.Setter = setter;
            MyAPIGateway.TerminalControls.AddControl<T>(c);

            return c;
        }

        internal static IMyTerminalControlButton AddButton<T>(T block, string name, string title, string tooltip, Action<IMyTerminalBlock> action, Func<IMyTerminalBlock, bool> enableGetter = null,  Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, T>(name);
            var d = GetDefaultEnabled();

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Visible = visibleGetter ?? d;
            c.Enabled = enableGetter;
            c.Action = action;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            return c;
        }

        internal static IMyTerminalControlSeparator Separator<T>(T block, string name, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, T>(name);
            var d = GetDefaultEnabled();

            c.Enabled = enabledGetter ?? d;
            c.Visible = visibleGetter ?? d;
            MyAPIGateway.TerminalControls.AddControl<T>(c);

            return c;
        }

        internal static IMyTerminalControlColor AddColorEditor<T>(T block, string name, string title, string tooltip, Func<IMyTerminalBlock, Color> getter, Action<IMyTerminalBlock, Color> setter, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, T>(name);
            var d = GetDefaultEnabled();

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Enabled = enabledGetter ?? d;
            c.Visible = visibleGetter ?? d;
            c.Getter = getter;
            c.Setter = setter;
            MyAPIGateway.TerminalControls.AddControl<T>(c);

            return c;
        }

        internal static IMyTerminalControlSlider AddSlider<T>(T block, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var s = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);
            var d = GetDefaultEnabled();

            s.Title = MyStringId.GetOrCompute(title);
            s.Tooltip = MyStringId.GetOrCompute(tooltip);
            s.Enabled = enabledGetter ?? d;
            s.Visible = visibleGetter ?? d;
            s.Getter = getter;
            s.Setter = setter;
            s.Writer = (b, v) => v.Append(getter(b).ToString("N2"));
            MyAPIGateway.TerminalControls.AddControl<T>(s);
            return s;
        }

        internal static IMyTerminalControlSlider AddModSlider<T>(T block, string name, string title, string tooltip, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null, Action<IMyTerminalBlock, StringBuilder> writer = null) where T : IMyTerminalBlock
        {
            var s = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);
            var d = GetDefaultEnabled();

            s.Title = MyStringId.GetOrCompute(title);
            s.Tooltip = MyStringId.GetOrCompute(tooltip);
            s.Enabled = enabledGetter ?? d;
            s.Visible = visibleGetter ?? d;
            s.Getter = getter;
            s.Setter = setter;
            s.Writer = writer;
            MyAPIGateway.TerminalControls.AddControl<T>(s);
            return s;
        }


        internal static IMyTerminalControlCombobox AddCombobox<T>(T block, string name, string title, string tooltip, Func<IMyTerminalBlock, long> getter, Action<IMyTerminalBlock, long> setter, Action<List<MyTerminalControlComboBoxItem>> fillAction, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var cmb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>(name);
            var d = GetDefaultEnabled();

            cmb.Title = MyStringId.GetOrCompute(title);
            cmb.Tooltip = MyStringId.GetOrCompute(tooltip);
            cmb.Enabled = enabledGetter ?? d;
            cmb.Visible = visibleGetter ?? d;
            cmb.ComboBoxContent = fillAction;
            cmb.Getter = getter;
            cmb.Setter = setter;
            MyAPIGateway.TerminalControls.AddControl<T>(cmb);
            return cmb;
        }

        internal static IMyTerminalControl[] AddVectorEditor<T>(T block, string name, string title, string tooltip, Func<IMyTerminalBlock, Vector3> getter, Action<IMyTerminalBlock, Vector3> setter, float min = -10, float max = 10, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null, string writerFormat = "0.##") where T : IMyTerminalBlock
        {
            var controls = new IMyTerminalControl[4];

            var d = GetDefaultEnabled();

            var lb = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, T>(name + "_Label");
            lb.Label = MyStringId.GetOrCompute(title);
            lb.Enabled = enabledGetter ?? d;
            lb.Visible = visibleGetter ?? d;
            MyAPIGateway.TerminalControls.AddControl<T>(lb);
            controls[0] = lb;

            var x = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name + "_X");
            x.Title = MyStringId.GetOrCompute("X");
            x.Tooltip = MyStringId.GetOrCompute(tooltip);
            x.Writer = (b, s) => s.Append(getter(b).X.ToString(writerFormat));
            x.Getter = b => getter(b).X;
            x.Setter = (b, v) =>
            {
                var vc = getter(b);
                vc.X = v;
                setter(b, vc);
            };
            x.Enabled = enabledGetter ?? d;
            x.Visible = visibleGetter ?? d;
            x.SetLimits(min, max);
            MyAPIGateway.TerminalControls.AddControl<T>(x);
            controls[1] = x;

            var y = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name + "_Y");
            y.Title = MyStringId.GetOrCompute("Y");
            y.Tooltip = MyStringId.GetOrCompute(tooltip);
            y.Writer = (b, s) => s.Append(getter(b).Y.ToString(writerFormat));
            y.Getter = b => getter(b).Y;
            y.Setter = (b, v) =>
            {
                var vc = getter(b);
                vc.Y = v;
                setter(b, vc);
            };
            y.Enabled = enabledGetter ?? d;
            y.Visible = visibleGetter ?? d;
            y.SetLimits(min, max);
            MyAPIGateway.TerminalControls.AddControl<T>(y);
            controls[2] = y;

            var z = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name + "_Z");
            z.Title = MyStringId.GetOrCompute("Z");
            z.Tooltip = MyStringId.GetOrCompute(tooltip);
            z.Writer = (b, s) => s.Append(getter(b).Z.ToString(writerFormat));
            z.Getter = b => getter(b).Z;
            z.Setter = (b, v) =>
            {
                var vc = getter(b);
                vc.Z = v;
                setter(b, vc);
            };
            z.Enabled = enabledGetter ?? d;
            z.Visible = visibleGetter ?? d;
            z.SetLimits(min, max);
            MyAPIGateway.TerminalControls.AddControl<T>(z);
            controls[3] = z;

            return controls;
        }

        internal static IMyTerminalControlCheckbox AddCheckbox<T>(T block, string name, string title, string tooltip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> enabledGetter = null, Func<IMyTerminalBlock, bool> visibleGetter = null) where T : IMyTerminalBlock
        {
            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, T>(name);
            var d = GetDefaultEnabled();

            c.Title = MyStringId.GetOrCompute(title);
            c.Tooltip = MyStringId.GetOrCompute(tooltip);
            c.Getter = getter;
            c.Setter = setter;
            c.Visible = visibleGetter ?? d;
            c.Enabled = enabledGetter ?? d;

            MyAPIGateway.TerminalControls.AddControl<T>(c);
            return c;
        }

        private static Func<IMyTerminalBlock, bool> GetDefaultEnabled()
        {
            return b => b.BlockDefinition.SubtypeId.StartsWith("DSControl");
        }
    }
}
