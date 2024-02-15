using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Invalid.BlinkDrive
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class BlinkDriveSession : MySessionComponentBase
    {
        public static BlinkDriveSession Instance;
        public Action<long> Update = (x) => { };


        public override void LoadData()
        {
            MyLog.Default.WriteLineAndConsole("BlinkDriveSession LoadData method called");

            Instance = this;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(63547, HandleMessage);
        }

        public void requestjump(long entityid)
        {
            MyAPIGateway.Multiplayer.SendMessageToOthers(63547, MyAPIGateway.Utilities.SerializeToBinary(entityid));
        }

        private void HandleMessage(ushort id, byte[] obj, ulong senderId, bool isServer)
        {
            long entityid = MyAPIGateway.Utilities.SerializeFromBinary<long>(obj);
            Update?.Invoke(entityid);
        }

        protected override void UnloadData()
        {
            Instance = null;
            MyLog.Default.WriteLineAndConsole("BlinkDriveSession UnloadData method called");
        }
    }


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "BlinkDriveCustomLarge")]
    public class BlinkDrive : MyGameLogicComponent, IMyEventProxy
    {
        private IMyCubeBlock block;
        static bool controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                block = Entity as IMyCubeBlock;
                if (block == null)
                {
                    MyLog.Default.WriteLineAndConsole("Entity is not a cube block. Cannot initialize BlinkDrive component.");
                    return;
                }

                MyLog.Default.WriteLineAndConsole("Init method called");

                if (BlinkDriveSession.Instance != null)
                {
                    BlinkDriveSession.Instance.Update += Update;
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole("BlinkDriveSession is null");
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"An error occurred during BlinkDrive initialization: {ex}");
            }
        }

        private void Update(long obj)
        {
            if (obj == block.EntityId)
            {
                MyLog.Default.WriteLineAndConsole("PerformBlink method called");
                // Calculate the forward vector and the target teleportation position
                Vector3D forwardVector = block.WorldMatrix.Forward;
                Vector3D teleportPosition = block.CubeGrid.WorldMatrix.Translation + (forwardVector * 1000); // 1km forward

                // Construct the new world matrix for the grid at the teleportation position
                MatrixD newWorldMatrix = block.CubeGrid.WorldMatrix;
                newWorldMatrix.Translation = teleportPosition;

                // Perform the teleportation
                block.CubeGrid.Teleport(newWorldMatrix);
                MyLog.Default.WriteLineAndConsole("Teleporting to " + teleportPosition.ToString());

                // Notify the player that the blink drive has been activated
                MyAPIGateway.Utilities.ShowNotification("Blink Drive Activated!", 2000, "Green");
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

            // Create a terminal button for activating the Blink Drive
            var blinkDriveButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>("BlinkDrive_ActivateButton");
            blinkDriveButton.Enabled = (b) => b != null && b.GameLogic != null && b.GameLogic is BlinkDrive && b.GameLogic.GetAs<BlinkDrive>() != null;
            blinkDriveButton.Visible = (b) => b != null && b.GameLogic != null && b.GameLogic is BlinkDrive && b.GameLogic.GetAs<BlinkDrive>() != null;
            blinkDriveButton.Title = MyStringId.GetOrCompute("Activate Blink Drive");
            blinkDriveButton.Tooltip = MyStringId.GetOrCompute("Activates the Blink Drive for a single jump.");
            blinkDriveButton.Action = (b) =>
            {
                if (b?.GameLogic != null)
                {
                    var drive = b.GameLogic.GetAs<BlinkDrive>();
                    if (drive != null)
                    {
                        MyLog.Default.WriteLineAndConsole("Button action called");
                        BlinkDriveSession.Instance.requestjump(b.EntityId);
                    }
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole("Terminal block's GameLogic is null.");
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(blinkDriveButton);

            // Now, add an action for cockpit activation
            var blinkDriveCockpitAction = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDriveActivate");
            blinkDriveCockpitAction.Name = new StringBuilder("Activate Blink Drive");
            blinkDriveCockpitAction.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
            blinkDriveCockpitAction.Action = b =>
            {
                if (b?.GameLogic != null)
                {
                    var drive = b.GameLogic.GetAs<BlinkDrive>();
                    if (drive != null)
                    {
                        MyLog.Default.WriteLineAndConsole("Cockpit action called");
                        BlinkDriveSession.Instance.requestjump(b.EntityId);
                    }
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole("Terminal block's GameLogic is null.");
                }
            };
            blinkDriveCockpitAction.Writer = (b, sb) =>
            {
                sb.Append("Activates the Blink Drive for a single jump.");
            };
            blinkDriveCockpitAction.Enabled = b => b != null && b.GameLogic != null && b.GameLogic is BlinkDrive && b.GameLogic.GetAs<BlinkDrive>() != null;

            MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(blinkDriveCockpitAction);
        }


        public override void Close()
        {
            MyLog.Default.WriteLineAndConsole("Close method called");

            if (BlinkDriveSession.Instance != null)
            {
                BlinkDriveSession.Instance.Update -= Update;
            }
            else
            {
                MyLog.Default.WriteLineAndConsole("BlinkDriveSession is null");
            }
        }
    }
}
