namespace DefenseShields
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Support;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Ingame;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;
    using IMyDoor = Sandbox.ModAPI.IMyDoor;
    using IMyGasGenerator = Sandbox.ModAPI.IMyGasGenerator;
    using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenGenerator), false, "DSSupergen")]
    public class O2Generators : MyGameLogicComponent
    {
        private const double MysteriousH2ToO2 = (double)783 / 3;
        private const double ForgottenMagicConst = 10.3316326531d;

        internal readonly Dictionary<IMyDoor, DoorStatus> Doors = new Dictionary<IMyDoor, DoorStatus>();
        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
        internal bool InThisTerminal => Session.Instance.LastTerminalId == O2Generator.EntityId;
        private int _airIPercent = -1;
        private int _count = -1;
        private int _lCount = -1;

        private double _shieldVolFilled;
        private double _oldShieldVol;
        private bool _firstSync;

        private bool _isServer;
        private bool _isDedicated;
        private bool _doorsStage1;
        private bool _doorsStage2;
        private bool _doorsStage3;
        private bool _doorsStage4;

        private IMyInventory _inventory;

        internal ShieldGridComponent ShieldComp;

        internal int RotationTime { get; set; }
        internal int AnimationLoop { get; set; }
        internal int TranslationTime { get; set; }
        internal float EmissiveIntensity { get; set; }

        internal bool SettingsUpdated { get; set; }
        internal bool ClientUiUpdate { get; set; }
        internal bool IsFunctional { get; set; }
        internal bool IsWorking { get; set; }
        internal bool AllInited { get; set; }
        internal bool Suspended { get; set; }
        internal bool IsStatic { get; set; }
        internal bool BlockIsWorking { get; set; }
        internal bool BlockWasWorking { get; set; }
        internal bool ContainerInited { get; set; }
        internal bool AirPressure { get; set; }

        internal MyResourceSourceComponent Source { get; set; }
        internal O2GeneratorState O2State { get; set; }
        internal O2GeneratorSettings O2Set { get; set; }

        internal IMyGasGenerator O2Generator { get; set; }
        internal MyCubeGrid MyGrid { get; set; }
        internal MyCubeBlock MyCube { get; set; }

        public override void OnAddedToContainer()
        {
            if (!ContainerInited)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                O2Generator = (IMyGasGenerator)Entity;
                ContainerInited = true;
            }
            if (Entity.InScene) OnAddedToScene();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                StorageSetup();
            }
            catch (Exception ex) { Log.Line($"Exception in EntityInit: {ex}"); }
        }

        public override void OnAddedToScene()
        {
            try
            {
                MyGrid = (MyCubeGrid)O2Generator.CubeGrid;
                MyCube = O2Generator as MyCubeBlock;
                RegisterEvents();
                if (Session.Enforced.Debug == 3) Log.Line($"OnAddedToScene: - O2GeneatorId [{O2Generator.EntityId}]");
            }
            catch (Exception ex) { Log.Line($"Exception in OnAddedToScene: {ex}"); }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            try
            {
                Session.Instance.O2Generators.Add(this);
                Source = O2Generator.Components.Get<MyResourceSourceComponent>();
                _isServer = Session.Instance.IsServer;
                _isDedicated = Session.Instance.DedicatedServer;
                AirPressure = MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization; 
                RemoveControls();
                CreateUi();
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateOnceBeforeFrame: {ex}"); }
        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                if (_count++ == 5)
                {
                    _count = 0;
                    _lCount++;
                    if (_lCount == 10) _lCount = 0;
                }

                var wait = _isServer && _count != 0 && O2State.State.Backup;

                MyGrid = MyCube.CubeGrid;
                if (wait || MyGrid?.Physics == null) return;
                if (!_isDedicated && _count == 0) TerminalRefresh();

                if (!O2GeneratorReady()) return;

                if (SettingsUpdated || ClientUiUpdate) SettingsUpdate();

                if (_count == 5 && (!_doorsStage1 || !_doorsStage2)) DoorTightnessFix();
                else if (_count == 5 && _doorsStage1 && _doorsStage2 && !_doorsStage3) DoorTightnessFix();
                else if (_count == 0 && _doorsStage3 && !_doorsStage4) DoorTightnessFix();

                if (_isServer && _count == 0)
                {
                    Pressurize();
                    NeedUpdate(O2State.State.Pressurized, true);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateBeforeSimulation: {ex}"); }
        }

        public override bool IsSerialized()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (O2Generator.Storage != null)
                {
                    O2State.SaveState();
                    O2Set.SaveSettings();
                }
            }
            return false;
        }

        public override void OnRemovedFromScene()
        {
            try
            {
                if (!Entity.MarkedForClose)
                {
                    return;
                }
                if (Session.Instance.O2Generators.Contains(this)) Session.Instance.O2Generators.Remove(this);
                if (ShieldComp?.ActiveO2Generator == this) ShieldComp.ActiveO2Generator = null;
                RegisterEvents(false);
                IsWorking = false;
                IsFunctional = false;
            }
            catch (Exception ex) { Log.Line($"Exception in OnRemovedFromScene: {ex}"); }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene) OnRemovedFromScene();
        }

        public override void Close()
        {
            base.Close();
            try
            {
                if (Session.Instance.O2Generators.Contains(this)) Session.Instance.O2Generators.Remove(this);
            }
            catch (Exception ex) { Log.Line($"Exception in Close: {ex}"); }
        }

        public override void MarkForClose()
        {
            try
            {
            }
            catch (Exception ex) { Log.Line($"Exception in MarkForClose: {ex}"); }
            base.MarkForClose();
        }

        internal static void RemoveControls()
        {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<Sandbox.ModAPI.Ingame.IMyGasGenerator>(out actions);
            var aRefill = actions.First((x) => x.Id.ToString() == "Refill");
            aRefill.Enabled = block => false;
            var aAutoRefill = actions.First((x) => x.Id.ToString() == "Auto-Refill");
            aAutoRefill.Enabled = block => false;

            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<Sandbox.ModAPI.Ingame.IMyGasGenerator>(out controls);
            var cRefill = controls.First((x) => x.Id.ToString() == "Refill");
            cRefill.Enabled = block => false;
            cRefill.Visible = block => false;
            cRefill.RedrawControl();

            var cAutoRefill = controls.First((x) => x.Id.ToString() == "Auto-Refill");
            cAutoRefill.Enabled = block => false;
            cAutoRefill.Visible = block => false;
            cAutoRefill.RedrawControl();
        }

        internal void RestartDoorFix()
        {
            if (Session.Enforced.Debug == 3) Log.Line($"RestartDoorFix - O2GeneratorId[{O2Generator.EntityId}]");
            if (!_doorsStage3 || !_doorsStage2 || !_doorsStage3 || !_doorsStage4)
            {
                if (Session.Enforced.Debug == 3) Log.Line($"RestartDoorFix already running:{!_doorsStage1} - {!_doorsStage2} - {!_doorsStage3} - {!_doorsStage4}! - O2GeneratorId[{O2Generator.EntityId}]");
                return;
            }
            _doorsStage1 = false;
            _doorsStage2 = false;
            _doorsStage3 = false;
            _doorsStage4 = false;
        }

        internal void UpdateState(O2GeneratorStateValues newState)
        {
            if (newState.MId > O2State.State.MId)
            {
                O2State.State = newState;
                if (!_isDedicated) UpdateVisuals();
                if (Session.Enforced.Debug == 3) Log.Line($"UpdateState - O2GenId [{O2Generator.EntityId}]:\n{newState}");
            }
        }

        internal void UpdateSettings(O2GeneratorSettingsValues newSettings)
        {
            if (newSettings.MId > O2Set.Settings.MId)
            {
                if (Session.Enforced.Debug == 3) Log.Line($"UpdateSettings for O2Generator - Fix:{newSettings.FixRoomPressure} - O2GenId [{O2Generator.EntityId}]");
                SettingsUpdated = true;
                O2Set.Settings = newSettings;
            }
        }

        private void SettingsUpdate()
        {
            if (_count == 3)
            {
                if (SettingsUpdated)
                {
                    if (Session.Enforced.Debug == 3) Log.Line($"SettingsUpdated: server:{_isServer} - FixRooms:{O2Set.Settings.FixRoomPressure} - O2GeneratorId [{O2Generator.EntityId}]");
                    SettingsUpdated = false;
                    O2Set.SaveSettings();
                    O2State.SaveState();
                    if (!ClientUiUpdate && O2Set.Settings.FixRoomPressure) RestartDoorFix();
                }
            }
            else if (_count == 4 && !SettingsUpdated)
            {
                if (ClientUiUpdate)
                {
                    if (Session.Enforced.Debug == 3) Log.Line($"ClientUiUpdate: server:{_isServer} - FixRooms:{O2Set.Settings.FixRoomPressure} - O2GeneratorId [{O2Generator.EntityId}]");
                    ClientUiUpdate = false;
                    MyCube.UpdateTerminal();
                    O2Generator.RefreshCustomInfo();
                    if (!_isServer) O2Set.NetworkUpdate();
                    if (O2Set.Settings.FixRoomPressure) RestartDoorFix();
                }
            }
        }

        private void SendFixBroadcast()
        {
            var sendMessage = false;
            if (MyAPIGateway.Session?.Player?.Character?.WorldVolume != null)
            {
                if (ShieldComp.DefenseShields.ShieldSphere.Intersects(MyAPIGateway.Session.Player.Character.WorldVolume)) sendMessage = true;
            }

            /*
            foreach (var player in Session.Instance.Players.Values)
            {
                if (player.IdentityId != MyAPIGateway.Session.Player.IdentityId) continue;
                if (!ShieldComp.DefenseShields.ShieldSphere.Intersects(player.Character.WorldVolume)) continue;
                sendMessage = true;
                break;
            }
            */
            if (sendMessage) MyAPIGateway.Utilities.ShowNotification("[ " + MyGrid.DisplayName + " ]" + " -- Keen Bug, Shield Pressurizer Room Fixer, fixing rooms, resetting doors!", 8000, "Red");
        }

        private void DoorTightnessFix()
        {
            if (!AirPressure)
            {
                _doorsStage1 = true;
                _doorsStage2 = true;
                _doorsStage3 = true;
                _doorsStage4 = true;
                O2Set.Settings.FixRoomPressure = false;
                if (Session.Enforced.Debug == 1) Log.Line("AirPressure is disabled");
                return;
            }
            if (!_doorsStage1)
            {
                Doors.Clear();
                foreach (var grid in ShieldComp.DefenseShields.ProtectedEntCache.Keys)
                {
                    if (!(grid is MyCubeGrid)) continue;
                    foreach (var myCube in ((MyCubeGrid)grid).GetFatBlocks())
                    {
                        var myDoor = myCube as IMyDoor;
                        if (myDoor != null)
                        {
                            var status = myDoor.Status;
                            if (status == DoorStatus.Opening || status == DoorStatus.Open)
                            {
                                status = DoorStatus.Open;
                                myDoor.CloseDoor();
                            }
                            else
                            {
                                status = DoorStatus.Closed;
                                myDoor.OpenDoor();
                            }
                            Doors.Add(myDoor, status);
                        }
                    }
                }
                if (!_isDedicated) SendFixBroadcast();
                _doorsStage1 = true;
            }
            else if (!_doorsStage2)
            {
                foreach (var item in Doors)
                {
                    if (item.Value == DoorStatus.Open)
                    {
                        item.Key.OpenDoor();
                    }
                    else
                    {
                        item.Key.CloseDoor();
                    }

                }
                _doorsStage2 = true;
            }
            else if (!_doorsStage3)
            {
                foreach (var item in Doors)
                {
                    if (item.Value == DoorStatus.Closed)
                    {
                        item.Key.OpenDoor();
                    }
                }
                _doorsStage3 = true;
            }
            else if (!_doorsStage4)
            {
                foreach (var item in Doors)
                {
                    if (item.Value == DoorStatus.Closed)
                    {
                        item.Key.OpenDoor();
                        item.Key.CloseDoor();
                    }
                }
                _doorsStage4 = true;
                O2Set.Settings.FixRoomPressure = false;
                O2Set.SaveSettings();
                Doors.Clear();
            }
        }

        private void Pressurize()
        {
            var sc = ShieldComp;
            var shieldFullVol = sc.ShieldVolume;
            var startingO2Fpercent = sc.DefaultO2 + sc.DefenseShields.DsState.State.IncreaseO2ByFPercent;

            if (shieldFullVol < _oldShieldVol)
            {
                var ratio = _oldShieldVol / shieldFullVol;
                if (startingO2Fpercent * ratio > 1) startingO2Fpercent = 1d;
                else startingO2Fpercent = startingO2Fpercent * ratio;
            }
            else if (shieldFullVol > _oldShieldVol)
            {
                var ratio = _oldShieldVol / shieldFullVol;
                startingO2Fpercent = startingO2Fpercent * ratio;
            }
            _oldShieldVol = shieldFullVol;

            _shieldVolFilled = shieldFullVol * startingO2Fpercent;
            if (!_isDedicated) UpdateAirEmissives(startingO2Fpercent);

            var shieldVolStillEmpty = shieldFullVol - _shieldVolFilled;
            if (!(shieldVolStillEmpty > 0)) return;

            var amount = _inventory.CurrentVolume.RawValue;
            if (amount <= 0) return;
            if (amount - ForgottenMagicConst > 0)
            {
                _inventory.RemoveItems(0, 2700);
                _shieldVolFilled += ForgottenMagicConst * MysteriousH2ToO2;
            }
            else
            {
                _inventory.RemoveItems(0, _inventory.CurrentVolume);
                _shieldVolFilled += amount * MysteriousH2ToO2;
            }
            if (_shieldVolFilled > shieldFullVol) _shieldVolFilled = shieldFullVol;

            var shieldVolPercentFull = _shieldVolFilled * 100.0;
            var fPercentToAddToDefaultO2Level = (shieldVolPercentFull / shieldFullVol * 0.01) - sc.DefaultO2;

            sc.DefenseShields.DsState.State.IncreaseO2ByFPercent = fPercentToAddToDefaultO2Level;
            sc.O2Updated = true;
            if (Session.Enforced.Debug == 3) Log.Line($"default:{ShieldComp.DefaultO2} - Filled/(Max):{O2State.State.VolFilled}/({shieldFullVol}) - ShieldO2Level:{sc.DefenseShields.DsState.State.IncreaseO2ByFPercent} - O2Before:{MyAPIGateway.Session.OxygenProviderSystem.GetOxygenInPoint(MyAPIGateway.Session.Player.GetPosition())}");
        }

        private void TerminalRefresh()
        {
            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel && Session.Instance.LastTerminalId == O2Generator.EntityId)
            {
                O2Generator.RefreshCustomInfo();
                MyCube.UpdateTerminal();
            }
        }

        private bool InitO2Generator()
        {
            if (!AllInited)
            {
                if (_isServer)
                {
                    if (ShieldComp?.DefenseShields?.MyGrid != MyGrid) MyGrid.Components.TryGet(out ShieldComp);

                    if (ShieldComp?.DefenseShields == null || ShieldComp?.ActiveO2Generator != null || !ShieldComp.DefenseShields.Warming || ShieldComp.ShieldVolume <= 0) return false;
                    ShieldComp.ActiveO2Generator = this;
                    _oldShieldVol = ShieldComp.ShieldVolume;
                    _inventory = MyCube.GetInventory();
                }
                else
                {
                    if (ShieldComp?.DefenseShields?.MyGrid != MyGrid) MyGrid.Components.TryGet(out ShieldComp);

                    if (ShieldComp?.DefenseShields == null) return false;
                    if (ShieldComp.ActiveO2Generator == null) ShieldComp.ActiveO2Generator = this;
                }

                Source.Enabled = false;
                O2Generator.AutoRefill = false;
                RemoveControls();
                O2Generator.AppendingCustomInfo += AppendingCustomInfo;
                ResetAirEmissives(-1);
                BlockWasWorking = true;
                AllInited = true;
                return true;
            }
            return true;
        }

        private bool O2GeneratorReady()
        {
            if (ShieldComp?.DefenseShields?.MyGrid != MyGrid) MyGrid.Components.TryGet(out ShieldComp);
            if (_isServer)
            {
                if ((!AllInited && !InitO2Generator()) || !BlockWorking()) return false;
            }
            else
            {
                if (!AllInited && !InitO2Generator()) return false;
                if (ShieldComp?.DefenseShields == null) return false;

                if (!O2State.State.Backup && ShieldComp.ActiveO2Generator != this) ShieldComp.ActiveO2Generator = this;

                if (!O2State.State.Pressurized) return false;
            }
            return true;
        }

        private bool BlockWorking()
        {
            if (_count <= 0) IsStatic = MyGrid.Physics.IsStatic;

            if (!O2Generator.Enabled || !IsFunctional || !IsStatic || !IsWorking)
            {
                if (O2State.State.Pressurized) UpdateAirEmissives(0f);
                NeedUpdate(O2State.State.Pressurized, false);
                return false;
            }

            if (ShieldComp?.DefenseShields == null)
            {
                NeedUpdate(O2State.State.Pressurized, false);
                return false;
            }

            if (ShieldComp.ActiveO2Generator != this)
            {
                if (ShieldComp.ActiveO2Generator == null)
                {
                    ShieldComp.ActiveO2Generator = this;
                    O2State.State.Backup = false;
                }
                else if (ShieldComp.ActiveO2Generator != this)
                {
                    O2State.State.Backup = true;
                    O2State.State.Pressurized = false;
                }
            }

            if (!O2State.State.Backup && ShieldComp.ActiveO2Generator == this)
            {
                NeedUpdate(O2State.State.Pressurized, true);
                return true;
            }
            NeedUpdate(O2State.State.Pressurized, false);
            return false;
        }

        private void UpdateVisuals()
        {
            UpdateAirEmissives(O2State.State.O2Level);
        }

        private void NeedUpdate(bool onState, bool turnOn)
        {
            var o2State = O2State.State;
            if (ShieldComp?.DefenseShields == null)
            {
                if (O2State.State.Pressurized)
                {
                    o2State.Pressurized = false;
                    o2State.VolFilled = 0;
                    o2State.DefaultO2 = 0;
                    o2State.O2Level = 0;
                    o2State.ShieldVolume = 0;
                    O2State.SaveState();
                    O2State.NetworkUpdate();
                }
                return;
            }

            var conState = ShieldComp.DefenseShields.DsState.State;
            var o2Level = conState.IncreaseO2ByFPercent + ShieldComp.DefaultO2;
            var o2Change = !o2State.VolFilled.Equals(_shieldVolFilled) || !o2State.DefaultO2.Equals(ShieldComp.DefaultO2) || !o2State.ShieldVolume.Equals(ShieldComp.ShieldVolume) || !o2State.O2Level.Equals(o2Level);
            if (!onState && turnOn)
            {
                o2State.Pressurized = true;
                o2State.VolFilled = _shieldVolFilled;
                o2State.DefaultO2 = ShieldComp.DefaultO2;
                o2State.O2Level = o2Level;
                o2State.ShieldVolume = ShieldComp.ShieldVolume;
                O2State.SaveState();
                O2State.NetworkUpdate();
            }
            else if (onState & !turnOn)
            {
                o2State.Pressurized = false;
                o2State.VolFilled = _shieldVolFilled;
                o2State.DefaultO2 = ShieldComp.DefaultO2;
                o2State.O2Level = o2Level;
                o2State.ShieldVolume = ShieldComp.ShieldVolume;
                O2State.SaveState();
                O2State.NetworkUpdate();
            }
            else if (o2Change)
            {
                o2State.VolFilled = _shieldVolFilled;
                o2State.DefaultO2 = ShieldComp.DefaultO2;
                o2State.O2Level = o2Level;
                o2State.ShieldVolume = ShieldComp.ShieldVolume;
                O2State.SaveState();
                O2State.NetworkUpdate();
            }
        }

        private void UpdateAirEmissives(double fPercent)
        {
            var tenPercent = fPercent * 10;
            if ((int)tenPercent != _airIPercent) _airIPercent = (int)tenPercent;
            else return;
            if (tenPercent > 9) tenPercent = 9;
            ResetAirEmissives(tenPercent);
        }

        private void ResetAirEmissives(double tenPercent)
        {
            for (int i = 0; i < 10; i++)
            {
                if (tenPercent < 0 || i > tenPercent)
                {
                    O2Generator.SetEmissiveParts("Emissive" + i, Color.Transparent, 0f);
                }
                else
                {
                    O2Generator.SetEmissiveParts("Emissive" + i, UtilsStatic.GetAirEmissiveColorFromDouble(i * 10), 1f);
                }
            }
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            if (!O2State.State.Pressurized)
            {
                stringBuilder.Append("\n" +
                                     "\n[ Generator Standby ]");
            }
            else
            {
                stringBuilder.Append("\n" +
                                     "\n[Ice-to-Air volumetric ratio]: 261.3" +
                                     "\n[Shield Volume]: " + O2State.State.ShieldVolume.ToString("N0") +
                                     "\n[Volume Filled]: " + O2State.State.VolFilled.ToString("N0") +
                                     "\n[Backup Generator]: " + O2State.State.Backup +
                                     "\n[Internal O2 Lvl]: " + ((O2State.State.O2Level + O2State.State.DefaultO2) * 100).ToString("0") + "%" +
                                     "\n[External O2 Lvl]: " + (O2State.State.DefaultO2 * 100).ToString("0") + "%");
            }
        }

        private void StorageSetup()
        {
            if (O2Set == null) O2Set = new O2GeneratorSettings(O2Generator);
            if (O2State == null) O2State = new O2GeneratorState(O2Generator);
            if (O2Generator.Storage == null) O2State.StorageInit();

            O2Set.LoadSettings();
            O2State.LoadState();
            if (MyAPIGateway.Multiplayer.IsServer) O2Set.Settings.FixRoomPressure = false;
        }

        private void RegisterEvents(bool register = true)
        {
            if (register)
            {
                MyCube.IsWorkingChanged += IsWorkingChanged;
                IsWorkingChanged(MyCube);
            }
            else
            {
                O2Generator.AppendingCustomInfo -= AppendingCustomInfo;
                MyCube.IsWorkingChanged -= IsWorkingChanged;
            }
        }

        private void IsWorkingChanged(MyCubeBlock myCubeBlock)
        {
            IsFunctional = myCubeBlock.IsFunctional;
            IsWorking = myCubeBlock.IsWorking;
        }

        private void CreateUi()
        {
            O2Ui.CreateUi(O2Generator);
        }

    }
}