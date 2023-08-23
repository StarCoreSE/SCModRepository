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
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using VRage.GameServices;
using System.Linq;
using Sandbox;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.Render.Scene;
using System.Drawing;
using Color = VRageMath.Color;

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
        Vector3D relTrans, relForward, relUp;
        public BoundingBoxD gridBox;
        public MatrixD gridMatrix;
        private BoundingBoxD aabb;
        public Color BillboardRED;
        public Vector4 color;
       
        public bool runonce = false;
        private MyStringId Material = MyStringId.GetOrCompute("Square");
        public MyStringHash MaterialHash = MyStringHash.GetOrCompute("SciFi");

        public GridR(MyCubeGrid grid, EntRender entRender = null)
        {
            this.grid = grid;
            this.entRender = entRender ?? this.entRender;
        }

        public void UpdateMatrix(MatrixD renderMatrix)
        {
            renderMatrix.Translation += Vector3D.TransformNormal(relTrans, renderMatrix);
            grid.WorldMatrix = renderMatrix;
            gridMatrix = renderMatrix;
            gridBox = grid.PositionComp.LocalAABB;

            // renderMatrix.Translation += renderMatrix.Forward;
            MatrixD cameramatrixD = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D cameraTranslation = MyAPIGateway.Session.Camera.WorldMatrix.Translation;
            Vector3D cameraForward = MyAPIGateway.Session.Camera.WorldMatrix.Forward;
            Vector3D cameraDown = MyAPIGateway.Session.Camera.WorldMatrix.Down;
            Vector3D cameraRight = MyAPIGateway.Session.Camera.WorldMatrix.Right;
            Vector3D leftCameraVector = MyAPIGateway.Session.Camera.WorldMatrix.Left;
            Vector3D upCameraVector = MyAPIGateway.Session.Camera.WorldMatrix.Up;
            Vector3D backgroundvector =
                cameraTranslation +
                (cameraForward * 1f) 
              + (cameraRight * 0.75f) 
              + (cameraDown * 0.35f); ;

            Vector4 color = (Color.Lime * 0.05f).ToVector4();
            MyTransparentGeometry.AddBillboardOriented(Material, color, backgroundvector, leftCameraVector, upCameraVector, 0.25f, 0.25f, null);

            //MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("WeaponLaser"), Color.White.ToVector4(), backgroundvector + leftCameraVector, cameraRight, 0.5f, 0.5f, BlendTypeEnum.SDR);
            //MySimpleObjectDraw.DrawAttachedTransparentBox(ref gridMatrix, ref gridBox, ref BillboardRED, uint.MaxValue ,ref cameramatrixD, MySimpleObjectRasterizer.SolidAndWireframe, wiredivratio, 0.04f, MyStringId.GetOrCompute("Square"), MyStringId.GetOrCompute("WeaponLaser"), false, MyBillboard.BlendTypeEnum.SDR);


        }

        public void DoRescale()
        {
            var volume = grid.PositionComp.WorldVolume;
            scale = 0.039 / volume.Radius * (grid.GridSizeEnum == MyCubeSize.Small ? 0.8 : 1);
            relTrans = Vector3D.TransformNormal(grid.WorldMatrix.Translation - grid.PositionComp.WorldAABB.Center, MatrixD.Transpose(grid.WorldMatrix)) * scale;
            grid.PositionComp.Scale = (float)scale;
        }

        public void DoCleanup()
        {
            grid.ChangeGridOwnership(MyAPIGateway.Session.Player.IdentityId, MyOwnershipShareModeEnum.Faction);


            //if (grid.IsPowered) grid.SwitchPower();



            List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();

            IMyCubeGrid iGrid = grid as IMyCubeGrid;
            iGrid.GetBlocks(allBlocks);
            iGrid.Render.DrawInAllCascades = true;
            iGrid.Render.FastCastShadowResolve = true;
            iGrid.Render.MetalnessColorable = true;
            string OrangeHex = "#F5F5DC";
            Vector3 orangeHSVOffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(OrangeHex)));
            orangeHSVOffset = new Vector3((float)Math.Round(orangeHSVOffset.X, 2), (float)Math.Round(orangeHSVOffset.Y, 2), (float)Math.Round(orangeHSVOffset.Z, 2));
            iGrid.ColorBlocks(iGrid.Min, iGrid.Max, orangeHSVOffset);

            //iGrid.Render.Transparency = 0.05f;
            foreach (var block in allBlocks)
            {
                block.Dithering = 2.7f;
                //block.CubeGrid

             //   gridMatrix = block.CubeGrid.WorldMatrix;

              //  gridBox = new BoundingBoxD();
            //    gridBox = block.CubeGrid.PositionComp.LocalAABB;

               // block.GetWorldBoundingBox(out gridBox);
               // MySimpleObjectDraw.DrawTransparentBox(ref gridMatrix, ref gridBox, ref BillboardRED, MySimpleObjectRasterizer.Solid, 1, 0.04f, MyStringId.GetOrCompute("Square"), MyStringId.GetOrCompute("Square"), false, -1, MyBillboard.BlendTypeEnum.PostPP,1, null);
           //   block.CubeGrid.Render.ShadowBoxLod = false;
           //   block.CubeGrid.Render.EnableColorMaskHsv = true;
           //   block.CubeGrid.Render.OffsetInVertexShader = true;
           //  block.CubeGrid.Transparent = true;
           //  block.CubeGrid.Render.Transparency = 0.01f;
           //  block.CubeGrid.FastCastShadowResolve = false;
           // // block.CubeGrid.Render.UpdateTransparency();
           //   block.CubeGrid.Render.UpdateRenderObject(true, false);
           //   block.UpdateVisual();
                // block.FatBlock?.Render.UpdateTransparency();
                // grid.ChangeColorAndSkin(grid.GetCubeBlock(block.Position), purpleHSVoffset);
            }

            foreach (var fatblock in grid.GetFatBlocks())
            {
                DisableBlock(fatblock as IMyFunctionalBlock);
                StopEffects(fatblock as IMyExhaustBlock);
                DisableBlock(fatblock as IMyLightingBlock);

            }
                
            //MyVisualScriptLogicProvider.SetAlphaHighlight(iGrid.Name, true, 10, 1, Color.Fuchsia, -1, null, 2f);
            

            // grid.Render.Transparency = -0.01f;


            // string purpleHEx = "#FF0000";
            //    Vector3 purpleHSVoffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(purpleHEx)));
            //    purpleHSVoffset = new Vector3((float)Math.Round(purpleHSVoffset.X, 2), (float)Math.Round(purpleHSVoffset.Y, 2), (float)Math.Round(purpleHSVoffset.Z, 2));
            // grid.ChangeColorAndSkin(grid.GetCubeBlock(block.Position), purpleHSVoffset);
            // List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();
            //    IMyCubeGrid iGrid = grid as IMyCubeGrid;
            //    iGrid.GetBlocks(allBlocks);
            // 
            //     //grid.ColorBlocks(grid.Min, grid.Max, purpleHSVoffset, false, false);
            //     ////iGrid.ColorBlocks(iGrid.Min, iGrid.Max, purpleHSVoffset);
            //     ////grid.ColorGrid(purpleHSVoffset, false, false);
            // 

            //   //grid.Render.Transparency = -0.01f;
        }

        private void DisableBlock(IMyFunctionalBlock block)
        {
            if (block != null)
            {

                block.Enabled = false;
                block.Render.ShadowBoxLod = false;
                block.SlimBlock.Dithering = 1.95f; // this works!
                block.Visible = false;
                block.SlimBlock.UpdateVisual();
                block.Render.UpdateTransparency();

            
            }

        }

        private void MakeTransparent(IMyCubeBlock block)
        {
            if (block != null)
            {
                block.SlimBlock.Dithering = 2.5f; // this works!
                                       //slim.CubeGrid.ColorBlocks(slim.Position, slim.Position, new Vector3(0, 0, 0));
                string OrangeHex = "#FFA500";
                Vector3 orangeHSVOffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(OrangeHex)));
                orangeHSVOffset = new Vector3((float)Math.Round(orangeHSVOffset.X, 2), (float)Math.Round(orangeHSVOffset.Y, 2), (float)Math.Round(orangeHSVOffset.Z, 2));
                grid.ChangeColorAndSkin(grid.GetCubeBlock(block.Position), orangeHSVOffset);
                block.SlimBlock.UpdateVisual();
            }

        }

        private void StopEffects(IMyExhaustBlock exhaust)
        {
            exhaust?.StopEffects();
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
        public List<IMyCubeBlock> DelList = new List<IMyCubeBlock>();
        public List<Vector3I> SlimList = new List<Vector3I>();
        public List<Vector3I> SlimDelList = new List<Vector3I>();
        public List<Vector3I> FatDelList = new List<Vector3I>();
        public Dictionary<IMyCubeBlock, int> DelDict = new Dictionary<IMyCubeBlock, int>();
        public Dictionary<Vector3I, int> SlimDelDict = new Dictionary<Vector3I, int>();
        public MyStringHash stringHash = MyStringHash.GetOrCompute("Hazard_Armor");
        public Dictionary<Vector3I, float> BlockIntegrityDict = new Dictionary<Vector3I, float>();
        public Dictionary<Vector3I, float> FatBlockIntegrityDict = new Dictionary<Vector3I, float>();
        public List<DamageEntry> DamageEntries = new List<DamageEntry>();
        public static float TotalDamageSum = 0;
        public int SlimBlocksDestroyed = 0;
        public int FatBlocksDestroyed = 0;
        public GridG(List<GridR> gridGroup, double rotationForwardBase) { Init(gridGroup, rotationForwardBase); }
        public GridG(GridR gridR, double rotationForwardBase) { Init(new List<GridR> { gridR }, rotationForwardBase); }
        private void Init(List<GridR> group, double rotationForwardBase) { gridGroup = group; this.rotationForwardBase = rotationForwardBase; }

        public void DoCleanup() { ExecuteActionOnGrid(g => g.DoCleanup(), ref doneInitialCleanup); }
        public void DoRescale() { ExecuteActionOnGrid(g => g.DoRescale(), ref doneRescale); }
        private void ExecuteActionOnGrid(Action<GridR> action, ref bool flag) { foreach (var sg in gridGroup) { if (sg.grid != null) { action(sg); flag = true; } } }
        
        public void DoBlockRemove(Vector3I position)
        {
            SlimList.Clear(); SlimList.Add(position);
            foreach (var subgrid in gridGroup)
            {
                if (subgrid.grid == null) continue;
                var slim = subgrid.grid.GetCubeBlock(position) as IMySlimBlock;
                if (slim == null) continue;
                float integrity = slim.MaxIntegrity;
                //MyVisualScriptLogicProvider.SetAlphaHighlight(slim.CubeGrid.Name, true, 2, 1, Color.Red, -1, null, 0.9f);
                if (slim.FatBlock == null && (!SlimDelDict.ContainsKey(slim.Position))) 
                {
                    slim.Dithering = 1.1f; // this works!
                    //slim.CubeGrid.ColorBlocks(slim.Position, slim.Position, new Vector3(0, 0, 0));
                    string redHex = "#FF0000";
                    Vector3 redHSVOffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(redHex)));
                    redHSVOffset = new Vector3((float)Math.Round(redHSVOffset.X, 2), (float)Math.Round(redHSVOffset.Y, 2), (float)Math.Round(redHSVOffset.Z, 2));
                    subgrid.grid.ChangeColorAndSkin(subgrid.grid.GetCubeBlock(slim.Position), redHSVOffset, stringHash);
                    
                 
                    slim.UpdateVisual();
                    int time = timer + 200; 
                    SlimDelDict.Add(slim.Position, time);
                    BlockIntegrityDict[slim.Position] = integrity;
                }
                else
                {
                    
                    slim.Dithering = 2.5f;
                    MyVisualScriptLogicProvider.SetHighlightLocal(slim.FatBlock.Name, 10, 10, Color.Red);
                    int time = slim.FatBlock.Mass > 500 ? timer + 200 : timer + 10;
                    if (!DelDict.ContainsKey(slim.FatBlock)) DelDict.Add(slim.FatBlock, time);
                    FatBlockIntegrityDict[slim.Position] = integrity;
                }
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
            string damageMessage = "Total Damage: " + FormatDamage(TotalDamageSum) +
                                  "\nSlim Damage Last 10 Seconds: " + FormatDamage(slimDamageLast10Seconds) +
                                  "\nFatBlock Damage Last 10 Seconds: " + FormatDamage(fatBlockDamageLast10Seconds) +
                                  "\nSlim Blocks Destroyed: " + SlimBlocksDestroyed +
                                  "\nFat Blocks Destroyed: " + FatBlocksDestroyed;
            MyAPIGateway.Utilities.ShowNotification(damageMessage, 16, MyFontEnum.Red); // this is the notification that displays the debug message, use texthudAPI instead
        }
        public void UpdateMatrix(MatrixD renderMatrix, MatrixD rotMatrix)
        {
            if (!doneRescale || !doneInitialCleanup) return;
            timer++;
            DelList.Clear();
            SlimDelList.Clear();
            FatDelList.Clear();

            float slimDamageLast10Seconds = 0;
            float fatBlockDamageLast10Seconds = 0;
            float slimDamageThisFrame = 0;
            float fatBlockDamageThisFrame = 0;


            foreach (var fatblock in DelDict.Keys) { if (DelDict[fatblock] == timer) { fatblock.Close(); DelList.Add(fatblock); FatDelList.Add(fatblock.Position); } }
            
            foreach (var item in DelList) DelDict.Remove(item);

            foreach (var slim in SlimDelDict.Keys) { if (SlimDelDict[slim] == timer) {SlimDelList.Add(slim); /* add vis here*/ } }


            foreach (var subgrid in gridGroup)
            {
                if (subgrid.grid == null) continue;

                foreach (var item in SlimDelList)
                {
                    slimDamageThisFrame += BlockIntegrityDict[item];
                    BlockIntegrityDict.Remove(item);
                    subgrid.grid.RazeGeneratedBlocks(SlimDelList);
                    
                }

                foreach (var item in FatDelList)
                {
                    fatBlockDamageThisFrame += FatBlockIntegrityDict[item];
                    FatBlockIntegrityDict.Remove(item);

                }
                FatBlocksDestroyed += DelList.Count; // Assuming DelList contains FatBlocks
                SlimBlocksDestroyed += SlimDelList.Count;
            }



            DamageEntries.Add(new DamageEntry(slimDamageThisFrame, fatBlockDamageThisFrame, timer));

            List<DamageEntry> oldEntries = new List<DamageEntry>();
            foreach (var entry in DamageEntries)
            {
                if (timer - entry.Timestamp <= 600)
                {
                    slimDamageLast10Seconds += entry.SlimDamage;
                    fatBlockDamageLast10Seconds += entry.FatBlockDamage;
                }
                else
                    oldEntries.Add(entry);
            }

            TotalDamageSum += slimDamageLast10Seconds;
            TotalDamageSum += fatBlockDamageLast10Seconds;
            foreach (var oldEntry in oldEntries)
                DamageEntries.Remove(oldEntry);

            // Display both damages
            DisplayTotalDamage(slimDamageLast10Seconds, fatBlockDamageLast10Seconds);


            rotationForward = rotationForwardBase + rotationForward;
            var rotateMatrix = MatrixD.CreateRotationY(rotationForwardBase);
            renderMatrix = rotateMatrix * renderMatrix;
            var origTranslation = renderMatrix.Translation;
            renderMatrix = rotMatrix * renderMatrix;
            renderMatrix.Translation = origTranslation;
            foreach (var subgrid in gridGroup) { if (subgrid.grid != null) subgrid.UpdateMatrix(renderMatrix); }

            // I don't know why this works but it does. Don't touch it.
        }
    }

    public class EntVis
    {
        public MyCubeGrid realGrid;
        public MatrixD realGridBaseMatrix;
        public GridG visGrid;
        public int lifetime;
        public ushort netID = 39302;
        public bool isClosed;
        public double xOffset, yOffset, rotOffset;
        public List<IMySlimBlock> BlocksForBillboards = new List<IMySlimBlock>();
        public List<MyBillboard> persistantbillboards = new List<MyBillboard>();
        public Color BillboardRED;

        public EntVis(MyCubeGrid realGrid, double xOffset, double yOffset, double rotOffset)
        {
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

        public void BlockRemoved(Vector3I pos) => visGrid?.DoBlockRemove(pos);

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
                

                //      grid.Render.PersistentFlags -= MyPersistentEntityFlags2.CastShadows;
                //      grid.DisplayName = "";
                //      //grid.IsPreview = true;
                //      grid.GridPresenceTier = MyUpdateTiersGridPresence.Tier1;
                //      grid.RemoveFromGamePruningStructure();
                //       string purpleHEx = "#7851A9";
                //          Vector3 purpleHSVoffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(purpleHEx)));
                //          purpleHSVoffset = new Vector3((float)Math.Round(purpleHSVoffset.X, 2), (float)Math.Round(purpleHSVoffset.Y, 2), (float)Math.Round(purpleHSVoffset.Z, 2));
                //      grid.ColorGrid(purpleHSVoffset, false);
                //    grid.GetCubeBlock(BlocksForBillboards);

                // var bruh = new Vector3I(0, 0, 0);
                //    var igrid = (IMyCubeGrid)grid;

                // igrid.GetBlocks(BlocksForBillboards);
                //    BuildBlocksClient
                //  grid.ChangeColorAndSkin(grid.BuildBlocksClient)
                //     var igrid = (IMyCubeGrid)grid;

                //     igrid.GetBlocks(BlocksForBillboards);

          //       foreach (var block in BlocksForBillboards)
          //       {
          //         var squaremat =  MyStringId.GetOrCompute("Square");
          //           var color = Color.Red;
          //           BoundingBoxD box;
          //           box = new BoundingBoxD(block.Min, block.Max);
          //           MatrixD MatrixD = block.CubeGrid.WorldMatrix;
          //           BillboardRED = Color.Red;
          //        uint renderObjectID = block.CubeGrid.Render.GetRenderObjectID();
          //           MySimpleObjectDraw.DrawTransparentBox(ref MatrixD, ref box, ref color, MySimpleObjectRasterizer.Solid, 1, 0.04f, squaremat, squaremat, false, -1 , BlendTypeEnum.AdditiveTop, 1, persistantbillboards);
          //           //MySimpleObjectDraw.DrawAttachedTransparentBox(ref realGridBaseMatrix,ref box,ref BillboardRED, renderObjectID,ref MatrixD,MySimpleObjectRasterizer.SolidAndWireframe, 4,0.001f,squaremat,squaremat,false);
          //    }
                MyAPIGateway.Entities.AddEntity(grid); //right hewre
                visGrid = new GridG(new GridR(grid), rotOffset);
            }, "completing the call");
        }

        public void Update()
        {
            UpdateVisLogic();
            UpdateVisPosition();
            UpdateRealLogic();
            lifetime++;
        }

        private void UpdateVisPosition()
        {
            if (visGrid != null && realGrid != null && !realGrid.MarkedForClose)
            {
                var playerCamera = MyAPIGateway.Session.Camera;
                var renderMatrix = playerCamera.WorldMatrix;
                renderMatrix.Translation += renderMatrix.Forward * (0.1 / (0.6 * playerCamera.FovWithZoom)) + renderMatrix.Right * xOffset + renderMatrix.Down * yOffset;
                visGrid.UpdateMatrix(renderMatrix, realGrid.WorldMatrix * MatrixD.Invert(renderMatrix));
            }
        }

        private void UpdateVisLogic()
        {
            if (visGrid == null) return;
            if (!visGrid.doneInitialCleanup) visGrid.DoCleanup();
            if (!visGrid.doneRescale) visGrid.DoRescale();
        }

        private void UpdateRealLogic()
        {
            if (realGrid?.MarkedForClose == true || realGrid?.Physics == null || !realGrid.IsPowered) Close();
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

    // Networking
    [ProtoInclude(1000, typeof(UpdateGridPacket))]
    [ProtoInclude(2000, typeof(FeedbackDamagePacket))]
    [ProtoContract]
    public class Packet
    {
        public Packet()
        {
        }
    }

    [ProtoContract]
    public class UpdateGridPacket : Packet
    {
        [ProtoMember(1)]
        public RegUpdateType regUpdateType;
        [ProtoMember(2)]
        public List<long> entityIds;

        public UpdateGridPacket()
        {
        }

        public UpdateGridPacket(List<long> registerEntityIds, RegUpdateType regUpdateType)
        {
            this.entityIds = new List<long>(registerEntityIds);
            this.regUpdateType = regUpdateType;
        }

        public UpdateGridPacket(long registerEntityId, RegUpdateType regUpdateType)
        {
            this.entityIds = new List<long>
            {
                registerEntityId
            };
            this.regUpdateType = regUpdateType;
        }
    }

    [ProtoContract]
    public class FeedbackDamagePacket : Packet
    {
        [ProtoMember(11)]
        public long entityId;
        [ProtoMember(12)]
        public Vector3I position;

        public FeedbackDamagePacket()
        {
        }

        public FeedbackDamagePacket(long entityId, Vector3I position)
        {
            this.entityId = entityId;
            this.position = position;
        }
    }

    public enum RegUpdateType
    {
        Add,
        Remove
    }

    public enum RequestPaperDoll
    {
        On,
        Off
    }

    public enum ViewState
    {
        Idle,
        Searching,
        SearchingAll,
        SearchingWC,
        Locked,
        GoToIdle,
        GoToIdleWC,
        DoubleSearching
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Visual : MySessionComponentBase
    {
        public ushort feedbackNetID = 38492;
        public ushort netID = 39302;
        Dictionary<ulong, List<IMyCubeGrid>> serverTracker = new Dictionary<ulong, List<IMyCubeGrid>>();
        bool validInputThisTick = false;
        public ViewState viewState = ViewState.Idle;
        public RequestPaperDoll requestPaperDoll = RequestPaperDoll.Off;
        List<EntVis> allVis = new List<EntVis>();
        WcApi wcAPI;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
        }

        public override void LoadData()
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(feedbackNetID, FeedbackHandler);
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                wcAPI = new WcApi();
                wcAPI.Load(WCRegistered, true);
            }
        }

        private void WCRegistered()
        {
            // This needs to be here
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Session.Player?.Character == null || MyAPIGateway.Session.Camera == null) return;

            validInputThisTick = ValidInput();

            if (validInputThisTick && IsAdmin(MyAPIGateway.Session.Player) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.T))
            {
                viewState = viewState == ViewState.GoToIdleWC ? ViewState.SearchingWC : ViewState.GoToIdleWC;
                ToggleRequestPaperDoll();
            }

            if (viewState == ViewState.SearchingWC)
            {
                MyEntity controlEnt = (MyEntity)(MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit);
                ExecuteViewStateSearchingWC(controlEnt);
            }

            if (viewState == ViewState.GoToIdle || viewState == ViewState.GoToIdleWC)
            {
                ClearAllVis();
                viewState = viewState == ViewState.GoToIdleWC && requestPaperDoll == RequestPaperDoll.On ? ViewState.SearchingWC : ViewState.Idle;
            }
        }

        private void ToggleRequestPaperDoll()
        {
            requestPaperDoll = requestPaperDoll == RequestPaperDoll.On ? RequestPaperDoll.Off : RequestPaperDoll.On;
            string status = requestPaperDoll == RequestPaperDoll.On ? "ENABLED" : "DISABLED";
            MyAPIGateway.Utilities.ShowNotification($"PAPER DOLL {status}", 1000, requestPaperDoll == RequestPaperDoll.On ? "Green" : "Red");
        }

        private void ExecuteViewStateSearchingWC(MyEntity controlEnt)
        {
            if (controlEnt != null && wcAPI != null)
            {
                var ent = wcAPI.GetAiFocus(controlEnt, 0);
                if (ent != null)
                {
                    MyCubeGrid cGrid = ent as MyCubeGrid;
                    if (cGrid != null && cGrid.Physics != null)
                    {
                        allVis.Add(new EntVis(cGrid, 0.10, 0.05, 0));
                        viewState = ViewState.Locked;
                    }
                    else viewState = ViewState.GoToIdleWC;
                }
                else viewState = ViewState.GoToIdleWC;
            }
            else viewState = ViewState.GoToIdleWC;
        }

        private void ClearAllVis()
        {
            foreach (var entVis in allVis) entVis.Close();
            allVis.Clear();
        }
        public override void Draw()
        {




            if (MyAPIGateway.Utilities.IsDedicated) return;
            IMyCharacter charac = MyAPIGateway.Session.Player?.Character;
            if (charac == null) return;
            IMyCamera currentCamera = MyAPIGateway.Session.Camera;
            if (currentCamera == null) return;
            if (viewState == ViewState.Locked)
            {
                for (int i = allVis.Count - 1; i >= 0; i--)
                {
                    allVis[i].Update();
                    if (allVis[i].isClosed) allVis.RemoveAtFast(i);
                }
                MyEntity controlEnt = null;
                if (MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity is IMyCockpit)
                {
                    IMyCockpit cockpit = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit;
                    controlEnt = cockpit.CubeGrid as MyEntity;
                }
                if (controlEnt != null && wcAPI != null)
                {
                    var ent = wcAPI.GetAiFocus(controlEnt, 0);
                    if (ent != null)
                    {
                        MyCubeGrid cGrid = ent as MyCubeGrid;
                        if (cGrid != null && cGrid.Physics != null)
                        {
                            bool isTracked = false;
                            foreach (var vis in allVis)
                            {
                                if (vis.realGrid.EntityId == cGrid.EntityId)
                                {
                                    isTracked = true;
                                    break;
                                }
                            }
                            if (!isTracked)
                            {
                                foreach (var entVis in allVis)
                                {
                                    entVis.Close();
                                }
                                allVis.Clear();
                                EntVis vis = new EntVis(cGrid, 0.10, 0.05, 0);
                                allVis.Add(vis);
                            }
                        }
                    }
                    else
                    {
                        foreach (var entVis in allVis)
                        {
                            entVis.Close();
                        }
                        allVis.Clear();
                    }
                }
                if (allVis.Count == 0 || requestPaperDoll == RequestPaperDoll.Off) viewState = ViewState.GoToIdleWC;
            }
        }

        private void NetworkHandler(ushort arg1, byte[] arg2, ulong incomingSteamID, bool arg4)
        {
            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet != null && MyAPIGateway.Session.IsServer)
            {
                var updateGridPacket = packet as UpdateGridPacket;
                if (updateGridPacket != null)
                {
                    UpdateServerTracker(incomingSteamID, updateGridPacket);
                }
            }
        }

        private void FeedbackHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet != null)
            {
                var feedbackDamagePacket = packet as FeedbackDamagePacket;
                if (feedbackDamagePacket != null)
                {
                    foreach (var entVis in allVis)
                    {
                        if (entVis.realGrid?.EntityId == feedbackDamagePacket.entityId)
                        {
                            entVis.BlockRemoved(feedbackDamagePacket.position);
                        }
                    }
                }
            }
        }

        private void UpdateServerTracker(ulong steamID, UpdateGridPacket updateGridPacket)
        {
            if (updateGridPacket.regUpdateType == RegUpdateType.Add)
            {
                if (serverTracker.ContainsKey(steamID))
                {
                    AddGridToTracker(steamID, updateGridPacket.entityIds);
                }
                else
                {
                    List<IMyCubeGrid> gridTracker = CreateGridTracker(updateGridPacket.entityIds);
                    serverTracker.Add(steamID, gridTracker);
                }
            }
            else if (updateGridPacket.regUpdateType == RegUpdateType.Remove)
            {
                if (serverTracker.ContainsKey(steamID))
                {
                    RemoveGridFromTracker(steamID, updateGridPacket.entityIds);
                }
            }
        }

        private void AddGridToTracker(ulong steamID, List<long> entityIds)
        {
            foreach (var entId in entityIds)
            {
                IMyCubeGrid cubeGrid = MyAPIGateway.Entities.GetEntityById(entId) as IMyCubeGrid;
                if (cubeGrid != null)
                {
                    cubeGrid.OnBlockRemoved += ServerBlockRemoved;
                    serverTracker[steamID].Add(cubeGrid);
                }
            }
        }

        private List<IMyCubeGrid> CreateGridTracker(List<long> entityIds)
        {
            List<IMyCubeGrid> gridTracker = new List<IMyCubeGrid>();
            foreach (var entId in entityIds)
            {
                IMyCubeGrid cubeGrid = MyAPIGateway.Entities.GetEntityById(entId) as IMyCubeGrid;
                if (cubeGrid != null)
                {
                    cubeGrid.OnBlockRemoved += ServerBlockRemoved;
                    gridTracker.Add(cubeGrid);
                }
            }
            return gridTracker;
        }

        private void RemoveGridFromTracker(ulong steamID, List<long> entityIds)
        {
            foreach (var entId in entityIds)
            {
                IMyCubeGrid cubeGrid = MyAPIGateway.Entities.GetEntityById(entId) as IMyCubeGrid;
                if (cubeGrid != null)
                {
                    cubeGrid.OnBlockRemoved -= ServerBlockRemoved;
                    serverTracker[steamID].Remove(cubeGrid);
                }
            }
        }

        private void ServerBlockRemoved(IMySlimBlock obj)
        {
            var dmgGrid = obj.CubeGrid;
            foreach (var steamID in serverTracker.Keys)
            {
                if (serverTracker[steamID]?.Count > 0)
                {
                    foreach (var checkGrid in serverTracker[steamID])
                    {
                        if (checkGrid.EntityId == dmgGrid.EntityId)
                        {
                            var feedbackDamagePacket = new FeedbackDamagePacket(dmgGrid.EntityId, obj.Position);
                            var byteArray = MyAPIGateway.Utilities.SerializeToBinary(feedbackDamagePacket);
                            MyAPIGateway.Multiplayer.SendMessageTo(feedbackNetID, byteArray, steamID);
                            break;
                        }
                    }
                }
            }
        }

        private bool ValidInput()
        {
            return MyAPIGateway.Session.CameraController != null && !MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None;
        }

        private bool IsAdmin(IMyPlayer sender)
        {
            return sender != null && (sender.PromoteLevel == MyPromoteLevel.Admin || sender.PromoteLevel == MyPromoteLevel.Owner);
        }

        protected override void UnloadData()
        {
            foreach (var entVis in allVis)
            {
                entVis.Close();
            }
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
            }
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(feedbackNetID, FeedbackHandler);
            wcAPI?.Unload();
        }
    }
}