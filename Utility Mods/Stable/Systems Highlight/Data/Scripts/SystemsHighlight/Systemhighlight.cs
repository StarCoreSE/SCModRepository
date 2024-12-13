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

        // Debug Billlboards
        Color debugStrikeColor = Color.GreenYellow;
        Vector4 strikeColorRef;
        private List<MyBillboard> persistBillboard = new List<MyBillboard>();

        private IMyHudNotification notifStatus = null;
        private IMyHudNotification notifDebug = null;

        // CoreSys API and Addon ModIDs
        public static WcApi CoreSysAPI;
        private readonly ulong AQD_Gyros_ID = 2621169600;
        private readonly ulong AQD_Armor_ID = 1;

        const string From = "SysHL";
        const string Message = "/hlhelp for list of Commands";

        private float Transparency;
        private int HighlightIntensity;

        private bool DebugToggle = false;
        private bool SeenMessage = false;
        private bool AQD_Gyros_Installed = false;
        private bool AQD_Armor_Installed = false;
        private bool WCInstalled = false;
        
        #region Overrides
        // Vanilla Update/Load Methods
        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEnteredSender += HandleMessage;
        }

        public override void LoadData()
        {
            // Return if Server-Side | Only Runs on Client
            if (MyAPIGateway.Session.IsServer)
                return;

            List<MyObjectBuilder_Checkpoint.ModItem> loadedMods = MyAPIGateway.Session.Mods;            

            foreach (MyObjectBuilder_Checkpoint.ModItem mod in loadedMods)
            {
                if (mod.PublishedFileId == AQD_Gyros_ID)
                {
                    AQD_Gyros_Installed = true;
                    Log.Info("AQD - Upgradable Gyros Detected");
                }

                if (mod.PublishedFileId == AQD_Armor_ID)
                {
                    AQD_Armor_Installed = true;
                    Log.Info("AQD - Armor Expansion Detected");
                }
            }

            // Init Non-Standard Command Dict
            commandHandlers = new Dictionary<string, Action<HighlightFilterType, string, List<IMySlimBlock>, IMyCubeGrid>>
            {
                { "/hlcustom", (f, m, b, g) => HandleCustomHighlight(m, b, g) },
                { "/hlsettransparency", (f, m, b, g) => HandleSetTransparency(m) },
                { "/hlsetintensity", (f, m, b, g) => HandleSetIntensity(m) },
                { "/hllight", (f ,m, b, g) => HandleHighlightWrapper(HighlightFilterType.LightArmor, b, g) },
                { "/hlheavy", (f, m, b, g) => HandleHighlightWrapper(HighlightFilterType.HeavyArmor, b, g) },
            };

            // Set Default Settings
            Transparency = -0.5f;
            HighlightIntensity = 3;

            strikeColorRef = debugStrikeColor.ToVector4();
        }

        protected override void UnloadData()
        {
            try
            {
                // Unsubscribe from events
                MyAPIGateway.Utilities.MessageEnteredSender -= HandleMessage;

                // Clear all highlights and remove grid split handlers
                foreach (var grid in ActiveGrids.Values)
                {
                    if (grid != null)
                    {
                        List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                        grid.GetBlocks(blocks);
                        ClearHighlight(blocks, grid);
                        grid.OnGridSplit -= HandleGridSplit;
                    }
                }

                // Clear collections
                highlightedEntitiesPerGrid.Clear();
                gridDrawLists.Clear();
                ActiveGrids.Clear();
                commandHandlers.Clear();

                // Remove persistent billboards
                MyTransparentGeometry.RemovePersistentBillboards(persistBillboard);
                persistBillboard.Clear();

                // Dispose notifications
                if (notifStatus != null)
                {
                    notifStatus.Hide();
                    notifStatus = null;
                }
                if (notifDebug != null)
                {
                    notifDebug.Hide();
                    notifDebug = null;
                }

                // Unload CoreSysAPI
                if (CoreSysAPI != null && (CoreSysAPI.IsReady || WCInstalled))
                {
                    CoreSysAPI.Unload();
                    CoreSysAPI = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in UnloadData: {ex}");
            }
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
            // Draw any Blocks stored in per-grid non-fatblock armor dict
            if (ActiveGrids.Any() && gridDrawLists.Any())
            {
                foreach (IMyCubeGrid grid in ActiveGrids.Values)
                {
                    if (grid == null || grid.MarkedForClose || !gridDrawLists.ContainsKey(grid))
                        continue;

                    Dictionary<IMySlimBlock, HighlightFilterType> slimBlockDict = gridDrawLists[grid];

                    foreach (var entry in slimBlockDict.ToList()) // Use ToList to avoid collection modification issues
                    {
                        IMySlimBlock slimBlock = entry.Key;

                        if (slimBlock == null || slimBlock.CubeGrid == null || slimBlock.CubeGrid.MarkedForClose)
                        {
                            slimBlockDict.Remove(slimBlock);
                            continue;
                        }

                        HighlightFilterType filterType = entry.Value;

                        if (slimBlock.Dithering != 1.0f)
                            slimBlock.Dithering = 1.0f;

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
                        Color c = filterType == HighlightFilterType.LightArmor ? Color.Lime : Color.MediumVioletRed * 2f;

                        MySimpleObjectDraw.DrawTransparentBox(ref blockWorldMatrix, ref box, ref c, MySimpleObjectRasterizer.Solid, 1, 0.001f, null, null, true, -1, BlendTypeEnum.AdditiveTop, 1000f);
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

            sendToOthers = false;
            bool handled = false;
            HighlightFilterType type = HighlightFilterType.None;

            if (message.Contains("/hlhelp"))
            {
                HandleHelpCommand();
                return;
            }
            else if (message.Contains("/hlcolorhelp"))
            {
                HandleColorHelpCommand();
                return;
            }
            else if (message.Contains("/hldebug"))
            {
                HandleToggleDebug();
                return;
            }

            IHitInfo strike;
            IMyCubeGrid cubeGrid;
            List<IMySlimBlock> gridBlocks;
            HandleRaycastAndGetGrid(out strike, out cubeGrid, out gridBlocks);

            if (gridBlocks == null || cubeGrid == null || strike == null)
            {               
                SetStatus("Invalid or No Target", 3000, "Red");
            }
          
            foreach (var command in commandHandlers.Keys)
            {
                if (message.Contains(command))
                {
                    commandHandlers[command](type, message, gridBlocks, cubeGrid);
                    handled = true;
                    break;
                }
            }

            if (handled) 
                return;

            if (message.Contains("/hlconv"))
            {
                SetStatus($"Highlighting Conveyors \n Transparency: {Transparency} | Intensity: {HighlightIntensity}", 3000, "Green");
                HandleHighlightWrapper(HighlightFilterType.Conveyor, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlthrust"))
            {
                SetStatus($"Highlighting Thrusters \n Transparency: {Transparency} | Intensity: {HighlightIntensity}", 3000, "Green");
                HandleHighlightWrapper(HighlightFilterType.Thruster, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlpower"))
            {
                SetStatus($"Highlighting Power Blocks \n Transparency: {Transparency} | Intensity: {HighlightIntensity}", 3000, "Green");
                HandleHighlightWrapper(HighlightFilterType.Power, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlweapon"))
            {
                SetStatus($"Highlighting Weapon Blocks \n Transparency: {Transparency} | Intensity: {HighlightIntensity}", 3000, "Green");
                HandleHighlightWrapper(HighlightFilterType.Weapon, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hldamage"))
            {
                SetStatus($"Highlighting Damaged Blocks \n Transparency: {Transparency} | Intensity: {HighlightIntensity}", 3000, "Green");
                HandleHighlightWrapper(HighlightFilterType.Damage, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlsteering"))
            {
                SetStatus($"Highlighting Steering Blocks \n Transparency: {Transparency} | Intensity: {HighlightIntensity}", 3000, "Green");
                HandleHighlightWrapper(HighlightFilterType.Steering, gridBlocks, cubeGrid);
            }
            else if (message.Contains("/hlclear"))
            {
                SetStatus("All Highlights Cleared", 3000, "Green");
                ClearHighlight(gridBlocks, cubeGrid);
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
                "   Highlights Weapons Blocks\n" +
                 "\n/hllight : \n" +
                "   Highlights Light Armor Blocks\n" +
                 "\n/hlheavy : \n" +
                "   Highlights Heavy Armor Blocks\n" +
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
            string[] args = message.Split(' ');

            if (args.Length > 1)
            {
                float tempTransparency;
                if (float.TryParse(args[1], out tempTransparency))
                    Transparency = tempTransparency;
                else
                    SetStatus("Failed to Set Transparency, Argument Unrecognized", 3000, "Red");
            }
        }

        public void HandleSetIntensity(string message)
        {
            string[] args = message.Split(' ');

            if (args.Length > 1)
            {
                int tempIntensity;
                if (int.TryParse(args[1], out tempIntensity))
                    HighlightIntensity = tempIntensity;
                else
                    SetStatus("Failed to Set Intensity, Argument Unrecognized", 3000, "Red");
            }
        }

        private void HandleCustomHighlight(string message, List<IMySlimBlock> gridBlocks, IMyCubeGrid cubeGrid)
        {
            string[] args = message.Split(' ');
            string subtype = "";
            string color = "";
            if (args == null || args.Length < 2 || args[0] != "/hlcustom") return;
            if (args.Length >= 2) subtype = args[1];
            if (args.Length >= 3) color = args[2];
            if (args.Length > 3) SetStatus("Too many arguments provided. Only subtype and color will be used.", 3000, "Red");

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
                    return (block.FatBlock is IMyConveyor || block.FatBlock is IMyConveyorTube);
                case HighlightFilterType.Thruster:
                    return (block.FatBlock is IMyThrust);
                case HighlightFilterType.Power:
                    return (block.FatBlock is IMyPowerProducer);
                case HighlightFilterType.Steering:
                    return (block.FatBlock != null && (block.FatBlock is IMyGyro || (AQD_Gyros_Installed && (block.FatBlock.BlockDefinition.SubtypeName.Equals("AQD_LG_GyroBooster") || block.FatBlock.BlockDefinition.SubtypeName.Equals("AQD_LG_GyroUpgrade")))));
                case HighlightFilterType.Weapon:
                    return CoreCheckHelper(block);
                case HighlightFilterType.Damage:
                    return (!block.FatBlock.IsFunctional);
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

            // Create Fatblock Dictionary for Targetted Grid
            if (!highlightedEntitiesPerGrid.TryGetValue(cubeGrid, out gridHighlightedEntities))
            {
                gridHighlightedEntities = new Dictionary<IMySlimBlock, HighlightFilterType>();
                highlightedEntitiesPerGrid[cubeGrid] = gridHighlightedEntities;
            }        

            // Create Slimblock Dictionary for Targetted Grid
            if (!gridDrawLists.TryGetValue(cubeGrid, out gridDrawnBlocks))
            {
                gridDrawnBlocks = new Dictionary<IMySlimBlock, HighlightFilterType>();
                gridDrawLists[cubeGrid] = gridDrawnBlocks;
            }

            // Add Grid to Active Grids
            if (!ActiveGrids.Keys.Contains(cubeGridID) && !ActiveGrids.Values.Contains(cubeGrid))
            {
                cubeGrid.OnGridSplit += HandleGridSplit;
                cubeGrid.OnMarkForClose += HandleGridClose;
                ActiveGrids.Add(cubeGridID, cubeGrid);
            }

            foreach (var block in blockList.Where(block => block != null))
            {
                if (IsBlockOfType(type, block, customType ?? ""))
                {
                    if (DebugToggle)
                    {
                        Log.Info($"Block of Type {type} Found. Block: {block}");
                    }

                    if ((type == HighlightFilterType.LightArmor || type == HighlightFilterType.HeavyArmor) && block.FatBlock == null)
                    {
                        // Slimblock Dictionary Handling
                        if (!gridDrawnBlocks.ContainsKey(block))
                        {
                            gridDrawnBlocks.Add(block, type);

                            if (DebugToggle)
                            {
                                Log.Info($"Adding Block to Draw Lists: {block}");
                            }
                        }
                    }
                    else if ((type != HighlightFilterType.LightArmor || type != HighlightFilterType.HeavyArmor) && block.FatBlock != null)
                    {
                        // Fatblock Dictionary Handling
                        if (!gridHighlightedEntities.ContainsKey(block))
                        {
                            gridHighlightedEntities.Add(block, type);

                            if (DebugToggle)
                            {
                                Log.Info($"Adding Block to Dictionary: {block}");
                            }
                        }
                    }
                    else
                    {
                        Log.Info($"Cases Unmatched: Block {block} for Type {type}");
                    }

                    HandleHighlighting(block, gridHighlightedEntities, type, color);
                }
                else
                {
                    if (DebugToggle)
                    {
                        Log.Info($"Block Was Not of Type {type}. Block: {block}");
                    }

                    // Handle Fatblock Dithering
                    if (block.FatBlock != null && !gridHighlightedEntities.ContainsKey(block))
                    {
                        block.Dithering = Transparency;
                    }
                    else if (block.FatBlock != null && gridHighlightedEntities.ContainsKey(block))
                    {                       
                        continue;
                    }

                    // Handle Slimblock Dithering
                    if (block.FatBlock == null && !gridDrawnBlocks.ContainsKey(block))
                    {
                        block.Dithering = Transparency;
                    }

                }
            }
            
            // Non-Existant Subtype Error Message
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

            // Return if not Dictionaried
            if (!highlightedEntities.ContainsKey(block))
                return;

            Color highlightColor = type == HighlightFilterType.Custom ? HandleCustomColor(customColor) : HandleTypeColor(type, isFunctional);
            int pulseTimeInFrames = isFunctional ? -1 : 3; // Pulse if not functional
            int thickness = isFunctional ? HighlightIntensity : 6;

            // Fatblock SetHighlight
            if (block.FatBlock != null)
            {
                block.Dithering = 0;
                MyVisualScriptLogicProvider.SetHighlightLocal(block.FatBlock.Name, thickness, pulseTimeInFrames, highlightColor);

                // Fatblock Debug Logging
                if (DebugToggle && block.FatBlock != null)
                {
                    Log.Info($"Highlighted {block.FatBlock.Name} as Type: {type}, Color: {highlightColor}, Thickness: {thickness}, Pulse: {pulseTimeInFrames}");
                }
            }         

            // Slimblock Debug Logging      
            if (DebugToggle && block.FatBlock == null)
            {
                Log.Info($"Highlighted Armor Block as Type: {type}");
            }
        }

        public void ClearHighlight(List<IMySlimBlock> blockList, IMyCubeGrid cubeGrid)
        {
            var cubeGridID = cubeGrid.EntityId;
            Dictionary<IMySlimBlock, HighlightFilterType> gridHighlightedEntities;
            Dictionary<IMySlimBlock, HighlightFilterType> gridDrawnBlocks;

            if (highlightedEntitiesPerGrid.TryGetValue(cubeGrid, out gridHighlightedEntities))
            {
                // Iterate Active Highlights and Clear
                foreach (var highlightedEntity in gridHighlightedEntities)
                {
                    string name = highlightedEntity.Key.FatBlock?.Name ?? highlightedEntity.Key.GetType().Name;

                    MyVisualScriptLogicProvider.SetHighlightLocal(name, thickness: -1);

                    if (DebugToggle)
                    {
                        Log.Info($"Clearing Highlight: {name} ");
                    }         
                }

                gridHighlightedEntities.Clear();

                if (DebugToggle)
                {
                    Log.Info($"Highlight Dictionary Cleared");
                }
            }     

            if (gridDrawLists.TryGetValue(cubeGrid, out gridDrawnBlocks))
            {
                foreach (var drawnBlock in gridDrawnBlocks.Keys)
                {
                    if (DebugToggle)
                    {
                        Log.Info($"Clearing Draw: {drawnBlock.BlockDefinition.Id.SubtypeName.ToLower()} ");
                    }
                }

                gridDrawnBlocks.Clear();

                if (DebugToggle)
                {
                    Log.Info($"Draw Dictionary Cleared");
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
                return block.FatBlock is IMyLargeTurretBase || block.FatBlock is IMySmallMissileLauncher;
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
                DebugToggle = false;
                MyTransparentGeometry.RemovePersistentBillboards(persistBillboard);               
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
            gridBlocks = new List<IMySlimBlock>();
            MyAPIGateway.Physics.CastRay(camera_matrix.Translation, camera_matrix.Translation + camera_matrix.Forward * 150, out strike);          

            if (strike != null)
            {
                if (strike.HitEntity is IMyCubeGrid)
                {
                    cubeGrid = strike.HitEntity as IMyCubeGrid;
                    cubeGrid.GetBlocks(gridBlocks);
                }

                if (DebugToggle && cubeGrid != null && gridBlocks.Any())
                {
                    DebugStatus($"Raycast Target: {cubeGrid.DisplayName}", 6000, "Green");
                    Log.Info($"Raycast hit at {strike.Position} on grid [{cubeGrid.DisplayName}/{cubeGrid.EntityId}] with {gridBlocks.Count} blocks collected.");
                    Log.Info($"Drawing Point at {strike.Position}");
                    MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("WhiteDot"), strikeColorRef, strike.Position, 1f, 0f, -1, BlendTypeEnum.SDR, persistBillboard);
                    
                    foreach (var block in gridBlocks)
                    {
                        Log.Info($"Collected block: {block.BlockDefinition.Id.SubtypeName}");
                    }
                }
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

        private void HandleGridClose(IMyEntity entity) 
        { 
            var grid = entity as IMyCubeGrid;
            if (grid == null)
                return;

            if (ActiveGrids.ContainsKey(grid.EntityId))
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                ClearHighlight(blocks, grid);

                ActiveGrids.Remove(grid.EntityId);
                highlightedEntitiesPerGrid.Remove(grid);
                gridDrawLists.Remove(grid);

                grid.OnMarkForClose -= HandleGridClose;
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