using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using MIG.Shared.CSharp;
using VRage.Utils;
using VRageMath;

namespace MIG.Shared.SE
{
    public static class GuiHelper
    {

        public static long DoubleClickTimer = 0;
        
        public static void DoubleClick(ref long timer, Action action)
        {
            var now = SharpUtils.msTimeStamp();
            if (now - timer > 500)
            {
                timer = now;
            }
            else
            {
                action();
                DoubleClickTimer += 1000;
            }
        }
        
        public static void DoubleClick(this IMyTerminalControlButton button)
        {
            var action = button.Action;
            button.Action = (xx) =>
            {
                DoubleClick(ref DoubleClickTimer, () => action(xx));
            };
        }
        
        public static IMyTerminalControlSlider CreateSlider<T,Z> (this IMyTerminalControls system, string id, string name, string tooltip, float min, float max, 
            Func<T, float> getter, Action<T, StringBuilder> writer, Action<T, float> setter, Func<IMyTerminalBlock, T> getterT, Func<T, bool> enabled= null, Func<T, bool> visible = null, bool update = false)
        {
            var XControl = system.CreateControl<IMyTerminalControlSlider, Z>(typeof(T).Name+ "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.SetLimits(min, max);
            XControl.Enabled = IsEnabled(enabled, getterT);
            XControl.Visible = IsVisible (visible, getterT);


            XControl.Getter = (b) => getter(getterT (b));
            XControl.Writer = (b, t) => writer(getterT (b), t);
            XControl.Setter = (b, v) =>
            {
                setter(getterT (b), v);
                if (update) XControl.UpdateVisual();
            };

            XControl.Enabled = XControl.Enabled.TryCatch(name + " Enabled");
            XControl.Visible = XControl.Visible.TryCatch(name + " Visible");
            XControl.Getter  = XControl.Getter.TryCatch(name + " Getter");
            XControl.Setter  = XControl.Setter.TryCatch(name + " Setter");
            XControl.Writer  = XControl.Writer.TryCatch(name + " Writer");
            
            system.AddControl<Z>(XControl);

            return XControl;
        }

        public static IMyTerminalControlLabel CreateLabel<T, Z>(this IMyTerminalControls system, string id, MyStringId text)
        {
            var control = system.CreateControl<IMyTerminalControlLabel, IMyCargoContainer>(typeof(T).Name+ "_" + id);
            control.Label = text;
            MyAPIGateway.TerminalControls.AddControl<IMyCargoContainer>(control);
            return control;
        }

        
        
        public static IMyTerminalControlButton CreateButton<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip, Action<T> action, Func<IMyTerminalBlock, T> getterT, Func<T, bool> enabled = null, Func<T, bool> visible = null)
        {
            var XControl = system.CreateControl<IMyTerminalControlButton, Z>(typeof(T).Name+ "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.Enabled = IsEnabled(enabled, getterT);
            XControl.Visible = IsVisible (visible, getterT);
            XControl.Action = (b) => action(getterT (b));

            XControl.Enabled = XControl.Enabled.TryCatch(name + " Enabled");
            XControl.Visible = XControl.Visible.TryCatch(name + " Visible");
            XControl.Action  = XControl.Action.TryCatch(name + " Action");
            
            system.AddControl<Z>(XControl);
            return XControl;
        }

        public static IMyTerminalControlColor CreateColorPicker<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip, Func<T, Color> getter, Action<T, Color> setter, Func<IMyTerminalBlock, T> getterT, Func<T, bool> enabled = null, Func<T, bool> visible = null)
        {
            var XControl = system.CreateControl<IMyTerminalControlColor, Z>(typeof(T).Name+ "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.Enabled = IsEnabled(enabled, getterT);
            XControl.Visible = IsVisible (visible, getterT);
            XControl.Getter = (b) => getter(getterT (b));
            XControl.Setter = (b, v) => setter(getterT (b), v);
            
            XControl.Enabled = XControl.Enabled.TryCatch(name + " Enabled");
            XControl.Visible = XControl.Visible.TryCatch(name + " Visible");
            XControl.Getter  = XControl.Getter.TryCatch(name + " Getter");
            XControl.Setter  = XControl.Setter.TryCatch(name + " Setter");

            system.AddControl<Z>(XControl);

            return XControl;
        }

        public static IMyTerminalControlCombobox CreateCombobox<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip, Func<T, long> getter, Action<T, long> setter, List<MyStringId> texts, Func<IMyTerminalBlock, T> getterT, Func<T, bool> enabled = null, Func<T, bool> visible=null, bool update = false)
        {
            var XControl = system.CreateControl<IMyTerminalControlCombobox, Z>(typeof(T).Name+ "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);

            XControl.Getter = (b) => getter(getterT (b));
            XControl.Setter = (b, v) =>
            {
                setter(getterT (b), v);
                if(update) XControl.UpdateVisual();
            };

            XControl.Enabled = IsEnabled(enabled, getterT);
            XControl.Visible = IsVisible (visible, getterT);


            XControl.ComboBoxContent = (b) =>
            {
                var c = 0;
                foreach (var x in texts) {
                    var i = new VRage.ModAPI.MyTerminalControlComboBoxItem();
                    i.Key = c;
                    i.Value = x;
                    c++;
                    b.Add(i);
                }
            };
            XControl.SupportsMultipleBlocks = true;
            
            XControl.Enabled = XControl.Enabled.TryCatch(name + " Enabled");
            XControl.Visible = XControl.Visible.TryCatch(name + " Visible");
            XControl.Getter  = XControl.Getter.TryCatch(name + " Getter");
            XControl.Setter  = XControl.Setter.TryCatch(name + " Setter");
            XControl.ComboBoxContent  = XControl.ComboBoxContent.TryCatch(name+ " ComboBoxContent");
            
            system.AddControl<Z>(XControl);

            return XControl;
        }
        
        public static IMyTerminalControlCheckbox CreateCheckbox<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip,
            Func<T, bool> getter,  Action<T, bool> setter, Func<IMyTerminalBlock, T> getterT = null, Func<T, bool> enabled = null, Func<T, bool> visible = null, bool update = false)
        {
            var XControl = system.CreateControl<IMyTerminalControlCheckbox, Z>(typeof(T).Name+ "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);

            XControl.Enabled = IsEnabled(enabled, getterT);
            XControl.Visible = IsVisible (visible, getterT);
            XControl.Getter = (b) => getter(getterT (b));
            XControl.Setter = (b, v) =>
            {
                setter(getterT (b), v);
                if(update) XControl.UpdateVisual();
            };

            XControl.Enabled = XControl.Enabled.TryCatch(name + " Enabled");
            XControl.Visible = XControl.Visible.TryCatch(name + " Visible");
            XControl.Getter  = XControl.Getter.TryCatch(name + " Getter");
            XControl.Setter  = XControl.Setter.TryCatch(name + " Setter");
            system.AddControl<Z>(XControl);
            return XControl;
        }
        
        public static IMyTerminalControlOnOffSwitch CreateOnOff<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip,
            Func<T, bool> getter,  Action<T, bool> setter, Func<IMyTerminalBlock, T> getterT = null, Func<T, bool> enabled = null, Func<T, bool> visible = null, bool update = false)
        {
            var XControl = system.CreateControl<IMyTerminalControlOnOffSwitch, Z>(typeof(T).Name+ "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);

            XControl.Enabled = IsEnabled(enabled, getterT);
            XControl.Visible = IsVisible (visible, getterT);
            XControl.Getter = (b) => getter(getterT (b));
            XControl.Setter = (b, v) =>
            {
                setter(getterT (b), v);
                if(update) XControl.UpdateVisual();
            };

            XControl.Enabled = XControl.Enabled.TryCatch(name + " Enabled");
            XControl.Visible = XControl.Visible.TryCatch(name + " Visible");
            XControl.Getter  = XControl.Getter.TryCatch(name + " Getter");
            XControl.Setter  = XControl.Setter.TryCatch(name + " Setter");
            system.AddControl<Z>(XControl);
            return XControl;
        }


        public static IMyTerminalControlTextbox CreateTextbox<T, Z>(this IMyTerminalControls system, string id, string name, string tooltip,
           Func<T, StringBuilder> getter, Action<T, StringBuilder> setter, Func<IMyTerminalBlock, T> getterT = null, Func<T, bool> enabled = null, Func<T, bool> visible = null, bool update = false)
        {
            var XControl = system.CreateControl<IMyTerminalControlTextbox, Z>(typeof(T).Name+ "_" + id);
            XControl.Title = MyStringId.GetOrCompute(name);
            XControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            XControl.Enabled = IsEnabled(enabled, getterT);
            XControl.Visible = IsVisible(visible, getterT);
            XControl.Getter = (b) => getter(getterT (b));
            XControl.Setter = (b, v) =>
            {
                setter(getterT (b), v);
                if (update) XControl.UpdateVisual();
            };
            
            XControl.Enabled = XControl.Enabled.TryCatch(name + " Enabled");
            XControl.Visible = XControl.Visible.TryCatch(name + " Visible");
            XControl.Getter  = XControl.Getter.TryCatch(name + " Getter");
            XControl.Setter  = XControl.Setter.TryCatch(name + " Setter");
            
            system.AddControl<Z>(XControl);
            return XControl;
        }
        
        private static Func<IMyTerminalBlock, bool> IsEnabled<T> (Func<T, bool> enabled, Func<IMyTerminalBlock, T> getterT)
        {
            return (b) =>
            {
                var bb = getterT(b);
                if (bb == null) return false;
                return enabled?.Invoke(bb) ?? true;
            };
        } 
        
        private static Func<IMyTerminalBlock, bool> IsVisible<T> (Func<T, bool> isVisible, Func<IMyTerminalBlock, T> getterT)
        {
            return (b) =>
            {
                var bb = getterT(b);
                if (bb == null) return false;
                return isVisible?.Invoke(bb) ?? true;
            };
        } 
    }
}
