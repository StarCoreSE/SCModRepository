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

namespace DynamicResistence
{
    public static class DynaResistTerminalControls
    {
        const string IdPrefix = "SCDefMod_"; // highly recommended to tag your properties/actions like this to avoid colliding with other mods'

        static bool Done = false;

        public static void DoOnce(IMyModContext context) 
        {
            if(Done)
                return;
            Done = true;

            CreateControls();
            CreateActions(context);
        }

        static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<DynamicResistLogic>() != null;
        }

        static void CreateControls()
        {
            {
                IMyTerminalControlSlider polarizationValueSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyConveyorSorter>(IdPrefix + "HullPolarization");
                polarizationValueSlider.Title = MyStringId.GetOrCompute("Hull Polarization");

                polarizationValueSlider.Tooltip = MyStringId.GetOrCompute("Adjusts the mount of Damage Absorbed by the Block");
                polarizationValueSlider.SetLimits(0, 30);
                polarizationValueSlider.Writer = (b, sb) =>
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                    {
                        float v = logic.HullPolarization;
                        sb.Append(Math.Round(v, 0, MidpointRounding.ToEven)).Append("%");
                    }
                };
                polarizationValueSlider.Visible = p => p.BlockDefinition.SubtypeId.Contains("LargeBlockConveyorSorter");
                polarizationValueSlider.Enabled = p => p.BlockDefinition.SubtypeId.Contains("LargeBlockConveyorSorter");
                polarizationValueSlider.Getter = (b) => b?.GameLogic?.GetAs<DynamicResistLogic>()?.HullPolarization ?? 0;
                polarizationValueSlider.Setter = (b, v) =>
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                        logic.HullPolarization = MathHelper.Clamp(v, 0f, 30f);
                };
                polarizationValueSlider.SupportsMultipleBlocks = true;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(polarizationValueSlider);
            }
        }

        static void CreateActions(IMyModContext context)
        {
            {
                var increasePolarization = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "PolarizationIncrease");

                increasePolarization.Name = new StringBuilder("Increase Polarization");
                increasePolarization.ValidForGroups = true;
                increasePolarization.Icon = @"Textures\GUI\Icons\Actions\CharacterToggle.dds";
                increasePolarization.Action = (b) => 
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                    {
                        logic.HullPolarization = logic.HullPolarization + 1;
                    }
                };
                increasePolarization.Writer = (b, sb) =>
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                    {
                        float v = logic.HullPolarization;
                        sb.Append(Math.Round(v, 0, MidpointRounding.ToEven)).Append("%");
                    }
                };
                increasePolarization.InvalidToolbarTypes = new List<MyToolbarType>()
                {
                    MyToolbarType.ButtonPanel,
                    MyToolbarType.Character,
                };
                increasePolarization.Enabled = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(increasePolarization);
            }

            //Decrease Polarization
            {
                var decreasePolarization = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + "PolarizationDecrease");

                decreasePolarization.Name = new StringBuilder("Decrease Polarization");
                decreasePolarization.ValidForGroups = true;
                decreasePolarization.Icon = @"Textures\GUI\Icons\Actions\CharacterToggle.dds";
                decreasePolarization.Action = (b) =>
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                    {
                        logic.HullPolarization = logic.HullPolarization - 1;
                    }
                };
                decreasePolarization.Writer = (b, sb) =>
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                    {
                        float v = logic.HullPolarization;
                        sb.Append(Math.Round(v, 0, MidpointRounding.ToEven)).Append("%");
                    }
                };
                decreasePolarization.InvalidToolbarTypes = new List<MyToolbarType>( )
                {
                    MyToolbarType.ButtonPanel,
                    MyToolbarType.Character,
                };
                decreasePolarization.Enabled = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(decreasePolarization);
            }
        }

        /*static void CreateProperties()
        {
            // Terminal controls automatically generate properties like these, but you can also add new ones manually without the GUI counterpart.
            // The main use case is for PB to be able to read them.
            // The type given is only limited by access, can only do SE or .NET types, nothing custom (except methods because the wrapper Func/Action is .NET).
            // For APIs, one can send a IReadOnlyDictionary<string, Delegate> for a list of callbacks. Just be sure to use a ImmutableDictionary to avoid getting your API hijacked.
            {
                var CurrentPolarizationValue = MyAPIGateway.TerminalControls.CreateProperty<Vector3, IMyConveyorSorter>(IdPrefix + "CurrentPolarValue");

                CurrentPolarizationValue.Getter = (b) =>
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                    {
                        logic.HullPolarization = logic.HullPolarization;
                    }
                };

                CurrentPolarizationValue.Setter = (b, v) =>
                {
                    var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                    if (logic != null)
                    {
                        logic.HullPolarization = logic.HullPolarization;
                    }
                };

                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(CurrentPolarizationValue);


                // a mod or PB can use it like:
                //Vector3 vec = gyro.GetValue<Vector3>("YourMod_SampleProp");
                // just careful with sending mutable reference types, there's no serialization inbetween so the mod/PB can mutate your reference.
            }
          }*/

    }
}