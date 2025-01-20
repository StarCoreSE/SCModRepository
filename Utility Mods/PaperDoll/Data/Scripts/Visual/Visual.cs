using System;
using System.Collections.Generic;
using ProtoBuf;
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


    public class EntRender
    {
        public MyLight light;

        public EntRender()
        {
            light = new MyLight();
        }
    }

    public class EntVis
    {
        public MyCubeGrid realGrid;
        public MatrixD realGridBaseMatrix;
        public GridGroup visGrid;
        public GridGroup visGridSelf;
        public GridGroup visGridString;
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
                visGrid = new GridGroup(new GridRendering(grid), rotOffset);
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

    public enum ReqPDoll
    {
        On,
        Off

    }

    public enum ReqPDollSelf
    {

        SelfOn,
        SelfOff

    }

    public enum ViewState
    {
        Idle,
        Searching,
        SearchingAll,
        SearchingWC,
        Locked,
        GoIdle,
        GoIdleWC,
        DoubleSearching,
    }

    public enum ViewStateSelf
    {
        IdleSelf,
        LockedSelf,
        GoIdle,
        GoIdleWC,
        GoIdleSelf,
        SearchingSelf
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Visual : MySessionComponentBase
    {
        public ushort feedbackNetID = 38492;
        public ushort netID = 39302;
        Dictionary<ulong, List<IMyCubeGrid>> sTrkr = new Dictionary<ulong, List<IMyCubeGrid>>();
        bool validInputThisTick = false; 
        public ViewState viewState = ViewState.Idle;
        public ViewStateSelf viewStateSelf = ViewStateSelf.IdleSelf;
        public ReqPDoll reqPDoll = ReqPDoll.Off;
        public ReqPDollSelf reqPDollSelf = ReqPDollSelf.SelfOff;
        private MyStringId PDollBGSprite = MyStringId.TryGet("paperdollBG");
        public List<EntVis> allVis = new List<EntVis>();
        public List<EntVis> allVisSelf = new List<EntVis>();
        WcApi wcAPI;
        public HudAPIv2 hudAPI;
        public BillBoardHUDMessage billmessage;
        public HUDMessage gHud;

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
                hudAPI = new HudAPIv2(CreateHud);
            }
        }

        private void WCRegistered() { } // needs to be here


        private int tickCounter = 0;
        private const int TickThreshold = 30; 

        public override void UpdateAfterSimulation()
        {
            if (IsInvalidSession()) return;

            tickCounter++;

            if (tickCounter >= TickThreshold)
            {
                tickCounter = 0; // Reset the counter

                switch (viewState)
                {
                    case ViewState.SearchingWC:
                        HanVSearchWC();
                        break;
                    case ViewState.GoIdle:
                    case ViewState.GoIdleWC:
                        HandleViewStateIdle();
                        break;
                }

                switch (viewStateSelf)
                {
                    case ViewStateSelf.GoIdleSelf:
                        HandleViewStateIdleSelf();
                        break;
                    case ViewStateSelf.SearchingSelf:
                        HanVSearchSelf();
                        break;
                }
            }

            //HandleHUDUpdates(); // current doesn't display anything but its hooked up
            HandleUserInput();
        }



        private bool IsInvalidSession()
        {
            return MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Session.Camera == null;
        }

        private void HandleHUDUpdates()
        {
            if (hudAPI.Heartbeat)
            {
                UpdateHud();
            }
        }

        private void HandleUserInput()
        {
            validInputThisTick = ValidInput();

            if (validInputThisTick)
            {
                // Check if Control key is pressed
                if (MyAPIGateway.Input.IsKeyPress(MyKeys.Control))
                {
                    // Check if T key is pressed
                    if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.T))
                    {
                        ToggleViewState();
                        ToggleRequestPaperDoll();
                    }
                    // Check if R key is pressed
                    else if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.R))
                    {
                        ToggleViewStateSelf();
                        ToggleRequestPaperDollSelf();
                    }
                }
            }
        }

        private bool ValidInput()
        {
            var gui = MyAPIGateway.Gui;
            var session = MyAPIGateway.Session;
            var controlledEntity = session?.Player?.Controller?.ControlledEntity;

            // Perform null checks to prevent NullReferenceException
            if (gui == null || session == null || controlledEntity == null)
            {
                return false;
            }

            return session.CameraController != null &&
                   !gui.ChatEntryVisible &&
                   !gui.IsCursorVisible &&
                   gui.GetCurrentScreen == MyTerminalPageEnum.None &&
                   controlledEntity is IMyCockpit;
        }

        private void ToggleViewState()
        {
            // Updated to handle SelfRender state
            if (viewState == ViewState.GoIdleWC)
            {
                viewState = ViewState.SearchingWC;
            }
            else
            {
                
                viewState = ViewState.GoIdleWC;
            }
        }

        private void ToggleViewStateSelf()
        {
            if (viewStateSelf == ViewStateSelf.SearchingSelf)
            {
                viewStateSelf = ViewStateSelf.GoIdleSelf;
            }
            else
            {
                viewStateSelf = ViewStateSelf.SearchingSelf;
            }
        }

        private IMyHudNotification statusNotification = null;

        private void ToggleRequestPaperDollSelf()
        {
            if (reqPDollSelf == ReqPDollSelf.SelfOn)
            {
                reqPDollSelf = ReqPDollSelf.SelfOff;
            }
            else
            {
                reqPDollSelf = ReqPDollSelf.SelfOn;
            }

            string status = reqPDollSelf.ToString().ToUpper();
            string color = "Orange";  // Different color for Self, for example

            if (statusNotification == null)
                statusNotification = MyAPIGateway.Utilities.CreateNotification($"PAPER DOLL {status}", 1000);
            statusNotification.Hide();
            statusNotification.Text = $"PAPER DOLL {status}";
            statusNotification.Font = color;
            statusNotification.Show();

            //MyAPIGateway.Utilities.ShowNotification($"PAPER DOLL {status}", 1000, color);
        }

        private void HanVSearchSelf()
        {
            HandEx(() =>
            {
                MyEntity controlEntSelf = (MyEntity)(MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit);
                if (controlEntSelf != null)
                {
                    ExecuteVSearchUpdateSelf(controlEntSelf);
                }
            }, "HanVSearchSelf");
        }

        private void HanVSearchWC()
        {
            HandEx(() =>
            {
                MyEntity controlEnt = (MyEntity)(MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit);
                if (controlEnt != null)
                {
                    ExecuteVSearchUpdate(controlEnt);
                }
            }, "HanVSearchWC");
        }


        private void HandleViewStateIdle()
        {
            ClearAVis();
            if (viewState == ViewState.GoIdleWC && reqPDoll == ReqPDoll.On)
            {
                viewState = ViewState.SearchingWC;
            }
            else
            {
                viewState = ViewState.Idle;
            }
        }

        private void HandleViewStateIdleSelf()
        {
            ClearAVisSelf();
            if (viewStateSelf == ViewStateSelf.GoIdleSelf && reqPDollSelf == ReqPDollSelf.SelfOn)
            {
                viewStateSelf = ViewStateSelf.SearchingSelf;
            }
            else
            {
                viewStateSelf = ViewStateSelf.GoIdleSelf;
            }
        }

        

        private void ToggleRequestPaperDoll()
        {
            // Updated to handle Self state
            if (reqPDoll == ReqPDoll.On)
            {
                reqPDoll = ReqPDoll.Off;
            }
            else
            {
                reqPDoll = ReqPDoll.On;
            }

            string status = reqPDoll.ToString().ToUpper();
            string color = "Green";

            if (statusNotification == null)
                statusNotification = MyAPIGateway.Utilities.CreateNotification($"PAPER DOLL {status}", 1000);
            statusNotification.Hide();
            statusNotification.Text = $"PAPER DOLL {status}";
            statusNotification.Font = color;
            statusNotification.Show();

            //MyAPIGateway.Utilities.ShowNotification($"PAPER DOLL {status}", 1000, color);
        }
        private void ExecuteVSearchUpdate(MyEntity controlEnt)
        {
            HandEx(() =>
            {

                if (controlEnt == null || wcAPI == null)
                    throw new ArgumentNullException();

                var ent = wcAPI.GetAiFocus(controlEnt, 0);

                if (ent == null)
                {
                    ToggleViewState();
                    ToggleRequestPaperDoll();
                    throw new InvalidOperationException("GetAiFocus returned null");}

                MyCubeGrid cGrid = ent as MyCubeGrid;

                if (cGrid == null)
                    throw new InvalidCastException("Cast to MyCubeGrid failed");

                if (cGrid.Physics == null)
                    throw new NullReferenceException("Physics is null");

                allVis.Add(new EntVis(cGrid, 0.11, 0.05, 0));

                viewState = ViewState.Locked;

            }, "ExecuteVSearchUpdate");
        }


        private void ExecuteVSearchUpdateSelf(MyEntity controlEntSelf)
        {
            if (controlEntSelf == null)
            {
                viewStateSelf = ViewStateSelf.SearchingSelf;
                return;
            }

            var currentCockpit = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity as IMyCockpit;
            var ent = currentCockpit.CubeGrid;

            if (ent == null)
            {
                viewStateSelf = ViewStateSelf.GoIdleSelf;
                return;
            }

            MyCubeGrid sGrid = ent as MyCubeGrid;

            if (sGrid != null && sGrid.Physics != null)
            {
                allVisSelf.Add(new EntVis(sGrid, -0.11, 0.05, 0));
                viewStateSelf = ViewStateSelf.LockedSelf;
            }
            else
            {
                viewStateSelf = ViewStateSelf.GoIdleSelf;
            }
        }

        private void ClearAVis()
        {
            foreach (var entVis in allVis)
            {
                entVis.Close();
            }
            allVis.Clear();
        }

        private void ClearAVisSelf()
        {
            foreach (var entVisSelf in allVisSelf)
            {
                entVisSelf.Close();
            }
            allVisSelf.Clear();
        }


        public void CreateHud()
        {
            InitializeMainReadout();
            InitializeBillMessage();
        }

        private void InitializeMainReadout()
        {
            gHud = new HUDMessage(
                Scale: 2f,
                Font: "BI_SEOutlined",
                Message: new StringBuilder("deez"),
                Origin: new Vector2D(-.99, .99),
                HideHud: false,
                Blend: BlendTypeEnum.PostPP)
            {
                Visible = false,
                InitialColor = Color.GreenYellow * 0.75f,
            };
        }

        private void InitializeBillMessage()
        {
            billmessage = new BillBoardHUDMessage(
                PDollBGSprite,
                new Vector2D(0, 0),
                Color.Lime * 0.75f,
                new Vector2(0, 0),
                -1, 1, 1, 1, 0,
                false, true,
                BlendTypeEnum.PostPP)
            {
                Visible = false,
            };
        }

        public void UpdateHud()
        {
            HandEx(() =>
            {
                if (gHud == null || billmessage == null)
                {
                    CreateHud();
                }
                gHud.Message.Clear();
            }, "initializing HUD");

            foreach (var entVis in allVis)
            {
                UpdateHudElement(entVis);
            }
        }

        private void UpdateHudElement(EntVis entVis)
        {
            HandEx(() =>
            {
                float tempScaling = GridRendering.billboardScaling * 25;
                Vector3D position = GridRendering.hateVector;
                Vector3D targetHudPos = MyAPIGateway.Session.Camera.WorldToScreen(ref position);
                Vector2D newOrigin = new Vector2D(targetHudPos.X, targetHudPos.Y);
                Vector3D cameraForward = MyAPIGateway.Session.Camera.WorldMatrix.Forward;
                Vector3D toTarget = position - MyAPIGateway.Session.Camera.WorldMatrix.Translation;
                float fov = MyAPIGateway.Session.Camera.FieldOfViewAngle;
                var angle = GetAngBetwDeg(toTarget, cameraForward);
              //  string bruh = GridGroup.slimblocksToClose.ToString();
                var distance = Vector3D.Distance(MyAPIGateway.Session.Camera.WorldMatrix.Translation, position);

                gHud.Visible = true;
                gHud.Scale = tempScaling - MathHelper.Clamp(distance / 20000, 0, 0.9) + (30 / Math.Max(60, angle * angle * angle));
              //  gHud.Message.Append(bruh);
                gHud.Origin = new Vector2D(targetHudPos.X, targetHudPos.Y);
                gHud.Offset = -gHud.GetTextLength() / 2 + new Vector2(0, 0.3f);
            }, "updating HUD element for " + entVis);
        }

        private static double GetAngBetwDeg(Vector3D vectorA, Vector3D vectorB)
        {
            vectorA.Normalize();
            vectorB.Normalize();
            return Math.Acos(MathHelper.Clamp(vectorA.Dot(vectorB), -1, 1)) * (180.0 / Math.PI);
        }

        public override void Draw()
        {
            HandEx(() =>
            {
                if (InvalForDraw()) return;

                if (allVis == null)
                {
                    MyLog.Default.WriteLine("allVis is null");
                    return;
                }

                if (allVisSelf == null)
                {
                    MyLog.Default.WriteLine("allVisSelf is null");
                    return;
                }

                if (viewState == ViewState.Locked)
                {
                    UpdateAllVis();
                    HandleControlEntity();
                }

                if (viewStateSelf == ViewStateSelf.LockedSelf)
                {
                    UpdateAllVisSelf();
                    HandleControlEntitySelf();
                }

            }, "Drawing On-Screen Elements");
        }

        private bool InvalForDraw()
        {
            return MyAPIGateway.Utilities.IsDedicated ||
                   MyAPIGateway.Session.Player?.Character == null ||
                   MyAPIGateway.Session.Camera == null;
        }

        private void UpdateAllVis()
        {
            for (int i = allVis.Count - 1; i >= 0; i--)
            {
                allVis[i].Update();
                if (allVis[i].isClosed) allVis.RemoveAtFast(i);
            }
        }

        private void UpdateAllVisSelf()
        {
            for (int i = allVisSelf.Count - 1; i >= 0; i--)
            {
                allVisSelf[i].Update();
                if (allVisSelf[i].isClosed) allVisSelf.RemoveAtFast(i);
            }
        }

        private void HandleControlEntity()
        {
            MyEntity cEnt = null;
            if (MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity is IMyCockpit)
            {
                IMyCockpit cock = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit;
                cEnt = cock.CubeGrid as MyEntity;
            }

            if (cEnt != null && wcAPI != null)
            {
                ManEntFoc(cEnt);
            }
            else
            {
                //ClearAVis(); //this would clear your paper doll whenever out of cockpit
            }

            if (allVis.Count == 0 || reqPDoll == ReqPDoll.Off)
            {
                viewState = ViewState.GoIdleWC;
            }
        }

        private void HandleControlEntitySelf()
        {
            MyEntity sEnt = null;
            if (MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity is IMyCockpit)
            {
                IMyCockpit cock = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as IMyCockpit;
                sEnt = cock.CubeGrid as MyEntity;
            }
            else
            {
                //ClearAVisSelf(); //this would clear your paper doll whenever out of cockpit
            }

            if (allVisSelf.Count == 0 || reqPDollSelf == ReqPDollSelf.SelfOff)
            {
                viewStateSelf = ViewStateSelf.GoIdleSelf;
            }
        }


        private void ManEntFoc(MyEntity cEnt)
        {
            var ent = wcAPI.GetAiFocus(cEnt, 0);
            if (ent == null)
            {
                ClearAVis();
            }

            MyCubeGrid cGrid = ent as MyCubeGrid;
            if (cGrid != null && cGrid.Physics != null)
            {
                bool isTrack = IsEntityTracked(cGrid);
                if (!isTrack)
                {
                    ClearAVis();
                    EntVis vis = new EntVis(cGrid, 0.11, 0.05, 0);
                    allVis.Add(vis);
                }
            }
            else
            {
                ClearAVis();
            }
        }



        private bool IsEntityTracked(MyCubeGrid cGrid)
        {
            foreach (var vis in allVis)
            {
                if (vis.realGrid.EntityId == cGrid.EntityId)
                {
                    return true;
                }
            }
            return false;
        }


        private void NetworkHandler(ushort arg1, byte[] arg2, ulong iSID, bool arg4)
        {
            HandEx(() =>
            {
                if (IsInvalidPacket(arg2)) return;

                var packet = DesPacket(arg2);
                if (packet == null || !MyAPIGateway.Session.IsServer) return;

                var uGP = packet as UpdateGridPacket;
                if (uGP == null) return;

                UpSerTrkr(iSID, uGP);

            }, "Handling Network Packet");
        }

        private bool IsInvalidPacket(byte[] arg2)
        {
            if (arg2 == null)
            {
                MyLog.Default.WriteLine("Null argument 'arg2' NetworkHandler!");
                return true;
            }
            return false;
        }

        private Packet DesPacket(byte[] arg2)
        {
            return MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
        }
        private void FeedbackHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            HandEx(() =>
            {
                if (ArgInvalid(arg2)) return;

                var packet = DesPacket(arg2);
                if (packet == null) return;

                var fDP = packet as FeedbackDamagePacket;
                if (fDP == null) return;

                UpEnFd(fDP);
                UpEnFdSelf(fDP);

            }, "Handling Feedback Packet");
        }

        private bool ArgInvalid(byte[] arg2)
        {
            if (arg2 == null || allVis == null)
            {
                MyLog.Default.WriteLine("Null arguments to FeedbackHandler.");
                return true;
            }
            return false;
        }

        private void UpEnFd(FeedbackDamagePacket fDP)
        {
            foreach (var eVis in allVis)
            {
                if (eVis?.realGrid?.EntityId == fDP.entityId)
                {
                    eVis.BlockRemoved(fDP.position);
                }
            }
        }

        private void UpEnFdSelf(FeedbackDamagePacket fDP)
        {
            foreach (var sVis in allVisSelf)
            {
                if (sVis?.realGrid?.EntityId == fDP.entityId)
                {
                    sVis.BlockRemoved(fDP.position);
                }
            }
        }

        private void UpSerTrkr(ulong sID, UpdateGridPacket uGP)
        {
            HandEx(() =>
            {
                if (ArgInvalid(uGP)) return;

                switch (uGP.regUpdateType)
                {
                    case RegUpdateType.Add:
                        HandleAddOperation(sID, uGP);
                        break;
                    case RegUpdateType.Remove:
                        HandleRemoveOperation(sID, uGP);
                        break;
                }

            }, "Updating Server Tracker");
        }

        private bool ArgInvalid(UpdateGridPacket uGP)
        {
            if (uGP == null || sTrkr == null)
            {
                MyLog.Default.WriteLine("Null in UpdateServerTracker.");
                return true;
            }
            return false;
        }

        private void HandleAddOperation(ulong sID, UpdateGridPacket uGP)
        {
            if (sTrkr.ContainsKey(sID))
            {
                AddGrdTrkr(sID, uGP.entityIds);
            }
            else
            {
                List<IMyCubeGrid> gTrack = CreateGrdTrkr(uGP.entityIds);
                sTrkr.Add(sID, gTrack);
            }
        }

        private void HandleRemoveOperation(ulong sID, UpdateGridPacket uGP)
        {
            if (sTrkr.ContainsKey(sID))
            {
                RemGrdTrkr(sID, uGP.entityIds);
            }
        }

        // Adds grids to the server tracker
        private void AddGrdTrkr(ulong sID, List<long> eID)
        {
            HandEx(() =>
            {
                if (AreArgumentsInvalid(eID)) return;

                foreach (var entId in eID)
                {
                    AddEntityToTracker(sID, entId);
                }

            }, "Adding Grids to Tracker");
        }

        // Creates a new grid tracker
        private List<IMyCubeGrid> CreateGrdTrkr(List<long> eIDs)
        {
            List<IMyCubeGrid> gTracker = new List<IMyCubeGrid>();

            HandEx(() =>
            {
                if (eIDs == null)
                {
                    LogInvalidArguments("CreateGTrack");
                    return;
                }

                foreach (var entId in eIDs)
                {
                    AddEntToTrk(gTracker, entId);
                }

            }, "Creating GTrack");

            return gTracker;
        }

        // Removes grids from the server tracker
        private void RemGrdTrkr(ulong sID, List<long> eID)
        {
            HandEx(() =>
            {
                if (AreArgumentsInvalid(eID, sID)) return;

                foreach (var entId in eID)
                {
                    RemEntTrk(sID, entId);
                }

            }, "Removing Grids from Tracker");
        }

        // Helper methods
        private bool AreArgumentsInvalid(List<long> eID, ulong? sID = null)
        {
            if (eID == null || sTrkr == null || (sID.HasValue && !sTrkr.ContainsKey(sID.Value)))
            {
                LogInvalidArguments("Arguments are null or missing keys");
                return true;
            }
            return false;
        }

        private void LogInvalidArguments(string mName)
        {
            MyLog.Default.WriteLine($"Null arguments provided to {mName}. Exiting to prevent issues.");
        }

        private void AddEntityToTracker(ulong sID, long entId)
        {
            IMyCubeGrid cGrid = MyAPIGateway.Entities.GetEntityById(entId) as IMyCubeGrid;
            if (cGrid != null)
            {
                cGrid.OnBlockRemoved += SerBRem;
                if (sTrkr.ContainsKey(sID))
                {
                    sTrkr[sID].Add(cGrid);
                }
                else
                {
                    MyLog.Default.WriteLine($"SteamID {sID} not found in serverTracker. Abandon ship!");
                }
            }
        }

        private void AddEntToTrk(List<IMyCubeGrid> gTrack, long entId)
        {
            IMyCubeGrid cGrid = MyAPIGateway.Entities.GetEntityById(entId) as IMyCubeGrid;
            if (cGrid != null)
            {
                cGrid.OnBlockRemoved += SerBRem;
                gTrack.Add(cGrid);
            }
        }

        private void RemEntTrk(ulong sID, long entId)
        {
            IMyCubeGrid cGrid = MyAPIGateway.Entities.GetEntityById(entId) as IMyCubeGrid;
            if (cGrid != null)
            {
                cGrid.OnBlockRemoved -= SerBRem;
                sTrkr[sID]?.Remove(cGrid);
            }
        }


        //fun stops here
        private void SerBRem(IMySlimBlock obj)
        {
            HandEx(() =>
            {
                if (obj == null || sTrkr == null)
                {
                    MyLog.Default.WriteLine("Null arguments in ServerBlockRemoved.");
                    return;
                }

                var dgrd = obj.CubeGrid;
                if (dgrd == null) return;

                foreach (var sID in sTrkr.Keys)
                {
                    if (sTrkr[sID]?.Count > 0)
                    {
                        foreach (var cGrid in sTrkr[sID])
                        {
                            if (cGrid?.EntityId == dgrd.EntityId)
                            {
                                var fDP = new FeedbackDamagePacket(dgrd.EntityId, obj.Position);
                                var byteArray = MyAPIGateway.Utilities.SerializeToBinary(fDP);
                                MyAPIGateway.Multiplayer.SendMessageTo(feedbackNetID, byteArray, sID);
                                break;
                            }
                        }
                    }
                }
            }, "Removing Server Block");
        }
        private static void HandEx(Action act, string ctx)
        {
            try { act(); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Err {ctx}: {e.Message}");
                MyAPIGateway.Utilities.ShowNotification($"Error in {ctx}. Check Log.", 6, MyFontEnum.Red);
            }
        }



        //private bool IsAdmin(IMyPlayer s) => s != null && (s.PromoteLevel == MyPromoteLevel.Admin || s.PromoteLevel == MyPromoteLevel.Owner);

        protected override void UnloadData()
        {
            foreach (var e in allVis) e.Close();
            foreach (var e in allVisSelf) e.Close();
            var mp = MyAPIGateway.Multiplayer;
            if (MyAPIGateway.Session.IsServer) mp.UnregisterSecureMessageHandler(netID, NetworkHandler);
            mp.UnregisterSecureMessageHandler(feedbackNetID, FeedbackHandler);
            wcAPI?.Unload();
        }

    }

}
