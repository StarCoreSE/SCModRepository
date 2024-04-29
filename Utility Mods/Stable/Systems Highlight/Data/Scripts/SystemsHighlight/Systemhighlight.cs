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
using CoreSystems.Api;
using static VRageRender.MyBillboard;
using VRage.Game.Entity;
using VRage.Render.Scene;
using VRageRender.Messages;

namespace StarCore.SystemHighlight
{
    public enum HighlightFilterType
    {
        Conveyor,
        Thruster,
        Steering,
        Power,
        LightArmor,
        HeavyArmor,
        Weapon,
        Damage,
        Custom,
        None,
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SubsystemHighlight : MySessionComponentBase
    {

        private Dictionary<IMyCubeGrid, Dictionary<IMySlimBlock, HighlightFilterType>> highlightedEntitiesPerGrid = new Dictionary<IMyCubeGrid, Dictionary<IMySlimBlock, HighlightFilterType>>();
        private Dictionary<IMyCubeGrid, Dictionary<IMySlimBlock, HighlightFilterType>> gridDrawLists = new Dictionary<IMyCubeGrid, Dictionary<IMySlimBlock, HighlightFilterType>>();
        private Dictionary<long, IMyCubeGrid> ActiveGrids = new Dictionary<long, IMyCubeGrid>();
        private Dictionary<string, Action<HighlightFilterType, string, List<IMySlimBlock>, IMyCubeGrid>> commandHandlers;
        private List<MyBillboard> persistBillboard = new List<MyBillboard>();
        private IMyHudNotification notifStatus = null;
        private IMyHudNotification notifDebug = null;

        public static WcApi CoreSysAPI;
        private readonly ulong AQDID = 2621169600;

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
            MyAPIGateway.Utilities.MessageEnteredSender += HandleMessage;
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

            commandHandlers = new Dictionary<string, Action<HighlightFilterType, string, List<IMySlimBlock>, IMyCubeGrid>>
            {
                { "/hlcustom", (f, m, b, g) => HandleCustomHighlight(m, b, g) },
                { "/hlsettransparency", (f, m, b, g) => HandleSetTransparency(m) },
                { "/hlsetintensity", (f, m, b, g) => HandleSetIntensity(m) },
                { "/hllight", (f ,m, b, g) => HandleHighlightWrapper(HighlightFilterType.LightArmor, b, g) },
                { "/hlheavy", (f, m, b, g) => HandleHighlightWrapper(HighlightFilterType.HeavyArmor, b, g) },
            };

            Transparency = -0.5f;
            HighlightIntensity = 3;
        }

        protected override void UnloadData()
        {
            CoreSysAPI.Unload();
            CoreSysAPI = null;

            MyAPIGateway.Utilities.MessageEnteredSender -= HandleMessage;
        }

