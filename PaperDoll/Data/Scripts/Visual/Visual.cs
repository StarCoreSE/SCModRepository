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
        public EntRender entRender = new EntRender();
        public MatrixD controlMatrix;
        public double scale, tempscale = 0.8;
        internal Task GridTask = new Task();
        Vector3D relTrans, relForward, relUp;

        public GridR(MyCubeGrid grid, EntRender entRender = null)
        {
            this.grid = grid;
            this.entRender = entRender ?? this.entRender;
        }

        public void UpdateMatrix(MatrixD renderMatrix)
        {
            renderMatrix.Translation += Vector3D.TransformNormal(relTrans, renderMatrix);
            grid.WorldMatrix = renderMatrix;
        }

        public void DoRescale()
        {
            var volume = grid.PositionComp.WorldVolume;
            scale = 0.028 / volume.Radius * (grid.GridSizeEnum == MyCubeSize.Small ? 0.8 : 1);
            relTrans = Vector3D.TransformNormal(grid.WorldMatrix.Translation - grid.PositionComp.WorldAABB.Center, MatrixD.Transpose(grid.WorldMatrix)) * scale;
            grid.PositionComp.Scale = (float)scale;
        }

        public void DoCleanup()
        {
            foreach (var fatblock in grid.GetFatBlocks())
            {
                DisableBlock(fatblock as IMyFunctionalBlock);
                StopEffects(fatblock as IMyExhaustBlock);
                DisableBlock(fatblock as IMyLightingBlock);
            }

            if (grid.IsPowered) grid.SwitchPower();
        }

        private void DisableBlock(IMyFunctionalBlock block)
        {
            if (block != null) block.Enabled = false;
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
        public Dictionary<IMyCubeBlock, int> DelDict = new Dictionary<IMyCubeBlock, int>();
        public Dictionary<Vector3I, int> SlimDelDict = new Dictionary<Vector3I, int>();
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
                if (slim.FatBlock == null && (!SlimDelDict.ContainsKey(slim.Position))) { int time = slim.Mass > 1500 ? timer + 200 : timer + 10; SlimDelDict.Add(slim.Position, time);}
                else
                {
                    slim.Dithering = 2.5f;
                    MyVisualScriptLogicProvider.SetHighlightLocal(slim.FatBlock.Name, 10, 10, Color.Red);
                    int time = slim.FatBlock.Mass > 1500 ? timer + 200 : timer + 10;
                    if (!DelDict.ContainsKey(slim.FatBlock)) DelDict.Add(slim.FatBlock, time);
                }
            }
        }

        public void UpdateMatrix(MatrixD renderMatrix, MatrixD rotMatrix)
        {
            if (!doneRescale || !doneInitialCleanup) return;
            timer++;
            DelList.Clear();
            SlimDelList.Clear();
            foreach (var fatblock in DelDict.Keys) { if (DelDict[fatblock] == timer) { fatblock.Close(); DelList.Add(fatblock); } }
            foreach (var item in DelList) DelDict.Remove(item);

            foreach (var slim in SlimDelDict.Keys) { if (SlimDelDict[slim] == timer) {SlimDelList.Add(slim); /* add visuals for slimblock here*/ } }


            foreach (var subgrid in gridGroup)
            {
                if (subgrid.grid == null) continue;

                foreach (var item in SlimDelList) { subgrid.grid.RazeGeneratedBlocks(SlimDelList); }

            }




            rotationForward = rotationForwardBase + rotationForward;
            var rotateMatrix = MatrixD.CreateRotationY(rotationForwardBase);
            renderMatrix = rotateMatrix * renderMatrix;
            var origTranslation = renderMatrix.Translation;
            renderMatrix = rotMatrix * renderMatrix;
            renderMatrix.Translation = origTranslation;
            foreach (var subgrid in gridGroup) { if (subgrid.grid != null) subgrid.UpdateMatrix(renderMatrix); }
        }
    }
    //subgrid.grid.RazeGeneratedBlocks(SlimList)

    public class EntVis
    {
        public MyCubeGrid realGrid;
        public MatrixD realGridBaseMatrix;
        public GridG visGrid;
        public int lifetime;
        public ushort netID = 39302;
        public bool isClosed;
        public double xOffset, yOffset, rotOffset;

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
                grid.SyncFlag = grid.Save = grid.Render.NearFlag = grid.Render.FadeIn = false;
                grid.Render.CastShadows = false;
                grid.DisplayName = "";
                grid.RemoveFromGamePruningStructure();
                MyAPIGateway.Entities.AddEntity(grid);
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
                        allVis.Add(new EntVis(cGrid, 0.12, 0.03, 0));
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