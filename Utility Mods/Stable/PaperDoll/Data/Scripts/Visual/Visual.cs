using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using CoreSystems.Api;
using ParallelTasks;
using VRageRender;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using Color = VRageMath.Color;
using Draygo.API;
using System.Text;
using Sandbox.Definitions;
using static Draygo.API.HudAPIv2;
using System.Linq;
using System.Diagnostics;
using Sandbox.Game.Entities.Cube;

namespace klime.Visual
{
    //Render grid
    public class GridR
    {
        public MyCubeGrid grid;
        public EntRender entRender = new EntRender();
        public MatrixD controlMatrix;
        public double scale, tempscale = 0.8;
        internal Task GridTask = new Task();
        Vector3D relTrans;
        public BoundingBoxD gridBox;
        public static MatrixD gridMatrix;
        public static MatrixD gridMatrixBackground;
        public static float billboardScaling;
        public Color BillboardRED;
        public Vector4 Billboardcolor;
        private MyStringId PaperDollBGSprite = MyStringId.TryGet("paperdollBG");
        private String ArmorMat = "Hazard_Armor";
        string ArmorRecolorHex = "#02f726";
        public bool runonce = false;
        public float oldFOV;
        public bool needsRescale;
        public static Vector3D GridBoxCenter;
        public static Vector3D GridBoxCenterGlobal;
        public static Vector3D hateVector;
        //public MyStringHash MaterialHash = MyStringHash.GetOrCompute("SciFi");

        public GridR(MyCubeGrid grid, EntRender entRender = null)
        {
            this.grid = grid;
            this.entRender = entRender ?? this.entRender;
        }

        public void UpdateMatrix(MatrixD renderMatrix)
        {
            var camera = MyAPIGateway.Session.Camera;
            float newFov = camera.FovWithZoom;
            if (oldFOV != newFov) { oldFOV = newFov; needsRescale = true; }

            float aspect = camera.ViewportSize.X / camera.ViewportSize.Y, fovTan = (float)Math.Tan(newFov * 0.5f), scaleFov = 0.1f * fovTan;
            Vector2D offset = new Vector2D(0.1, 0.1) * scaleFov;
            offset.X *= aspect; offset.Y *= aspect;

            float scale = scaleFov * 0.15f;


            MatrixD clonedWorldMatrix = grid.WorldMatrix;
            clonedWorldMatrix.Translation += Vector3D.Transform(grid.PositionComp.LocalAABB.Center, grid.PositionComp.WorldMatrixRef);

            renderMatrix.Translation += Vector3D.TransformNormal(relTrans, clonedWorldMatrix);

            MatrixD rotRenderMatrix = MatrixD.CreateTranslation(grid.PositionComp.WorldAABB.Center) * renderMatrix;
            hateVector = renderMatrix.Translation;
            hateVector -= Vector3D.TransformNormal(relTrans, clonedWorldMatrix);

            float lowerLimit = newFov < 1.2 ? 0.03f : 0.04f;
            float backOffsetQtr = (scale - 0.001f) * 0.25f, backOffsetHalf = (scale - 0.001f) * 0.2f, backOffsetEighth = (scale - 0.001f) * 0.125f;

            MatrixD transpMatrix = MatrixD.Transpose(renderMatrix);
            Vector3D moveVecBack = Vector3D.TransformNormal(-camera.WorldMatrix.Forward, renderMatrix);
            Vector3D moveVecLeft = Vector3D.TransformNormal(-camera.WorldMatrix.Right, renderMatrix);
            MatrixD bruhMatrix = renderMatrix;
            bruhMatrix.Translation += moveVecBack * backOffsetQtr + moveVecLeft * backOffsetHalf;

            MatrixD painMatrix = bruhMatrix, greaterPainMatrix = bruhMatrix;
            greaterPainMatrix.Translation += moveVecBack * backOffsetEighth + moveVecLeft * backOffsetEighth;
            painMatrix.Translation += Vector3D.TransformNormal(camera.WorldMatrix.Forward, renderMatrix) * backOffsetEighth + Vector3D.TransformNormal(camera.WorldMatrix.Right, renderMatrix) * backOffsetEighth;

            Vector3D left = camera.WorldMatrix.Left, up = camera.WorldMatrix.Up;
            float tempBillboardScaling = MathHelper.Clamp((float)grid.PositionComp.Scale * 0.9f, lowerLimit, 0.09f);
            billboardScaling = tempBillboardScaling;



            if (needsRescale)
            {
                float backOffset = (scale - 0.001f) * 0.1f;
                if (backOffset > 0)
                {
                    Vector3D moveVec = Vector3D.TransformNormal(-camera.WorldMatrix.Forward, transpMatrix);
                    renderMatrix.Translation += moveVec * backOffset;
                }
                DoRescale();
            }


            grid.WorldMatrix = renderMatrix;
            gridMatrixBackground = renderMatrix;


            AddBillboard(Color.Lime * 0.75f, hateVector, left, up, tempBillboardScaling, BlendTypeEnum.SDR);

        }

