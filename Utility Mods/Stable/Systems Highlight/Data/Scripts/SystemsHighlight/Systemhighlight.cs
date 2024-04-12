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
using StarCore.SystemHighlight.APISession;
using static VRageRender.MyBillboard;
using VRage.Game.Entity;

namespace StarCore.SystemHighlight
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SubsystemHighlight : MySessionComponentBase
    {
        private Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>> highlightedEntitiesPerGrid = new Dictionary<IMyCubeGrid, Dictionary<IMyEntity, int>>();
        private Dictionary<string, Action<string, List<IMySlimBlock>, IMyCubeGrid>> commandHandlers;
        private List<MyBillboard> persistBillboard = new List<MyBillboard>();
        private IMyHudNotification notifStatus = null;
        private IMyHudNotification notifDebug = null;

        private ulong AQDID = 2621169600;

        const string From = "SysHL";
        const string Message = "/hlhelp for list of Commands";

        private float Transparency;
        private int HighlightIntensity;

        private bool DebugToggle = false;
        private bool SeenMessage = false;
        private bool AQDInstalled = false;
        private bool WCInstalled = false;

        #region Overrides
        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += HandleMessage;
        }

        public override void LoadData()
        {
            if (MyAPIGateway.Session.IsServer && MyAPIGateway.Utilities.IsDedicated)
                return;

            List<MyObjectBuilder_Checkpoint.ModItem> loadedMods = MyAPIGateway.Session.Mods;

            foreach (MyObjectBuilder_Checkpoint.ModItem mod in loadedMods)
            {
                if (mod.PublishedFileId == AQDID)
                {
                    AQDInstalled = true;
                    Log.Info("AQD - Upgradable Gyros Detected");
                }                   
            }

            HandleCommandDictionaryInit();

            Transparency = -0.5f;
            HighlightIntensity = 3;
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

                if (CoreSysAPI._wcApi != null)
                {
                    if (CoreSysAPI._wcApi.IsReady && !WCInstalled)
                    {
                        WCInstalled = true;
                        Log.Info("WeaponCore Detected");
                    }
                }

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

            if (gridBlocks == null || cubeGrid == null || strike == null)
            {
                if (message.Contains("/hl") && strike == null && (!message.Contains("/hlhelp") || !message.Contains("/hldebug")))
                {
                    sendToOthers = false;
                    SetStatus("No Target", 3000, "Red");
                }
                else if (message.Contains("/hlhelp"))
                {
                    HandleHelpCommand();
                }
                else if (message.Contains("/hldebug"))
                {
                    HandleToggleDebug();
                }
                else
                    return;
            }

            sendToOthers = false;
            bool handled = false;

            foreach (var command in commandHandlers.Keys)
            {
                if (message.Contains(command))
                {
                    commandHandlers[command](message, gridBlocks, cubeGrid);
                    handled = true;
                    break; // Assuming only one command per message
                }
            }

            if (handled) return;

            if (message.Contains("/hlconv"))
            {
                HandleHighlightWrapper(1, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlthrust"))
            {
                HandleHighlightWrapper(2, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlpower"))
            {
                HandleHighlightWrapper(3, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlweapon"))
            {
                HandleHighlightWrapper(4, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hldamage"))
            {
                HandleHighlightWrapper(6, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlsteering"))
            {
                HandleHighlightWrapper(7, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlclear"))
            {
                SetStatus("All Highlights Cleared", 3000, "Green");
                ClearHighlight(gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hldebug"))
            {
                HandleToggleDebug();
            }
            else if (message.Contains("/hlhelp"))
            {
                HandleHelpCommand();
            }
            else if (message.Contains("/hlcolorhelp"))
            {
                HandleColorHelpCommand();
            }

        }

        private void HandleHighlightWrapper(int type, List<IMySlimBlock> blockList, IMyCubeGrid grid)
        {
            if (DebugToggle)
            {
                Log.Info($"HandleMessage: Highlight Triggered | Type {type}");
            }
            HandleHighlight(blockList, type, null, grid, null);
        }

        private void HandleHelpCommand()
        {
            MyAPIGateway.Utilities.ShowMissionScreen(
                "Systems Highlight",
                ""/*Empty to Null Prefix*/, 
                "Systems Highlight Help Menu",
                "/hlconv : \n" +
                "   Highlights Conveyors\n" +
                "\n/hlthrust : \n" +
                "   Highlights Thrusters\n" +
                "\n/hlpower : \n" +
                "   Highlights Power Producing Blocks\n" +
                "\n/hlsteering : \n" +
                "   Highlights Gyroscopes [Supports AQD Upgrades if Installed]\n" +
                "\n/hlweapon : \n" +
                "   Highlights Weapons Blocks [Includes Sorters]\n" +
                "\n/hlcustomadd : \n" +
                "   Adds Blocks to Current Highlight | Accepts SubtypeIDs \n" +
                "   Example: [/hlcustomadd MySubtypeID Green]\n" +
                "\n/hlcolorhelp : \n" +
                "   List of Colors Supported by /hlcustomadd\n" +
                "\n/hldamage : \n" +
                "   Highlights All Non-Functional Blocks\n" +
                "\n/hlclear : \n" +
                "   Removes Active Highlight\n" +
                "\n/hlsetintensity : \n" +
                "   Sets Highlight Intensity, Takes 0 to 10\n" +
                "\n/hlsettransparency : \n" +
                "   Sets Block Transparency, Takes -1 to 1\n" +
                "\n/hlhelp : \n" +
                "   Opens This Menu\n" +
                "\n/hldebug : \n" +
                "   Toggles Debug Mode\n"
                );
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

            var highlightStrategies = new Dictionary<int, Func<IMySlimBlock, bool>>() 
            {
                { 1, block => block.FatBlock is IMyConveyor || block.FatBlock is IMyConveyorTube },
                { 2, block => block.FatBlock is IMyThrust },
                { 3, block => block.FatBlock is IMyPowerProducer },               
                { 5, block => block.BlockDefinition.Id.SubtypeId.ToString().ToLower() == customType.ToLower() },
                { 6, block => !block.FatBlock.IsFunctional },
            };

            highlightStrategies.Add(7, block => block.FatBlock is IMyGyro || 
                (AQDInstalled && (block.FatBlock.BlockDefinition.SubtypeName.Equals("AQD_LG_GyroBooster") || 
                block.FatBlock.BlockDefinition.SubtypeName.Equals("AQD_LG_GyroUpgrade"))));

            highlightStrategies.Add(4, block => CoreCheckHelper(block));


            foreach (var block in blockList.Where(block => block != null && block.FatBlock != null))
            {
                if (highlightStrategies.ContainsKey(type) && highlightStrategies[type](block))
                {
                    HandleDictionary(gridHighlightedEntities, block, type);
                    HandleHighlighting(block, gridHighlightedEntities, type, color);
                }
            }

            foreach (var block in blockList)
            {
                if (block != null)
                {
                    HandleDithering(gridHighlightedEntities, block, Transparency);
                }
            }

            if (type == 5 && !gridHighlightedEntities.Values.Any(v => v == 5))
            {
                SetStatus($"No Blocks of {customType} Found on Grid", 3000, "Red");
            }

        }   

        public void HandleSetTransparency(string message)
        {
            string remainingMessage = message.Substring(18);
            int spaceIndex = remainingMessage.IndexOf(' ');

            if (spaceIndex >= 0)
            {
                if (spaceIndex + 1 < remainingMessage.Length)
                {
                    float tempTransparency;
                    float.TryParse(remainingMessage.Substring(spaceIndex + 1), out tempTransparency);

                    Transparency = tempTransparency;
                }                    
            }
        }

        public void HandleSetIntensity(string message)
        {
            string remainingMessage = message.Substring(15);
            int spaceIndex = remainingMessage.IndexOf(' ');

            if (spaceIndex >= 0)
            {
                if (spaceIndex + 1 < remainingMessage.Length)
                {
                    int tempIntensity;
                    int.TryParse(remainingMessage.Substring(spaceIndex + 1), out tempIntensity);

                    HighlightIntensity = tempIntensity;
                }             
            }
        }

        private void HandleHighlighting(IMySlimBlock block, Dictionary<IMyEntity, int> highlightedEntities, int type, string customColor = null)
        {
            Color highlightColor = type == 5 ? HandleCustomColor(customColor) : HandleTypeColor(type, block.FatBlock.IsFunctional);

            int pulseTimeInFrames = block.FatBlock.IsFunctional ? -1 : 3; // Pulse if not functional
            int thickness = block.FatBlock.IsFunctional ? HighlightIntensity : 6;

            MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness, pulseTimeInFrames, highlightColor);

            if (!highlightedEntities.ContainsKey(block.FatBlock))
            {
                highlightedEntities[block.FatBlock] = type;
                SetStatusForType(type); // Update status message based on type
            }

            if (DebugToggle)
            {
                Log.Info($"Highlighted {block.FatBlock.Name} as Type: {type}, Color: {highlightColor}, Thickness: {thickness}, Pulse: {pulseTimeInFrames}");
            }
        }

        public static void HandleDithering(Dictionary<IMyEntity, int> gridHighlightedEntities, IMySlimBlock block, float Transparency)
        {
            if (block.FatBlock != null && !gridHighlightedEntities.ContainsKey(block.FatBlock))
            {
                block.Dithering = Transparency;
            }
            else if (block.FatBlock != null && gridHighlightedEntities.ContainsKey(block.FatBlock))
            {
                return;
            }
            else if (block.FatBlock == null)
            {
                block.Dithering = Transparency;
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
        private bool CoreCheckHelper(IMySlimBlock block)
        {
            var entBlock = block as MyEntity;
            if (WCInstalled)
            {
                return entBlock != null && CoreSysAPI._wcApi.HasCoreWeapon(entBlock);
            }
            else
            {
                return block.FatBlock is IMyLargeTurretBase || block.FatBlock is IMySmallMissileLauncher;
            }
        }

        private void SetStatusForType(int type)
        {
            string message = null;

            switch (type)
            {
                case 1:
                    message = "Highlighting Conveyors. Type /hlclear to Clear.";
                    break;
                case 2:
                    message = "Highlighting Thrusters. Type /hlclear to Clear.";
                    break;
                case 3:
                    message = "Highlighting Power. Type /hlclear to Clear.";
                    break;
                case 4:
                    message = "Highlighting Weapons. Type /hlclear to Clear.";
                    break;
                case 5:
                    message = "Highlighting Custom Type. Type /hlclear to Clear.";
                    break;
                case 6:
                    message = "Highlighting Damaged. Type /hlclear to Clear.";
                    break;
                case 7:
                    message = "Highlighting Steering. Type /hlclear to Clear.";
                    break;
                default:
                    message = null; // This will handle any unspecified types
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                SetStatus(message, 3000, "Green");
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

            if (DebugToggle && strike != null)
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

        public void HandleCommandDictionaryInit()
        {
            commandHandlers = new Dictionary<string, Action<string, List<IMySlimBlock>, IMyCubeGrid>> 
            {
                { "/hlcustomadd", (m, b, g) => HandleCustomHighlight(m, b, g) },
                { "/hlsettransparency", (m, b, g) => HandleSetTransparency(m) },
                { "/hlsetintensity", (m, b, g) => HandleSetIntensity(m) },
                // Add other commands as needed
            };
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

        public static Color HandleCustomColor(string colorName)
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

        private static Color HandleTypeColor(int type, bool isFunctional)
        {
            if (!isFunctional || type == 6) return Color.Red;

            switch (type)
            {
                case 1:
                    return Color.Yellow; // Conveyors
                case 2:
                    return Color.Green; // Thrusters
                case 3:
                    return Color.SkyBlue; // Power
                case 4:
                    return Color.Orange; // Weapons
                case 7:
                    return Color.Indigo; // Steering
                default:
                    return Color.White; // Default for unspecified types
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