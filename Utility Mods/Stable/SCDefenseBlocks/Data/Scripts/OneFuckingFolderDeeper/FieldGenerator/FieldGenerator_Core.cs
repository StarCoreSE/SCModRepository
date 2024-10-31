using System;
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
    public class Config
    {
        public const bool SimplifiedMode = true;

        public const float PerModuleAmount = 12.5f;
        public const int MaxModuleCount = 4;

        public const int MaxSiegeTime = 150;
        public const int SiegePowerDraw = 900;

        public const int DamageEventThreshold = 6;
        public const int ResetInterval = 3;

        public const float SizeModifierMax = 0.8f;
        public const int MaxBlockCount = 35000;

        public const float SizeModifierMin = 1.2f;
        public const int MinBlockCount = 2500;

        public const float MaxPowerDraw = 500.00f;
        public const float MinPowerDraw = 50.00f;
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

                // Initialize power system first - do this for both client and server
                Sink = Block.Components.Get<MyResourceSinkComponent>();
                if (Sink != null) {
                    var powerReq = new MyResourceSinkInfo() {
                        ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                        MaxRequiredInput = Config.MaxPowerDraw,
                        RequiredInputFunc = CalculatePowerDraw
                    };
                    Sink.AddType(ref powerReq);
                    Sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, CalculatePowerDraw);
                    Sink.Update();
                    MyLog.Default.WriteLineAndConsole($"Power system initialized. Max Draw: {Config.MaxPowerDraw} MW");
                }
                else {
                    MyLog.Default.WriteLineAndConsole("Failed to get ResourceSinkComponent!");
                }

                if (IsServer) {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() => {
                        Block.Model.GetDummies(_coreDummies);
                        Block.CubeGrid.OnBlockAdded += OnBlockAdded;
                        Block.CubeGrid.OnBlockRemoved += OnBlockRemoved;

                        InitExistingUpgrades();
                        LoadSettings();
                        Settings.FieldPower = Math.Min(Settings.FieldPower, Settings.MaxFieldPower);
                        SaveSettings();

                        // Register damage handler
                        MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleResistence);
                    });
                }

                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
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

                        // Update sink and check power before changing any settings
                        Sink?.Update();
                        bool hasPower = Sink != null && Sink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, requiredPower);
                        float availablePower = Sink?.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) ?? 0f;

                        MyLog.Default.WriteLineAndConsole($"Power Status - Required: {requiredPower:F2} MW, Available: {availablePower:F2} MW, Current Field: {Settings.FieldPower:F1}%");

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
                        else {
                            // Calculate sustainable power level
                            float sustainablePowerPercentage = (availablePower - Config.MinPowerDraw) / (Config.MaxPowerDraw - Config.MinPowerDraw) * 100f;
                            float targetPower = Math.Max(0, sustainablePowerPercentage - 5f); // Add a small buffer

                            if (Settings.FieldPower > targetPower) {
                                MyLog.Default.WriteLineAndConsole($"Reducing power to sustainable level: {targetPower:F1}%");
                                Settings.FieldPower = targetPower;
                                SaveSettings(); // Ensure the new value is saved
                            }

                            if (Settings.SiegeMode) {
                                CancelSiegeMode();
                            }
                        }
                    }
                    else {
                        // Block not working
                        if (Settings.FieldPower > 0)
                            Settings.FieldPower = 0;
                        if (Settings.SiegeMode)
                            CancelSiegeMode();
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
                    }
                }
            }

            CalculateUpgradeAmounts();
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

        private void CalculateUpgradeAmounts() {
            float newMaxPower = Settings.MinFieldPower + (_moduleCount * Config.PerModuleAmount);
            if (Math.Abs(Settings.MaxFieldPower - newMaxPower) > 0.001f) {
                Settings.MaxFieldPower = newMaxPower;
                if (Settings.FieldPower > Settings.MaxFieldPower) {
                    Settings.FieldPower = Settings.MaxFieldPower;
                }
            }
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
                float powerDraw = Config.MinPowerDraw + (fieldPowerPercentage * (Config.MaxPowerDraw - Config.MinPowerDraw));

                MyLog.Default.WriteLineAndConsole($"FieldGenerator Power Draw: {powerDraw:F2} MW (Field Power: {Settings.FieldPower:F1}%, Min: {Config.MinPowerDraw}, Max: {Config.MaxPowerDraw})");
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
                return false;
            }

            try {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<FieldGeneratorSettings>(Convert.FromBase64String(rawData));
                if (loadedSettings == null)
                    return false;

                Settings.CopyFrom(loadedSettings);
                return true;
            }
            catch (Exception e) {
                MyAPIGateway.Utilities.ShowNotification("Failed to load field generator settings! Check the logs for more info.");
                MyLog.Default.WriteLineAndConsole("Failed to load field generator settings! Exception: " + e);
            }

            return false;
        }

        internal void SaveSettings() {
            if (Block == null || Settings == null)
                return;

            if (Block.Storage == null)
                Block.Storage = new MyModStorageComponent();

            string rawData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings));

            // Use TryAdd instead of Add to prevent potential exceptions
            if (Block.Storage.ContainsKey(SettingsGuid))
                Block.Storage[SettingsGuid] = rawData;
            else
                Block.Storage.Add(SettingsGuid, rawData);

            // Optional verification
            var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<FieldGeneratorSettings>(Convert.FromBase64String(rawData));
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
