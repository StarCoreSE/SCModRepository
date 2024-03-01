using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Game;
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
using VRageRender;

namespace Invalid.BlinkDrive
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "BlinkDriveLarge")]
    public class BlinkDrive : MyGameLogicComponent, IMyEventProxy
    {
        private IMyCollector block;
        private MyResourceSinkComponent sink;
        private MySync<ushort, SyncDirection.BothWays> JumpChargesSync;
        private ushort CachedJumpCharges = MaxCharges;
        private MySync<float, SyncDirection.FromServer> JumpTimerSync;
        private const int RechargeTimeSeconds = 5;
        private const int MaxCharges = 3;
        private float GetPowerDraw()
        {
            if (JumpTimerSync > 0)
                return 100;
            return 0.25f;
        }

        static bool controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;

            block = (IMyCollector)Entity;

            if (MyAPIGateway.Session.IsServer)
            {
                JumpChargesSync.Value = MaxCharges;
                JumpChargesSync.ValueChanged += JumpChargesSync_ValueChanged;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block == null || block.CubeGrid == null)
                return;

            if (block.CubeGrid.Physics == null)
                return;

            sink = Entity.Components.Get<MyResourceSinkComponent>();
            sink?.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, GetPowerDraw);
            sink?.Update();

            if (!controlsCreated)
            {
                CreateTerminalControls();
                controlsCreated = true;
            }
        }

        private void JumpChargesSync_ValueChanged(MySync<ushort, SyncDirection.BothWays> obj)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                if (CanJump && CachedJumpCharges - 1 == obj.Value)
                {
                    //MyLog.Default.WriteLineAndConsole("Server received jump request. Charges: " + obj.Value);
                    PerformBlink();
                }

                if (JumpTimerSync.Value <= 0 && obj.Value < MaxCharges) // Start timer if isn't currently running
                {
                    StartRechargeTimer();
                }
            }
        }

        private bool CanJump => block.IsWorking && block.IsFunctional && MaxCharges > 0;

        private void PerformBlink()
        {
            MatrixD originalMatrixDir = block.WorldMatrix;
            var originalPosition = block.CubeGrid.WorldMatrix.Translation; // Original position
            var teleportPosition = originalPosition + (originalMatrixDir.Forward * 1000); // Teleported position

            MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;

            (block.CubeGrid as MyEntity).Teleport(newWorldMatrix);

            MyParticleEffect hate;
            MyParticlesManager.TryCreateParticleEffect("Blink_Test_Open", ref originalMatrixDir, ref originalPosition, 0, out hate);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("HESFAST", originalPosition);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("HESFAST", teleportPosition);
            MyVisualScriptLogicProvider.CreateParticleEffectAtEntity("Blink_Test_Close", Entity.Name); // i am lazy -aristeas

            CachedJumpCharges = JumpChargesSync.Value;
        }

        private void StartRechargeTimer()
        {
            if (JumpChargesSync.Value >= MaxCharges)
                return;
            if (JumpTimerSync.Value <= 0)
                JumpTimerSync.Value = RechargeTimeSeconds;

            sink?.Update();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!MyAPIGateway.Session.IsServer || block == null || block.CubeGrid == null)
                return;

            if (block.CubeGrid.Physics == null)
                return;

            if (JumpTimerSync.Value > 0)
            {
                JumpTimerSync.Value -= 1/60f;
            }
            else if (JumpChargesSync.Value < MaxCharges)
            {
                JumpChargesSync.Value++;
                CachedJumpCharges = JumpChargesSync.Value;
                if (JumpTimerSync.Value <= 0 && JumpChargesSync.Value < MaxCharges)
                    JumpTimerSync.Value = RechargeTimeSeconds;

                Entity.Components.Get<MyResourceSinkComponent>()?.Update();
            }
        }

        private static void CreateTerminalControls()
        {
            MyLog.Default.WriteLineAndConsole("Blinkdrive CreateTerminalControls method called");

            var blinkDriveButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCollector>("BlinkDrive_ActivateButton");
            blinkDriveButton.Enabled = (b) => b.GameLogic is BlinkDrive;
            blinkDriveButton.Visible = (b) => b.GameLogic is BlinkDrive;
            blinkDriveButton.Title = MyStringId.GetOrCompute("Activate Blink Drive");
            blinkDriveButton.Tooltip = MyStringId.GetOrCompute("Activates the Blink Drive for a single jump.");
            blinkDriveButton.Action = (b) =>
            {
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null && drive.JumpChargesSync.Value > 0)
                {
                    MyLog.Default.WriteLineAndConsole("Button action called");
                    MyLog.Default.WriteLineAndConsole("Sending action to server...");
                    drive.JumpChargesSync.Value--;
                    drive.CachedJumpCharges = drive.JumpChargesSync.Value;
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(blinkDriveButton);

            var blinkDriveCockpitAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("BlinkDriveActivate");
            blinkDriveCockpitAction.Name = new StringBuilder("Activate Blink Drive");
            blinkDriveCockpitAction.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
            blinkDriveCockpitAction.Action = (b) =>
            {
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null && drive.JumpChargesSync.Value > 0)
                {
                    MyLog.Default.WriteLineAndConsole("Blinkdrive Cockpit action called");
                    drive.JumpChargesSync.Value--;
                    drive.CachedJumpCharges = drive.JumpChargesSync.Value;
                }
            };
            blinkDriveCockpitAction.Writer = (b, sb) =>
            {
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null)
                {
                    if (drive.JumpTimerSync.Value > 0)
                    {
                        if (drive.JumpTimerSync.Value >= 10) // Avoid cutoff in the toolbar value
                            sb.Append($"{Math.Round(drive.JumpTimerSync.Value, 0)}s ");
                        else
                            sb.Append($"{Math.Round(drive.JumpTimerSync.Value, 1)}s ");
                    }
                    sb.Append($"C:{drive.JumpChargesSync.Value}");
                }
                else
                {
                    sb.Append("Unable to retrieve charge information.");
                }
            };
            blinkDriveCockpitAction.Enabled = (b) => b.GameLogic is BlinkDrive;

            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(blinkDriveCockpitAction);
        }

        public override void Close()
        {
            base.Close();
            if (JumpChargesSync != null)
            {
                JumpChargesSync.ValueChanged -= JumpChargesSync_ValueChanged;
            }
        }
    }
}
