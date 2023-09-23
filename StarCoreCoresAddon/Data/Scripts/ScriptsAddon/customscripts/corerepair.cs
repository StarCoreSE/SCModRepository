using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRageMath;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace StarCoreCoreRepair
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "SC_LightCore", "SC_MediumCore", "SC_MediumCore_2x2", "SC_HeavyCore", "SC_HeavyCore_3x3x3", "SC_AdminCore", "SC_SGDroneCore")]
    public class StarCoreCoreRepair : MyGameLogicComponent, IMyEventProxy
    {
        MySync<float, SyncDirection.BothWays> blockDamageModifierSync = null;
        private IMyCollector shipCore;
        private IMyHudNotification notifStatus = null;
        private DateTime repairStartTime;
        private TimeSpan repairDelay = TimeSpan.FromSeconds(30);
        private bool repairTimerActive = false;
        private bool userHasControl = true;  // New flag to track if the user has control

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // Other existing code
            //if (!MyAPIGateway.Session.IsServer) return;
            shipCore = Entity as IMyCollector;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            SetStatus($"StarCoreCoreRepair Initialized", 5000, MyFontEnum.Green);

            // Attach event handler after API has initialized blockDamageModifierSync
            if (blockDamageModifierSync != null)
            {
                blockDamageModifierSync.ValueChanged += BlockDamageModifierSync_ValueChanged;
            }
            else
            {
                // Handle or log that blockDamageModifierSync is null.
            }
        }



        private void BlockDamageModifierSync_ValueChanged(MySync<float, SyncDirection.BothWays> obj)
        {
            shipCore.SlimBlock.BlockGeneralDamageModifier = obj.Value;
        }


        public override void UpdateOnceBeforeFrame()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        private bool? lastFunctionalState = null;
        private bool allowPowerGeneration = true;

        private int tickCounter = 0;  // Add this line to your class fields
        private const int TICKS_PER_SECOND = 60;  // Assuming 60 ticks per second

        public override void UpdateAfterSimulation()
        {
            tickCounter++;
            if (tickCounter % TICKS_PER_SECOND != 0)
            {
                return;
            }

            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;

            bool isFunctionalNow = shipCore.IsFunctional;

            if (lastFunctionalState != isFunctionalNow)
            {
                lastFunctionalState = isFunctionalNow;

                if (isFunctionalNow)
                {
                    float newModifier = 1.0f; // New value for BlockGeneralDamageModifier
                    shipCore.SlimBlock.BlockGeneralDamageModifier = newModifier;
                    blockDamageModifierSync.Value = newModifier;  // Update MySync variable

                    SetStatus($"Core is functional.", 2000, MyFontEnum.Green);
                    repairTimerActive = false;
                    allowPowerGeneration = true;
                    userHasControl = true;
                }
                else
                {
                    float newModifier = 0.01f; // New value for BlockGeneralDamageModifier
                    shipCore.SlimBlock.BlockGeneralDamageModifier = newModifier;
                    blockDamageModifierSync.Value = newModifier;  // Update MySync variable

                    SetStatus($"Core is non-functional. Repair timer started.", 2000, MyFontEnum.Red);
                    repairStartTime = DateTime.UtcNow;
                    repairTimerActive = true;
                    allowPowerGeneration = false;
                    userHasControl = false;
                }
            }

            if (repairTimerActive)
            {
                TimeSpan timeRemaining = repairDelay - (DateTime.UtcNow - repairStartTime);
                SetStatus($"Time until core repair: {timeRemaining.TotalSeconds:F0} seconds.", 1000, MyFontEnum.Red);

                if (timeRemaining <= TimeSpan.Zero)
                {
                    DoRepair();
                    repairTimerActive = false;
                    allowPowerGeneration = true;
                    userHasControl = true;
                }
            }

            ForceEnabledState(isFunctionalNow);
        }



        private void ForceEnabledState(bool isFunctional)
        {
            if (isFunctional)
            {
                shipCore.Enabled = true;
                //SetStatus($"Core forced ON due to functionality.", 2000, MyFontEnum.Green);
                TogglePowerGenerationBlocks(true);
            }
            else
            {
                shipCore.Enabled = false;
                //SetStatus($"Core forced OFF due to non-functionality.", 2000, MyFontEnum.Red);
                TogglePowerGenerationBlocks(false);
            }
        }

        private void TogglePowerGenerationBlocks(bool enable)
        {
            var blocks = new List<IMySlimBlock>();
            shipCore.CubeGrid.GetBlocks(blocks);

            foreach (var block in blocks)
            {
                var functionalBlock = block.FatBlock as IMyFunctionalBlock;

                if (functionalBlock != null && functionalBlock != shipCore)
                {
                    // Check if the block is a battery or reactor
                    if (functionalBlock is IMyBatteryBlock || functionalBlock is IMyReactor)
                    {
                        if (userHasControl)  // Check if the user has control over the power blocks
                        {
                            functionalBlock.Enabled = enable;
                        }
                        else  // If not, forcibly turn off the power blocks
                        {
                            functionalBlock.Enabled = false;
                        }
                    }
                }
            }
            //string statusMessage = enable ? "enabled" : "forced off";
            //SetStatus($"All power generation blocks on grid {statusMessage}.", 2000, enable ? MyFontEnum.Green : MyFontEnum.Red);
        }




        // Add this field to your class to store the original owner ID.

        private void DoRepair()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;

            IMySlimBlock slimBlock = shipCore.SlimBlock;
            if (slimBlock == null) return;

            // Fetch the original owner ID of the grid.
            long gridOwnerId = shipCore.CubeGrid.BigOwners.Count > 0 ? shipCore.CubeGrid.BigOwners[0] : 0;

            // If the grid has an owner, proceed with repair and ownership change.
            if (gridOwnerId != 0)
            {
                float repairAmount = 9999;
                slimBlock.IncreaseMountLevel(repairAmount, 0L, null, 0f, false, MyOwnershipShareModeEnum.Faction);

                // Try casting to MyCubeBlock and change the owner.
                MyCubeBlock cubeBlock = shipCore as MyCubeBlock;
                if (cubeBlock != null)
                {
                    cubeBlock.ChangeBlockOwnerRequest(gridOwnerId, MyOwnershipShareModeEnum.Faction);
                }

                SetStatus($"Core repaired.", 2000, MyFontEnum.Green);
            }
            else
            {
                // Handle the case where the grid has no owner.
                SetStatus($"Core could not be repaired: Grid has no owner.", 2000, MyFontEnum.Red);
            }
        }



        private void SetStatus(string text, int aliveTime = 300, string font = MyFontEnum.Green)
        {
            if (notifStatus == null)
                notifStatus = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);

            notifStatus.Hide();
            notifStatus.Font = font;
            notifStatus.Text = text;
            notifStatus.AliveTime = aliveTime;
            notifStatus.Show();
        }

        public override void Close()
        {
            // Cleanup logic here, if necessary
        }
    }
}
