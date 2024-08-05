using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Draygo.API;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.Input;
using VRage.Utils;
using Sandbox.Game.Entities;

namespace KlimeDraygoMath.CastSpectator
{
    public struct CameraState
    {
        public MatrixD localMatrix;
        public MatrixD lastOrientation;
        public Vector3D localVector;

        public double localDistance;

        public CameraMode lockmode;

        public IMyEntity lockEntity;
        public bool islocked;
    }

    public enum CameraMode : int
    {
        None,
        Free,
        Follow,
        Orbit,
        Track,
    }

    public enum FindAndMoveState : int
    {
        Idle,
        GoToSearch,
        Searching,
        GoToMove,
        InMove,
        InMoveLookback,
        GoToIdle
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class CastSpectator : MySessionComponentBase
    {
        private SpecCamTarget CurrentTarget = new SpecCamTarget();
        private List<SpecCamTarget> SavedTargets = new List<SpecCamTarget>(10);

        private IMyCamera m_playerCamera;
        private CameraState ObsCameraState = new CameraState()
        {
            localMatrix = MatrixD.Identity,
            lastOrientation = MatrixD.Identity,
            localVector = Vector3D.Zero,
            localDistance = 1f,

            lockmode = CameraMode.None,
            lockEntity = null,
            islocked = false
        };


        private MySpectator m_specCam;
        public SpecCamPreferences m_Pref = new SpecCamPreferences();
        bool m_actionpressed = false;


        public Vector3 m_SmoothMI = Vector3D.Zero;
        public Vector2 m_SmoothMouse = Vector2.Zero;
        public float m_smoothRoll = 0f;


        HudAPIv2 TextHUD;
        public bool m_isDedicated = false;
        public bool m_isServer = false;
        public bool m_init = false;

        public bool m_SmoothCamera = false;
        public bool m_GlobalSmoothing = false;

        private bool m_PeriodicSwitchEnabled = false;
        private float m_PeriodicSwitchInterval = 5f; // Default 5 seconds
        private DateTime m_LastSwitchTime;
        private bool m_PeriodicSwitchRandom = false;
        private int m_CurrentTargetIndex = 0;


        public StringBuilder statusSB = new StringBuilder();

        //Find and Move vars
        FindAndMoveState m_FindAndMoveState = FindAndMoveState.Idle;
        IMyCubeGrid moveGrid;
        public double maxSearchDistance = 20000;
        public double maxSearchAngle = 0.2; //Rads
        int viewAnimFrame;
        int maxViewAnimFrame = 60;
        int rotationAnimFrame;
        int maxRotationAnimFrame = 60;

        Vector3D origStart = Vector3D.Zero;
        Vector3D origFor = Vector3D.Zero;
        Vector3D origUp = Vector3D.Zero;
        List<IMyCubeGrid> chosenGrids = new List<IMyCubeGrid>();

        //Track vars
        public Vector3D initTrackUp = Vector3D.Zero;
        public double trackRoll = 0;
        //Debug track vars
        public Vector3D debugCenter = Vector3D.Zero;
        public MyStringId debugTexture;

        public float InQuint(float t) => t * t * t * t * t;
        public float OutQuint(float t) => 1 - InQuint(1 - t);

        private bool HideHud
        {
            get
            {
                return m_Pref.HideHud;
            }
            set
            {
                if (m_Pref != null)
                {
                    m_Pref.HideHud = value;
                    if (CameraMessage != null)
                    {
                        if (value)
                        {
                            CameraMessage.Options |= HudAPIv2.Options.HideHud;
                        }
                        else
                        {
                            CameraMessage.Options &= ~HudAPIv2.Options.HideHud;
                        }

                    }

                }
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            debugTexture = MyStringId.GetOrCompute("Square");
        }

        public void SetTarget(IMyEntity ent)
        {
            UpdateLockEntity(ent);
            if (MyAPIGateway.Session != null)
            {
                m_playerCamera = MyAPIGateway.Session.Camera;
                m_specCam = MyAPIGateway.Session.CameraController as MySpectator;
            }
        }

        public IMyEntity GetTarget()
        {
            return ObsCameraState.lockEntity;
        }
        public void SetMode(int mode)
        {
            ObsCameraState.lockmode = (CameraMode)mode;
        }

        public int GetMode()
        {
            return (int)ObsCameraState.lockmode;
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;
            if (!m_init)
                InitSCB();
            if (m_isDedicated || m_Pref == null)
                return;


            m_playerCamera = MyAPIGateway.Session.Camera;
            m_specCam = MyAPIGateway.Session.CameraController as MySpectator;
            statusSB.Clear();
            statusSB.Append(" ");

            if ((MyAPIGateway.Session.IsCameraUserControlledSpectator 
                && !MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible 
                && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None))
            {
                #region keys
                if (m_Pref.ToggleLock.IsKeybindPressed())
                {
                    if (m_actionpressed == false)
                    {
                        ObsCameraState.islocked = false;
                    }
                    m_actionpressed = true;

                    var endray = m_playerCamera.Position + m_playerCamera.WorldMatrix.Forward * 1000;
                    IHitInfo tempHIt = null;
                    MyAPIGateway.Physics.CastRay(m_playerCamera.Position, endray, out tempHIt);
                    if (tempHIt != null)
                    {
                        if (tempHIt.HitEntity is IMyCubeGrid || tempHIt.HitEntity is IMyCharacter)
                        {
                            if (tempHIt.HitEntity.Physics != null)
                            {
                                if (ObsCameraState.lockEntity != null)
                                {
                                    Clear();
                                }

                                SetTarget(tempHIt.HitEntity);
                                SetMode(1);
                            }
                        }
                    }
                }
                else
                {
                    m_actionpressed = false;
                    if (!ObsCameraState.islocked)
                    {
                        Clear();
                    }
                }
                if (m_Pref.SwitchMode.IsKeybindPressed())
                {
                    switch (ObsCameraState.lockmode)
                    {
                        case CameraMode.Free:
                            ObsCameraState.lockmode = CameraMode.Follow;
                            break;
                        case CameraMode.Follow:
                            ObsCameraState.lockmode = CameraMode.Orbit;
                            break;
                        case CameraMode.Orbit:
                            ObsCameraState.lockmode = CameraMode.Track;
                            InitTrack();
                            break;
                        case CameraMode.Track:
                            ObsCameraState.lockmode = CameraMode.Free;
                            break;
                    }
                }

                if (m_Pref.ToggleSmoothCamera.IsKeybindPressed())
                {
                    ToggleSmoothCamera();
                }

                if (m_Pref.FreeMode.IsKeybindPressed())
                {
                    ObsCameraState.lockmode = CameraMode.Free;
                }

                if (m_Pref.FollowMode.IsKeybindPressed())
                {
                    ObsCameraState.lockmode = CameraMode.Follow;
                }

                if (m_Pref.OrbitMode.IsKeybindPressed())
                {
                    ObsCameraState.lockmode = CameraMode.Orbit;
                }

                if (m_Pref.TrackMode.IsKeybindPressed())
                {
                    ObsCameraState.lockmode = CameraMode.Track;
                    InitTrack();
                }

                //save or load camera
                foreach (var saved in SavedTargets)
                {
                    if (saved.IsActivateKeybindPressed())
                    {
                        if (saved.HasState)
                        {
                            ObsCameraState = saved.State;
                        }
                    }
                    if (saved.IsModifyKeybindPressed())
                    {
                        saved.State = ObsCameraState;

                        // Show notification when a preset is saved
                        string entityName = ObsCameraState.lockEntity?.DisplayName ?? "Unknown";
                        MyAPIGateway.Utilities.ShowNotification($"Preset {SavedTargets.IndexOf(saved) + 1} saved to {entityName}", 2000, MyFontEnum.White);
                    }
                }

                if (m_Pref.PeriodicSwitch.IsKeybindPressed())
                {
                    TogglePeriodicSwitch();
                }

                if (m_PeriodicSwitchEnabled)
                {
                    TimeSpan timeSinceLastSwitch = DateTime.Now - m_LastSwitchTime;
                    if (timeSinceLastSwitch.TotalSeconds >= m_PeriodicSwitchInterval)
                    {
                        SwitchToNextTarget();
                        m_LastSwitchTime = DateTime.Now;
                    }
                }

                //Move to target
                if (m_Pref.FindAndMove.IsKeybindPressed())
                {
                    if (m_FindAndMoveState == FindAndMoveState.Idle)
                    {
                        m_FindAndMoveState = FindAndMoveState.GoToSearch;
                    }
                }

                if (m_Pref.FindAndMoveSpin.IsKeybindPressed())
                {
                    if (m_FindAndMoveState == FindAndMoveState.Idle)
                    {
                        m_FindAndMoveState = FindAndMoveState.GoToSearch;
                    }
                }

                if (m_Pref.CyclePlayerUp.IsKeybindPressed())
                {
                    try
                    {
                        var players = new List<IMyPlayer>();
                        var allCharacters = new List<IMyCharacter>();
                        MyAPIGateway.Players.GetPlayers(players);

                        foreach (var player in players)
                        {
                            if (player.Character == null) continue;
                            allCharacters.Add(player.Character);
                        }

                        // Sort characters alphabetically by name
                        allCharacters.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));

                        IMyCharacter targetCharacter = null;
                        int currentIndex = -1;

                        if (ObsCameraState.lockEntity != null)
                        {
                            var currentCharacter = ObsCameraState.lockEntity as IMyCharacter;
                            if (currentCharacter != null)
                            {
                                currentIndex = allCharacters.IndexOf(currentCharacter);
                            }

                            Clear();
                        }

                        if (currentIndex == -1)
                        {
                            targetCharacter = allCharacters.FirstOrDefault();
                        }
                        else
                        {
                            targetCharacter = allCharacters[(currentIndex + 1) % allCharacters.Count];
                        }

                        if (targetCharacter != null)
                        {
                            PositionAndLookAtCharacter(targetCharacter);
                            SetTarget(targetCharacter);
                            SetMode(2);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                if (m_Pref.CyclePlayerDown.IsKeybindPressed())
                {
                    try
                    {
                        var players = new List<IMyPlayer>();
                        var allCharacters = new List<IMyCharacter>();
                        MyAPIGateway.Players.GetPlayers(players);

                        foreach (var player in players)
                        {
                            if (player.Character == null) continue;
                            allCharacters.Add(player.Character);
                        }

                        // Sort characters alphabetically by name
                        allCharacters.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));

                        IMyCharacter targetCharacter = null;
                        int currentIndex = -1;

                        if (ObsCameraState.lockEntity != null)
                        {
                            var currentCharacter = ObsCameraState.lockEntity as IMyCharacter;
                            if (currentCharacter != null)
                            {
                                currentIndex = allCharacters.IndexOf(currentCharacter);
                            }

                            Clear();
                        }

                        if (currentIndex == -1)
                        {
                            targetCharacter = allCharacters.FirstOrDefault();
                        }
                        else
                        {
                            targetCharacter = allCharacters[(currentIndex - 1 + allCharacters.Count) % allCharacters.Count];
                        }

                        if (targetCharacter != null)
                        {
                            PositionAndLookAtCharacter(targetCharacter);
                            SetTarget(targetCharacter);
                            SetMode(2);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                #endregion
            }
            if (MyAPIGateway.Input.IsAnyAltKeyPressed() && MyAPIGateway.Input.IsKeyPress(MyKeys.F10))
            {
                Clear();
            }

            if (ObsCameraState.lockEntity != null && ObsCameraState.lockEntity.Physics != null && m_specCam != null)
            {

                statusSB.Clear();
                if (!MyAPIGateway.Session.IsCameraControlledObject)
                {
                    statusSB.Append(ObsCameraState.lockmode.ToString());
                }


                Vector3 mi = MyAPIGateway.Input.GetPositionDelta();
                Vector2 mouse = MyAPIGateway.Input.GetRotation() * 0.005f * m_specCam.SpeedModeAngular;
                float ri = MyAPIGateway.Input.GetRoll() * 0.02f * m_specCam.SpeedModeAngular;

                if (!MyAPIGateway.Session.IsCameraUserControlledSpectator || MyAPIGateway.Gui.ChatEntryVisible || MyAPIGateway.Gui.IsCursorVisible || !(MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None))
                {
                    //dont move when player is in the menus
                    mi = Vector3D.Zero;
                    mouse = Vector2.Zero;
                    ri = 0f;
                }

                if (m_SmoothCamera && MyAPIGateway.Session.IsCameraUserControlledSpectator && m_FindAndMoveState == FindAndMoveState.Idle)
                {
                    if (m_SmoothMI.LengthSquared() < 0.00001)
                    {
                        m_SmoothMI = Vector3.Zero;
                    }

                    if (m_SmoothMouse.LengthSquared() < 0.00001)
                    {
                        m_SmoothMouse = Vector2.Zero;
                    }

                    if (Math.Abs(m_smoothRoll) < 0.00001)
                    {
                        m_smoothRoll = 0f;
                    }

                    m_SmoothMI += mi;
                    mi = Vector3D.Lerp(Vector3D.Zero, m_SmoothMI, m_Pref.SmoothCameraLERP * 0.99f + 0.01f);
                    m_SmoothMI -= mi;
                    m_SmoothMouse += mouse;
                    mouse = Vector2.Lerp(Vector2.Zero, m_SmoothMouse, m_Pref.SmoothCameraLERP * 0.99f + 0.01f);
                    m_SmoothMouse -= mouse;
                    m_smoothRoll += ri;
                    ri = m_smoothRoll * (m_Pref.SmoothCameraLERP * 0.99f + 0.01f);
                    m_smoothRoll -= ri;
                }
                else
                {
                    m_SmoothMouse = Vector2.Zero;
                    m_SmoothMI = Vector3.Zero;
                    m_smoothRoll = 0f;
                }

                mi *= m_specCam.SpeedModeLinear;
                trackRoll += ri;

                if (MyAPIGateway.Session.IsCameraUserControlledSpectator)
                {
                    Vector3D upv, rightv, forwardv;
                    if (ri != 0)
                    {
                        MyUtils.VectorPlaneRotation(ObsCameraState.localMatrix.Up, ObsCameraState.localMatrix.Right, out upv, out rightv, ri);
                        ObsCameraState.localMatrix.Right = rightv;
                        ObsCameraState.localMatrix.Up = upv;
                    }
                    if (mouse.Y != 0)
                    {
                        MyUtils.VectorPlaneRotation(ObsCameraState.localMatrix.Right, ObsCameraState.localMatrix.Forward, out rightv, out forwardv, -mouse.Y);
                        ObsCameraState.localMatrix.Right = rightv;
                        ObsCameraState.localMatrix.Forward = forwardv;

                    }
                    if (mouse.X != 0)
                    {
                        MyUtils.VectorPlaneRotation(ObsCameraState.localMatrix.Up, ObsCameraState.localMatrix.Forward, out upv, out forwardv, mouse.X);
                        ObsCameraState.localMatrix.Up = upv;
                        ObsCameraState.localMatrix.Forward = forwardv;
                    }
                }

                switch (ObsCameraState.lockmode)
                {
                    case CameraMode.Free:
                        Vector3D WorldMoveFree = mi.X * m_playerCamera.WorldMatrix.Right
                            + mi.Y * m_playerCamera.WorldMatrix.Up
                            + mi.Z * m_playerCamera.WorldMatrix.Backward;
                        m_specCam.Position = ObsCameraState.lockEntity.WorldVolume.Center + ObsCameraState.localVector + WorldMoveFree;
                        break;
                    case CameraMode.Follow:
                        Vector3D WorldMoveFollow = mi.X * ObsCameraState.localMatrix.Right
                            + mi.Y * ObsCameraState.localMatrix.Up
                            + mi.Z * ObsCameraState.localMatrix.Backward;
                        var move = ObsCameraState.localMatrix.Translation + WorldMoveFollow;
                        ObsCameraState.localMatrix.Translation = move;
                        var fworldmatrix = ObsCameraState.lockEntity.WorldMatrix;
                        var targetm = LocalToWorld(ObsCameraState.localMatrix, fworldmatrix);
                        m_specCam.Position = targetm.Translation;
                        m_specCam.SetTarget(m_specCam.Position + targetm.Forward, targetm.Up);

                        break;
                    case CameraMode.Orbit:
                        var lookAt = ObsCameraState.lockEntity.WorldVolume.Center;
                        ObsCameraState.localDistance += mi.Z;
                        ObsCameraState.localDistance = Math.Max(Math.Abs(ObsCameraState.localDistance), ObsCameraState.lockEntity.WorldVolume.Radius);
                        var targetmat = LocalToWorld(ObsCameraState.localMatrix, ObsCameraState.lockEntity.WorldMatrix);
                        m_specCam.Position = lookAt - (targetmat.Forward * ObsCameraState.localDistance);
                        m_specCam.SetTarget(lookAt + targetmat.Forward, targetmat.Up);
                        break;

                    case CameraMode.Track:
                        var trackTarget = ObsCameraState.lockEntity.WorldVolume.Center;
                        var forVec = Vector3D.Normalize(trackTarget - m_specCam.Position);

                        var crossVec = Vector3D.Normalize(Vector3D.Cross(forVec, initTrackUp));
                        var rotationMatrix = MatrixD.CreateFromAxisAngle(crossVec, Math.PI / 2);

                        var upVec = Vector3D.Normalize(Vector3D.Rotate(forVec, rotationMatrix));

                        var rotationRollMatrix = MatrixD.CreateFromAxisAngle(forVec, trackRoll);
                        upVec = Vector3D.Normalize(Vector3D.Rotate(upVec, rotationRollMatrix));

                        if (MyAPIGateway.Input.IsNewKeyReleased(MyKeys.Q) || MyAPIGateway.Input.IsNewKeyReleased(MyKeys.E))
                        {
                            initTrackUp = m_playerCamera.WorldMatrix.Up;
                            trackRoll = 0;
                        }

                        m_specCam.SetTarget(trackTarget, upVec);

                        break;

                    default:
                        break;

                }

                var reconstruct = ObsCameraState.lastOrientation = m_specCam.Orientation;
                reconstruct.Translation = m_specCam.Position;
                ObsCameraState.localMatrix = WorldToLocalNI(reconstruct, ObsCameraState.lockEntity.WorldMatrixNormalizedInv);

                ObsCameraState.localVector = m_specCam.Position - ObsCameraState.lockEntity.WorldVolume.Center;
                if (ObsCameraState.lockmode != CameraMode.Orbit)
                {
                    ObsCameraState.localDistance = ObsCameraState.localVector.Length();
                }
            }

            if (m_FindAndMoveState == FindAndMoveState.GoToSearch)
            {
                if (m_specCam == null)
                {
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                    return;
                }

                m_FindAndMoveState = FindAndMoveState.Searching;
            }

            if (m_FindAndMoveState == FindAndMoveState.Searching)
            {
                var chosenGroup = GetRaycastFocus() ?? GetConeFocus();
                if (chosenGroup == null)
                {
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                    return;
                }
                chosenGrids.Clear();
                chosenGroup.GetGrids(chosenGrids);
                if (chosenGrids.Count == 0)
                {
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                    return;
                }

                chosenGrids = chosenGrids.OrderByDescending(x => x.PositionComp.WorldVolume.Radius).ToList();
                moveGrid = chosenGrids.FirstOrDefault();
                if (moveGrid == null)
                {
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                    return;
                }

                m_FindAndMoveState = FindAndMoveState.GoToMove;
            }

            if (m_FindAndMoveState == FindAndMoveState.GoToMove)
            {
                Clear();
                origStart = m_specCam.Position;
                origFor = m_specCam.Orientation.Forward;
                origUp = m_specCam.Orientation.Up;

                if (MyAPIGateway.Input.IsAnyCtrlKeyPressed())
                {
                    m_FindAndMoveState = FindAndMoveState.InMoveLookback;
                }
                else
                {
                    m_FindAndMoveState = FindAndMoveState.InMove;
                }
            }

            if (m_FindAndMoveState == FindAndMoveState.InMove)
            {
                bool complete = false;

                if (moveGrid == null || moveGrid.Physics == null || MyAPIGateway.Session.IsCameraControlledObject)
                {
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                    return;
                }

                Vector3D currentStartPosition = m_specCam.Position;
                Vector3D focusCentralPosition = moveGrid.WorldAABB.Center;
                Vector3D direction = Vector3D.Normalize(focusCentralPosition - currentStartPosition);
                double pullbackDist = moveGrid.PositionComp.WorldVolume.Radius;
                pullbackDist *= 1.5;

                double travelDist = (focusCentralPosition - currentStartPosition).Length() - pullbackDist;
                Vector3D endPosition = currentStartPosition + (direction * travelDist);

                if (travelDist < 0)
                {
                    complete = true;
                }

                if (viewAnimFrame < maxViewAnimFrame && !complete)
                {
                    var realRatio = (double)viewAnimFrame / maxViewAnimFrame;
                    var easingRatio = OutQuint((float)realRatio);
                    m_specCam.Position = Vector3D.Lerp(origStart, endPosition, easingRatio);
                    viewAnimFrame += 1;
                }
                else
                {
                    complete = true;
                }

                if (complete)
                {
                    SetTarget(moveGrid);
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                }
            }

            if (m_FindAndMoveState == FindAndMoveState.InMoveLookback)
            {
                bool complete = false;

                if (moveGrid == null || moveGrid.Physics == null || MyAPIGateway.Session.IsCameraControlledObject)
                {
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                    return;
                }

                double pushOut = moveGrid.PositionComp.WorldVolume.Radius * 1.5;
                Vector3D endPosition = moveGrid.WorldAABB.Center + (origFor * pushOut);

                if (viewAnimFrame < maxViewAnimFrame + 1 && rotationAnimFrame < maxRotationAnimFrame + 1 && !complete)
                {
                    var moveRatio = (double)viewAnimFrame / maxViewAnimFrame;
                    var moveEaseRatio = OutQuint((float)moveRatio);

                    var rotateRatio = (double)rotationAnimFrame/ maxRotationAnimFrame;
                    var rotateEaseRatio = OutQuint((float)rotateRatio);

                    if (viewAnimFrame < maxViewAnimFrame + 1)
                    {
                        m_specCam.Position = Vector3D.Lerp(origStart, endPosition, moveEaseRatio);
                    }

                    if (rotationAnimFrame < maxRotationAnimFrame + 1)
                    {
                        var lerpedRotation = MathHelper.Lerp(0, Math.PI, rotateEaseRatio);
                        MatrixD rotMat = MatrixD.CreateFromAxisAngle(origUp, lerpedRotation);
                        var finalFor = Vector3D.Rotate(origFor, rotMat);
                        m_specCam.SetTarget(m_specCam.Position + finalFor, m_specCam.Orientation.Up);
                    }

                    if (moveRatio > 0.1)
                    {
                        rotationAnimFrame += 1;
                    }

                    viewAnimFrame += 1;
                }
                else
                {
                    complete = true;
                }

                if (complete)
                {
                    SetTarget(moveGrid);
                    m_FindAndMoveState = FindAndMoveState.GoToIdle;
                }
            }

            if (m_FindAndMoveState == FindAndMoveState.GoToIdle)
            {
                moveGrid = null;
                viewAnimFrame = 0;
                rotationAnimFrame = 0;

                origStart = Vector3D.Zero;
                origFor = Vector3D.Zero;
                origUp = Vector3D.Zero;
                m_FindAndMoveState = FindAndMoveState.Idle;
            }
        }

        private void PositionAndLookAtCharacter(IMyCharacter targetCharacter)
        {
            if (m_specCam == null || targetCharacter == null) return;

            var targetPosition = targetCharacter.WorldMatrix.Translation + targetCharacter.WorldMatrix.Up * 2
                + targetCharacter.WorldMatrix.Backward * 3 + targetCharacter.WorldMatrix.Right * 0.5;

            m_specCam.Position = targetPosition;
            m_specCam.SetTarget(targetCharacter.WorldMatrix.Translation + targetCharacter.WorldMatrix.Up + (targetCharacter.WorldMatrix.Forward * 10), targetCharacter.WorldMatrix.Up);
        }

        private void InitTrack()
        {
            if (m_playerCamera != null)
            {
                initTrackUp = m_playerCamera.WorldMatrix.Up;
                debugCenter = m_playerCamera.WorldMatrix.Translation;
            }
            else
            {
                initTrackUp = Vector3D.Up;
            }

            trackRoll = 0;
        }

        private void UpdateLockEntity(IMyEntity lockEntity)
        {
            ObsCameraState.lockEntity = lockEntity;
            ObsCameraState.islocked = true;

            if (ObsCameraState.lockmode == CameraMode.None)
            {
                SetMode(1);
            }

            if (ObsCameraState.lockmode == CameraMode.Track)
            {
                InitTrack();
            }

            if (ObsCameraState.lockEntity != null && m_specCam != null)
            {
                var reconstruct = m_specCam.Orientation;
                reconstruct.Translation = m_specCam.Position;
                ObsCameraState.localMatrix = WorldToLocalNI(reconstruct, ObsCameraState.lockEntity.WorldMatrixNormalizedInv);
                ObsCameraState.localVector = m_specCam.Position - ObsCameraState.lockEntity.WorldVolume.Center;
                ObsCameraState.localDistance = ObsCameraState.localVector.Length();
            }
        }

        private void Clear()
        {
            ObsCameraState.lockEntity = null;
        }

        private void InitSCB()
        {
            m_init = true;
            m_isServer = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
            m_isDedicated = (MyAPIGateway.Utilities.IsDedicated && m_isServer);
            for (int i = 0; i < 10; i++)
            {
                SavedTargets.Add(new SpecCamTarget());
            }

            if (!m_isDedicated)
            {
                m_Pref = SpecCamPreferences.loadXML();
                SpecCamTarget.LoadKeybinds(SavedTargets, m_Pref.SaveTarget);
                TextHUD = new HudAPIv2(onRegistered);

            }

        }
        HudAPIv2.HUDMessage CameraMessage;
        HudAPIv2.MenuRootCategory SpectatorCameraRootCat;
        HudAPIv2.MenuSubCategory KeybindCat, SaveTargetCat;
        HudAPIv2.MenuKeybindInput LockTargetInput, NextModeInput, ModeFreeInput, ModeFollowInput, ModeOrbitInput, ModeTrackInput, FindAndMoveInput, FindAndMoveSpinInput, 
            CameraSmoothKeybind, CyclePlayerUp, CyclePlayerDown;
        HudAPIv2.MenuSliderInput CameraSmoothRate;
        HudAPIv2.MenuItem CameraSmoothOnOff, HideHudOnOff;
        HudAPIv2.MenuItem Reset;

        HudAPIv2.MenuKeybindInput PeriodicSwitchInput;
        HudAPIv2.MenuSliderInput PeriodicSwitchIntervalSlider;
        HudAPIv2.MenuItem PeriodicSwitchRandomToggle;

        private void onRegistered()
        {
            if (CameraMessage != null)
                return;

            CameraMessage = new HudAPIv2.HUDMessage();
            CameraMessage.Message = statusSB;
            CameraMessage.Flush();
            CameraMessage.Scale = 1;
            CameraMessage.Origin = new Vector2D(-1, 1);
            CameraMessage.Blend = BlendTypeEnum.PostPP;
            CameraMessage.Visible = true;
            if (m_Pref.HideHud)
                CameraMessage.Options |= HudAPIv2.Options.HideHud;

            SpectatorCameraRootCat = new HudAPIv2.MenuRootCategory("Spectator Camera", HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Spectator Camera Options");
            KeybindCat = new HudAPIv2.MenuSubCategory("Keybinds", SpectatorCameraRootCat, "Keybinds");
            LockTargetInput = new HudAPIv2.MenuKeybindInput("Lock Target - " + m_Pref.ToggleLock.ToString(), KeybindCat, "Press any Key [Lock Target]", SetLockTargetKeybind);
            NextModeInput = new HudAPIv2.MenuKeybindInput("Next Mode - " + m_Pref.SwitchMode.ToString(), KeybindCat, "Press any Key [Switch Mode]", SetNextModeKeybind);
            FindAndMoveInput = new HudAPIv2.MenuKeybindInput("Find and Move - " + m_Pref.FindAndMove.ToString(), KeybindCat, "Press any Key [Find and Move]", SetFindAndMoveKeybind);
            FindAndMoveSpinInput = new HudAPIv2.MenuKeybindInput("Find and Move Spin - " + m_Pref.FindAndMoveSpin.ToString(), KeybindCat, "Press any Key [Find and Move Spin]", SetFindAndMoveSpinKeybind);
            CameraSmoothKeybind = new HudAPIv2.MenuKeybindInput("Toggle Smooth Camera - " + m_Pref.ToggleSmoothCamera.ToString(), KeybindCat, "Press any Key [Smooth Camera]", SetSmoothCameraInputKeybind);

            PeriodicSwitchInput = new HudAPIv2.MenuKeybindInput("Periodic Switch - " + m_Pref.PeriodicSwitch.ToString(), KeybindCat, "Press any Key [Periodic Switch]", SetPeriodicSwitchKeybind);
            PeriodicSwitchIntervalSlider = new HudAPIv2.MenuSliderInput("Switch Interval: " + m_PeriodicSwitchInterval + "s", SpectatorCameraRootCat, m_PeriodicSwitchInterval / 30f, "Adjust interval (1-30 seconds)", SetPeriodicSwitchInterval, SliderToInterval);
            PeriodicSwitchRandomToggle = new HudAPIv2.MenuItem(m_PeriodicSwitchRandom ? "Random Switch: On" : "Random Switch: Off", SpectatorCameraRootCat, TogglePeriodicSwitchRandom);

            // Remove Cycle Player Up and Cycle Player Down inputs from the menu
            // CyclePlayerUp = new HudAPIv2.MenuKeybindInput("Cycle Player Up - " + m_Pref.CyclePlayerUp.ToString(), KeybindCat, "Press any Key [Cycle Player Up]", SetCyclePlayerUp);
            // CyclePlayerDown = new HudAPIv2.MenuKeybindInput("Cycle Player Down - " + m_Pref.CyclePlayerDown.ToString(), KeybindCat, "Press any Key [Cycle Player Down]", SetCyclePlayerDown);

            ModeFreeInput = new HudAPIv2.MenuKeybindInput("Mode Free - " + m_Pref.FreeMode.ToString(), KeybindCat, "Press any Key [Free Mode]", SetFreeModeInputKeybind);
            ModeFollowInput = new HudAPIv2.MenuKeybindInput("Mode Follow - " + m_Pref.FollowMode.ToString(), KeybindCat, "Press any Key [Follow Mode]", SetFollowModeInputKeybind);
            ModeOrbitInput = new HudAPIv2.MenuKeybindInput("Mode Orbit - " + m_Pref.OrbitMode.ToString(), KeybindCat, "Press any Key [Locked Mode]", SetOrbitModeInputKeybind);
            ModeTrackInput = new HudAPIv2.MenuKeybindInput("Mode Track - " + m_Pref.TrackMode.ToString(), KeybindCat, "Press any Key [Track Mode]", SetTrackModeInputKeybind);
            SaveTargetCat = new HudAPIv2.MenuSubCategory("Save Target", KeybindCat, "Save Target Options");

            int i = 0;
            foreach (var saved in SavedTargets)
            {
                saved.InitMenu(++i, SaveTargetCat, this);
            }

            CameraSmoothOnOff = new HudAPIv2.MenuItem(m_SmoothCamera ? "Smooth Camera On" : "Smooth Camera Off", SpectatorCameraRootCat, ToggleSmoothCamera);
            HideHudOnOff = new HudAPIv2.MenuItem(m_Pref.HideHud ? "Hud Hidden" : "HUD Always Visible", SpectatorCameraRootCat, ToggleHideHud);

            CameraSmoothRate = new HudAPIv2.MenuSliderInput(string.Format("Camera Smooth Rate {0:N0}", SmoothSenseToValue(m_Pref.SmoothCameraLERP).ToString()), SpectatorCameraRootCat, m_Pref.SmoothCameraLERP, "Higher is less smooth", SmoothOnSubmit, SmoothSenseToValue);

            Reset = new HudAPIv2.MenuItem("Reset to default", SpectatorCameraRootCat, ResetPrefDefaults);
        }

        private void UpdateMenu()
        {
            LockTargetInput.Text = "Lock Target - " + m_Pref.ToggleLock.ToString();
            NextModeInput.Text = "Next Mode - " + m_Pref.SwitchMode.ToString();
            FindAndMoveInput.Text = "Find and Move - " + m_Pref.FindAndMove.ToString();
            FindAndMoveSpinInput.Text = "Find and Move Spin - " + m_Pref.FindAndMoveSpin.ToString();
            ModeFreeInput.Text = "Mode Free - " + m_Pref.FreeMode.ToString();
            ModeFollowInput.Text = "Mode Follow - " + m_Pref.FollowMode.ToString();
            ModeOrbitInput.Text = "Mode Orbit - " + m_Pref.OrbitMode.ToString();
            ModeTrackInput.Text = "Mode Track - " + m_Pref.TrackMode.ToString();

            CameraSmoothKeybind.Text = "Toggle Smooth Camera - " + m_Pref.ToggleSmoothCamera.ToString();
            CameraSmoothOnOff.Text = m_SmoothCamera ? "Smooth Camera On" : "Smooth Camera Off";
            CameraSmoothRate.Text = string.Format("Camera Smooth Rate {0:N0}", SmoothSenseToValue(m_Pref.SmoothCameraLERP).ToString());

            CameraSmoothRate.InitialPercent = m_Pref.SmoothCameraLERP;

            // Remove updates for Cycle Player Up and Cycle Player Down
            // CyclePlayerUp.Text = "Cycle Player Up - " + m_Pref.CyclePlayerUp.ToString();
            // CyclePlayerDown.Text = "Cycle Player Down - " + m_Pref.CyclePlayerDown.ToString();

            PeriodicSwitchInput.Text = "Periodic Switch - " + m_Pref.PeriodicSwitch.ToString();
            PeriodicSwitchIntervalSlider.Text = "Switch Interval: " + m_PeriodicSwitchInterval + "s";
            PeriodicSwitchIntervalSlider.InitialPercent = m_PeriodicSwitchInterval / 30f;
            PeriodicSwitchRandomToggle.Text = m_PeriodicSwitchRandom ? "Random Switch: On" : "Random Switch: Off";

            HideHudOnOff.Text = m_Pref.HideHud ? "Hud Hidden" : "HUD Always Visible";
        }

        private void ToggleHideHud()
        {
            HideHud = !HideHud;

            UpdateMenu();
        }

        private void SetLockTargetKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.ToggleLock = newKeybind;
            UpdateMenu();
        }

        private void SetNextModeKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.SwitchMode = newKeybind;
            UpdateMenu();
        }

        private void SetFindAndMoveKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.FindAndMove = newKeybind;
            UpdateMenu();
        }

        private void SetFindAndMoveSpinKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.FindAndMoveSpin = newKeybind;
            UpdateMenu();
        }

        private void SetFreeModeInputKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.FreeMode = newKeybind;
            UpdateMenu();
        }

