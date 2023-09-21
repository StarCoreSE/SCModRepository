using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace StarCoreCoreRepair
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockBeacon_LightCore", "LargeBlockBeacon_MediumCore", "LargeBlockBeacon_MediumCore_2x2", "LargeBlockBeacon_HeavyCore", "LargeBlockBeacon_HeavyCore_3x3x3")]
    public class StarCoreCoreRepair : MyGameLogicComponent
    {
        private IMyBeacon shipCore;
        private IMyHudNotification notifStatus = null;
        private DateTime repairStartTime;
        private TimeSpan repairDelay = TimeSpan.FromSeconds(30);
        private bool repairTimerActive = false;
        private bool userHasControl = true;  // New flag to track if the user has control

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            shipCore = Entity as IMyBeacon;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            SetStatus($"StarCoreCoreRepair Initialized", 5000, MyFontEnum.Green);
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        private bool? lastFunctionalState = null;
        private bool allowPowerGeneration = true;

        public override void UpdateAfterSimulation()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;

            bool isFunctionalNow = shipCore.IsFunctional;

            if (lastFunctionalState != isFunctionalNow)
            {
                lastFunctionalState = isFunctionalNow;

                if (isFunctionalNow)
                {
                    shipCore.SlimBlock.BlockGeneralDamageModifier = 1.0f;
                    SetStatus($"Core is functional.", 2000, MyFontEnum.Green);
                    repairTimerActive = false;
                    allowPowerGeneration = true;
                    userHasControl = true;  // Give control back to the user
                }
                else
                {
                    shipCore.SlimBlock.BlockGeneralDamageModifier = 0.01f;
                    SetStatus($"Core is non-functional. Repair timer started.", 2000, MyFontEnum.Red);
                    repairStartTime = DateTime.UtcNow;
                    repairTimerActive = true;
                    allowPowerGeneration = false;
                    userHasControl = false;  // Take control away from the user
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
                    userHasControl = true;  // Give control back to the user when repaired
                }
            }

            ForceEnabledState(isFunctionalNow);
        }

        private void ForceEnabledState(bool isFunctional)
        {
            if (isFunctional)
            {
                shipCore.Enabled = true;
                SetStatus($"Core forced ON due to functionality.", 2000, MyFontEnum.Green);
                TogglePowerGenerationBlocks(true);
            }
            else
            {
                shipCore.Enabled = false;
                SetStatus($"Core forced OFF due to non-functionality.", 2000, MyFontEnum.Red);
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
            string statusMessage = enable ? "enabled" : "forced off";
            SetStatus($"All power generation blocks on grid {statusMessage}.", 2000, enable ? MyFontEnum.Green : MyFontEnum.Red);
        }




        private void DoRepair()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;

            IMySlimBlock slimBlock = shipCore.SlimBlock;
            if (slimBlock == null) return;

            float repairAmount = 9999;
            slimBlock.IncreaseMountLevel(repairAmount, 0L, null, 0f, false, MyOwnershipShareModeEnum.Faction);
            SetStatus($"Core repaired.", 2000, MyFontEnum.Green);
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
