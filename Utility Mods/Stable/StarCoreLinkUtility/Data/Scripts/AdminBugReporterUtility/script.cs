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
               
                MyVisualScriptLogicProvider.OpenSteamOverlay("https://steamcommunity.com/linkfilter/?url=https://docs.google.com/document/d/1XR2VdfeDoWqNARjCFNCiQLdTx5UxEPOpgEdyHQgfQWQ");
						
            }	

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.F3) && ValidInput()) //hey dumbass, use this before the url. fucking keen https://steamcommunity.com/linkfilter/?url={url}
            {
               
                MyVisualScriptLogicProvider.OpenSteamOverlay("https://steamcommunity.com/linkfilter/?url=https://github.com/StarCoreSE/SCModRepository/wiki/Important-Commands");
						
            }				
			
			if (MyAPIGateway.Input.IsKeyPress(MyKeys.LeftControl) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.F2) && ValidInput()) //hey dumbass, use this before the url. fucking keen https://steamcommunity.com/linkfilter/?url={url}
            {
               
                MyVisualScriptLogicProvider.OpenSteamOverlay("https://steamcommunity.com/linkfilter/?url=https://docs.google.com/forms/d/e/1FAIpQLSev5nBSZ3oaQWLFYE3HG0N5F_x1gJUDPblx13vi-E0SA-P18Q/viewform");
						
            }	
				
        }

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{

            //MyAPIGateway.Utilities.ShowMessage("GODFORSAKEN ASTRONAUT", "Press Shift + F2 to open the StarCore Infodoc" );
            //MyAPIGateway.Utilities.ShowMessage("GODFORSAKEN ASTRONAUT", "Press Ctrl + F2 to open an issue submission form");
			
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