        public override void UpdateAfterSimulation()
        {
            if (!SeenMessage && MyAPIGateway.Session?.Player?.Character != null)
            {
                CoreSysAPI = new WcApi();
                CoreSysAPI.Load();

                if (CoreSysAPI.IsReady)
                {
                    WCInstalled = true;
                    Log.Info("CoreSystems Mod Detected");
                }

                SeenMessage = true;
                MyAPIGateway.Utilities.ShowMessage(From, Message);

                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleDamageEvent);

                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate));
            }
        }

        public override void Draw()
        {
            foreach (IMyCubeGrid grid in ActiveGrids.Values)
            {
                if (grid != null && gridDrawLists.ContainsKey(grid))
                {
                    Dictionary<IMySlimBlock, HighlightFilterType> slimBlockDict = gridDrawLists[grid];

                    foreach (KeyValuePair<IMySlimBlock, HighlightFilterType> entry in slimBlockDict)
                    {
                        IMySlimBlock slimBlock = entry.Key;
                        HighlightFilterType filterType = entry.Value;

                        Vector3D blockPosition;
                        Matrix blockRotation;

                        slimBlock.ComputeWorldCenter(out blockPosition);
                        slimBlock.Orientation.GetMatrix(out blockRotation);

                        MatrixD gridRotationMatrix = slimBlock.CubeGrid.WorldMatrix;
                        gridRotationMatrix.Translation = Vector3D.Zero;
                        blockRotation *= gridRotationMatrix;
                        MatrixD blockWorldMatrix = MatrixD.CreateWorld(blockPosition, blockRotation.Forward, blockRotation.Up);

                        float unit = slimBlock.CubeGrid.GridSize * 0.5f;
                        Vector3 halfExtents = new Vector3((float)unit, (float)unit, (float)unit);
                        BoundingBoxD box = new BoundingBoxD(-halfExtents, halfExtents);
                        Color c = Color.White;
                        if (filterType == HighlightFilterType.LightArmor)
                        {
                            c = Color.Lime;
                        }
                        else if (filterType == HighlightFilterType.HeavyArmor)
                        {
                            c = Color.Violet;
                        }                        

                        MySimpleObjectDraw.DrawTransparentBox(ref blockWorldMatrix, ref box, ref c, MySimpleObjectRasterizer.Solid, 1, 0.001f, null, null, true, -1, BlendTypeEnum.AdditiveTop, 255);
                    }
                }               
            }        
        }
        #endregion

        #region HandleMessages
        public void HandleMessage(ulong id, String message, ref bool sendToOthers)
        {
            if (!message.Contains("/hl"))
            {
                return;
            }

            IHitInfo strike;
            IMyCubeGrid cubeGrid;
            List<IMySlimBlock> gridBlocks;
            HandleRaycastAndGetGrid(out strike, out cubeGrid, out gridBlocks);

            if (gridBlocks == null || cubeGrid == null || strike == null)
            {
                if (strike == null && (!message.Contains("/hlhelp") || !message.Contains("/hldebug")))
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
            HighlightFilterType type = HighlightFilterType.None;

            foreach (var command in commandHandlers.Keys)
            {
                if (message.Contains(command))
                {
                    commandHandlers[command](type, message, gridBlocks, cubeGrid);
                    handled = true;
                    break; // Assuming only one command per message
                }
            }

            if (handled) 
                return;

            if (message.Contains("/hlconv"))
            {
                HandleHighlightWrapper(HighlightFilterType.Conveyor, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlthrust"))
            {
                HandleHighlightWrapper(HighlightFilterType.Thruster, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlpower"))
            {
                HandleHighlightWrapper(HighlightFilterType.Power, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlweapon"))
            {
                HandleHighlightWrapper(HighlightFilterType.Weapon, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hldamage"))
            {
                HandleHighlightWrapper(HighlightFilterType.Damage, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlsteering"))
            {
                HandleHighlightWrapper(HighlightFilterType.Steering, gridBlocks, cubeGrid);
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

        private void HandleHighlightWrapper(HighlightFilterType type, List<IMySlimBlock> blockList, IMyCubeGrid grid)
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
                "\n/hlcustom : \n" +
                "   Adds Blocks to Current Highlight | Accepts SubtypeIDs \n" +
                "   Example: [/hlcustom MySubtypeID Green]\n" +
                "\n/hlcolorhelp : \n" +
                "   List of Colors Supported by /hlcustom\n" +
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
            MyAPIGateway.Utilities.ShowMissionScreen(
                "Systems Highlight",
                ""/*Empty to Null Prefix*/,
                "Systems Highlight Color List",
                "Red : \n" +
                "   lightred | darkred | red\n" +
                "Green : \n" +
                "   lightgreen | darkgreen | green\n" +
                "Blue : \n" +
                "   lightblue | darkblue | blue\n" +
                "Yellow : \n" +
                "   yellow\n" +
                "Purple : \n" +
                "   purple\n" +
                "White : \n" +
                "   Default Color and Fallback if Command Unrecognized\n"
                );
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

        private void HandleCustomHighlight(string message, List<IMySlimBlock> gridBlocks, IMyCubeGrid cubeGrid)
        {
            string[] args = message.Split(' ');
            string subtype;
            string color;
            if (args == null || args[0] != "/hlcustom") return;        
            switch (args.Length)
            {
                case 2:
                    {
                        subtype = args[1];
                        color = "";
                    }
                    break;
                case 3:
                    {
                        subtype = args[1];
                        color = args[2];
                    }
                    break;
                default:
                    {
                        subtype = "";
                        color = "";
                    }
                    break;
            }               
            
            HandleHighlight(gridBlocks, HighlightFilterType.Custom, subtype, cubeGrid, color);

            if (DebugToggle)
            {
                Log.Info($"HandleCustomHighlight: customsubtypeid: {subtype}");
                Log.Info($"HandleCustomHighlight: color: {color}");
            }
        }

        public bool IsBlockOfType(HighlightFilterType type, IMySlimBlock block, string customType = "")
        {
            switch (type)
            {
                case HighlightFilterType.Conveyor: 
                    return (block.FatBlock != null && block.FatBlock is IMyConveyor || block.FatBlock is IMyConveyorTube);
                case HighlightFilterType.Thruster: 
                    return (block.FatBlock != null && block.FatBlock is IMyThrust);
                case HighlightFilterType.Power: 
                    return (block.FatBlock != null && block.FatBlock is IMyPowerProducer);
                case HighlightFilterType.Steering:
                    return (block.FatBlock != null && (block.FatBlock is IMyGyro || (AQDInstalled && (block.FatBlock.BlockDefinition.SubtypeName.Equals("AQD_LG_GyroBooster") || block.FatBlock.BlockDefinition.SubtypeName.Equals("AQD_LG_GyroUpgrade")))));
                case HighlightFilterType.Weapon:
                    return (CoreCheckHelper(block));                 
                case HighlightFilterType.Damage:
                    return (block.FatBlock != null && !block.FatBlock.IsFunctional);               
                case HighlightFilterType.LightArmor: 
                    return (block.FatBlock == null && !block.BlockDefinition.Id.SubtypeName.ToLower().Contains("heavy"));                
                case HighlightFilterType.HeavyArmor:
                    return (block.FatBlock == null && block.BlockDefinition.Id.SubtypeName.ToLower().Contains("heavy"));                
                case HighlightFilterType.Custom: 
                    return (block.BlockDefinition.Id.SubtypeId.ToString().ToLower() == customType.ToLower());                
                default: 
                    return false;
            }
        }

        public void HandleHighlight(List<IMySlimBlock> blockList, HighlightFilterType type, string customType, IMyCubeGrid cubeGrid, string color)
        {
            if (cubeGrid == null)
                return;

            var cubeGridID = cubeGrid.EntityId;
            Dictionary<IMySlimBlock, HighlightFilterType> gridHighlightedEntities;
            Dictionary<IMySlimBlock, HighlightFilterType> gridDrawnBlocks;

            if (!highlightedEntitiesPerGrid.TryGetValue(cubeGrid, out gridHighlightedEntities))
            {
                gridHighlightedEntities = new Dictionary<IMySlimBlock, HighlightFilterType>();
                highlightedEntitiesPerGrid[cubeGrid] = gridHighlightedEntities;
            }

            if (!ActiveGrids.Keys.Contains(cubeGridID) && !ActiveGrids.Values.Contains(cubeGrid))
            {
                cubeGrid.OnGridSplit += HandleGridSplit;
                ActiveGrids.Add(cubeGridID, cubeGrid);
            }

            if (!gridDrawLists.TryGetValue(cubeGrid, out gridDrawnBlocks))
            {
                gridDrawnBlocks = new Dictionary<IMySlimBlock, HighlightFilterType>();
                gridDrawLists[cubeGrid] = gridDrawnBlocks;
            }

            foreach (var block in blockList.Where(block => block != null))
            {
                if (IsBlockOfType(type, block, customType ?? ""))
                {
                    if (!gridHighlightedEntities.ContainsKey(block))
                    {
                        gridHighlightedEntities.Add(block, type);

                        if (DebugToggle)
                        {
                            Log.Info($"Adding Block to Dictionary: {block}");
                        }
                    }
                    HandleHighlighting(block, gridHighlightedEntities, type, color);

                    if ((type == HighlightFilterType.LightArmor || type == HighlightFilterType.HeavyArmor) && block.FatBlock == null)
                    {
                        if (!gridDrawnBlocks.ContainsKey(block))
                        {
                            gridDrawnBlocks.Add(block, type);

                            if (DebugToggle)
                            {
                                Log.Info($"Adding Block to Draw Lists: {block}");
                            }
                        }
                        block.Dithering = Transparency;
                    }
                }
                else
                {
                    if (block.FatBlock != null && !gridHighlightedEntities.ContainsKey(block))
                    {
                        block.Dithering = Transparency;
                    }
                    else if (block.FatBlock != null && gridHighlightedEntities.ContainsKey(block))
                    {
                        return;
                    }
                    else if (block.FatBlock == null)
                    {
                        block.Dithering = Transparency;
                    }
                }
            }

            if (type == HighlightFilterType.Custom && !gridHighlightedEntities.Values.Any(v => v == HighlightFilterType.Custom))
            {
                SetStatus($"No Blocks of {customType} Found on Grid", 3000, "Red");
            }
        }

        private void HandleHighlighting(IMySlimBlock block, Dictionary<IMySlimBlock, HighlightFilterType> highlightedEntities, HighlightFilterType type, string customColor = null)
        {
            bool isFunctional;     

            if (block.FatBlock != null)
                isFunctional = block.FatBlock.IsFunctional;
            else
                isFunctional = block.IsFullIntegrity;

            Color highlightColor = type == HighlightFilterType.Custom ? HandleCustomColor(customColor) : HandleTypeColor(type, isFunctional);
            int pulseTimeInFrames = isFunctional ? -1 : 3; // Pulse if not functional
            int thickness = isFunctional ? HighlightIntensity : 6;

            if (block.FatBlock != null)
            {
                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness, pulseTimeInFrames, highlightColor);
            }         

            if (!highlightedEntities.ContainsKey(block))
            {
                highlightedEntities[block] = type;
            }

            if (DebugToggle && block.FatBlock != null)
            {
                Log.Info($"Highlighted {block.FatBlock.Name} as Type: {type}, Color: {highlightColor}, Thickness: {thickness}, Pulse: {pulseTimeInFrames}");
            }
            else if (DebugToggle && block.FatBlock == null)
            {
                Log.Info($"Highlighted Armor Block as Type: {type}");
            }
        }

        public void ClearHighlight(List<IMySlimBlock> blockList, IMyCubeGrid cubeGrid)
        {
            var cubeGridID = cubeGrid.EntityId;
            Dictionary<IMySlimBlock, HighlightFilterType> gridHighlightedEntities;

            if (highlightedEntitiesPerGrid.TryGetValue(cubeGrid, out gridHighlightedEntities))
            {
                foreach (var highlightedEntity in gridHighlightedEntities)
                {
                    string name = highlightedEntity.Key.FatBlock?.Name ?? highlightedEntity.Key.GetType().Name;
                    if (DebugToggle)
                    {
                        Log.Info($"Clearing Highlight: {name} ");
                    }
                    MyVisualScriptLogicProvider.SetHighlightLocal(name, thickness: -1);
                }
                gridHighlightedEntities.Clear();
                if (DebugToggle)
                {
                    Log.Info($"Highlight Dictionary Cleared ");
                }
            }

            Dictionary<IMySlimBlock, HighlightFilterType> gridDrawnBlocks;

            if (gridDrawLists.TryGetValue(cubeGrid, out gridDrawnBlocks))
            {
                gridDrawnBlocks.Clear();

                if (DebugToggle)
                {
                    Log.Info($"Highlight Dictionary Cleared ");
                }
            }

            if (ActiveGrids.ContainsKey(cubeGridID) && ActiveGrids[cubeGridID] == cubeGrid)
            {
                cubeGrid.OnGridSplit -= HandleGridSplit;
                ActiveGrids.Remove(cubeGridID);
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

        private bool CoreCheckHelper(IMySlimBlock block)
        {
            if (WCInstalled)
            {
                var entBlock = block.FatBlock as MyEntity;
                return entBlock != null && CoreSysAPI.HasCoreWeapon(entBlock);
            }
            else
            {
                return block.FatBlock is IMyLargeTurretBase || block.FatBlock is IMySmallMissileLauncher;
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

        private void HandleRaycastAndGetGrid(out IHitInfo strike, out IMyCubeGrid cubeGrid, out List<IMySlimBlock> gridBlocks)
        {
            var player_camera = MyAPIGateway.Session.Camera;
            var camera_matrix = player_camera.WorldMatrix;
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

        private void HandleDamageEvent(object target, ref MyDamageInformation info)
        {
            var targetBlock = target as IMySlimBlock;

            if (targetBlock != null)
            {
                var targetID = targetBlock.CubeGrid.EntityId;
                if (ActiveGrids.ContainsKey(targetID))
                {
                    List<IMySlimBlock> targetBlocks = new List<IMySlimBlock>();
                    targetBlock.CubeGrid.GetBlocks(targetBlocks);

                    SetStatus("Grid Damaged! Highlights Cancelled!", 9000, "Red");
                    ClearHighlight(targetBlocks, targetBlock.CubeGrid);
                }
            }
        }

        private void HandleGridSplit(IMyCubeGrid originalGrid, IMyCubeGrid newGrid)
        {
            if (originalGrid != null && newGrid != null)
            {
                var originalGridID = originalGrid.EntityId;
                if (ActiveGrids.ContainsKey(originalGridID))
                {
                    List<IMySlimBlock> originalGridBlocks = new List<IMySlimBlock>();
                    List<IMySlimBlock> newGridBlocks = new List<IMySlimBlock>();

                    originalGrid.GetBlocks(originalGridBlocks);
                    newGrid.GetBlocks(newGridBlocks);

                    SetStatus("Grid Split Detected! Highlights Cancelled!", 9000, "Red");

                    ClearHighlight(originalGridBlocks, originalGrid);
                    ClearHighlight(newGridBlocks, newGrid);
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

        private static Color HandleTypeColor(HighlightFilterType type, bool isFunctional)
        {
            if (!isFunctional || type == HighlightFilterType.Damage) return Color.Red;
            switch (type)
            {
                case HighlightFilterType.Conveyor: 
                    return Color.Yellow;
                case HighlightFilterType.Thruster: 
                    return Color.Green;
                case HighlightFilterType.Power: 
                    return Color.SkyBlue;
                case HighlightFilterType.Weapon: 
                    return Color.Orange;
                case HighlightFilterType.Steering: 
                    return Color.Indigo;
                case HighlightFilterType.Custom: 
                    return Color.White;
                default: 
                    return Color.White;
            }
        }      
        #endregion
    }
}