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

namespace klime.Visual
{
    //Render grid
    public class GridR
    {
        public MyCubeGrid grid;
        public EntRender entRender;
        public MatrixD controlMatrix;
        public double scale;
        public double tempscale = 0.8;
        Vector3D relTrans;
        Vector3D relForward;
        Vector3D relUp;
        internal Task GridTask = new Task();

        public GridR(MyCubeGrid grid, EntRender entRender = null)
        {
            this.grid = grid;
            this.entRender = entRender;
            if (this.entRender == null)
            {
                entRender = new EntRender();
            }
        }

        public void UpdateMatrix(MatrixD renderMatrix)
        {
            renderMatrix.Translation += Vector3D.TransformNormal(relTrans, renderMatrix);
            grid.WorldMatrix = renderMatrix;
        }

        public void DoRescale()
        {
            var volume = grid.PositionComp.WorldVolume;
            scale = 0.028 / volume.Radius;
            if (grid.GridSizeEnum == MyCubeSize.Small) scale *= 0.8;

            relTrans = Vector3D.TransformNormal(grid.WorldMatrix.Translation - grid.PositionComp.WorldAABB.Center, MatrixD.Transpose(grid.WorldMatrix));
            relTrans *= scale;
            grid.PositionComp.Scale = (float)scale;
        }

        public void DoCleanup()
        {

            HashSet<IMyEntity> subparts = new HashSet<IMyEntity>();
            foreach (var fatblock in grid.GetFatBlocks())
            {
                IMyFunctionalBlock fBlock = fatblock as IMyFunctionalBlock;
                if (fBlock != null)
                {
                    fBlock.Enabled = false;
                    //fBlock.Render.eff
                }

                IMyExhaustBlock exhaust = fatblock as IMyExhaustBlock;
                if (exhaust != null)
                {
                    exhaust.StopEffects();
                }
                IMyLightingBlock light = fatblock as IMyLightingBlock;
                if (light != null)
                {
                    light.Enabled = false;
                }

            }

            if (grid.IsPowered)
            {
                grid.SwitchPower();
            }
//
//   grid.ChangeGridOwnership(MyAPIGateway.Session.Player.IdentityId, MyOwnershipShareModeEnum.Faction);
// 
         
       //   string whiteHex = "#FFFFFF";
       //   Vector3 whiteHSVOffset = MyColorPickerConstants.HSVToHSVOffset(ColorExtensions.ColorToHSV(ColorExtensions.HexToColor(whiteHex)));
       //   whiteHSVOffset = new Vector3((float)Math.Round(whiteHSVOffset.X, 2), (float)Math.Round(whiteHSVOffset.Y, 2), (float)Math.Round(whiteHSVOffset.Z, 2));
       // 
       //   List<IMySlimBlock> allBlocks = new List<IMySlimBlock>();
       //   IMyCubeGrid iGrid = grid as IMyCubeGrid;
       //   iGrid.GetBlocks(allBlocks);
       //
       //    //grid.ColorBlocks(grid.Min, grid.Max, whiteHSVOffset, false, false);
       //    ////iGrid.ColorBlocks(iGrid.Min, iGrid.Max, whiteHSVOffset);
       //    ////grid.ColorGrid(whiteHSVOffset, false, false);
       //
       //  foreach (var block in allBlocks)
       //  {
       //      block.Dithering = 0.1f;
       //      //grid.ChangeColorAndSkin(grid.GetCubeBlock(block.Position), whiteHSVOffset);
       //  }
       //  //grid.Render.Transparency = -0.01f;


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
        public bool doneInitialCleanup = false;
        public bool doneRescale = false;
        public double rotationForward;
        public double rotationUp;
        public double rotationForwardBase;
        public int timer;
        public List<IMyCubeBlock> DelList = new List<IMyCubeBlock>();
        public List<Vector3I> SlimList = new List<Vector3I>();
        public Dictionary<IMyCubeBlock, int> DelDict = new Dictionary<IMyCubeBlock, int>();

        public GridG(List<GridR> gridGroup, double rotationForwardBase)
        {
            this.gridGroup = new List<GridR>(gridGroup); // Allocation?
            this.rotationForwardBase = rotationForwardBase;
        }

        public GridG(GridR gridR, double rotationForwardBase)
        {
            gridGroup = new List<GridR>();
            gridGroup.Add(gridR);
            this.rotationForwardBase = rotationForwardBase;
        }

        public void DoCleanup()
        {
            foreach (var sg in gridGroup)
            {
                if (sg.grid != null)
                {
                    sg.DoCleanup();
                    doneInitialCleanup = true;
                }
            }
        }

        public void DoRescale()
        {
            foreach (var sg in gridGroup)
            {
                if (sg.grid != null)
                {
                    sg.DoRescale();
                    doneRescale = true;
                }
            }
        }

        public void DoBlockRemove(Vector3I position)
        {
            SlimList.Clear();
            SlimList.Add(position);
            foreach (var subgrid in gridGroup)
            {
                if (subgrid.grid != null)
                {
                    var slim = subgrid.grid.GetCubeBlock(position) as IMySlimBlock;
                    if (slim != null)
                    {
                        if (slim.FatBlock == null)
                        {
                            subgrid.grid.RazeGeneratedBlocks(SlimList);
                        }
                        else
                        {
                            slim.Dithering = 2.5f;
                            if (slim.FatBlock.Mass > 1500 && !DelDict.ContainsKey(slim.FatBlock))
                            {
                                MyVisualScriptLogicProvider.SetHighlightLocal(slim.FatBlock.Name, 10, 10, Color.Red);
                                DelDict.Add(slim.FatBlock, timer + 200);
                            }
                            else if (!DelDict.ContainsKey(slim.FatBlock))
                            {
                                DelDict.Add(slim.FatBlock, (timer + 10));
                            }
                        }
                    }
                }
            }
        }

        public void UpdateMatrix(MatrixD renderMatrix, MatrixD rotMatrix)
        {
            if (!doneRescale || !doneInitialCleanup)
            {
                return;
            }
            timer++;
            DelList.Clear();
            foreach (var fatblock in DelDict.Keys)
            {
                if (DelDict[fatblock] == timer)
                {
                    fatblock.Close();
                    DelList.Add(fatblock);
                }
            }
            foreach (var item in DelList)
            {
                DelDict.Remove(item);
            }
            this.rotationForward = rotationForwardBase + rotationForward;
            var rotateMatrix = MatrixD.CreateRotationY(rotationForwardBase);
            renderMatrix = rotateMatrix * renderMatrix;
            var origTranslation = renderMatrix.Translation;
            var origRotation = renderMatrix.Rotation;
            renderMatrix = rotMatrix * renderMatrix;
            renderMatrix.Translation = origTranslation;
            foreach (var subgrid in gridGroup)
            {
                if (subgrid.grid != null)
                {
                    subgrid.UpdateMatrix(renderMatrix);
                }
            }
        }
    }

