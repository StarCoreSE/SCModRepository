using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Epstein_Fusion_DS.HeatParts.ExtendableRadiators
{
    internal static class RadiatorControls
    {
        private static bool _done = false;

        static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<ExtendableRadiator>() != null;
        }

        public static void DoOnce() // called by GyroLogic.cs
        {
            if (_done)
                return;
            _done = true;

            // these are all the options and they're not all required so use only what you need.
            CreateControls();
        }

        private static void CreateControls()
        {
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyTerminalBlock>("Radiator_ToggleExtended");
                c.Title = MyStringId.GetOrCompute("Toggle Extension");
                c.Tooltip = MyStringId.GetOrCompute("Extends or retracts radiator panels attached to this block.");
                c.SupportsMultipleBlocks = true;

                c.Visible = CustomVisibleCondition;

                c.OnText = MyStringId.GetOrCompute("Extended");
                c.OffText = MyStringId.GetOrCompute("Retracted");

                c.Getter = (b) => b?.GameLogic?.GetAs<ExtendableRadiator>()?.IsExtended ?? false;
                c.Setter = (b, v) =>
                {
                    var logic = b?.GameLogic?.GetAs<ExtendableRadiator>();
                    if (logic != null)
                        logic.IsExtended = v;
                    c.UpdateVisual();
                };

                MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(c);
            }

            {
                var a = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Radiator_ToggleExtended");
                a.Name = new StringBuilder("Toggle Extension");
                a.ValidForGroups = true;

                a.Enabled = CustomVisibleCondition;

                a.Writer = (block, builder) =>
                {
                    builder.Clear();
                    if (block?.GameLogic?.GetAs<ExtendableRadiator>()?.IsExtended ?? false)
                        builder.Append("Extended");
                    else
                        builder.Append("Retracted");
                };

                a.Action = (block) =>
                {
                    var logic = block?.GameLogic?.GetAs<ExtendableRadiator>();
                    if (logic != null)
                        logic.IsExtended = !logic.IsExtended;
                };

                MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(a);
            }

            {
                var a = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Radiator_SetExtendedTrue");
                a.Name = new StringBuilder("Extend Radiator");
                a.ValidForGroups = true;

                a.Enabled = CustomVisibleCondition;

                a.Writer = (block, builder) =>
                {
                    builder.Clear();
                    if (block?.GameLogic?.GetAs<ExtendableRadiator>()?.IsExtended ?? false)
                        builder.Append("Extended");
                    else
                        builder.Append("Retracted");
                };

                a.Action = (block) =>
                {
                    var logic = block?.GameLogic?.GetAs<ExtendableRadiator>();
                    if (logic != null && !logic.IsExtended)
                        logic.IsExtended = true;
                };

                MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(a);
            }

            {
                var a = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Radiator_SetExtendedFalse");
                a.Name = new StringBuilder("Retract Radiator");
                a.ValidForGroups = true;

                a.Enabled = CustomVisibleCondition;

                a.Writer = (block, builder) =>
                {
                    builder.Clear();
                    if (block?.GameLogic?.GetAs<ExtendableRadiator>()?.IsExtended ?? false)
                        builder.Append("Extended");
                    else
                        builder.Append("Retracted");
                };

                a.Action = (block) =>
                {
                    var logic = block?.GameLogic?.GetAs<ExtendableRadiator>();
                    if (logic != null && logic.IsExtended)
                        logic.IsExtended = false;
                };

                MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(a);
            }
        }
    }
}
