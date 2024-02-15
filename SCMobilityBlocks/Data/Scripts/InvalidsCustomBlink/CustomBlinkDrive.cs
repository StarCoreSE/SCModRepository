using Sandbox.Common.ObjectBuilders;
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
        private MySync<bool, SyncDirection.BothWays> activateBlink;
        static bool controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            activateBlink.ValueChanged += ActivateBlink_ValueChanged;

            block = (IMyCubeBlock)Entity;
        }

        private void ActivateBlink_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            if (obj.Value)
            {
                PerformBlink();
            }
        }

        private void PerformBlink()
        {
            // Calculate the forward vector and the target teleportation position
            Vector3D forwardVector = block.WorldMatrix.Forward;
            Vector3D teleportPosition = block.CubeGrid.WorldMatrix.Translation + (forwardVector * 1000); // 1km forward

            // Construct the new world matrix for the grid at the teleportation position
            MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;

            // Perform the teleportation
            (block.CubeGrid as MyEntity).Teleport(newWorldMatrix);

            // Notify the player that the blink drive has been activated
            MyAPIGateway.Utilities.ShowNotification("Blink Drive Activated!", 2000, "Green");
        }


        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!controlsCreated)
            {
                CreateTerminalControls();
            }
        }

        static void CreateTerminalControls()
        {
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
                    // Perform Blink without toggling a value, ensuring a single activation
                    drive.PerformBlink();
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
                    // Directly call PerformBlink to simulate a button press
                    drive.PerformBlink();
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

            // Unsubscribe from events
            if (activateBlink != null)
            {
                activateBlink.ValueChanged -= ActivateBlink_ValueChanged;
            }
        }
    }
}
