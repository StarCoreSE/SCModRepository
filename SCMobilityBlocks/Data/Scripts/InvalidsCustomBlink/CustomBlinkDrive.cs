using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

namespace Invalid.BlinkDrive
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "BlinkDriveCustomLarge")]
    public class BlinkDrive : MyGameLogicComponent, IMyEventProxy
    {
        private IMyCubeBlock block;
        private MySync<bool, SyncDirection.BothWays> requestJumpSync;
        static bool controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            block = (IMyCubeBlock)Entity;

            MyLog.Default.WriteLineAndConsole("Init method called");

            //requestJumpSync.Value = false;
            // Initialize the MySync variable with an initial value
            requestJumpSync.ValueChanged += RequestJumpSync_ValueChanged;
        }

        private void RequestJumpSync_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            MyLog.Default.WriteLineAndConsole("RequestJumpSync_ValueChanged method called");
            if (obj.Value)
            {
                MyLog.Default.WriteLineAndConsole("PerformBlink will be called");
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    PerformBlink();
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole("PerformBlink skipped because not running on server.");
                }
            }
            else
            {
                MyLog.Default.WriteLineAndConsole("Value is false");
            }
        }

        private void PerformBlink()
        {
            MyLog.Default.WriteLineAndConsole("PerformBlink method called");
            // Calculate the forward vector and the target teleportation position
            Vector3D forwardVector = block.WorldMatrix.Forward;
            Vector3D teleportPosition = block.CubeGrid.WorldMatrix.Translation + (forwardVector * 1000); // 1km forward

            // Construct the new world matrix for the grid at the teleportation position
            MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;

            // Perform the teleportation
            (block.CubeGrid as MyEntity).Teleport(newWorldMatrix);
            ///MyVisualScriptLogicProvider.SetEntityPosition(block.CubeGrid.Name, teleportPosition);
            MyLog.Default.WriteLineAndConsole("Teleporting to " + teleportPosition.ToString());

            // Notify the player that the blink drive has been activated
            MyAPIGateway.Utilities.ShowNotification("Blink Drive Activated!", 2000, "Green");

            // Reset the request after performing the jump
            requestJumpSync.Value = false;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            MyLog.Default.WriteLineAndConsole("UpdateOnceBeforeFrame method called");
            if (!controlsCreated)
            {
                CreateTerminalControls();
            }
        }

        private static void CreateTerminalControls()
        {
            MyLog.Default.WriteLineAndConsole("CreateTerminalControls method called");
            controlsCreated = true;

            // Create a terminal button for activating the Blink Drive
            var blinkDriveButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>("BlinkDrive_ActivateButton");
            blinkDriveButton.Enabled = (b) => b.GameLogic is BlinkDrive;
            blinkDriveButton.Visible = (b) => b.GameLogic is BlinkDrive;
            blinkDriveButton.Title = MyStringId.GetOrCompute("Activate Blink Drive");
            blinkDriveButton.Tooltip = MyStringId.GetOrCompute("Activates the Blink Drive for a single jump.");
            blinkDriveButton.Action = (b) =>
            {
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null)
                {
                    MyLog.Default.WriteLineAndConsole("Button action called");
                    // Set the request to jump to true
                    drive.requestJumpSync.Value = true;
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(blinkDriveButton);

            // Now, add an action for cockpit activation
            var blinkDriveCockpitAction = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDriveActivate");
            blinkDriveCockpitAction.Name = new StringBuilder("Activate Blink Drive");
            // Specify an icon for the action. Adjust the path to match an actual icon file in your mod or game assets.
            blinkDriveCockpitAction.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
            blinkDriveCockpitAction.Action = b =>
            {
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null)
                {
                    MyLog.Default.WriteLineAndConsole("Cockpit action called");
                    // Set the request to jump to true
                    drive.requestJumpSync.Value = true;
                }
            };
            blinkDriveCockpitAction.Writer = (b, sb) =>
            {
                sb.Append("Activates the Blink Drive for a single jump.");
            };
            blinkDriveCockpitAction.Enabled = b => b.GameLogic is BlinkDrive;

            MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(blinkDriveCockpitAction);
        }

        public override void Close()
        {
            base.Close();
            MyLog.Default.WriteLineAndConsole("Close method called");

            // Dispose the MySync variable
            if (requestJumpSync != null)
            {
                requestJumpSync.ValueChanged -= RequestJumpSync_ValueChanged;
                requestJumpSync = null;
            }
        }
    }
}