    // Overall visualization
    public class EntVis
    {
        public MyCubeGrid realGrid;
        public MatrixD realGridBaseMatrix;
        public GridG visGrid;
        public int lifetime;
        public ushort netID = 39302;
        public bool isClosed = false;
        public double xOffset;
        public double yOffset;
        public double rotOffset;
        int timerRot = 0;

        public EntVis(MyCubeGrid realGrid, double xOffset, double yOffset, double rotOffset)
        {
            this.realGrid = realGrid;
            this.realGridBaseMatrix = realGrid.WorldMatrix;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.rotOffset = rotOffset;
            lifetime = 0;
            RegisterEvents();
            GenerateClientGrids();
        }

        private void RegisterEvents()
        {
            UpdateGridPacket regGridPacket = new UpdateGridPacket(realGrid.EntityId, RegUpdateType.Add);
            var byteArray = MyAPIGateway.Utilities.SerializeToBinary(regGridPacket);
            MyAPIGateway.Multiplayer.SendMessageTo(netID, byteArray, MyAPIGateway.Multiplayer.ServerId);
        }

        public void BlockRemoved(Vector3I pos) => visGrid?.DoBlockRemove(pos);

        public void GenerateClientGrids()
        {
            try
            {
                var realOB = realGrid.GetObjectBuilder() as MyObjectBuilder_CubeGrid;
                MyEntities.RemapObjectBuilder(realOB);
                realOB.CreatePhysics = false;
                MyAPIGateway.Entities.CreateFromObjectBuilderParallel(realOB, false, CompleteCall);
            }
            catch (Exception e)
            {
                // Log the error message for debugging purposes
                MyLog.Default.WriteLine($"Error generating client grids: {e.Message}");
                // Show an on-screen message to the player
                MyAPIGateway.Utilities.ShowNotification("An error occurred while generating client grids. Please check the log for more details.", 5000, MyFontEnum.Red);
            }
        }


