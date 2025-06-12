using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace StarCore.RemoveRingSGOptions
{
    public static class RingRotor_HideButton
    {
        static bool Done = false;

        public static void DoOnce() // called by SensorLogic.cs
        {
            if(Done)
                return;

            Done = true;

            EditControls();
            EditActions();
        }

        static bool AppendedCondition(IMyTerminalBlock block)
        {
            // if block has this gamelogic component then return false to hide the control/action.
            return block?.GameLogic?.GetAs<OptionRemoval_SGRingHead>() == null;
        }

        static void EditControls()
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyMotorAdvancedStator>(out controls);

            foreach (IMyTerminalControl c in controls)
            {
                switch (c.Id)
                {
                    case "AddSmallRotorTopPart":
                    {
                        c.Visible = RingRotor_TerminalChainedDelegate.Create(c.Visible, AppendedCondition); // hides
                        break;
                    }
                }
            }
        }

        static void EditActions()
        {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyMotorAdvancedStator>(out actions);

            foreach(IMyTerminalAction a in actions)
            {
                switch(a.Id)
                {
                    case "AddSmallRotorTopPart":
                    {
                        a.Enabled = RingRotor_TerminalChainedDelegate.Create(a.Enabled, AppendedCondition);
                        break;
                    }
                }
            }
        }
    }
}
