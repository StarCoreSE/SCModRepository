using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using Color = VRageMath.Color;

namespace klime.Visual
{
    public class GridGroup
    {
        #region Fields and Constants

        public List<GridRendering> gridGroup;
        public bool doneInitialCleanup, doneRescale;
        public double rotationForward, rotationUp, rotationForwardBase;
        public int timer;
        public Queue<IMyCubeBlock> DelList = new Queue<IMyCubeBlock>();
        public Queue<Vector3I> SlimList = new Queue<Vector3I>();
        public Queue<Vector3I> SlimDelList = new Queue<Vector3I>();
        public Queue<Vector3I> SlimDelQueue = new Queue<Vector3I>();
        public List<Vector3I> SlimDelListMP = new List<Vector3I>();
        public static int slimblocksToClose;
        public static int fatblocksToClose;

        public Queue<Vector3I> FatDelList = new Queue<Vector3I>();
        public MyStringHash stringHash;
        public Dictionary<Vector3I, float> BlockIntegrityDict = new Dictionary<Vector3I, float>();
        public Dictionary<Vector3I, float> FatBlockIntegrityDict = new Dictionary<Vector3I, float>();
        public static string DamageEntriesString = "";
        public static float TotalDamageSum = 0;
        public int SlimBlocksDestroyed = 0;
        public int FatBlocksDestroyed = 0;
        public Vector3D Position;

        private const float SlimBlockTransparency = 0.01f;
        private const float FatBlockTransparency = 1.5f;
        private const int SlimBlockCloseDecrement = 500;
        private const int FatBlockCloseDecrement = 500;
        private const int ProcessInterval = 120;
        private const int SlimBlockProcessInterval = 240;

        #endregion

        #region Initialization

        public GridGroup(List<GridRendering> gridGroup, double rotationForwardBase)
        {
            Init(gridGroup, rotationForwardBase);
        }

        public GridGroup(GridRendering gridR, double rotationForwardBase)
        {
            Init(new List<GridRendering> { gridR }, rotationForwardBase);
        }

        private void Init(List<GridRendering> group, double rotationForwardBase)
        {
            gridGroup = group;
            this.rotationForwardBase = rotationForwardBase;
        }

        #endregion

        #region Grid Setup

        public void DoCleanup() { ExecuteActionOnGrid(g => g.DoCleanup(), ref doneInitialCleanup); }
        public void DoRescale() { ExecuteActionOnGrid(g => g.DoRescale(), ref doneRescale); }

        private void ExecuteActionOnGrid(Action<GridRendering> action, ref bool flag)
        {
            foreach (var sg in gridGroup)
            {
                if (sg.grid != null)
                {
                    try
                    {
                        action(sg);
                        flag = true;
                    }
                    catch (Exception e)
                    {
                        MyLog.Default.WriteLine($"Error executing action on grid: {e.Message}");
                    }
                }
            }
        }

        #endregion

        #region Block Removal Process

        public void DoBlockRemove(Vector3I position)
        {
            for (int GGC = 0; GGC < gridGroup.Count; GGC++)
            {
                GridRendering subgrid = gridGroup[GGC];
                ProcessSubgrid(subgrid, position);
            }
        }

        private void ProcessSubgrid(GridRendering subgrid, Vector3I position)
        {
            if (subgrid.grid == null) return;

            var slim = subgrid.grid.GetCubeBlock(position) as IMySlimBlock;
            if (slim == null) return;

            if (slim.FatBlock == null)
            {
                ProcessSlimBlock(slim, subgrid);
                slimblocksToClose += SlimBlockCloseDecrement;
            }
            else
            {
                ProcessFatBlock(slim);
                fatblocksToClose += FatBlockCloseDecrement;
            }
        }

        private void ProcessSlimBlock(IMySlimBlock slim, GridRendering subgrid)
        {
            slim.Dithering = 1.45f;
            var blockKind = slim.Mass >= 500 ? 1 : 2;
            var colorHex = blockKind == 1 ? "#FF0000" : "#FFA500";
            UpdateSlimColorAndVisual(slim, subgrid.grid, colorHex);
        }

        private void UpdateSlimColorAndVisual(IMySlimBlock slim, MyCubeGrid subgrid, string colorHex)
        {
            var stringHash = MyStringHash.GetOrCompute("Neon_Colorable_Lights");
            var colorHSVOffset = GetRoundedHSVOffset(colorHex);
            
            subgrid.ChangeColorAndSkin(subgrid.GetCubeBlock(slim.Position), colorHSVOffset, stringHash);
       
            SetTransparency(slim, SlimBlockTransparency);

            SlimDelQueue.Enqueue(slim.Position);
            if (slimblocksToClose > 500)
            {
                slimblocksToClose++;
            }
            else
            {
                slimblocksToClose += 100;
            }
        }

        private static Vector3 GetRoundedHSVOffset(string colorHex)
        {
            var colorHSVOffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSVDX11(ColorExtensions.HexToColor(colorHex)));
            return new Vector3((float)Math.Round(colorHSVOffset.X, 2), (float)Math.Round(colorHSVOffset.Y, 2), (float)Math.Round(colorHSVOffset.Z, 2));
        }

        private void ProcessFatBlock(IMySlimBlock slim)
        {
            var customtimer = timer + 200;
            var color = GetFatBlockColor(slim.FatBlock.BlockDefinition.TypeId.ToString(), ref customtimer);
            int time = slim.FatBlock.Mass > 500 ? customtimer : timer + 10;

            SetTransparency(slim, FatBlockTransparency);
            DelList.Enqueue(slim.FatBlock);
            if (fatblocksToClose > 500)
            {
                fatblocksToClose++;
            }
            else
            {
                fatblocksToClose += 500;
            }

            MyVisualScriptLogicProvider.SetHighlightLocal(slim.FatBlock.Name, 3, 1, color);
        }

