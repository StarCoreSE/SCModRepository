using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Invalid.BlinkDrive
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "BlinkDriveLarge")]
    public class BlinkDrive : MyGameLogicComponent
    {
        private IMyCollector block;
        private MySync<bool, SyncDirection.BothWays> requestJumpSync;
        private int[] jumpCooldownTimers = new int[3];
        private const int rechargeTime = 60 * 60; // 10 seconds * 60 frames per second

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

            // Check if any charge is available
            foreach (int timer in jumpCooldownTimers)
            {
                if (timer <= 0)
                    return true;
            }

            return false;
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

        private Vector3D originalPosition;
        private Vector3D teleportPosition;

        private void PerformBlink()
        {
            Vector3D forwardVector = block.WorldMatrix.Forward;
            originalPosition = block.CubeGrid.WorldMatrix.Translation; // Original position
            teleportPosition = originalPosition + (forwardVector * 1000); // Teleported position

            MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;
            (block.CubeGrid as MyEntity).Teleport(newWorldMatrix);

            // Play particle effects at both original and teleported positions
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("HESFAST", originalPosition);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("HESFAST", teleportPosition);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("InvalidCustomBlinkParticleEnter", originalPosition);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("InvalidCustomBlinkParticleLeave", teleportPosition);

            // Reset jump request and start cooldown
            ResetJumpRequest();
            StartRechargeTimer();
        }

        private void StartRechargeTimer()
        {
            for (int i = 0; i < jumpCooldownTimers.Length; i++)
            {
                if (jumpCooldownTimers[i] <= 0)
                {
                    jumpCooldownTimers[i] = rechargeTime;
                    break;
                }
            }
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

            // Decrease cooldown timers if they're greater than 0
            for (int i = 0; i < jumpCooldownTimers.Length; i++)
            {
                if (jumpCooldownTimers[i] > 0)
                {
                    jumpCooldownTimers[i]--;
                }
            }
        }

        private float ComputePowerRequired()
        {
            float powerRequired = 0f;

            // Calculate power required for each charge being recharged
            foreach (int timer in jumpCooldownTimers)
            {
                if (timer > 0 && block.IsWorking && block.IsFunctional)
                {
                    // Power is consumed while recharging
                    powerRequired += 100f; // Assuming each charge consumes 100 MW
                }
            }

            return powerRequired;
        }

        private static void CreateTerminalControls()
        {
            MyLog.Default.WriteLineAndConsole("CreateTerminalControls method called");

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
                    int chargesRemaining = 0;
                    int nextCooldown = -1;
                    for (int i = 0; i < drive.jumpCooldownTimers.Length; i++)
                    {
                        int timer = drive.jumpCooldownTimers[i];
                        if (timer <= 0)
                        {
                            chargesRemaining++;
                        }
                        else if (nextCooldown == -1 || timer < nextCooldown)
                        {
                            // Find the shortest cooldown time
                            nextCooldown = timer;
                        }
                    }
                    if (nextCooldown != -1)
                    {
                        // Display cooldown time remaining for the next charge
                        sb.Append($"{nextCooldown / 60}s  ");
                    }
                    sb.Append($"C:{chargesRemaining}");
                }
                else
                {
                    sb.Append("Unable to retrieve charge information.");
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
