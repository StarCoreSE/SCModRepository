using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Game.Gui;
using System.Text;
using VRageMath;
using Sandbox.Game.Components;
using VRage.Sync;
using System;

namespace Blues_Thruster_Particles
{
    public static class CustomControls
    {
        public static bool controlsAdded = false;
        public static Dictionary<long, string> items = new Dictionary<long, string>();
        public static bool IsDedicated => MyAPIGateway.Utilities.IsDedicated;

        public static void AddControls(IMyModContext context)
        {
            if (controlsAdded)
                return;

            controlsAdded = true;
     

            var combobox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyThrust>("SetThrustAnimation");          
            combobox.Visible = SetVisibleTrueHydrogen;
            combobox.Title = VRage.Utils.MyStringId.GetOrCompute($"Thruster Effects");
            combobox.Tooltip = VRage.Utils.MyStringId.GetOrCompute($"Sets Thruster Animation");
            combobox.SupportsMultipleBlocks = true;
            combobox.Getter = GetThrusterAnimation;
            combobox.Setter = SetThrusterAnimation;
            combobox.ComboBoxContent = SetComboboxContent;
        
            var colorSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyThrust>("SetThrusterColor");
            colorSlider.Visible = (block) => true;
            colorSlider.Title = VRage.Utils.MyStringId.GetOrCompute($"Thruster Color");
            colorSlider.Tooltip = VRage.Utils.MyStringId.GetOrCompute($"Sets Thruster Color");
            colorSlider.SupportsMultipleBlocks = true;
            colorSlider.Getter = GetThrusterColor;
            colorSlider.Setter = SetThrusterColor;

            var alphaSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyThrust>("SetThrusterAlpha");
            alphaSlider.Title = colorSlider.Title = VRage.Utils.MyStringId.GetOrCompute($"Alpha");
            alphaSlider.Visible = (block) => true;
            alphaSlider.Setter = SetThrusterAlpha;
            alphaSlider.Getter = GetThrusterAlpha;
            alphaSlider.SetLogLimits(0f, 1f);
            alphaSlider.SetLimits(0f, 1f);
            alphaSlider.Writer = AlphaSliderWriter;
            alphaSlider.SupportsMultipleBlocks = true;


            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(combobox);

            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(colorSlider);
            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(alphaSlider);
    
        }

        private static void SetComboboxContent(List<MyTerminalControlComboBoxItem> list)
        {
            long itemKey = 1;
            foreach (var key in Globals.ParticleEffectsList.Keys)
            {
                if (key == "")
                    list.Add(new MyTerminalControlComboBoxItem { Key = itemKey, Value = VRage.Utils.MyStringId.GetOrCompute($"Vanilla") });
                else
                    list.Add(new MyTerminalControlComboBoxItem { Key = itemKey, Value = VRage.Utils.MyStringId.GetOrCompute($"{key}") });
                if (!items.ContainsKey(itemKey))
                    items.Add(itemKey, key);
                itemKey++;
            }

        }
        private static long GetThrusterAnimation(IMyTerminalBlock block)
        {
            var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();
            moddedThruster.LoadCustomData();
            if (moddedThruster != null)
            {
                foreach (var item in items)
                {
                    if (item.Value == moddedThruster.ParticleEffectToGenerate)
                    {
                        return item.Key;
                    }            
                }
            }
            return 1;
        }



        private static void AlphaSliderWriter(IMyTerminalBlock block, StringBuilder builder)
        {
            var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();

            if (moddedThruster != null)
            {
                var value = moddedThruster.FlameColor.W * 255;
                builder.Append($"Value {Math.Round(value)}");
                
            }
        }

        private static void SetThrusterAlpha(IMyTerminalBlock block, float alpha)
        {
            var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();

            if (moddedThruster != null)
            {
                moddedThruster.FlameColor.W = alpha;
                moddedThruster.UpdateCustomData();
            }
        }
        private static float GetThrusterAlpha(IMyTerminalBlock block)
        {
            var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();
            //moddedThruster.LoadCustomData();
            if (moddedThruster != null)
            {
                return moddedThruster.FlameColor.W;
            }
            return 0f;
        }


        private static Color GetThrusterColor(IMyTerminalBlock block)
        {
           
            var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();
            moddedThruster.LoadCustomData();
            if (moddedThruster != null)
            {
                return new Color(moddedThruster.FlameColor);
            }
            //CustomDataCheckHere
            return Color.White;
        }

        private static void SetThrusterColor(IMyTerminalBlock block, Color color)
        {
            var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();

            if (moddedThruster != null)
            {
                moddedThruster.FlameColor = color;             
                moddedThruster.UpdateCustomData();
            }
        }

        private static void SetThrusterAnimation(IMyTerminalBlock block, long key)
        {
            var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();

            if (moddedThruster != null)
            {
                if(items[key] == "Vanilla")
                {
                    moddedThruster.ParticleEffectToGenerate = "";
					moddedThruster.FlameColor = new Vector4(2.55f, 2.40f, 0.45f, 1f);
                }                  
                else
                {
                    moddedThruster.ParticleEffectToGenerate = items[key];
                    moddedThruster.FlameColor = Vector4.Zero;
                }
                moddedThruster.UpdateCustomData();

            }
        }


        private static bool SetVisibleTrueHydrogen(IMyTerminalBlock block)
        {
            if (block?.GameLogic?.GetAs<Thrusters>() != null)
            {
                var moddedThruster = block?.GameLogic?.GetAs<Thrusters>();

                if (moddedThruster.MyCoreBlockDefinition.FuelConverter.FuelId == Globals.HydrogenId)
                    return true;
            }
            return false;
        }

        static List<IMyTerminalControl> GetControls<T>() where T : IMyTerminalBlock
        {
            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<T>(out controls);

            return controls;
        }
    }
}
