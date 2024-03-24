using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
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
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace Invalid.BlinkDrive
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "BlinkDriveLarge")]
    public class BlinkDrive : MyGameLogicComponent, IMyEventProxy
    {
        #region Variables
        public const float JumpDistance = 1000;

        private IMyCollector Block;
        private MyResourceSinkComponent SinkComponent;
        private MySync<ushort, SyncDirection.BothWays> JumpChargesSync;
        private ushort CachedJumpCharges = MaxCharges;
        private MySync<float, SyncDirection.FromServer> JumpTimerSync;
        private const int RechargeTimeSeconds = 60;
        private const int MaxCharges = 3;

        private float GetPowerDraw => JumpTimerSync > 0 ? 100 : 0.25f;
        private bool CanJump => Block.IsWorking && Block.IsFunctional && MaxCharges > 0;

        static bool controlsCreated = false;
        #endregion

        #region Base Methods
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;

            Block = (IMyCollector)Entity;

            if (MyAPIGateway.Session.IsServer)
            {
                JumpChargesSync.Value = MaxCharges;
                JumpChargesSync.ValueChanged += JumpChargesSync_ValueChanged;
            }
            else
            {
                JumpTimerSync.ValueChanged += JumpTimerSync_ValueChanged;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (Block == null || Block.CubeGrid == null)
                return;

            if (Block.CubeGrid.Physics == null)
                return;

            SinkComponent = Entity.Components.Get<MyResourceSinkComponent>();
            SinkComponent?.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, GetPowerDraw);

            if (!controlsCreated)
            {
                CreateTerminalControls();
                controlsCreated = true;
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!MyAPIGateway.Session.IsServer || Block == null || Block.CubeGrid == null)
                return;

            if (Block.CubeGrid.Physics == null)
                return;

            if (JumpTimerSync.Value > 0)
            {
                JumpTimerSync.Value -= 1 / 60f;
            }
            else if (JumpChargesSync.Value < MaxCharges)
            {
                JumpChargesSync.Value++;
                CachedJumpCharges = JumpChargesSync.Value;
                if (JumpTimerSync.Value <= 0 && JumpChargesSync.Value < MaxCharges)
                    JumpTimerSync.Value = RechargeTimeSeconds;

                SinkComponent?.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, GetPowerDraw);
            }
        }

        public override void Close()
        {
            base.Close();
            if (JumpChargesSync != null)
            {
                JumpChargesSync.ValueChanged -= JumpChargesSync_ValueChanged;
            }
        }
        #endregion

        #region Sync Actions
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

        private void JumpTimerSync_ValueChanged(MySync<float, SyncDirection.FromServer> obj)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            SinkComponent?.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, GetPowerDraw); // goddamn it why doesn't keen sync this
        }
        #endregion

        public void RequestBlink()
        {
            if (JumpChargesSync.Value <= 0)
                return;

            // Check from all corners to limit fuckery
            List<IHitInfo> hitInfos = new List<IHitInfo>();
            Vector3D forward = Block.WorldMatrix.Forward * JumpDistance;

            // Check for blocker plates (??)

            // Theoretically this should work to collide with blocker plates But It Doesn't:tm:
            //MyAPIGateway.Physics.CastRay(Block.CubeGrid.GetPosition(), Block.CubeGrid.GetPosition() + forward, hitInfos, CollisionLayers.NoVoxelCollisionLayer);
            //MyAPIGateway.Utilities.ShowNotification("FUCK " + hitInfos.Count, font: "Red");

            IMyEntity topGridParent = Block.CubeGrid.GetTopMostParent(typeof(MyCubeGrid));
            BoundingBoxD gridWorldAABB = Block.CubeGrid.LocalAABB;
            for (int i = 0; i < 8; i++)
            {
                Vector3D corner = Vector3D.Transform(gridWorldAABB.GetCorner(i), Block.CubeGrid.WorldMatrix);
                MyAPIGateway.Physics.CastRay(corner, corner + forward, hitInfos, CollisionLayers.DefaultCollisionLayer);

                if (hitInfos.Count > 0 && (hitInfos.Count > 1 || hitInfos[0].HitEntity.GetTopMostParent(typeof(MyCubeGrid)) != topGridParent))
                {
                    MyAPIGateway.Utilities.ShowNotification("Obstacle Detected!", font: "Red");
                    return;
                }
            }

            JumpChargesSync.Value--;
            CachedJumpCharges = JumpChargesSync.Value;
        }

        private void PerformBlink()
        {
            MatrixD originalMatrixDir = Block.WorldMatrix;
            var originalPosition = originalMatrixDir.Translation; // Original position
            var teleportPosition = originalPosition + (originalMatrixDir.Forward * JumpDistance); // Teleported position

            MatrixD newWorldMatrix = Block.CubeGrid.WorldMatrix;
            newWorldMatrix.Translation = teleportPosition;

            (Block.CubeGrid as MyEntity).Teleport(newWorldMatrix);

            //MyParticleEffect hate;
            //MyParticlesManager.TryCreateParticleEffect("Blink_Test_Open", ref originalMatrixDir, ref originalPosition, 0, out hate);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("Blink_Test_Open", originalPosition);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("HESFAST", originalPosition);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("HESFAST", teleportPosition);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("Blink_Test_Close", teleportPosition);

            CachedJumpCharges = JumpChargesSync.Value;
        }

        private void StartRechargeTimer()
        {
            if (JumpChargesSync.Value >= MaxCharges)
                return;
            if (JumpTimerSync.Value <= 0)
                JumpTimerSync.Value = RechargeTimeSeconds;

            SinkComponent?.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, GetPowerDraw);
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
                b.GameLogic.GetAs<BlinkDrive>()?.RequestBlink();
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(blinkDriveButton);

            var blinkDriveCockpitAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("BlinkDriveActivate");
            blinkDriveCockpitAction.Name = new StringBuilder("Activate Blink Drive");
            blinkDriveCockpitAction.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
            blinkDriveCockpitAction.Action = (b) =>
            {
                b.GameLogic.GetAs<BlinkDrive>()?.RequestBlink();
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
    }
}