        private void AddBillboard(Color color, Vector3D pos, Vector3D left, Vector3D up, float scale, BlendTypeEnum blendType)
        {
            MyTransparentGeometry.AddBillboardOriented(PaperDollBGSprite, color.ToVector4(), pos, left, up, scale, blendType);
        }

        public void DoRescale()
        {
            HandleException(() => {

                var worldVolume = grid.PositionComp.WorldVolume;
                var worldMatrixRef = grid.PositionComp.WorldMatrixRef;
                var worldRadius = worldVolume.Radius;

                var camera = MyAPIGateway.Session.Camera;
                var newFov = camera.FovWithZoom;
                var fov = Math.Tan(newFov * 0.5);
                var scaleFov = 0.1f * fov;

                const float K = 1.35f;
                float hudScale = (float)(K / worldRadius);
                hudScale = MathHelper.Clamp(hudScale, 0.0001f, 0.05f);

                var minScale = worldRadius > 150 ? 0.0001f : 0.0005f;
                var scale = MathHelper.Clamp((float)(scaleFov * (hudScale * 0.23f)), minScale, 0.0008f);

                var modifiedCenter = Vector3D.Transform(GridBoxCenter, worldMatrixRef);
                controlMatrix *= MatrixD.CreateTranslation(-modifiedCenter) * worldMatrixRef;

                var localCenter = new Vector3D(worldVolume.Center);
                var trueCenter = Vector3D.Transform(localCenter, grid.WorldMatrix);

                grid.PositionComp.Scale = scale;
                relTrans = Vector3D.TransformNormal(GridBoxCenter, MatrixD.Transpose(grid.WorldMatrix)) * scale;
                GridBoxCenter = grid.PositionComp.LocalVolume.Center;
                relTrans = -GridBoxCenter;

                needsRescale = false;

            }, "rescaling grid");
        }


        public void DoCleanup()
        {
            // Ownership Change
            ChangeOwnership();

            // Rendering Setup
            SetupRendering();

            // Fetch and Process Blocks
            ProcessBlocks();
        }

        private void ChangeOwnership()
        {

            HandleException(() => {

                grid.ChangeGridOwnership(MyAPIGateway.Session.Player.IdentityId, MyOwnershipShareModeEnum.Faction);

            }, "changing grid ownership");

        }


        private void SetupRendering()
        {
            IMyCubeGrid iGrid = (IMyCubeGrid)grid;
            iGrid.Render.DrawInAllCascades = iGrid.Render.FastCastShadowResolve = iGrid.Render.MetalnessColorable = true;
        }