        private void CompleteCall(IMyEntity obj)
        {
            try
            {
                if (isClosed) return;
                var grid = (MyCubeGrid)obj;
                grid.SyncFlag = false;
                grid.Save = false;
                grid.Render.NearFlag = false;
                grid.RemoveFromGamePruningStructure();
                grid.Render.CastShadows = false;
                grid.Render.FadeIn = false;
                grid.DisplayName = "";
                MyAPIGateway.Entities.AddEntity(grid);
                visGrid = new GridG(new GridR(grid), rotOffset);
            }
            catch (Exception e)
            {
                // Log the error message for debugging purposes
                MyLog.Default.WriteLine($"Error in CompleteCall: {e.Message}");
                // Show an on-screen message to the player
                MyAPIGateway.Utilities.ShowNotification("An error occurred while completing the call. Please check the log for more details.", 5000, MyFontEnum.Red);
            }
        }


        public void Update()
        {
            UpdateVisLogic();
            UpdateVisPosition();
            UpdateRealLogic();
            lifetime += 1;
        }

        private void UpdateVisPosition()
        {
            var playerCamera = MyAPIGateway.Session.Camera;
            if (visGrid != null && realGrid != null && !realGrid.MarkedForClose)
            {
                var renderMatrix = playerCamera.WorldMatrix;
                var moveFactor = 0.6 * playerCamera.FovWithZoom;
                renderMatrix.Translation += renderMatrix.Forward * (0.1 / moveFactor) + renderMatrix.Right * xOffset + renderMatrix.Down * yOffset;

                // Calculate the rotation matrix to match the visual apparent rotation
                var rotationMatrix = MatrixD.Invert(renderMatrix);

                var rotMatrix = realGrid.WorldMatrix * rotationMatrix;
                visGrid.UpdateMatrix(renderMatrix, rotMatrix);
            }
        }

        private void UpdateVisLogic()
        {
            if (visGrid != null)
            {
                if (!visGrid.doneInitialCleanup) visGrid.DoCleanup();
                if (!visGrid.doneRescale) visGrid.DoRescale();
            }
        }

        private void UpdateRealLogic()
        {
            if (realGrid == null || realGrid.MarkedForClose || realGrid.Physics == null || !realGrid.IsPowered) Close();
        }

        public void Close()
        {
            if (visGrid != null)
            {
                foreach (var sub in visGrid.gridGroup)
                {
                    sub.grid.Close();
                }
            }
            UpdateGridPacket packet = new UpdateGridPacket(realGrid.EntityId, RegUpdateType.Remove);
            var array = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageTo(netID, array, MyAPIGateway.Multiplayer.ServerId);
            isClosed = true;
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
            if (MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Session.Player?.Character == null || MyAPIGateway.Session.Camera == null)
            {
                return;
            }
            if (ValidInput())
            {
                validInputThisTick = true;
            }
            else
            {
                validInputThisTick = false;
            }
            if (validInputThisTick && IsAdmin(MyAPIGateway.Session.Player) && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.T))
            {
                if (viewState == ViewState.GoToIdleWC)
                {
                    viewState = ViewState.SearchingWC;
                }
                else
                {
                    viewState = ViewState.GoToIdleWC;
                }
                if (requestPaperDoll == RequestPaperDoll.On)
                {
                    requestPaperDoll = RequestPaperDoll.Off;
                    MyAPIGateway.Utilities.ShowNotification("PAPER DOLL DISABLED", 1000, "Red");
                }
                else
                {
                    requestPaperDoll = RequestPaperDoll.On;
                    MyAPIGateway.Utilities.ShowNotification("PAPER DOLL ENABLED", 1000, "Green");
                }
            }
            if (viewState == ViewState.SearchingWC)
            {
                MyEntity controlEnt = (MyEntity)(MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit);
                if (controlEnt != null && wcAPI != null)
                {
                    var ent = wcAPI.GetAiFocus(controlEnt, 0);
                    if (ent != null)
                    {
                        MyCubeGrid cGrid = ent as MyCubeGrid;
                        if (cGrid != null && cGrid.Physics != null)
                        {
                            EntVis entVis = new EntVis(cGrid, 0.12, 0.03, 0);
                            allVis.Add(entVis);
                            viewState = ViewState.Locked;
                        }
                        else
                        {
                            viewState = ViewState.GoToIdleWC;
                        }
                    }
                    else
                    {
                        viewState = ViewState.GoToIdleWC;
                    }
                }
                else
                {
                    viewState = ViewState.GoToIdleWC;
                }
            }
            if (viewState == ViewState.GoToIdle || viewState == ViewState.GoToIdleWC)
            {
                foreach (var entVis in allVis)
                {
                    entVis.Close();
                }
                allVis.Clear();
                viewState = viewState == ViewState.GoToIdleWC && requestPaperDoll == RequestPaperDoll.On ? ViewState.SearchingWC : ViewState.Idle;
            }
        }

        public override void Draw()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }
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
                                EntVis vis = new EntVis(cGrid, 0.12, 0.03, 0);
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