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
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

namespace Invalid.BlinkDrive
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "BlinkDriveCustomLarge")]
    public class BlinkDrive : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private MySync<bool, SyncDirection.BothWays> requestJumpSync;
        private int jumpCooldownTimer;
        private const int cooldownDuration = 10 * 60; // 10 seconds * 60 frames per second

        static bool controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;

            block = (IMyCubeBlock)Entity;
           // requestJumpSync = new MySync<bool, SyncDirection.BothWays>(this, nameof(requestJumpSync));
            requestJumpSync.ValueChanged += RequestJumpSync_ValueChanged;
        }

        private void RequestJumpSync_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            if (obj.Value && CanJump())
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    PerformBlink();
                }
            }
            else
            {
                // Reset the request on the client side to ensure it's properly synchronized
                ResetJumpRequest();
            }
        }

        private bool CanJump()
        {
            return jumpCooldownTimer <= 0;
        }

        private void PerformBlink()
        {
            Vector3D forwardVector = block.WorldMatrix.Forward;
            Vector3D teleportPosition = block.CubeGrid.WorldMatrix.Translation + (forwardVector * 1000); // 1km forward
            MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;
            (block.CubeGrid as MyEntity).Teleport(newWorldMatrix);

            // Reset jump request and start cooldown
            ResetJumpRequest();
            jumpCooldownTimer = cooldownDuration;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (jumpCooldownTimer > 0)
            {
                jumpCooldownTimer--;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
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
            blinkDriveCockpitAction.Action = (b) =>
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
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null)
                {
                    int secondsRemaining = Math.Max(drive.jumpCooldownTimer / 60, 0); // Convert frames to seconds
                    sb.Append($"CD {secondsRemaining} s");
                }
                else
                {
                    sb.Append("Unable to retrieve cooldown information.");
                }
            };
            blinkDriveCockpitAction.Enabled = (b) => b.GameLogic is BlinkDrive;

            MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(blinkDriveCockpitAction);
        }

        private void ResetJumpRequest()
        {
            // This ensures the jump request is reset correctly on both server and clients
            requestJumpSync.Value = false;
        }

        public override void Close()
        {
            base.Close();
            if (requestJumpSync != null)
            {
                requestJumpSync.ValueChanged -= RequestJumpSync_ValueChanged;
            }
        }
    }
}