        private void SetFollowModeInputKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.FollowMode = newKeybind;
            UpdateMenu();
        }

        private void SetOrbitModeInputKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.OrbitMode = newKeybind;
            UpdateMenu();
        }

        private void SetTrackModeInputKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.TrackMode = newKeybind;
            UpdateMenu();
        }

        private object SmoothSenseToValue(float arg)
        {
            return Math.Floor(arg * 100f);
        }

        private void SmoothOnSubmit(float obj)
        {
            m_Pref.SmoothCameraLERP = obj;
            UpdateMenu();
        }

        private void ToggleSmoothCamera()
        {
            m_SmoothCamera = !m_SmoothCamera;
            UpdateMenu();
        }

        private void SetSmoothCameraInputKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.ToggleSmoothCamera = newKeybind;
            UpdateMenu();
        }

        private void SetPeriodicSwitchKeybind(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.PeriodicSwitch = newKeybind;
            UpdateMenu();
        }

        private void SetPeriodicSwitchInterval(float value)
        {
            m_PeriodicSwitchInterval = Math.Max(1f, Math.Min(30f, value * 30f));
            MyAPIGateway.Utilities.ShowNotification($"Switch interval set to {m_PeriodicSwitchInterval}s", 2000);
            UpdateMenu();
        }

        private object SliderToInterval(float value)
        {
            return Math.Round(value * 30f, 1) + "s";
        }

        private void TogglePeriodicSwitchRandom()
        {
            m_PeriodicSwitchRandom = !m_PeriodicSwitchRandom;
            UpdateMenu();
        }

        private void SetCyclePlayerUp(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.CyclePlayerUp = newKeybind;
            UpdateMenu();
        }

        private void SetCyclePlayerDown(MyKeys arg1, bool arg2, bool arg3, bool arg4)
        {
            var newKeybind = new Keybind(arg1, arg2, arg3, arg4);
            m_Pref.CyclePlayerDown = newKeybind;
            UpdateMenu();
        }

        private void ResetPrefDefaults()
        {
            m_SmoothCamera = false;
            m_Pref = new SpecCamPreferences();
            SpecCamTarget.LoadKeybinds(SavedTargets, m_Pref.SaveTarget);
            UpdateMenu();
        }

        private void TogglePeriodicSwitch()
        {
            m_PeriodicSwitchEnabled = !m_PeriodicSwitchEnabled;
            if (m_PeriodicSwitchEnabled)
            {
                m_LastSwitchTime = DateTime.Now;
                MyAPIGateway.Utilities.ShowNotification($"Periodic switching enabled (Interval: {m_PeriodicSwitchInterval}s)", 2000);
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("Periodic switching disabled", 2000);
            }
        }

        private void SwitchToNextTarget()
        {
            // Count the number of valid (set) presets
            int validPresetsCount = SavedTargets.Count(target => target.HasState);

            if (validPresetsCount == 0)
            {
                MyAPIGateway.Utilities.ShowNotification("No saved presets to switch between", 2000);
                m_PeriodicSwitchEnabled = false;
                return;
            }

            if (m_PeriodicSwitchRandom)
            {
                // Switch to a random preset (excluding the current one)
                int currentRandomIndex = SavedTargets.IndexOf(SavedTargets.FirstOrDefault(target => target.HasState && target.State.Equals(ObsCameraState)));
                int randomIndex;
                do
                {
                    randomIndex = new Random().Next(SavedTargets.Count);
                } while (!SavedTargets[randomIndex].HasState || randomIndex == currentRandomIndex);

                ObsCameraState = SavedTargets[randomIndex].State;
                MyAPIGateway.Utilities.ShowNotification($"Switched to preset {randomIndex + 1}", 1000);
            }
            else
            {
                // Find the next valid preset in sequential order
                int startIndex = m_CurrentTargetIndex;
                do
                {
                    m_CurrentTargetIndex = (m_CurrentTargetIndex + 1) % SavedTargets.Count;
                    if (SavedTargets[m_CurrentTargetIndex].HasState)
                    {
                        ObsCameraState = SavedTargets[m_CurrentTargetIndex].State;
                        MyAPIGateway.Utilities.ShowNotification($"Switched to preset {m_CurrentTargetIndex + 1}", 1000);
                        return;
                    }
                } while (m_CurrentTargetIndex != startIndex);

                // This should never happen due to the validPresetsCount check, but just in case
                MyAPIGateway.Utilities.ShowNotification("No valid presets found", 2000);
                m_PeriodicSwitchEnabled = false;
            }
        }

        private Vector3D WorldToLocal(Vector3D position, MatrixD worldMatrixNormalizedInv)
        {
            return Vector3D.Transform(position, worldMatrixNormalizedInv);
        }
        private Vector3D LocalToWorld(Vector3D localTransform, MatrixD worldMatrix)
        {
            return Vector3D.Transform(localTransform, worldMatrix);
        }
        private MatrixD WorldToLocal(MatrixD current, MatrixD worldMatrix)
        {
            return current * MatrixD.Normalize(MatrixD.Invert(worldMatrix));
        }
        private MatrixD WorldToLocalNI(MatrixD current, MatrixD worldMatrixNormalizedInv)
        {
            return current * worldMatrixNormalizedInv;
        }

        private MatrixD LocalToWorld(MatrixD local, MatrixD worldMatrix)
        {
            return local * worldMatrix;
        }

        public IMyGridGroupData GetConeFocus()
        {
            IMyCamera camera = MyAPIGateway.Session.Camera;

            List<IMyGridGroupData> gridGroups = new List<IMyGridGroupData>();
            MyAPIGateway.GridGroups.GetGridGroups(GridLinkTypeEnum.Physical, gridGroups);
            IMyGridGroupData solutionGroup = null;
            IMyGridGroupData backupGroup = null;

            double closestDistance = maxSearchDistance * 2;
            double closestBackupDistance = maxSearchDistance * 2;
            foreach (var group in gridGroups)
            {
                List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
                group.GetGrids(grids);
                // Exclude static grids
                grids = grids.Where(grid => !grid.IsStatic).ToList();
                if (grids.Count == 0) continue;

                grids.OrderBy(x => x.WorldAABB.Volume);
                IMyCubeGrid biggestGrid = grids[0];
                MyCubeGrid cBiggestGrid = biggestGrid as MyCubeGrid;

                if (cBiggestGrid == null || cBiggestGrid.Physics == null) continue;

                bool isBackup = cBiggestGrid.BlocksCount < 50;

                var vectorToGrid = cBiggestGrid.PositionComp.WorldAABB.Center - camera.WorldMatrix.Translation;
                var dirToGrid = Vector3D.Normalize(vectorToGrid);
                var angleToGrid = MyUtils.GetAngleBetweenVectors(dirToGrid, camera.WorldMatrix.Forward);

                //Main
                if (!isBackup && (angleToGrid < maxSearchAngle))
                {
                    var distance = vectorToGrid.Length();
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        solutionGroup = group;
                    }
                }

                //Backup
                if (isBackup && (angleToGrid < maxSearchAngle))
                {
                    var backupDistance = vectorToGrid.Length();
                    if (backupDistance < closestBackupDistance)
                    {
                        closestBackupDistance = backupDistance;
                        backupGroup = group;
                    }
                }
            }

            return solutionGroup ?? backupGroup;
        }

        public IMyGridGroupData GetRaycastFocus()
        {
            IMyGridGroupData gridGroup = null;
            IMyCamera camera = MyAPIGateway.Session.Camera;
            var start = camera.WorldMatrix.Translation;
            var end = start + camera.WorldMatrix.Forward * maxSearchDistance;

            IHitInfo hitInfo;
            MyAPIGateway.Physics.CastRay(start, end, out hitInfo);

            if (hitInfo == null || hitInfo.HitEntity == null) return gridGroup;

            MyCubeGrid grid = hitInfo.HitEntity as MyCubeGrid;

            //Raycast search doesn't hit backups - rely on cone searh for that. also exclude static grids
            if (grid == null || grid.Physics == null || grid.IsStatic) return gridGroup;

            gridGroup = MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Physical, grid);
            return gridGroup;
        }

        protected override void UnloadData()
        {
        }
    }
}