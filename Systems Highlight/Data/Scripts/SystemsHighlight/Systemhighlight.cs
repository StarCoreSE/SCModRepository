using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;

namespace StarCore.SystemHighlight
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SubsystemHighlight : MySessionComponentBase
    {
        private Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>> highlightedEntitiesPerGrid = new Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>>();

        private IMyHudNotification notifStatus = null;

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += MessageHandler;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= MessageHandler;
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

        public void MessageHandler(String message, ref bool sendToOthers)
        {
            IMyCharacter chr = MyAPIGateway.Session?.Player?.Character;
            IMySlimBlock aimed = chr?.EquippedTool?.Components?.Get<MyCasterComponent>()?.HitBlock as IMySlimBlock;

            var gridBlocks = new List<IMySlimBlock>();
            if (aimed != null)
            {
                IMyCubeGrid cubeGrid = aimed.CubeGrid;
                aimed.CubeGrid.GetBlocks(gridBlocks);
                if (message.Contains("/hlconv"))
                {
                    HandleHighlightCommand(gridBlocks, 1, null, cubeGrid);
                }
                else if (message.Contains("/hlthrust"))
                {
                    HandleHighlightCommand(gridBlocks, 2, null, cubeGrid);
                }
                else if (message.Contains("/hlpower"))
                {
                    HandleHighlightCommand(gridBlocks, 3, null, cubeGrid);
                }
                else if (message.Contains("/hlweapon"))
                {
                    HandleHighlightCommand(gridBlocks, 4, null, cubeGrid);
                }
                else if (message.Contains("/hlcustom"))
                {
                    var customType = string.Empty;

                    if (message != null)
                    {
                        customType = new string(message.Skip(10).ToArray());
                    }
                
                    if (customType != null)
                    {
                        HandleHighlightCommand(gridBlocks, 5, customType, cubeGrid);
                    }
                
                }
                else if (message.Contains("/hlhelp"))
                {
                    MyAPIGateway.Utilities.ShowNotification("/hlconv : Highlights Conveyors", 9000);
                    MyAPIGateway.Utilities.ShowNotification("/hlthrust : Highlights Thrusters", 9000);
                    MyAPIGateway.Utilities.ShowNotification("/hlpower : Highlights Power", 9000);
                    MyAPIGateway.Utilities.ShowNotification("/hlcustom : Accepts SubtypeIDs Ex: /hlcustom MySubtypeID", 9000);
                    MyAPIGateway.Utilities.ShowNotification("/hlundo : Removes Active Highlight", 9000);
                }
                else if (message.Contains("/hlundo"))
                {
                    SetStatus("All Highlights Cleared", 3000, "Green");
                    UndoHighlights(gridBlocks, cubeGrid);
                }
            }          
            else
            {
                SetStatus("No Target", 3000, "Red");
                return;
            }
        }

        public override void UpdateAfterSimulation()
        {

        }

        public void HandleHighlightCommand(List<IMySlimBlock> blockList, int type, string customType, IMyCubeGrid cubeGrid)
        {
            if (cubeGrid == null)
                return;

            int foundCount = 0;

            UndoHighlights(blockList, cubeGrid);

            Dictionary<IMyEntity, int> gridHighlightedEntities;
            if (!highlightedEntitiesPerGrid.TryGetValue(cubeGrid, out gridHighlightedEntities))
            {
                gridHighlightedEntities = new Dictionary<IMyEntity, int>();
                highlightedEntitiesPerGrid[cubeGrid] = gridHighlightedEntities;
            }

            foreach (var block in blockList)
            {
                if (block != null)
                {
                    if (block.FatBlock != null)
                    {
                        if (type == 1 && (block.FatBlock is IMyConveyor || block.FatBlock is IMyConveyorTube || block.FatBlock is IMyConveyor))
                        {
                            SetStatus($"Highlight set. Type /hlundo to Clear.", 3000, "Green");

                            if (!gridHighlightedEntities.ContainsKey(block.FatBlock))
                            {
                                gridHighlightedEntities.Add(block.FatBlock, type);
                            }

                            if (block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: Color.Yellow);
                            }
                            else if (!block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: 3, color: Color.Red);
                            }
                            else
                                return;
                        }
                        else if (type == 2 && (block.FatBlock is IMyThrust))
                        {
                            SetStatus($"Highlight set. Type /hlundo to Clear.", 3000, "Green");

                            if (!gridHighlightedEntities.ContainsKey(block.FatBlock))
                            {
                                gridHighlightedEntities.Add(block.FatBlock, type);
                            }

                            if (block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: Color.Green);
                            }
                            else if (block.FatBlock.IsFunctional == false)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                            }
                            else
                                return;
                        }
                        else if (type == 3 && (block.FatBlock is IMyPowerProducer))
                        {
                            SetStatus($"Highlight set. Type /hlundo to Clear.", 3000, "Green");

                            if (!gridHighlightedEntities.ContainsKey(block.FatBlock))
                            {
                                gridHighlightedEntities.Add(block.FatBlock, type);
                            }

                            if (block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: Color.Blue);
                            }
                            else if (block.FatBlock.IsFunctional == false)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                            }
                            else
                                return;
                        }
                        else if (type == 4 && (block.FatBlock is IMyConveyorSorter || block.FatBlock is IMyLargeTurretBase || block.FatBlock is IMySmallGatlingGun))
                        {
                            SetStatus($"Highlight set. Type /hlundo to Clear.", 3000, "Green");

                            if (!gridHighlightedEntities.ContainsKey(block.FatBlock))
                            {
                                gridHighlightedEntities.Add(block.FatBlock, type);
                            }

                            if (block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: Color.Orange);
                            }
                            else if (block.FatBlock.IsFunctional == false)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                            }
                            else
                                return;
                        }
                        else if (type == 5)
                        {
                            if ((block.BlockDefinition.Id.SubtypeId.ToString()).ToLower() == customType.ToLower())
                            {
                                SetStatus($"Highlighting {customType}", 3000, "Green");

                                foundCount++;

                                if (!gridHighlightedEntities.ContainsKey(block.FatBlock))
                                {
                                    gridHighlightedEntities.Add(block.FatBlock, type);
                                }

                                if (block.FatBlock.IsFunctional)
                                {
                                    MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: Color.Purple);
                                }
                                else if (block.FatBlock.IsFunctional == false)
                                {
                                    MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                                }
                                else
                                    return;
                            }                               
                        }
                    }

                    if (block.FatBlock != null && !gridHighlightedEntities.ContainsKey(block.FatBlock))
                    {
                        block.Dithering = -0.5f;
                    }
                    else if (block.FatBlock != null && gridHighlightedEntities.ContainsKey(block.FatBlock))
                    {
                        block.Dithering = 0f;
                    }
                    else if (block.FatBlock == null)
                    {
                        block.Dithering = -0.5f;
                    }
                }
            }

            if (type == 5 && foundCount == 0)
            {
                SetStatus($"No Blocks of {customType} Found on Grid", 3000, "Red");
                return;
            }
        }

        public void UndoHighlights(List<IMySlimBlock> blockList, IMyCubeGrid cubeGrid)
        {
            Dictionary<IMyEntity, int> gridHighlightedEntities;
            if (highlightedEntitiesPerGrid.TryGetValue(cubeGrid, out gridHighlightedEntities))
            {
                foreach (var highlightedEntity in gridHighlightedEntities)
                {
                    MyVisualScriptLogicProvider.SetHighlightLocal(highlightedEntity.Key.Name, thickness: -1);
                }
                gridHighlightedEntities.Clear();
            }

            foreach (var block in blockList)
            {
                if (block.Dithering < 0f || block.Dithering > 0f && block.CubeGrid.Physics != null)
                {
                    block.Dithering = 0f;
                }
            }
        }
    }
}