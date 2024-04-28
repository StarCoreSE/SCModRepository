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
        Armor,
        Weapon,
        Damage,
        Custom,
    }


    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SubsystemHighlight : MySessionComponentBase
    {

        private Dictionary<IMyCubeGrid, Dictionary<IMySlimBlock, HighlightFilterType>> highlightedEntitiesPerGrid = new Dictionary<IMyCubeGrid, Dictionary<IMySlimBlock, HighlightFilterType>>();
        private Dictionary<long, IMyCubeGrid> ActiveGrids = new Dictionary<long, IMyCubeGrid>();
        private Dictionary<string, Action<string, List<IMySlimBlock>, IMyCubeGrid>> commandHandlers;
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

        public List<IMySlimBlock> drawlist;



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

            CoreSysAPI = new WcApi();
            CoreSysAPI.Load();

            if (CoreSysAPI.IsReady)
            {
                WCInstalled = true;
                Log.Info("CoreSystems Mod Detected");
            }

            HandleCommandDictionaryInit();

            Transparency = -0.5f;
            HighlightIntensity = 3;
        }

        protected override void UnloadData()
        {
            if (drawlist != null)
            {
                foreach (var block in drawlist)
                {
                    block.Dithering = 0;
                }
                drawlist = null;
            }

            CoreSysAPI.Unload();
            CoreSysAPI = null;

            MyAPIGateway.Utilities.MessageEntered -= HandleMessage;
        }

        public override void UpdateAfterSimulation()
        {
            if (!SeenMessage && MyAPIGateway.Session?.Player?.Character != null)
            {
                SeenMessage = true;
                MyAPIGateway.Utilities.ShowMessage(From, Message);

                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleDamageEvent);

                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate));
            }
        }
        #endregion

        #region HandleMessages
        public void HandleMessage(String message, ref bool sendToOthers)
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
            else if (message.Contains("/hlarmor"))
            {
                HandleHighlightWrapper(HighlightFilterType.Armor, gridBlocks, cubeGrid);
                MyAPIGateway.Utilities.ShowNotification("hl armor run");
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
            if (args == null || args.Length != 3 || args[0] != "/hlcustom") return;
            string subTypeId = args[1];
            string color = args[2];
            HandleHighlight(gridBlocks, HighlightFilterType.Custom, subTypeId, cubeGrid, color);

            if (DebugToggle)
            {
                Log.Info($"HandleCustomHighlight: customsubtypeid: {subTypeId}");
                Log.Info($"HandleCustomHighlight: color: {color}");
            }
        }


        public bool IsBlockOfType(HighlightFilterType type, IMySlimBlock block, string customType = "")
        {
            switch (type)
            {
                case HighlightFilterType.Conveyor: return (block.FatBlock is IMyConveyor || block.FatBlock is IMyConveyorTube);
                case HighlightFilterType.Thruster: return (block.FatBlock is IMyThrust);
                case HighlightFilterType.Power: return (block.FatBlock is IMyPowerProducer);
                case HighlightFilterType.Armor: return (block.FatBlock == null && block.BlockDefinition.Id.SubtypeName.ToLower().Contains("armor"));
                case HighlightFilterType.Custom: return (block.BlockDefinition.Id.SubtypeId.ToString().ToLower() == customType.ToLower());
                default: return false;
            }
        }

        public void HandleHighlight(List<IMySlimBlock> blockList, HighlightFilterType type, string customType, IMyCubeGrid cubeGrid, string color)
        {
            if (cubeGrid == null)
                return;

            var cubeGridID = cubeGrid.EntityId;
            Dictionary<IMySlimBlock, HighlightFilterType> gridHighlightedEntities;

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

            if (drawlist == null)
            {
                drawlist = new List<IMySlimBlock>();
            }
            else
            {
                foreach (var block in drawlist)
                {
                    block.Dithering = 0;
                }
                drawlist.Clear();
            }

            foreach (var block in blockList.Where(block => block != null))
            {
                if (IsBlockOfType(type, block, customType ?? ""))
                {
                    HandleDictionary(gridHighlightedEntities, block, type);
                    HandleHighlighting(block, gridHighlightedEntities, type, color);
                    if (block.FatBlock == null)
                    {
                        block.Dithering = -1.0f;
                        drawlist.Add(block);
                    }
                }
                else
                {
                    block.Dithering = Transparency;
                }
            }

            if (type == HighlightFilterType.Custom && !gridHighlightedEntities.Values.Any(v => v == HighlightFilterType.Custom))
            {
                SetStatus($"No Blocks of {customType} Found on Grid", 3000, "Red");
            }

        }

        public override void Draw()
        {
            if (drawlist != null)
            {
                foreach (var block in drawlist)
                {
                    Vector3D blockPosition;
                    block.ComputeWorldCenter(out blockPosition);
                    Matrix blockRotation;
                    block.Orientation.GetMatrix(out blockRotation);
                    MatrixD gridRotationMatrix = block.CubeGrid.WorldMatrix;
                    gridRotationMatrix.Translation = Vector3D.Zero;
                    blockRotation *= gridRotationMatrix;
                    MatrixD blockWorldMatrix = MatrixD.CreateWorld(blockPosition, blockRotation.Forward, blockRotation.Up);

                    float unit = block.CubeGrid.GridSize * 0.5f;
                    Vector3 halfExtents = new Vector3((float)unit, (float)unit, (float)unit);
                    BoundingBoxD box = new BoundingBoxD(-halfExtents, halfExtents);
                    Color c = Color.Cyan;
                    c.A = 150;
                    MySimpleObjectDraw.DrawTransparentBox(ref blockWorldMatrix, ref box, ref c, MySimpleObjectRasterizer.Solid, 1);
                }
            }
        }

        private void HandleHighlighting(IMySlimBlock block, Dictionary<IMySlimBlock, HighlightFilterType> highlightedEntities, HighlightFilterType type, string customColor = null)
        {
            string name;
            bool isFunctional;

            if (block.FatBlock != null)
            {
                isFunctional = block.FatBlock.IsFunctional;
                name = block.FatBlock.Name;
            }
            else
            {
                isFunctional = block.IsFullIntegrity;
                name = block.BlockDefinition.Id.SubtypeId.ToString();

            }

            Color highlightColor = type == HighlightFilterType.Custom ? HandleCustomColor(customColor) : HandleTypeColor(type, isFunctional);

            if (block.FatBlock == null)
            {
                MyAPIGateway.Utilities.ShowNotification(block.Position.ToString());
            }

            int pulseTimeInFrames = isFunctional ? -1 : 3; // Pulse if not functional
            int thickness = isFunctional ? HighlightIntensity : 6;

            MyVisualScriptLogicProvider.SetHighlightLocal(name, thickness, pulseTimeInFrames, highlightColor);

            if (!highlightedEntities.ContainsKey(block))
            {
                highlightedEntities[block] = type;
                SetStatusForType(type); // Update status message based on type
            }

            if (DebugToggle)
            {
                Log.Info($"Highlighted {name} as Type: {type}, Color: {highlightColor}, Thickness: {thickness}, Pulse: {pulseTimeInFrames}");
            }
        }

        /*public static void GetSubpartsRecursive(MyEntity entity, List<MyEntitySubpart> subparts)
        {
            if (entity == null)
                return;
            if (subparts == null)
                return;
            if (entity.Subparts.Count == 0)
                return;

            foreach (var part in entity.Subparts.Values)
            {
                subparts.Add(part);
                GetSubpartsRecursive(part, subparts);
            }
        }*/

        public void ClearHighlight(List<IMySlimBlock> blockList, IMyCubeGrid cubeGrid)
        {
            if (drawlist != null) drawlist.Clear();
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

        private void SetStatusForType(HighlightFilterType type)
        {
            SetStatus($"Highlighting {type:G}. Type /hlclear to Clear.", 3000, "Green");
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

        public void HandleCommandDictionaryInit()
        {
            commandHandlers = new Dictionary<string, Action<string, List<IMySlimBlock>, IMyCubeGrid>>
            {
                { "/hlcustom", (m, b, g) => HandleCustomHighlight(m, b, g) },
                { "/hlsettransparency", (m, b, g) => HandleSetTransparency(m) },
                { "/hlsetintensity", (m, b, g) => HandleSetIntensity(m) },
                // Add other commands as needed
            };
        }

        public void HandleDictionary(Dictionary<IMySlimBlock, HighlightFilterType> gridHighlightedEntities, IMySlimBlock block, HighlightFilterType type)
        {
            if (!gridHighlightedEntities.ContainsKey(block))
            {
                gridHighlightedEntities.Add(block, type);

                if (DebugToggle)
                {
                    Log.Info($"Adding Block to Dictionary: {block}");
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
                case HighlightFilterType.Conveyor: return Color.Yellow;
                case HighlightFilterType.Thruster: return Color.Green;
                case HighlightFilterType.Power: return Color.SkyBlue;
                case HighlightFilterType.Weapon: return Color.Orange;
                case HighlightFilterType.Armor: return Color.Cyan;
                case HighlightFilterType.Steering: return Color.Indigo;
                case HighlightFilterType.Custom: return Color.White;
                default: return Color.White;
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