using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
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
        private int jumpCooldownTimer;
        private const int cooldownDuration = 10 * 60; // Cooldown duration in frames (10 seconds * 60 frames per second)

        static bool controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;

            block = (IMyCubeBlock)Entity;

            MyLog.Default.WriteLineAndConsole("Init method called");

            requestJumpSync.ValueChanged += RequestJumpSync_ValueChanged;
        }

        private void RequestJumpSync_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            MyLog.Default.WriteLineAndConsole("RequestJumpSync_ValueChanged method called");
            if (obj.Value && CanJump())
            {
                MyLog.Default.WriteLineAndConsole("PerformBlink will be called");
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    PerformBlink();
                    jumpCooldownTimer = cooldownDuration; // Set cooldown timer
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole("PerformBlink skipped because not running on server.");
                }
            }
            else
            {
                MyLog.Default.WriteLineAndConsole("Value is false or not ready for jump due to cooldown.");
            }
        }

        private bool CanJump()
        {
            return jumpCooldownTimer <= 0;
        }

        private void PerformBlink()
        {
            MyLog.Default.WriteLineAndConsole("PerformBlink method called");
            Vector3D forwardVector = block.WorldMatrix.Forward;
            Vector3D teleportPosition = block.CubeGrid.WorldMatrix.Translation + (forwardVector * 1000); // 1km forward
            MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;
            (block.CubeGrid as MyEntity).Teleport(newWorldMatrix);
            MyLog.Default.WriteLineAndConsole("Teleporting to " + teleportPosition.ToString());
            MyAPIGateway.Utilities.ShowNotification("Blink Drive Activated!", 2000, "Green");
            requestJumpSync.Value = false;
            jumpCooldownTimer = 0; // Reset cooldown timer
        }

        public override void UpdateAfterSimulation()
        {
              MyAPIGateway.Utilities.ShowNotification("FICL", 2000, "Green");
            if (jumpCooldownTimer > 0)
            {
                jumpCooldownTimer--;

                if (jumpCooldownTimer > 0)
                {
                    int secondsRemaining = jumpCooldownTimer / 60; // Convert frames to seconds
                    MyAPIGateway.Utilities.ShowNotification($"Jump Cooldown: {secondsRemaining} seconds", 1000, "White");
                }
            }
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
                    drive.requestJumpSync.Value = true;
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(blinkDriveButton);

            var blinkDriveCockpitAction = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDriveActivate");
            blinkDriveCockpitAction.Name = new StringBuilder("Activate Blink Drive");
            blinkDriveCockpitAction.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
            blinkDriveCockpitAction.Action = b =>
            {
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null)
                {
                    MyLog.Default.WriteLineAndConsole("Cockpit action called");
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

            if (requestJumpSync != null)
            {
                requestJumpSync.ValueChanged -= RequestJumpSync_ValueChanged;
                requestJumpSync = null;
            }
        }
    }
}
