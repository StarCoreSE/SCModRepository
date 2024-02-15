using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "BlinkDriveCustomLarge")]
    public class BlinkDrive : MyGameLogicComponent
    {
        private IMyCollector block;
        private MySync<bool, SyncDirection.BothWays> requestJumpSync;
        private int jumpCooldownTimer;
        private const int cooldownDuration = 10 * 60; // 10 seconds * 60 frames per second

        static bool controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;

            block = (IMyCollector)Entity;
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
            // Check if the block is working and functional
            if (!block.IsWorking || !block.IsFunctional)
                return false;

            // Check if the grid has at least 300 MW of available power
            return HasEnoughPower() && jumpCooldownTimer <= 0;
        }

        private bool HasEnoughPower()
        {
            // Get the power sink component
            var sink = block.Components.Get<MyResourceSinkComponent>();
            if (sink != null)
            {
                // Check if the available input power is at least 300 MW
                return sink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) >= 0.3f;
            }
            return false;
        }

        private void PerformBlink()
        {
            Vector3D forwardVector = block.WorldMatrix.Forward;
            Vector3D teleportPosition = block.CubeGrid.WorldMatrix.Translation + (forwardVector * 1000); // 1km forward
            MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;
            (block.CubeGrid as MyEntity).Teleport(newWorldMatrix);

            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("HESFAST", block.CubeGrid.PositionComp.GetPosition());

            // Reset jump request and start cooldown
            ResetJumpRequest();
            jumpCooldownTimer = cooldownDuration;
        }


        public override void UpdateOnceBeforeFrame()
        {
            // Check if the block and its cube grid are valid
            if (block == null || block.CubeGrid == null)
                return;

            // Check if the block's cube grid physics are valid
            if (block.CubeGrid.Physics == null)
                return;

            var sink = Entity.Components.Get<MyResourceSinkComponent>();

            // Initialize controls if not already created
            if (!controlsCreated)
            {
                CreateTerminalControls();
                controlsCreated = true;
            }

            if (sink != null)
            {
                sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, ComputePowerRequired);
                sink.Update();
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Check if the block and its cube grid are valid
            if (block == null || block.CubeGrid == null)
                return;

            // Check if the block's cube grid physics are valid
            if (block.CubeGrid.Physics == null)
                return;

            // Check if the block is still working and functional
            if (!block.IsWorking || !block.IsFunctional)
            {
                // If not, reset the jump request and do not update the cooldown timer
                ResetJumpRequest();
                return;
            }

            // Perform power calculation
            var sink = Entity.Components.Get<MyResourceSinkComponent>();
            if (sink != null)
            {
                sink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, ComputePowerRequired());
                sink.Update();
            }

            // Decrease cooldown timer if it's greater than 0
            if (jumpCooldownTimer > 0)
            {
                jumpCooldownTimer--;
            }
        }

        private float ComputePowerRequired()
        {
            if (!block.IsWorking || !block.IsFunctional || jumpCooldownTimer <= 0)
                return 0f;

            // You can add your power calculation logic here.
            // For example, you can return a constant value or calculate it based on some conditions.

            return 300.0f; // Return a constant value for demonstration purposes.
        }

        private static void CreateTerminalControls()
        {
            MyLog.Default.WriteLineAndConsole("CreateTerminalControls method called");
            controlsCreated = true;

            var blinkDriveButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCollector>("BlinkDrive_ActivateButton");
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
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(blinkDriveButton);

            var blinkDriveCockpitAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("BlinkDriveActivate");
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
                    if (!drive.CanJump() && drive.jumpCooldownTimer <= 0) // Check if cooldown is not in progress
                    {
                        sb.Append("NO POWER");
                    }
                    else
                    {
                        int secondsRemaining = Math.Max(drive.jumpCooldownTimer / 60, 0); // Convert frames to seconds
                        sb.Append($"CD {secondsRemaining}s");
                    }
                }
                else
                {
                    sb.Append("Unable to retrieve cooldown information.");
                }
            };
            blinkDriveCockpitAction.Enabled = (b) => b.GameLogic is BlinkDrive;

            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(blinkDriveCockpitAction);
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