        private void RecolorArmor(Vector3I pos)
        {
            Vector3 armorHSV = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(ArmorRecolorHex)));
            armorHSV = RoundVector(armorHSV, 2);
            MyCubeGrid iGrid = (MyCubeGrid)grid;
            //iGrid.SkinBlocks(grid.Min, grid.Max, armorHSV, ArmorMat);
            var stringHash = MyStringHash.GetOrCompute(ArmorMat);
            iGrid.ChangeColorAndSkin(iGrid.GetCubeBlock(pos), armorHSV, stringHash);
        }

        private static Vector3 RoundVector(Vector3 vec, int decimals)
        {
            return new Vector3((float)Math.Round(vec.X, decimals), (float)Math.Round(vec.Y, decimals), (float)Math.Round(vec.Z, decimals));
        }

        private void ProcessBlocks()
        {
            List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();
            IMyCubeGrid iGrid = (IMyCubeGrid)grid;
            iGrid.GetBlocks(allBlocks);
            int count = allBlocks.Count;
            int stride = MathHelper.Clamp(count / 16, 1, 2000);
            MyAPIGateway.Parallel.For(0,count, i =>
            {
                var block = allBlocks[i];
                var pos = block.Position;
                RecolorArmor(pos);
                DisableBlock(block.FatBlock as IMyFunctionalBlock);
                StopEffects(block.FatBlock as IMyExhaustBlock);
                SetTransparency(block, 0.36f);
            },stride);

        }



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
               // subpart.Value.Render.AddRenderObjects();
                SetTransparencyForSubparts(subpart.Value, transparency);
            }
        }

        private void DisableBlock(IMyFunctionalBlock block)
        {
            if (block != null)
            {
                block.Enabled = false;
            }

        }


        private void StopEffects(IMyExhaustBlock exhaust)
        {
            exhaust?.StopEffects();
        }


        private void HandleException(Action action, string errorContext)
        {
            try { action(); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error {errorContext}: {e.Message}");
                MyAPIGateway.Utilities.ShowNotification($"An error occurred while {errorContext}. Please check the log for more details.", 5000, MyFontEnum.Red);
            }
        }

    }

    public class EntRender
    {
        public MyLight light;

        public EntRender()
        {
            light = new MyLight();
        }
    }

    public class GridG
    {
        public List<GridR> gridGroup;
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
        //public Dictionary<IMyCubeBlock, int> DelDict = new Dictionary<IMyCubeBlock, int>();
       // public Dictionary<Vector3I, int> SlimDelDict = new Dictionary<Vector3I, int>();
        public MyStringHash stringHash;
        public Dictionary<Vector3I, float> BlockIntegrityDict = new Dictionary<Vector3I, float>();
        public Dictionary<Vector3I, float> FatBlockIntegrityDict = new Dictionary<Vector3I, float>();
        public HashSet<DamageEntry> DamageEntries = new HashSet<DamageEntry>();
        public static string DamageEntriesString = "";
        public static float TotalDamageSum = 0;
        public  int SlimBlocksDestroyed = 0;
        public  int FatBlocksDestroyed = 0;
        public Vector3D Position;
        public GridG(List<GridR> gridGroup, double rotationForwardBase) { Init(gridGroup, rotationForwardBase); }
        public GridG(GridR gridR, double rotationForwardBase) { Init(new List<GridR> { gridR }, rotationForwardBase); }
        private void Init(List<GridR> group, double rotationForwardBase) { gridGroup = group; this.rotationForwardBase = rotationForwardBase; }

        public void DoCleanup() { ExecuteActionOnGrid(g => g.DoCleanup(), ref doneInitialCleanup); }
        public void DoRescale() { ExecuteActionOnGrid(g => g.DoRescale(), ref doneRescale); }
        private void ExecuteActionOnGrid(Action<GridR> action, ref bool flag) { foreach (var sg in gridGroup) { if (sg.grid != null) { action(sg); flag = true; } } }



        public void DoBlockRemove(Vector3I position)
        {
            // HandleException(() => SlimListClearAndAdd(position), "Clearing and Adding to SlimList");
           // SlimListClearAndAdd(position);
            for (int GGC = 0; GGC < gridGroup.Count; GGC++)
            {
                GridR subgrid = gridGroup[GGC];
                //HandleException(() => ProcessSubgrid(subgrid, position), "Iterating through gridGroup");
                ProcessSubgrid(subgrid, position);
            }
        }

        private void SlimListClearAndAdd(Vector3I position)
        {
          //  SlimList.Clear();
            SlimList.Enqueue(position);
            slimblocksToClose += 500;
        }

        public void DebugShowblockstoRemove()
        {
            MyAPIGateway.Utilities.ShowMessage("SlimList", slimblocksToClose.ToString());
            MyAPIGateway.Utilities.ShowMessage("Fatlist", slimblocksToClose.ToString());
        }

        private void ProcessSubgrid(GridR subgrid, Vector3I position)
        {
            if (subgrid.grid == null) return;

            var slim = subgrid.grid.GetCubeBlock(position) as IMySlimBlock;
            if (slim == null) return;

            if (slim.FatBlock == null)
            {
                ProcessSlimBlock(slim, subgrid);
                slimblocksToClose += 500;
            }
            else
            {
                ProcessFatBlock(slim);
                fatblocksToClose += 500;
            }
        }

        private void ProcessSlimBlock(IMySlimBlock slim, GridR subgrid)
        {

            slim.Dithering = 1.25f;
            var blockKind = slim.Mass >= 500 ? 1 : 2;
            var colorHex = blockKind == 1 ? "#FF0000" : "#FFA500";
            UpdateSlimColorAndVisual(slim, subgrid.grid, colorHex);
        }

        private void UpdateSlimColorAndVisual(IMySlimBlock slim, MyCubeGrid subgrid, string colorHex)
        {
            var stringHash = MyStringHash.GetOrCompute("Neon_Colorable_Lights");
            var colorHSVOffset = GetRoundedHSVOffset(colorHex);
            
            subgrid.ChangeColorAndSkin(subgrid.GetCubeBlock(slim.Position), colorHSVOffset, stringHash);
       
            SetTransparency(slim, 0.01f);

            //SlimDelDict.Add(slim.Position, timer + 150);
            SlimDelQueue.Enqueue(slim.Position);
            if (slimblocksToClose > 500)
            {
                slimblocksToClose++;
            }
            else
            {
                slimblocksToClose += 100;
            }
            //BlockIntegrityDict[slim.Position] = integrity;
        }

        private static Vector3 GetRoundedHSVOffset(string colorHex)
        {
            var colorHSVOffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSVDX11(ColorExtensions.HexToColor(colorHex)));
            return new Vector3((float)Math.Round(colorHSVOffset.X, 2), (float)Math.Round(colorHSVOffset.Y, 2), (float)Math.Round(colorHSVOffset.Z, 2));
        }

        private void ProcessFatBlock(IMySlimBlock slim)
        {
            //slim.Dithering = 1.5f;
            var customtimer = timer + 200;
            var color = GetFatBlockColor(slim.FatBlock.BlockDefinition.TypeId.ToString(), ref customtimer);
            int time = slim.FatBlock.Mass > 500 ? customtimer : timer + 10;

            //if (!DelDict.ContainsKey(slim.FatBlock)) DelDict.Add(slim.FatBlock, time);
            SetTransparency(slim, 1.5f);
            DelList.Enqueue(slim.FatBlock);
            if (fatblocksToClose > 500)
            {
                fatblocksToClose++;
            }
            else
            {
                fatblocksToClose += 500;
            }

            //     FatBlockIntegrityDict[slim.Position] = integrity;
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
        private static void HandleException(Action action, string errorContext)
        {
            try { action(); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error {errorContext}: {e.Message}");
                MyAPIGateway.Utilities.ShowNotification($"An error occurred while {errorContext}. Please check the log for more details.", 5000, MyFontEnum.Red);
            }
        }
        public class DamageEntry
        {
            public float SlimDamage { get; set; }
            public float FatBlockDamage { get; set; }
            public int Timestamp { get; set; }
            public DamageEntry(float slimDamage, float fatBlockDamage, int timestamp)
            {
                SlimDamage = slimDamage;
                FatBlockDamage = fatBlockDamage;
                Timestamp = timestamp;
            }
        }
        public string FormatDamage(double damage)
        {
            string[] sizes = { "", "K", "M", "B", "T" }; // Add more if needed
            int order = 0;
            while (damage >= 1000 && order < sizes.Length - 1)
            {
                order++;
                damage /= 1000;
            }

            return damage.ToString("F1") + sizes[order];
        }

        public void DisplayTotalDamage(float slimDamageLast10Seconds, float fatBlockDamageLast10Seconds)
        {

            DamageEntriesString = "";
            string damageMessage = "Total Damage: " + FormatDamage(TotalDamageSum) +
                                  "\nSlim Damage Last 10 Seconds: " + FormatDamage(slimDamageLast10Seconds) +
                                  "\nFatBlock Damage Last 10 Seconds: " + FormatDamage(fatBlockDamageLast10Seconds) +
                                  "\nSlim Blocks Destroyed: " + SlimBlocksDestroyed +
                                  "\nFat Blocks Destroyed: " + FatBlocksDestroyed;

            DamageEntriesString = damageMessage;
        }
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

        private void InitializeFrameData()
        {
         //   DelList.Clear();
         //   SlimDelList.Clear();
         //   FatDelList.Clear();
        }

        private void ProcessFatBlocks()
        {

            if (fatblocksToClose % 120 > 0) 
            {
                return;
            }

            for (int i = 0; i < DelList.Count; i++)
            {
                var fatblock = DelList.Dequeue();
                fatblock.Close();
                fatblocksToClose -= 500;
                //DebugShowblockstoRemove();
            }
        }

        //   private void ProcessSlimBlocks()
        //   {
        //       foreach (var slim in SlimDelDict.Keys)
        //       {
        //           if (SlimDelDict[slim] == timer)
        //           {
        //              // SlimDelList.Add(slim); // add vis here
        //               SlimDelQueue.Enqueue(slim);
        //           }
        //       }
        //   }

        private void UpdateBlockDestructionStats()
        {


            for (int i = 0; i < gridGroup.Count; i++)
            {
                UpdateSlimBlockDestruction(gridGroup[i]);
            }

          //  DamageEntries.Add(new DamageEntry(slimDamageThisFrame, fatBlockDamageThisFrame, timer));
        }

        private void UpdateSlimBlockDestruction(GridR subgrid)
        {

            if (slimblocksToClose % 240 > 0) return;

            for (int i = 0; i < SlimDelQueue.Count; i++)
            {
                // subgrid.grid.RazeBlock(SlimDelQueue.Dequeue());
                //subgrid.grid.RazeGeneratedBlocks(SlimDelListMP);
                SlimDelListMP.Add(SlimDelQueue.Dequeue());
                slimblocksToClose -= 500;
                //DebugShowblockstoRemove();
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
                         //SlimDelList.Add(slim.Position);
                         SlimDelQueue.Enqueue(slim.Position);
                         SlimDelListMP.Add(slim.Position);
                         SlimBlocksDestroyed++;
                     }
                 }
             }
         }, stride);
            
        }

        private void UpdateFatBlockDestruction(ref float fatBlockDamageThisFrame)
        {
         //  foreach (var item in FatDelList)
         //  {
         //      fatBlockDamageThisFrame += FatBlockIntegrityDict[item];
         //      FatBlockIntegrityDict.Remove(item);
         //  }
        }

        private void AggregateDamageOverTime()
        {
            float slimDamageLast10Seconds = 0;
            float fatBlockDamageLast10Seconds = 0;

            List<DamageEntry> oldEntries = new List<DamageEntry>();
            foreach (var entry in DamageEntries)
            {
                if (timer - entry.Timestamp <= 600)
                {
                    slimDamageLast10Seconds += entry.SlimDamage;
                    fatBlockDamageLast10Seconds += entry.FatBlockDamage;
                }
                else
                {
                    oldEntries.Add(entry);
                }
            }



            TotalDamageSum += slimDamageLast10Seconds;
            TotalDamageSum += fatBlockDamageLast10Seconds;

            foreach (var oldEntry in oldEntries) DamageEntries.Remove(oldEntry);
            DisplayTotalDamage(slimDamageLast10Seconds, fatBlockDamageLast10Seconds);
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
    }

    public class EntVis
    {
        public MyCubeGrid realGrid;
        public MatrixD realGridBaseMatrix;
        public GridG visGrid;
        public GridG visGridSelf;
        public GridG visGridString;
        public int lifetime;
        public ushort netID = 39302;
        public bool isClosed;
        public double xOffset, yOffset, rotOffset;
        public List<IMySlimBlock> BlocksForBillboards = new List<IMySlimBlock>();
        public List<MyBillboard> persistantbillboards = new List<MyBillboard>();
        public Color BillboardRED;
        public Vector4 Billboardcolor;
        private MyStringId PaperDollBGSprite = MyStringId.TryGet("paperdollBG");
        private readonly Stopwatch stopwatch = new Stopwatch();
        private long lastUpdateTime = 0;
        private readonly long interval;  // in ticks
        private readonly double updateIntervalInSeconds = 0.5 / 120;

        public EntVis(MyCubeGrid realGrid, double xOffset, double yOffset, double rotOffset)
        {
            stopwatch.Start();
            interval = (long)(updateIntervalInSeconds * 10000000);  // Convert seconds to ticks

            this.realGrid = realGrid;
            this.realGridBaseMatrix = realGrid.WorldMatrix;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.rotOffset = rotOffset;

            RegisterEvents();
            GenerateClientGrids();
        }

        private void RegisterEvents() => SendMessage(new UpdateGridPacket(realGrid.EntityId, RegUpdateType.Add));

        private void SendMessage(object packet) => MyAPIGateway.Multiplayer.SendMessageTo(netID, MyAPIGateway.Utilities.SerializeToBinary(packet), MyAPIGateway.Multiplayer.ServerId);

        public void BlockRemoved(Vector3I pos)
        {
            visGrid?.DoBlockRemove(pos);
            //add hitmarker sound here
        }


        public void GenerateClientGrids()
        {
            HandleException(() =>
            {
                var realOB = (MyObjectBuilder_CubeGrid)realGrid.GetObjectBuilder();
                realOB.CreatePhysics = false;
                MyEntities.RemapObjectBuilder(realOB);
                MyAPIGateway.Entities.CreateFromObjectBuilderParallel(realOB, false, CompleteCall);
            }, "generating client grids");
        }

        private void CompleteCall(IMyEntity obj)
        {
            HandleException(() =>
            {
                if (isClosed) return;
                var grid = (MyCubeGrid)obj;
                grid.SyncFlag = grid.Save = grid.Render.NearFlag = grid.Render.FadeIn = grid.Render.FadeOut = grid.Render.CastShadows = grid.Render.NeedsResolveCastShadow = false;
                grid.GridPresenceTier = MyUpdateTiersGridPresence.Tier1;
                MyAPIGateway.Entities.AddEntity(grid); 
                visGrid = new GridG(new GridR(grid), rotOffset);
            }, "completing the call");
        }

        public void Update()
        {
            long currentTime = stopwatch.ElapsedTicks;
            UpdateVisLogic();
            UpdateRealLogic();
            if (currentTime - lastUpdateTime >= interval)
            {
                UpdateVisPosition();
                lastUpdateTime = currentTime;
            }

            
        }


        // Declare these as class-level variables to reuse and minimize memory allocation.
        private Vector2D offset = new Vector2D();
        private Vector3D localCenterRealGrid = new Vector3D();
        private Vector3D position = new Vector3D();
        private MatrixD offsetMatrix = new MatrixD();

        private void UpdateVisPosition()
        {
            if (visGrid == null || realGrid == null || realGrid.MarkedForClose)
                return;

            var camera = MyAPIGateway.Session.Camera;
            double newFov = camera.FovWithZoom;
            double fov = Math.Tan(newFov * 0.5);
            double aspectRatio = camera.ViewportSize.X / camera.ViewportSize.Y;
            double scaleFov = 0.1 * fov;

            offset.X = xOffset + 2.52;
            offset.Y = yOffset + 1.5;
            offset.X *= scaleFov * aspectRatio;
            offset.Y *= scaleFov;

            var tempMatrix = camera.WorldMatrix;
            position = Vector3D.Transform(new Vector3D(offset.X, offset.Y, 10 * scaleFov), tempMatrix);

            float scale = (float)(scaleFov * (2.55f * 0.23f));

            localCenterRealGrid = realGrid.PositionComp.LocalAABB.Center;
            offsetMatrix = MatrixD.CreateTranslation(localCenterRealGrid - realGrid.PositionComp.WorldAABB.Center);
            var newWorldMatrix = offsetMatrix * realGrid.WorldMatrix;

            tempMatrix.Translation += tempMatrix.Forward * (0.1 / (0.6 * newFov)) + tempMatrix.Right * xOffset + tempMatrix.Down * yOffset;

            visGrid.UpdateMatrix(tempMatrix, newWorldMatrix * MatrixD.Invert(tempMatrix));
        }



        // Function to get the origin from UpdateBackground
        private Vector3D GetBillboardOrigin(IMyCamera camera)
        {
            var cameraMatrix = camera.WorldMatrix;
            var fov = Math.Tan(camera.FovWithZoom * 0.5);
            var scaleFov = 0.1 * fov;
            var offset = new Vector2D(xOffset + 13, yOffset - 10);
            offset.X *= scaleFov * (camera.ViewportSize.X / camera.ViewportSize.Y);
            offset.Y *= scaleFov;
            var position = Vector3D.Transform(new Vector3D(offset.X, offset.Y, -0.9), cameraMatrix);
            return position;
        }


        //DS reference code:
        public void UpdateBackground()
        {

            var camera = MyAPIGateway.Session.Camera;
            var newFov = camera.FovWithZoom;
            var aspectRatio = camera.ViewportSize.X / camera.ViewportSize.Y;

            var fov = Math.Tan(newFov * 0.5);
            var scaleFov = 0.1 * fov;
            var offset = new Vector2D(xOffset + 6.5, yOffset - 4.85);
            offset.X *= scaleFov * aspectRatio;
            offset.Y *= scaleFov;
            var tempMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var position = Vector3D.Transform(new Vector3D(offset.X, offset.Y, -.9), tempMatrix);
            //fix billboard rendering on top of paper doll when at maximum zoom by changing that position value from -.9 to something like -1.1 I guess
            var origin = position;
            var left = tempMatrix.Left;
            var up = tempMatrix.Up;
            var hudscale = 18f;
            var scale = (float)(scaleFov * (hudscale * 0.23f));

            Billboardcolor = (Color.Lime * 0.75f).ToVector4();
           // MyTransparentGeometry.AddBillboardOriented(PaperDollBGSprite, Billboardcolor, origin, left, up, scale, BlendTypeEnum.SDR);

        }


        private void UpdateVisLogic()
        {
            if (visGrid == null) return;
            if (!visGrid.doneInitialCleanup) { visGrid.DoCleanup(); return; }
            if (!visGrid.doneRescale) visGrid.DoRescale();
        }

        private void UpdateRealLogic()
        {
            if (realGrid?.MarkedForClose == true || realGrid?.Physics == null) Close();
        }

        public void Close()
        {
            visGrid?.gridGroup.ForEach(sub => sub.grid.Close());
            SendMessage(new UpdateGridPacket(realGrid.EntityId, RegUpdateType.Remove));
            isClosed = true;
        }

        private void HandleException(Action action, string errorContext)
        {
            try { action(); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error {errorContext}: {e.Message}");
                MyAPIGateway.Utilities.ShowNotification($"An error occurred while {errorContext}. Please check the log for more details.", 5000, MyFontEnum.Red);
            }
        }
    }

}
