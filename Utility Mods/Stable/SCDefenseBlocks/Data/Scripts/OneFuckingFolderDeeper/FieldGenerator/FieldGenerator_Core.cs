﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using ProtoBuf;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.Sync;
using VRage.Network;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Utils;
using Jnick_SCModRepository.SCDefenseBlocks.Data.Scripts.OneFuckingFolderDeeper.MetalFoam;

namespace Starcore.FieldGenerator
{
    public class Config {
        // Power-related constants that should match FieldGeneratorSettings
        public const float MaxPowerDraw = 500.00f;  // Power consumption at max field power
        public const float MinPowerDraw = 50.00f;   // Power consumption at min field power

        // Field Power related constants
        public const float PerModuleAmount = 12.5f;  // Power increase per module
        public const int MaxModuleCount = 4;         // Maximum number of modules

        // These effectively define the field power range
        // MaxFieldPower would be PerModuleAmount * MaxModuleCount = 50%
        // MinFieldPower would be 0%

        // Other configs not related to power/field settings
        public const bool SimplifiedMode = true;
        public const int MaxSiegeTime = 150;
        public const int SiegePowerDraw = 900;
        public const int DamageEventThreshold = 6;
        public const int ResetInterval = 3;
        public const float SizeModifierMax = 0.8f;
        public const int MaxBlockCount = 35000;
        public const float SizeModifierMin = 1.2f;
        public const int MinBlockCount = 2500;
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "FieldGen_Core")]
    public class FieldGenerator : MyGameLogicComponent {
        public MySync<bool, SyncDirection.BothWays> SiegeModeSync;
        public MySync<float, SyncDirection.BothWays> FieldPowerSync;
        public MySync<float, SyncDirection.BothWays> MaxFieldPowerSync;
        public MySync<float, SyncDirection.BothWays> MinFieldPowerSync;
        public MySync<bool, SyncDirection.BothWays> SiegeCooldownActiveSync;
        public MySync<int, SyncDirection.BothWays> SiegeElapsedTimeSync;
        public MySync<int, SyncDirection.BothWays> SiegeCooldownTimeSync;
        public MySync<float, SyncDirection.BothWays> StabilitySync;

        private IMyCollector Block;
        public FieldGeneratorSettings Settings;
        public readonly Guid SettingsGuid = new Guid("59e91d1a-eddc-4f72-ba8d-3951eec82e9e");
        public MyResourceSinkComponent Sink = null;
        private readonly bool IsServer = MyAPIGateway.Session.IsServer;

        private Dictionary<string, IMyModelDummy> _coreDummies;
        private HashSet<long> _attachedModuleIds;
        private int _moduleCount = 0;
        private int _damageEventCounter = 0;
        private int _resetCounter = 0;
        private List<IMySlimBlock> _gridBlocks;
        private int _gridBlockCount;
        private IMyHudNotification notifSiege = null;
        private IMyHudNotification notifPower = null;

        #region Core Methods
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            try {
                base.Init(objectBuilder);
                Block = (IMyCollector)Entity;

                // Initialize all collections before any potential serialization
                _coreDummies = new Dictionary<string, IMyModelDummy>();
                _attachedModuleIds = new HashSet<long>();
                _gridBlocks = new List<IMySlimBlock>();
                _gridBlockCount = 0;

                // Initialize settings
                Settings = new FieldGeneratorSettings(this);
                Settings.MinFieldPower = 0;
                Settings.MaxFieldPower = Config.PerModuleAmount;
                Settings.FieldPower = 0;
                Settings.Stability = 100;

                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"FieldGenerator.Init: {e}");
            }
        }

        public override void UpdateOnceBeforeFrame() {
            try {
                base.UpdateOnceBeforeFrame();
                if (Block?.CubeGrid?.Physics == null)
                    return;

                FieldGeneratorControls.DoOnce(ModContext);

                // Initialize power system first
                Sink = Block.Components.Get<MyResourceSinkComponent>();

                // Initialize settings and upgrades before power system setup
                if (IsServer) {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        Block.Model.GetDummies(_coreDummies);
                        Block.CubeGrid.OnBlockAdded += OnBlockAdded;
                        Block.CubeGrid.OnBlockRemoved += OnBlockRemoved;

                        InitExistingUpgrades();
                        MyLog.Default.WriteLineAndConsole($"After upgrade init - Module count: {_moduleCount}, Max Power: {Settings.MaxFieldPower}%");

                        LoadSettings();
                        Settings.FieldPower = Math.Min(Settings.FieldPower, Settings.MaxFieldPower);
                        SaveSettings();

                        MyLog.Default.WriteLineAndConsole($"Final initialization - Field Power: {Settings.FieldPower}%, Max Power: {Settings.MaxFieldPower}%");
                        MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleResistence);
                    });
                }

                // Set up power system after settings are loaded
                if (Sink != null) {
                    float initialPowerDraw = CalculatePowerDraw();
                    var powerReq = new MyResourceSinkInfo() {
                        ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                        MaxRequiredInput = Config.MaxPowerDraw,
                        RequiredInputFunc = CalculatePowerDraw
                    };
                    Sink.AddType(ref powerReq);
                    Sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, CalculatePowerDraw);
                    Sink.Update();  // Ensure power requirements are updated
                }

                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME; 
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"FieldGenerator.UpdateOnceBeforeFrame: {e}");
            }
        }

        public override void UpdateAfterSimulation() {
            try {
                if (!IsServer) return;

                if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0) {
                    if (Block.IsWorking && Block.IsFunctional) {
                        float requiredPower = CalculatePowerDraw();
                        Sink?.Update();
                        bool hasPower = Sink != null && Sink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, requiredPower);
                        float availablePower = Sink?.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) ?? 0f;

                        //MyLog.Default.WriteLineAndConsole($"Power Status - Required: {requiredPower:F2} MW, Available: {availablePower:F2} MW, Current Field: {Settings.FieldPower:F1}%");

                        if (hasPower) {
                            UpdateSiegeState();

                            if (!Config.SimplifiedMode) {
                                if (_damageEventCounter > Config.DamageEventThreshold) {
                                    Settings.Stability -= (1.6666666666667f * CalculateSizeModifier()) * (Settings.FieldPower / 50);
                                }
                                else {
                                    Settings.Stability = MathHelper.Clamp(Settings.Stability + 3, 0, 100);
                                }

                                if (_resetCounter < Config.ResetInterval) {
                                    _resetCounter++;
                                }
                                else {
                                    _resetCounter = 0;
                                    _damageEventCounter = 0;
                                }
                            }
                        }
                        // Server only disables the block if there's no power, never changes the field power value
                        else if (!Block.IsWorking) {
                            if (Settings.SiegeMode)
                                CancelSiegeMode();
                        }
                    }
                }
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"FieldGenerator.UpdateAfterSimulation: {e}");
            }
        }

        public override void UpdateAfterSimulation10() {
            try {
                if (IsClientInShip() || IsClientNearShip()) {
                    if (Settings.SiegeMode) {
                        SetSiegeNotification($"<S.I> Siege Mode Active | {Settings.SiegeElapsedTime} / {Config.MaxSiegeTime}", 600);
                    }
                    else if (!Settings.SiegeMode && Settings.SiegeCooldownActive) {
                        SetSiegeNotification($"<S.I> Siege Mode On Cooldown | {Settings.SiegeCooldownTime}", 600, "Red");
                    }

                    if (!Block.IsWorking) {
                        string reason = Block.IsFunctional ?
                            (Sink != null && !Sink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, CalculatePowerDraw()) ?
                                "Insufficient Power" : "Power System Error") :
                            "Block Damaged!";
                        SetPowerNotification($"<S.I> Generator Core is Offline! | {reason}", 600, "Red");
                    }
                }
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"FieldGenerator.UpdateAfterSimulation10: {e}");
            }
        }

        public override void UpdateAfterSimulation100() {
            try {
                if (Block?.IsWorking != true || Sink == null)
                    return;

                // this is shit but the power calc just slips through on a repaste without it so whatever
                float currentDraw = CalculatePowerDraw();
                Sink.Update();

                MyLog.Default.WriteLineAndConsole($"Power Update - Field Power: {Settings.FieldPower}%, Draw: {currentDraw:F2} MW");
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"FieldGenerator.UpdateAfterSimulation100: {e}");
            }
        }

        public override void Close() {
            try {
                if (IsServer) {
                    if (Block?.CubeGrid != null) {
                        Block.CubeGrid.OnBlockAdded -= OnBlockAdded;
                        Block.CubeGrid.OnBlockRemoved -= OnBlockRemoved;
                    }
                }

                Sink = null;
                Block = null;
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"FieldGenerator.Close: {e}");
            }
            base.Close();
        }
        #endregion

        #region Block Management
        private void OnBlockAdded(IMySlimBlock block) {
            if (block == null) return;

            _gridBlockCount++;
            if (!_gridBlocks.Contains(block))
                _gridBlocks.Add(block);

            if (block.FatBlock != null && block.FatBlock.BlockDefinition.SubtypeId == "FieldGen_Capacity_Upgrade") {
                if (IsNeighbour(block)) {
                    long entityId = block.FatBlock.EntityId;
                    if (!_attachedModuleIds.Contains(entityId) && _moduleCount < Config.MaxModuleCount) {
                        _attachedModuleIds.Add(entityId);
                        _moduleCount++;
                        CalculateUpgradeAmounts();
                    }
                }
            }
        }

        private void OnBlockRemoved(IMySlimBlock block) {
            if (block == null) return;

            _gridBlockCount--;
            if (_gridBlocks.Contains(block))
                _gridBlocks.Remove(block);

            if (block.FatBlock != null && block.FatBlock.BlockDefinition.SubtypeId == "FieldGen_Capacity_Upgrade") {
                long entityId = block.FatBlock.EntityId;
                if (_attachedModuleIds.Contains(entityId)) {
                    _attachedModuleIds.Remove(entityId);
                    _moduleCount--;
                    CalculateUpgradeAmounts();
                }
            }
        }

        private void HandleResistence(object target, ref MyDamageInformation info) {
            if (Block == null || !Block.IsWorking) return;

            IMySlimBlock targetBlock = target as IMySlimBlock;
            if (targetBlock?.CubeGrid != null) {
                if (targetBlock.CubeGrid.EntityId != Block.CubeGrid.EntityId) return;

                if (Settings.SiegeMode) {
                    info.Amount *= 0.1f;
                    return;
                }

                if (!Config.SimplifiedMode) {
                    _damageEventCounter++;
                }

                float roundedModifier = (float)Math.Round(1 - ((double)Settings.FieldPower / 100), 3);
                info.Amount *= roundedModifier;
            }
        }
        #endregion

        #region Siege Mode
        private void UpdateSiegeState() {
            if (Settings.SiegeMode && !Settings.SiegeCooldownActive) {
                if (Settings.SiegeElapsedTime + 1 <= Config.MaxSiegeTime) {
                    Settings.SiegeElapsedTime++;
                    SiegeBlockEnabler(_gridBlocks, false);
                }
                else {
                    EndSiegeMode();
                    return;
                }
            }

            if (!Settings.SiegeMode && !Settings.SiegeCooldownActive && Settings.SiegeElapsedTime > 0) {
                EndSiegeMode();
                return;
            }

            if (Settings.SiegeCooldownActive) {
                if (Settings.SiegeCooldownTime > 0) {
                    Settings.SiegeCooldownTime--;
                }
                else {
                    Settings.SiegeCooldownActive = false;
                }
            }
        }

        private void SiegeBlockEnabler(List<IMySlimBlock> allTerminalBlocks, bool enabled) {
            foreach (var block in allTerminalBlocks) {
                if (block.FatBlock != null && block.FatBlock.BlockDefinition.SubtypeId != "FieldGen_Core") {
                    var entBlock = block as MyEntity;
                    if (entBlock != null && FieldGeneratorSession.CoreSysAPI.HasCoreWeapon(entBlock)) {
                        FieldGeneratorSession.CoreSysAPI.SetFiringAllowed(entBlock, enabled);
                        var functionalBlock = block.FatBlock as IMyFunctionalBlock;
                        if (functionalBlock != null) {
                            functionalBlock.Enabled = enabled;
                        }
                    }
                }
            }
        }

        private void EndSiegeMode() {
            SiegeBlockEnabler(_gridBlocks, true);
            Settings.SiegeCooldownTime = (Settings.SiegeElapsedTime > 5) ? (Settings.SiegeElapsedTime * 2) : 5;
            Settings.SiegeElapsedTime = 0;
            Settings.SiegeCooldownActive = true;
        }

        private void CancelSiegeMode() {
            SiegeBlockEnabler(_gridBlocks, true);
            Settings.SiegeCooldownTime = 0;
            Settings.SiegeElapsedTime = 0;
        }
        #endregion

        #region Module Management
        private void InitExistingUpgrades() {
            if (!IsServer) return;

            MyLog.Default.WriteLineAndConsole("Starting upgrade initialization...");

            _attachedModuleIds.Clear();
            _moduleCount = 0;

            var neighbours = new List<IMySlimBlock>();
            Block.SlimBlock.GetNeighbours(neighbours);

            foreach (var n in neighbours) {
                if (n?.FatBlock == null || n.FatBlock.BlockDefinition.SubtypeId != "FieldGen_Capacity_Upgrade")
                    continue;

                if (IsModuleValid(n)) {
                    long entityId = n.FatBlock.EntityId;
                    if (!_attachedModuleIds.Contains(entityId)) {
                        _attachedModuleIds.Add(entityId);
                        _moduleCount++;
                        MyLog.Default.WriteLineAndConsole($"Found valid upgrade module. Total count: {_moduleCount}");
                    }
                }
            }

            CalculateUpgradeAmounts();
            MyLog.Default.WriteLineAndConsole($"Upgrade initialization complete. Module count: {_moduleCount}, Max Power: {Settings.MaxFieldPower}%");
        }

        private void CalculateUpgradeAmounts() {
            // Calculate new max power based on module count
            float newMaxPower = Settings.MinFieldPower + (_moduleCount * Config.PerModuleAmount);

            MyLog.Default.WriteLineAndConsole($"Calculating upgrades - Modules: {_moduleCount}, New Max Power: {newMaxPower}%");

            if (Math.Abs(Settings.MaxFieldPower - newMaxPower) > 0.001f) {
                Settings.MaxFieldPower = newMaxPower;
                // Ensure current field power doesn't exceed new maximum
                if (Settings.FieldPower > Settings.MaxFieldPower) {
                    Settings.FieldPower = Settings.MaxFieldPower;
                }

                MyLog.Default.WriteLineAndConsole($"Updated power settings - Max: {Settings.MaxFieldPower}%, Current: {Settings.FieldPower}%");
            }
        }

        private bool IsNeighbour(IMySlimBlock block) {
            List<IMySlimBlock> neighbours = new List<IMySlimBlock>();
            Block.SlimBlock.GetNeighbours(neighbours);
            return neighbours.Contains(block);
        }

        private bool IsModuleValid(IMySlimBlock neighbor) {
            var neighborDummies = new Dictionary<string, IMyModelDummy>();
            neighbor.FatBlock.Model.GetDummies(neighborDummies);

            foreach (var CoreDummy in _coreDummies) {
                Vector3D coreDummyPos = Vector3D.Transform(CoreDummy.Value.Matrix.Translation, Block.WorldMatrix);
                foreach (var neighborDummy in neighborDummies) {
                    Vector3D neighborDummyPos = Vector3D.Transform(neighborDummy.Value.Matrix.Translation, neighbor.FatBlock.WorldMatrix);
                    if (Vector3D.Distance(coreDummyPos, neighborDummyPos) < 0.5)
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region Utility Methods
        private float CalculateSizeModifier() {
            int clampedBlockCount = MathHelper.Clamp(_gridBlockCount, Config.MinBlockCount, Config.MaxBlockCount);
            float t = (float)(clampedBlockCount - Config.MinBlockCount) / (Config.MaxBlockCount - Config.MinBlockCount);
            return Config.SizeModifierMin + t * (Config.SizeModifierMax - Config.SizeModifierMin);
        }

        private float CalculatePowerDraw() {
            try {
                if (!Block.IsWorking || !Block.IsFunctional)
                    return 0f;

                if (Settings.SiegeMode)
                    return Config.SiegePowerDraw;

                // Calculate power draw based on field power percentage
                float fieldPowerPercentage = Settings.FieldPower / 100f;
                float powerRange = Config.MaxPowerDraw - Config.MinPowerDraw;
                float powerDraw = Config.MinPowerDraw + (fieldPowerPercentage * powerRange);

                return powerDraw;
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"FieldGenerator.CalculatePowerDraw: {e}");
                return Config.MinPowerDraw;
            }
        }

        private bool IsClientInShip() {
            if (Block != null) {
                foreach (var cockpit in Block.CubeGrid.GetFatBlocks<IMyCockpit>()) {
                    if (cockpit.Pilot != null && cockpit.Pilot.EntityId == MyAPIGateway.Session?.Player?.Character?.EntityId) {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsClientNearShip() {
            if (Block != null) {
                var bound = new BoundingSphereD(Block.CubeGrid.GetPosition(), 65);
                List<IMyEntity> nearEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref bound);
                foreach (var entity in nearEntities) {
                    if (entity != null && entity?.EntityId == MyAPIGateway.Session?.Player?.Character?.EntityId) {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region Settings Management
        internal bool LoadSettings() {
            string rawData;
            if (Block.Storage == null || !Block.Storage.TryGetValue(SettingsGuid, out rawData)) {
                MyLog.Default.WriteLineAndConsole("No settings found to load.");
                return false;
            }

            try {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<FieldGeneratorSettings>(Convert.FromBase64String(rawData));
                if (loadedSettings == null) {
                    MyLog.Default.WriteLineAndConsole("Failed to deserialize settings.");
                    return false;
                }

                Settings.CopyFrom(loadedSettings);

                // Update power requirements after loading settings
                if (Sink != null) {
                    float powerDraw = CalculatePowerDraw();
                    Sink.Update();
                }

                MyLog.Default.WriteLineAndConsole($"Settings loaded - Field Power: {Settings.FieldPower}%, Max: {Settings.MaxFieldPower}%");
                return true;
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"Failed to load field generator settings: {e}");
            }
            return false;
        }

        internal void SaveSettings() {
            if (Block == null || Settings == null)
                return;

            if (Block.Storage == null)
                Block.Storage = new MyModStorageComponent();

            // Save the current power draw state along with other settings
            string rawData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings));

            if (Block.Storage.ContainsKey(SettingsGuid))
                Block.Storage[SettingsGuid] = rawData;
            else
                Block.Storage.Add(SettingsGuid, rawData);

            // Update the power sink with current settings
            if (Sink != null) {
                float powerDraw = CalculatePowerDraw();
                Sink.Update();
            }
        }

        public override bool IsSerialized() {
            try {
                if (Block == null || Settings == null) {
                    Log.Error("Error in IsSerialized: block or Settings is null.");
                    return false;
                }
                SaveSettings();
            }
            catch (Exception e) {
                Log.Error($"Exception in IsSerialized: {e}");
            }
            return base.IsSerialized();
        }
        #endregion

        #region Notifications
        public void SetSiegeNotification(string text, int aliveTime = 300, string font = MyFontEnum.Green) {
            if (notifSiege == null)
                notifSiege = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);
            notifSiege.Hide();
            notifSiege.Font = font;
            notifSiege.Text = text;
            notifSiege.AliveTime = aliveTime;
            notifSiege.Show();
        }

        public void SetPowerNotification(string text, int aliveTime = 300, string font = MyFontEnum.Green) {
            if (notifPower == null)
                notifPower = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);
            notifPower.Hide();
            notifPower.Font = font;
            notifPower.Text = text;
            notifPower.AliveTime = aliveTime;
            notifPower.Show();
        }
        #endregion
    }
}