        private Color GetFatBlockColor(string typeId, ref int customtimer)
        {
            Color color = ColorExtensions.HexToColor("#8B0000");  // Default color

            Dictionary<string, Color> typeToColor = new Dictionary<string, Color>()
            {
                {"MyObjectBuilder_Gyro", Color.SteelBlue},
                {"MyObjectBuilder_ConveyorSorter", Color.Red},
                {"MyObjectBuilder_Thrust", Color.CadetBlue},
                {"MyObjectBuilder_GasTankDefinition", Color.CadetBlue},
                {"MyObjectBuilder_BatteryBlock", Color.Green},
                {"MyObjectBuilder_Reactor", Color.Green},
                {"MyObjectBuilder_SolarPanel", Color.Green},
                {"MyObjectBuilder_WindTurbine", Color.Green},
                {"MyObjectBuilder_Cockpit", Color.Purple},
            };

            if (typeToColor.ContainsKey(typeId))
            {
                color = typeToColor[typeId];
                if (typeId == "MyObjectBuilder_ConveyorSorter" || typeId == "MyObjectBuilder_Cockpit")
                {
                    customtimer = timer + 400;
                }
            }

            return color;
        }

        #endregion

        #region Main Update Loop

        public void UpdateMatrix(MatrixD renderMatrix, MatrixD rotMatrix)
        {
            if (!doneRescale || !doneInitialCleanup) return;
            if(slimblocksToClose > 0) slimblocksToClose--;
            if(fatblocksToClose > 0) fatblocksToClose--;

            timer++;
            UpdateBlockDestructionStats();
            ProcessFatBlocks();
            UpdateRenderMatrix(renderMatrix, rotMatrix);
        }

        private void UpdateBlockDestructionStats()
        {
            for (int i = 0; i < gridGroup.Count; i++)
            {
                UpdateSlimBlockDestruction(gridGroup[i]);
            }
        }

        private void ProcessFatBlocks()
        {
            if (fatblocksToClose % ProcessInterval > 0)
            {
                return;
            }

            while (DelList.Count > 0)
            {
                var fatBlock = DelList.Dequeue();
                fatBlock.Close();
                fatblocksToClose -= FatBlockCloseDecrement;
            }
        }

        private void UpdateSlimBlockDestruction(GridRendering subgrid)
        {
            if (slimblocksToClose % SlimBlockProcessInterval > 0) return;

            for (int i = 0; i < SlimDelQueue.Count; i++)
            {
                SlimDelListMP.Add(SlimDelQueue.Dequeue());
                slimblocksToClose -= SlimBlockCloseDecrement;
            }
            subgrid.grid.RazeGeneratedBlocks(SlimDelListMP);
            SlimDelListMP.Clear();  
            
            var stride = MathHelper.Clamp(SlimDelQueue.Count / 16, 1, 48);

            MyAPIGateway.Parallel.For(0, SlimDelQueue.Count, i =>
            {
                var slim = subgrid.grid.CubeBlocks as IMySlimBlock;
                if (slim != null)
                {
                    if (slim.FatBlock == null)
                    {
                        if (slim.Integrity <= 0)
                        {
                            SlimDelQueue.Enqueue(slim.Position);
                            SlimDelListMP.Add(slim.Position);
                            SlimBlocksDestroyed++;
                        }
                    }
                }
            }, stride);
        }

        private void UpdateRenderMatrix(MatrixD renderMatrix, MatrixD rotMatrix)
        {
            var origTranslation = renderMatrix.Translation;
            renderMatrix = rotMatrix * renderMatrix;
            renderMatrix.Translation = origTranslation;

            for (int i = 0; i < gridGroup.Count; i++)
            {
                gridGroup[i].UpdateMatrix(renderMatrix);
            }
        }

        #endregion

        #region Transparency Helpers

        private static void SetTransparency(IMySlimBlock cubeBlock, float transparency)
        {
            transparency = 0f - transparency;
            if (cubeBlock.Dithering != transparency || cubeBlock.CubeGrid.Render.Transparency != transparency)
            {
                cubeBlock.CubeGrid.Render.Transparency = transparency;
                cubeBlock.CubeGrid.Render.CastShadows = false;
                cubeBlock.Dithering = transparency;
                cubeBlock.UpdateVisual();
                if (cubeBlock.FatBlock is MyCubeBlock)
                {
                    MyCubeBlock fatBlock = (MyCubeBlock)cubeBlock.FatBlock;

                    fatBlock.Render.CastShadows = false;
                    SetTransparencyForSubparts(fatBlock, transparency);

                    if (fatBlock.UseObjectsComponent != null && fatBlock.UseObjectsComponent.DetectorPhysics != null)
                    {
                        fatBlock.UseObjectsComponent.DetectorPhysics.Enabled = false;
                    }
                }
            }
        }

        private static void SetTransparencyForSubparts(MyEntity renderEntity, float transparency)
        {
            renderEntity.Render.CastShadows = false;
            if (renderEntity.Subparts == null)
            {
                return;
            }

            foreach (KeyValuePair<string, MyEntitySubpart> subpart in renderEntity.Subparts)
            {
                subpart.Value.Render.Transparency = transparency;
                subpart.Value.Render.CastShadows = false;
                subpart.Value.Render.RemoveRenderObjects();
                subpart.Value.Render.AddRenderObjects();
                SetTransparencyForSubparts(subpart.Value, transparency);
            }
        }

        #endregion
    }
}