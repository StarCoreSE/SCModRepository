using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
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

            var blinkDriveActivation = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyUpgradeModule>("BlinkDrive_Activate");
            blinkDriveActivation.Enabled = (b) => b.GameLogic is BlinkDrive;
            blinkDriveActivation.Visible = (b) => b.GameLogic is BlinkDrive;
            blinkDriveActivation.Title = MyStringId.GetOrCompute("Activate Blink Drive");
            blinkDriveActivation.Getter = (b) => (b.GameLogic.GetAs<BlinkDrive>()?.activateBlink.Value) ?? false;
            blinkDriveActivation.Setter = (b, v) =>
            {
                var drive = b.GameLogic.GetAs<BlinkDrive>();
                if (drive != null)
                {
                    drive.activateBlink.Value = v; // Syncs the value
                }
            };
            blinkDriveActivation.OnText = MyStringId.GetOrCompute("On");
            blinkDriveActivation.OffText = MyStringId.GetOrCompute("Off");
            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(blinkDriveActivation);
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
