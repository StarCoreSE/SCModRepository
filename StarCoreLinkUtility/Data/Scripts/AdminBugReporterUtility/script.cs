using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;


namespace invalid.BugReporter
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ButtonExample : MySessionComponentBase
    {
        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.F2) && ValidInput()) //hey dumbass, use this before the url. fucking keen https://steamcommunity.com/linkfilter/?url={url}
            {
               
                MyVisualScriptLogicProvider.OpenSteamOverlay("https://github.com/StarCoreSE/SCModRepository/wiki/Important-Commands");
						
            }		
				
        }

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{

            MyAPIGateway.Utilities.ShowMessage("GODFORSAKEN ASTRONAUT", "Press Shift + F2 to open the StarCore Wiki!" );
			
           // MyAPIGateway.Utilities.ShowNotification("\n\n ALT+P = Starcore Pointsheet \n SHIFT+F2 = Report Bug", 30000, "Red"); //this is fucking stupid.
			
		}      



        private bool ValidInput()
        {
            if (MyAPIGateway.Session.CameraController != null && !MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible
                && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
            {
                return true;
            }
            return false;
        }

        protected override void UnloadData()
        {

        }
    }
}
