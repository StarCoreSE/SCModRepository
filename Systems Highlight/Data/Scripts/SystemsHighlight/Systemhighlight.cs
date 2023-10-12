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
using VRageRender;
using static VRageRender.MyBillboard;

namespace StarCore.SystemHighlight
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SubsystemHighlight : MySessionComponentBase
    {
        private Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>> highlightedEntitiesPerGrid = new Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>>();
        private List<MyBillboard> persistBillboard = new List<MyBillboard>();
        private IMyHudNotification notifStatus = null;
        private IMyHudNotification notifDebug = null;

        const string From = "SysHL";
        const string Message = "/hlhelp for list of Commands";

        private bool DebugToggle = false;
        private bool needsDoubleExecution = false;
        private bool SeenMessage = false;      

        #region Overrides
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
        #endregion

        #region HandleMessages
        public void HandleMessage(String message, ref bool sendToOthers)
        {
            IHitInfo strike;
            IMyCubeGrid cubeGrid;
            List<IMySlimBlock> gridBlocks;
            HandleRaycastAndGetGrid(out strike, out cubeGrid, out gridBlocks);

            if (message.Contains("/hl") && strike != null && cubeGrid != null && gridBlocks != null)
            {
                if (message.Contains("/hlconv"))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"HandleMessage: Conveyor Highlight Triggered | Type 1");
                    }
                    HandleHighlight(gridBlocks, 1, null, cubeGrid, null);
                }
                else if (message.Contains("/hlthrust"))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"HandleMessage: Thrust Highlight Triggered | Type 2");
                    }
                    HandleHighlight(gridBlocks, 2, null, cubeGrid, null);
                }
                else if (message.Contains("/hlpower"))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"HandleMessage: Power Highlight Triggered | Type 3");
                    }
                    HandleHighlight(gridBlocks, 3, null, cubeGrid, null);
                }
                else if (message.Contains("/hlweapon"))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"HandleMessage: Weapon Highlight Triggered | Type 4");
                    }
                    HandleHighlight(gridBlocks, 4, null, cubeGrid, null);
                }
                else if (message.Contains("/hlcustomadd"))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"HandleMessage: Custom Highlight Triggered | Type 5");
                    }
                    HandleCustomHighlight(message, gridBlocks, cubeGrid);
                    needsDoubleExecution = true;
                }
                else if (message.Contains("/hldamage"))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"HandleMessage: Damage Highlight Triggered | Type 6");
                    }
                    HandleHighlight(gridBlocks, 6, null, cubeGrid, null);
                }
                else if (message.Contains("/hlhelp"))
                    HandleHelpCommand();
                else if (message.Contains("/hlcolorhelp"))
                    HandleColorHelpCommand();
                else if (message.Contains("/hlclear"))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"HandleMessage: Highlight Clear Triggered");
                    }
                    SetStatus("All Highlights Cleared", 3000, "Green");
                    ClearHighlight(gridBlocks, cubeGrid);
                }
                else if (message.Contains("/hldebug"))
                {
                    HandleToggleDebug();
                }
            }
            else if (message.Contains("/hl") && strike == null)
            {
                SetStatus("No Target", 3000, "Red");
            }
            else
            {
                return;
            }

            if (needsDoubleExecution)
            {
                needsDoubleExecution = false;

                HandleCustomHighlight(message, gridBlocks, cubeGrid);
            }
        }

        private void HandleHelpCommand()
        {
            MyAPIGateway.Utilities.ShowMessage(From, "/hlconv : Highlights Conveyors");
            MyAPIGateway.Utilities.ShowMessage(From, "/hlthrust : Highlights Thrusters");
            MyAPIGateway.Utilities.ShowMessage(From, "/hlpower : Highlights Power");
            MyAPIGateway.Utilities.ShowMessage(From, "/hlweapon : Highlights Weapons");
            MyAPIGateway.Utilities.ShowMessage(From, "/hlcustomadd : Adds Blocks to Current Highlight | Accepts SubtypeIDs Ex: /hlcustomadd MySubtypeID Green");
            MyAPIGateway.Utilities.ShowMessage(From, "/hlhelp : This Command");
            MyAPIGateway.Utilities.ShowMessage(From, "/hlcolorhelp : List of Colors Supported by /hlcustomadd");
            MyAPIGateway.Utilities.ShowMessage(From, "/hlclear : Removes Active Highlight");
            MyAPIGateway.Utilities.ShowMessage(From, "/hldamage : Highlights All Non-Functional Blocks");
            MyAPIGateway.Utilities.ShowMessage(From, "/hldebug : Toggles Debug Mode");
        }

        private void HandleColorHelpCommand()
        {
            MyAPIGateway.Utilities.ShowMessage(From, "Red : lightred/darkred/red");
            MyAPIGateway.Utilities.ShowMessage(From, "Green : lightgreen/darkgreen/green");
            MyAPIGateway.Utilities.ShowMessage(From, "Blue : lightblue/darkblue/blue");
            MyAPIGateway.Utilities.ShowMessage(From, "Yellow : yellow");
            MyAPIGateway.Utilities.ShowMessage(From, "Purple : purple");
            MyAPIGateway.Utilities.ShowMessage(From, "White : Default if Unrecognized");
        }

        private void HandleCustomHighlight(string message, List<IMySlimBlock> gridBlocks, IMyCubeGrid cubeGrid)
        {
            var color = "default";
            var customType = string.Empty;

            string remainingMessage = message.Substring(13);
            int spaceIndex = remainingMessage.IndexOf(' ');

            if (spaceIndex >= 0)
            {
                customType = remainingMessage.Substring(0, spaceIndex);

                if (spaceIndex + 1 < remainingMessage.Length)
                    color = remainingMessage.Substring(spaceIndex + 1);
            }
            else
            {
                customType = remainingMessage;
            }

            if (customType != null && color != null)
            {
                HandleHighlight(gridBlocks, 5, customType, cubeGrid, color);
            }

            if (DebugToggle)
            {
                Log.Info($"HandleCustomHighlight: customsubtypeid: {customType}");
                Log.Info($"HandleCustomHighlight: color: {color}");
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
                        if (type == 1 && (block.FatBlock is IMyConveyor || block.FatBlock is IMyConveyorTube || block.FatBlock is IMyConveyor) && block != null)
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

                            if (DebugToggle)
                            {
                                Log.Info($"HandleConveyorHighlight: block being highlighted: {block.FatBlock.Name}");
                            }
                        }
                        else if (type == 2 && (block.FatBlock is IMyThrust) && block != null)
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

                            if (DebugToggle)
                            {
                                Log.Info($"HandleThrustHighlight: block being highlighted: {block.FatBlock.Name}");
                            }
                        }
                        else if (type == 3 && (block.FatBlock is IMyPowerProducer) && block != null)
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

                            if (DebugToggle)
                            {
                                Log.Info($"HandlePowerHighlight: block being highlighted: {block.FatBlock.Name}");
                            }
                        }
                        else if (type == 4 && (block.FatBlock is IMyConveyorSorter || block.FatBlock is IMyLargeTurretBase || block.FatBlock is IMySmallGatlingGun || block.FatBlock is IMySmallMissileLauncher || block.FatBlock is IMySmallMissileLauncherReload) && block != null)
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

                            if (DebugToggle)
                            {
                                Log.Info($"HandleWeaponHighlight: block being highlighted: {block.FatBlock.Name}");
                            }
                        }
                        else if (type == 5)
                        {
                            if ((block.BlockDefinition.Id.SubtypeId.ToString()).ToLower() == customType.ToLower() && block != null)
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

                                if (DebugToggle)
                                {
                                    Log.Info($"HandleCustomHighlight: block being highlighted: {block.FatBlock.Name}");
                                    Log.Info($"HandleCustomHighlight: blocks found: {foundCount}");

                                }
                            }                               
                        }
                        else if (type == 6 && !block.FatBlock.IsFunctional && block != null)
                        {
                            HandleDictionary(gridHighlightedEntities, block, type);

                            MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness: 6, pulseTimeInFrames: 3, color: Color.Red);

                            if (DebugToggle)
                            {
                                Log.Info($"HandleDamageHighlight: damaged block being highlighted: {block.FatBlock.Name}");
                            }
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

        public void ClearHighlight(List<IMySlimBlock> blockList, IMyCubeGrid cubeGrid)
        {
            Dictionary<IMyEntity, int> gridHighlightedEntities;
            if (highlightedEntitiesPerGrid.TryGetValue(cubeGrid, out gridHighlightedEntities))
            {
                foreach (var highlightedEntity in gridHighlightedEntities)
                {
                    if (DebugToggle)
                    {
                        Log.Info($"Clearing Highlight: {highlightedEntity.Key.Name} ");
                    }
                    MyVisualScriptLogicProvider.SetHighlightLocal(highlightedEntity.Key.Name, thickness: -1);
                }
                gridHighlightedEntities.Clear();
                if (DebugToggle)
                {
                    Log.Info($"Highlight Dictionary Cleared ");
                }
            }

            foreach (var block in blockList)
            {
                if (block.Dithering < 0f || block.Dithering > 0f && block.CubeGrid.Physics != null)
                {
                    block.Dithering = 0f;
                }
            }
        }
        #endregion

        #region Utilities
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

        private void DebugStatus(string text, int aliveTime = 300, string font = MyFontEnum.Green)
        {
            if (notifDebug == null)
                notifDebug = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);

            notifDebug.Hide();
            notifDebug.Font = font;
            notifDebug.Text = text;
            notifDebug.AliveTime = aliveTime;
            notifDebug.Show();
        }

        private void HandleRaycastAndGetGrid(out IHitInfo strike, out IMyCubeGrid cubeGrid, out List<IMySlimBlock> gridBlocks)
        {
            var player_camera = MyAPIGateway.Session.Camera;
            var camera_matrix = player_camera.WorldMatrix;
            strike = null;
            cubeGrid = null;
            IMySlimBlock final_block = null;
            MyAPIGateway.Physics.CastRay(camera_matrix.Translation, camera_matrix.Translation + camera_matrix.Forward * 150, out strike);

            if (DebugToggle)
            {
                Log.Info("Draw Point");
                Color color = Color.GreenYellow;
                var refcolor = color.ToVector4();
                MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("WhiteDot"), refcolor, strike.Position, 1f, 0f, -1, BlendTypeEnum.SDR, persistBillboard);
            }

            gridBlocks = new List<IMySlimBlock>();

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

                if (DebugToggle)
                {
                    DebugStatus($"Raycast Target: {final_block?.CubeGrid.DisplayName}", 6000, "Green");
                    Log.Info($"Raycast Target: {final_block?.CubeGrid.DisplayName}");
                }

                final_block?.CubeGrid.GetBlocks(gridBlocks);
            }
        }

        public void HandleDictionary(Dictionary<IMyEntity, int> gridHighlightedEntities, IMySlimBlock block, int type)
        {
            if (!gridHighlightedEntities.ContainsKey(block.FatBlock))
            {
                gridHighlightedEntities.Add(block.FatBlock, type);

                if (DebugToggle)
                {
                    Log.Info($"Adding Block to Dictionary: {block.FatBlock}");
                }
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

        public void HandleToggleDebug()
        {
            if (!DebugToggle)
            {
                DebugToggle = true;
                DebugStatus($"Debug: {DebugToggle}", 3000, "Green");
                Log.Info("Debug Enabled");
            }
            else if (DebugToggle)
            {
                MyTransparentGeometry.RemovePersistentBillboards(persistBillboard);
                DebugToggle = false;
                DebugStatus($"Debug: {DebugToggle}", 3000, "Red");
                Log.Info("Debug Disabled");
            }
            else
            {
                DebugStatus($"Unrecognized Command", 3000, "Red");
                return;
            }
        }
        #endregion
    }
}