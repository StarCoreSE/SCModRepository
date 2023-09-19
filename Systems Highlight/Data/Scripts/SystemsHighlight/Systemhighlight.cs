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
using VRage.Utils;
using VRageMath;

namespace StarCore.SystemHighlight
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SubsystemHighlight : MySessionComponentBase
    {
        private Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>> highlightedEntitiesPerGrid = new Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>>();

        private IMyHudNotification notifStatus = null;

        const string From = "SysHL";
        const string Message = "/hlhelp for list of Commands";

        bool SeenMessage = false;

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += HandleMessage;
        }

        public override void LoadData()
        {
            if (MyAPIGateway.Session.IsServer && MyAPIGateway.Utilities.IsDedicated)
                return;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= HandleMessage;
        }

        public override void UpdateAfterSimulation()
        {
            if (!SeenMessage && MyAPIGateway.Session?.Player?.Character != null)
            {
                SeenMessage = true;
                MyAPIGateway.Utilities.ShowMessage(From, Message);

                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate));
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

        public void HandleMessage(String message, ref bool sendToOthers)
        {
            var player_camera = MyAPIGateway.Session.Camera;
            var camera_matrix = player_camera.WorldMatrix;
            IHitInfo strike = null;
            IMyCubeGrid cubeGrid = null;
            IMySlimBlock final_block = null;
            MyAPIGateway.Physics.CastRay(camera_matrix.Translation, camera_matrix.Translation + camera_matrix.Forward * 150, out strike);

            var gridBlocks = new List<IMySlimBlock>();
            if (message.Contains("/hl"))
            {
                if (strike != null)
                {
                    if (strike.HitEntity is IMyCubeGrid)
                    {
                        cubeGrid = strike.HitEntity as IMyCubeGrid;

                        if (cubeGrid.Physics != null)
                        {
                            var pos = cubeGrid.WorldToGridInteger(strike.Position + camera_matrix.Forward * 0.1);
                            final_block = cubeGrid.GetCubeBlock(pos);
                        }
                    }

                    final_block.CubeGrid.GetBlocks(gridBlocks);
                    if (message.Contains("/hlconv"))
                    {
                        HandleHighlight(gridBlocks, 1, null, cubeGrid, null);
                    }
                    else if (message.Contains("/hlthrust"))
                    {
                        HandleHighlight(gridBlocks, 2, null, cubeGrid, null);
                    }
                    else if (message.Contains("/hlpower"))
                    {
                        HandleHighlight(gridBlocks, 3, null, cubeGrid, null);
                    }
                    else if (message.Contains("/hlweapon"))
                    {
                        HandleHighlight(gridBlocks, 4, null, cubeGrid, null);
                    }
                    else if (message.Contains("/hlcustomadd"))
                    {
                        var color = string.Empty;
                        var customType = string.Empty;
                        string remainingMessage = message.Substring(13);
                        int spaceIndex = remainingMessage.IndexOf(' ');

                        if (message != null)
                        {
                            if (spaceIndex >= 0)
                            {
                                customType = remainingMessage.Substring(0, spaceIndex);

                                if (spaceIndex + 1 < remainingMessage.Length)
                                {
                                    color = remainingMessage.Substring(spaceIndex + 1);
                                }
                                else
                                {
                                    color = "default";
                                }
                            }
                            else
                            {
                                customType = remainingMessage;
                                color = "default";
                            }
                        }

                        if (customType != null && color != null)
                        {
                            HandleHighlight(gridBlocks, 5, customType, cubeGrid, color);
                        }

                    }
                    else if (message.Contains("/hldamage"))
                    {
                        HandleHighlight(gridBlocks, 6, null, cubeGrid, null);
                    }
                    else if (message.Contains("/hlhelp"))
                    {
                        MyAPIGateway.Utilities.ShowMessage(From, "/hlconv : Highlights Conveyors");
                        MyAPIGateway.Utilities.ShowMessage(From, "/hlthrust : Highlights Thrusters");
                        MyAPIGateway.Utilities.ShowMessage(From, "/hlpower : Highlights Power");
                        MyAPIGateway.Utilities.ShowMessage(From, "/hlcustomadd : Adds Blocks to Current Highlight | Accepts SubtypeIDs Ex: /hlcustomadd MySubtypeID Green");
                        MyAPIGateway.Utilities.ShowMessage(From, "/hlhelp : This Command");
                        MyAPIGateway.Utilities.ShowMessage(From, "/hlcolorhelp : List of Colors Supported by /hlcustomadd");
                        MyAPIGateway.Utilities.ShowMessage(From, "/hlclear : Removes Active Highlight");
                    }
                    else if (message.Contains("/hlcolorhelp"))
                    {
                        MyAPIGateway.Utilities.ShowMessage(From, "Red : lightred/darkred/red");
                        MyAPIGateway.Utilities.ShowMessage(From, "Green : lightgreen/darkgreen/green");
                        MyAPIGateway.Utilities.ShowMessage(From, "Blue : lightblue/darkblue/blue");
                        MyAPIGateway.Utilities.ShowMessage(From, "Yellow : yellow");
                        MyAPIGateway.Utilities.ShowMessage(From, "Purple : purple");
                        MyAPIGateway.Utilities.ShowMessage(From, "White : Default if Unrecognized");
                    }
                    else if (message.Contains("/hlclear"))
                    {
                        SetStatus("All Highlights Cleared", 3000, "Green");
                        ClearHighlight(gridBlocks, cubeGrid);
                    }
                }
                else
                {
                    SetStatus("No Target", 3000, "Red");
                    return;
                }
            }
        }       

        public void HandleHighlight(List<IMySlimBlock> blockList, int type, string customType, IMyCubeGrid cubeGrid, string color)
        {
            if (cubeGrid == null)
                return;

            int foundCount = 0;

            if (type != 5)
            {
                ClearHighlight(blockList, cubeGrid);
            }          

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
                            SetStatus($"Highlighting Conveyors. Type /hlclear to Clear.", 3000, "Green");

                            HandleDictionary(gridHighlightedEntities, block, type);

                            if (block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: Color.Yellow);
                            }
                            else if (!block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                            }
                            else
                                return;
                        }
                        else if (type == 2 && (block.FatBlock is IMyThrust))
                        {
                            SetStatus($"Highlighting Thrusters. Type /hlclear to Clear.", 3000, "Green");

                            HandleDictionary(gridHighlightedEntities, block, type);

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
                            SetStatus($"Highlighting Power. Type /hlclear to Clear.", 3000, "Green");

                            HandleDictionary(gridHighlightedEntities, block, type);

                            if (block.FatBlock.IsFunctional)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: Color.SkyBlue);
                            }
                            else if (block.FatBlock.IsFunctional == false)
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                            }
                            else
                                return;
                        }
                        else if (type == 4 && (block.FatBlock is IMyConveyorSorter || block.FatBlock is IMyLargeTurretBase || block.FatBlock is IMySmallGatlingGun || block.FatBlock is IMySmallMissileLauncher || block.FatBlock is IMySmallMissileLauncherReload))
                        {
                            SetStatus($"Highlighting Weapons. Type /hlclear to Clear.", 3000, "Green");

                            HandleDictionary(gridHighlightedEntities, block, type);

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
                                SetStatus($"Highlighting {customType}. Type /hlclear to Clear.", 3000, "Green");

                                foundCount++;

                                HandleDictionary(gridHighlightedEntities, block, type);

                                if (block.FatBlock.IsFunctional)
                                {
                                    var colorString = HandleColor(color);
                                    MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 3, pulseTimeInFrames: -1, color: colorString);
                                }
                                else if (block.FatBlock.IsFunctional == false)
                                {
                                    MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                                }
                                else
                                    return;              
                            }                               
                        }
                        else if (type == 6 && !block.FatBlock.IsFunctional)
                        {
                            HandleDictionary(gridHighlightedEntities, block, type);

                            MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);
                        }
                    }

                    HandleDithering(gridHighlightedEntities, block);
                }
            }

            if (type == 5 && foundCount == 0)
            {
                SetStatus($"No Blocks of {customType} Found on Grid", 3000, "Red");
                return;
            }
        }

        public static void HandleDictionary(Dictionary<IMyEntity, int> gridHighlightedEntities, IMySlimBlock block, int type)
        {
            if (!gridHighlightedEntities.ContainsKey(block.FatBlock))
            {
                gridHighlightedEntities.Add(block.FatBlock, type);
            }
        }

        public static void HandleDithering(Dictionary<IMyEntity, int> gridHighlightedEntities, IMySlimBlock block)
        {
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

        public static Color HandleColor(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "red":
                    return Color.Red;
                case "green":
                    return Color.Green;
                case "blue":
                    return Color.Blue;              
                case "darkred":
                    return Color.DarkRed;
                case "darkgreen":
                    return Color.DarkGreen;
                case "darkblue":
                    return Color.DarkBlue;
                case "lightred":
                    return Color.Pink;
                case "lightgreen":
                    return Color.LightGreen;
                case "lightblue":
                    return Color.LightBlue;
                case "yellow":
                    return Color.Yellow;
                case "purple":
                    return Color.Purple;
                default:
                    return Color.White;
            }
        }

        public void ClearHighlight(List<IMySlimBlock> blockList, IMyCubeGrid cubeGrid)
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