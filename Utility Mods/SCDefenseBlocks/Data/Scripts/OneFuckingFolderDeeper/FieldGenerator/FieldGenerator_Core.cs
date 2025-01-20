using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRageMath;
using static Draygo.API.HudAPIv2;
using static VRageRender.MyBillboard;
using Sandbox.Game.Gui;
using Draygo.API;

namespace Starcore.FieldGenerator
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "FieldGen_Core")]
    public class FieldGenerator : MyGameLogicComponent, IMyEventProxy
    {
        private IMyCubeBlock Block;
        private readonly bool IsServer = MyAPIGateway.Session.IsServer;
        public readonly Guid SettingsID = new Guid("7A7AC398-FAE3-44E5-ABD5-8AE49434DDF6");

        private Generator_Settings Config = FieldGenerator_Config.Config;

        private int _damageEventCounter = 0;
        private float _stabilityChange = 0;
        private int _resetCounter = 0;
        private bool _lowStability = false;

        private int initValueDelayTicks = 60; // 1 second delay (60 ticks)
        private bool valuesInitialized = false;

        #region Sync Properties
        public MySync<bool, SyncDirection.BothWays> SiegeMode;
        public MySync<bool, SyncDirection.BothWays> SiegeCooldownActive;
        public MySync<bool, SyncDirection.FromServer> GridStopped = null;

        public MySync<int, SyncDirection.BothWays> SiegeElapsedTime;
        public MySync<int, SyncDirection.BothWays> SiegeCooldownTime;

        public MySync<float, SyncDirection.BothWays> FieldPower; // add on value change hook for Serverside Resistence
        public MySync<float, SyncDirection.BothWays> MaxFieldPower;
        public MySync<float, SyncDirection.BothWays> MinFieldPower;
        public MySync<float, SyncDirection.BothWays> SizeModifier;
        public MySync<float, SyncDirection.BothWays> Stability; // add on value change for handling zero stability if (IsServer && _stability == 0) HandleZeroStability();
        #endregion

        private Dictionary<string, IMyModelDummy> _coreDummies = new Dictionary<string, IMyModelDummy>();
        private HashSet<long> _attachedModuleIds = new HashSet<long>();
        private int _moduleCount = 0;

        private List<IMySlimBlock> _gridBlocks = new List<IMySlimBlock>();
        private int _gridBlockCount;             

        private MyResourceSinkComponent Sink = null;

        HUDMessage GeneratorHUD;
        StringBuilder GeneratorHUDContent;

        #region Overrides
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Block = (IMyFunctionalBlock)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null)
                return;

            FieldGeneratorControls.DoOnce(ModContext);        

            Sink = Block.Components.Get<MyResourceSinkComponent>();
            if (Sink != null)
            {
                Sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, CalculatePowerDraw);
                Sink.Update();
            }        

            if (IsServer)
            {
                Block.Model.GetDummies(_coreDummies);                         
                Block.CubeGrid.GetBlocks(_gridBlocks);
                _gridBlockCount = _gridBlocks.Count;

                LoadSettings();
                SaveSettings();

                if (!Config.SimplifiedMode)
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleDamageEvents);
                }          

                Block.CubeGrid.OnBlockAdded += OnBlockAdded;
                Block.CubeGrid.OnBlockRemoved += OnBlockRemoved;
                
                SiegeCooldownActive.ValueChanged += (obj) => SaveSettings();
                SiegeElapsedTime.ValueChanged += (obj) => SaveSettings();
                SiegeCooldownTime.ValueChanged += (obj) => SaveSettings();
                MaxFieldPower.ValueChanged += (obj) => SaveSettings();
                MinFieldPower.ValueChanged += (obj) => SaveSettings();
                SizeModifier.ValueChanged += (obj) => SaveSettings();

                SiegeMode.ValueChanged += SiegeMode_ValueChanged;
                FieldPower.ValueChanged += FieldPower_ValueChanged;
                Stability.ValueChanged += Stability_ValueChanged;
            }

            if (!IsServer)
            {
                GridStopped.ValueChanged += OnGridStopValueChange;
            }

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }  

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (IsServer)
            {
                if (!valuesInitialized)
                {
                    if (initValueDelayTicks > 0)
                    {
                        initValueDelayTicks--;
                    }
                    else
                    {
                        Stability.Value = 100;
                        InitExistingUpgrades();
                        valuesInitialized = true;
                    }
                }
            }

            if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0)
            {
                if (Block.IsWorking)
                {
                    if (IsServer)
                    {
                        UpdateSiegeState();

                        if (!Config.SimplifiedMode)
                        {
                            if (_lowStability && Stability.Value < 100)
                            {
                                Stability.Value = MathHelper.Clamp(Stability + 3, 0, 100);
                                HandleZeroStability();
                                if (Stability.Value == 100)
                                {
                                    _lowStability = false;
                                }
                                return;
                            }

                            SizeModifier.Value = CalculateSizeModifier();

                            if (_damageEventCounter > Config.DamageEventThreshold)
                            {
                                _stabilityChange = -((1.6666666666667f * SizeModifier.Value) * (FieldPower.Value / 50));
                            }
                            else
                            {
                                _stabilityChange = 3;
                            }

                            Stability.Value = MathHelper.Clamp(Stability + _stabilityChange, 0, 100);

                            if (_resetCounter < Config.ResetInterval)
                            {
                                _resetCounter++;
                                return;
                            }
                            else if (_resetCounter >= Config.ResetInterval)
                            {
                                _resetCounter = 0;
                                _damageEventCounter = 0;
                                return;
                            }
                        }
                    }
                    else
                        Sink.Update();
                }
                else if (!Block.IsWorking)
                {
                    if (IsServer)
                    {
                        if (FieldPower.Value > 0)
                            FieldPower.Value = 0;

                        if (SiegeMode.Value)
                        {
                            CancelSiegeMode();
                            SiegeMode.Value = false;
                        }

                       HandleResistence();
                    }
                    else
                        Sink.Update();                   
                }
            }  
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (IsClientInShip() || IsClientNearShip())
            {
                UpdateHUD();
            }
            else
                PurgeHUDMessage();    
        }

        public override void Close()
        {
            base.Close();

            PurgeHUDMessage();
           
            if (IsServer)
            {
                Block.CubeGrid.OnBlockAdded -= OnBlockAdded;
                Block.CubeGrid.OnBlockRemoved -= OnBlockRemoved;

                SiegeCooldownActive.ValueChanged -= (obj) => SaveSettings();
                SiegeElapsedTime.ValueChanged -= (obj) => SaveSettings();
                SiegeCooldownTime.ValueChanged -= (obj) => SaveSettings();
                MaxFieldPower.ValueChanged -= (obj) => SaveSettings();
                MinFieldPower.ValueChanged -= (obj) => SaveSettings();
                SizeModifier.ValueChanged -= (obj) => SaveSettings();

                SiegeMode.ValueChanged -= SiegeMode_ValueChanged;
                FieldPower.ValueChanged -= FieldPower_ValueChanged;
                Stability.ValueChanged -= Stability_ValueChanged;
            }

            if (!IsServer)
            {
                GridStopped.ValueChanged -= OnGridStopValueChange;
            }              

            Block = null;
        }
        #endregion

        #region Event Handlers
        private void OnBlockAdded(IMySlimBlock block)
        {
            if (block == null)
                return;

            _gridBlockCount++;

            if (!_gridBlocks.Contains(block))
                _gridBlocks.Add(block);

            if (block.FatBlock != null && block.FatBlock.BlockDefinition.SubtypeId == "FieldGen_Capacity_Upgrade")
            {
                if (IsNeighbour(block) && IsModuleValid(block))
                {
                    long entityId = block.FatBlock.EntityId;

                    if (!_attachedModuleIds.Contains(entityId) && _moduleCount < Config.MaxModuleCount)
                    {
                        _attachedModuleIds.Add(entityId);
                        _moduleCount++;

                        CalculateUpgradeAmounts();
                    }
                }
            }
        }

        private void OnBlockRemoved(IMySlimBlock block)
        {
            if (block == null)
                return;

            _gridBlockCount--;

            if (_gridBlocks.Contains(block))
                _gridBlocks.Remove(block);

            if (block.FatBlock != null && block.FatBlock.BlockDefinition.SubtypeId == "FieldGen_Capacity_Upgrade")
            {
                long entityId = block.FatBlock.EntityId;

                if (_attachedModuleIds.Contains(entityId))
                {
                    _attachedModuleIds.Remove(entityId);
                    _moduleCount--;

                    CalculateUpgradeAmounts();
                }
            }
        }

        private void HandleDamageEvents(object target, ref MyDamageInformation info)
        {
            if (Block == null || !Block.IsWorking)
                return;

            IMySlimBlock targetBlock = target as IMySlimBlock;

            if (targetBlock.CubeGrid != null && targetBlock != null)
            {
                IMyCubeGrid targetGrid = targetBlock.CubeGrid;

                if (targetGrid.EntityId != Block.CubeGrid.EntityId)
                    return;

                _damageEventCounter++;
                return;
            }
        }

        private void HandleResistence()
        {
            if (Block == null)
                return;

            if (!Block.IsWorking)
            {
                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(Block.CubeGrid.Name, 1);
                return;
            }

            if (SiegeMode.Value)
            {
                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(Block.CubeGrid.Name, (1 - Config.SiegeModeResistence));
                return;
            }
            else
                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(Block.CubeGrid.Name, (float)Math.Round(1 - ((double)FieldPower.Value / 100), 3));
        }

        private void HandleZeroStability()
        {
            if (Block == null || !Block.IsWorking || Stability.Value != 0)
                return;

            FieldPower.Value = 10;
            _lowStability = true;
        }

        private void SiegeMode_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            SaveSettings();
            Sink.Update();

            if (IsServer)
                HandleResistence();
        }

        private void FieldPower_ValueChanged(MySync<float, SyncDirection.BothWays> obj)
        {
            FieldPower.Value = MathHelper.Clamp(obj.Value, MinFieldPower.Value, MaxFieldPower.Value);
            SaveSettings();
            Sink.Update();

            if (IsServer)   
                HandleResistence();
        }

        private void Stability_ValueChanged(MySync<float, SyncDirection.BothWays> obj)
        {
            SaveSettings();

            if (IsServer)
                HandleZeroStability();
        }

        private void OnGridStopValueChange(MySync<bool, SyncDirection.FromServer> obj)
        {
            if (obj?.Value ?? false)
                Block.CubeGrid.Physics.LinearVelocity = Vector3.Zero;
        }
        #endregion

        #region Siege Mode
        private void UpdateSiegeState()
        {
            if (SiegeMode.Value && !SiegeCooldownActive.Value)
            {
                if (SiegeElapsedTime.Value + 1 <= Config.MaxSiegeTime)
                {                
                    SiegeElapsedTime.Value++;                   ;
                    SiegeBlockEnabler(Block.CubeGrid.GetFatBlocks<IMyFunctionalBlock>(), false);                   

                    if (Block.CubeGrid.Physics.LinearVelocity != Vector3D.Zero)
                    {
                        Block.CubeGrid.Physics.LinearVelocity = Vector3.Zero;
                        if (IsServer && !GridStopped.Value)
                            GridStopped.Value = true;
                    }
                }
                else
                {
                    EndSiegeMode();
                    SiegeMode.Value = false;
                    return;
                }
            }

            if (!SiegeMode.Value && !SiegeCooldownActive.Value && SiegeElapsedTime.Value > 0)
            {
                EndSiegeMode();
                return;
            }

            if (SiegeCooldownActive.Value)
            {
                if (SiegeCooldownTime.Value > 0)
                {
                    SiegeCooldownTime.Value--;
                }
                else
                {
                    SiegeCooldownActive.Value = false;
                }
            }
        }

        private void SiegeBlockEnabler(IEnumerable<IMyFunctionalBlock> allFunctionalBlocks, bool enabled)
        {
            foreach (var block in allFunctionalBlocks)
            {
                if (block != null && block.BlockDefinition.SubtypeId != "FieldGen_Core")
                {
                    var entBlock = block as MyEntity;
                    if (entBlock != null && FieldGeneratorSession.CoreSysAPI.HasCoreWeapon(entBlock))
                    {
                        FieldGeneratorSession.CoreSysAPI.SetFiringAllowed(entBlock, enabled);
                        block.Enabled = enabled;
                    }
                }
                else
                    continue;
            }
        }

        private void EndSiegeMode()
        {
            if (IsServer && GridStopped.Value)
                GridStopped.Value = false;

            SiegeBlockEnabler(Block.CubeGrid.GetFatBlocks<IMyFunctionalBlock>(), true);

            SiegeCooldownTime.Value = (SiegeElapsedTime.Value > 5) ? (SiegeElapsedTime.Value * 2) : 5;
            SiegeElapsedTime.Value = 0;
            SiegeCooldownActive.Value = true;
        }

        private void CancelSiegeMode()
        {
            if (IsServer && GridStopped.Value)
                GridStopped.Value = false;

            SiegeBlockEnabler(Block.CubeGrid.GetFatBlocks<IMyFunctionalBlock>(), true);

            SiegeCooldownTime.Value = 0;
            SiegeElapsedTime.Value = 0;
        }
        #endregion

        #region Utility
        public static T GetLogic<T>(long entityId) where T : MyGameLogicComponent
        {
            IMyEntity targetEntity = MyAPIGateway.Entities.GetEntityById(entityId);
            if (targetEntity == null)
            {
                Log.Info("GetLogic failed: Entity not found. Entity ID: " + entityId);
                return null;
            }

            IMyTerminalBlock targetBlock = targetEntity as IMyTerminalBlock;
            if (targetBlock == null)
            {
                Log.Info("GetLogic failed: Target entity is not a terminal block. Entity ID: " + entityId);
                return null;
            }

            var logic = targetBlock.GameLogic?.GetAs<T>();
            if (logic == null)
            {
                Log.Info("GetLogic failed: Logic component not found. Entity ID: " + entityId);
            }

            return logic;
        }

        private void InitExistingUpgrades()
        {
            List<long> validUpgradeModules = new List<long>();
            List<IMySlimBlock> neighbours = new List<IMySlimBlock>();
            Block.SlimBlock.GetNeighbours(neighbours);

            foreach (var n in neighbours)
            {
                if (n?.FatBlock == null)
                {
                    continue;
                }
                else if (n.FatBlock.BlockDefinition.SubtypeId == "FieldGen_Capacity_Upgrade" && IsModuleValid(n))
                {
                    validUpgradeModules.Add(n.FatBlock.EntityId);
                }
            }

            foreach (var entityId in validUpgradeModules)
            {
                if (!_attachedModuleIds.Contains(entityId) && _moduleCount < Config.MaxModuleCount)
                {
                    _attachedModuleIds.Add(entityId);
                    _moduleCount++;
                }
            }

            CalculateUpgradeAmounts();
        }

        private bool IsNeighbour(IMySlimBlock block)
        {
            List<IMySlimBlock> neighbours = new List<IMySlimBlock>();
            Block.SlimBlock.GetNeighbours(neighbours);
            return neighbours.Contains(block);
        }

        private bool IsModuleValid(IMySlimBlock neighbor)
        {
            var neighborDummies = new Dictionary<string, IMyModelDummy>();
            neighbor.FatBlock.Model.GetDummies(neighborDummies);

            foreach (var CoreDummy in _coreDummies)
            {
                Vector3D coreDummyPos = Vector3D.Transform(CoreDummy.Value.Matrix.Translation, Block.WorldMatrix);

                foreach (var neighborDummy in neighborDummies)
                {
                    Vector3D neighborDummyPos = Vector3D.Transform(neighborDummy.Value.Matrix.Translation, neighbor.FatBlock.WorldMatrix);

                    if (Vector3D.Distance(coreDummyPos, neighborDummyPos) < 0.5)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CalculateUpgradeAmounts()
        {
            MaxFieldPower.Value = MinFieldPower.Value + (_moduleCount * Config.PerModuleAmount);

            if (FieldPower.Value > MaxFieldPower.Value)
            {
                FieldPower.Value = MaxFieldPower.Value;
            }
        }

        private float CalculateSizeModifier()
        {
            int clampedBlockCount = MathHelper.Clamp(_gridBlockCount, Config.MinBlockCount, Config.MaxBlockCount);
            float t = (float)(clampedBlockCount - Config.MinBlockCount) / (Config.MaxBlockCount - Config.MinBlockCount);

            return Config.SizeModifierMin + t * (Config.SizeModifierMax - Config.SizeModifierMin);
        }

        public float CalculatePowerDraw()
        {
            if (SiegeMode.Value)
            {
                return Config.SiegePowerDraw;
            }

            float maxPossibleFieldPower = Config.PerModuleAmount * Config.MaxModuleCount;
            float clampedFieldPower = MathHelper.Clamp(FieldPower.Value, 0, maxPossibleFieldPower);
            float t = clampedFieldPower / maxPossibleFieldPower;

            return Config.MinPowerDraw + t * (Config.MaxPowerDraw - Config.MinPowerDraw);
        }

        private bool IsClientInShip()
        {
            if (Block != null)
            {
                foreach (var cockpit in Block.CubeGrid.GetFatBlocks<IMyCockpit>())
                {
                    if (cockpit.Pilot != null && cockpit.Pilot.EntityId == MyAPIGateway.Session?.Player?.Character?.EntityId)
                    {
                        return true;
                    }
                    else
                        continue;
                }
            }

            return false;
        }

        private bool IsClientNearShip()
        {
            if (Block != null)
            {
                var bound = new BoundingSphereD(Block.CubeGrid.GetPosition(), 65);
                List<IMyEntity> nearEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref bound);

                foreach (var entity in nearEntities)
                
                    if ( entity != null && entity?.EntityId == MyAPIGateway.Session?.Player?.Character?.EntityId)
                    {
                        return true;
                    }
                    else
                        continue;
            }                          

            return false;
        }
        #endregion

        #region Settings
        bool LoadSettings()
        {
            if (Block.Storage == null)
            {
                Log.Info($"LoadSettings: Block storage is null for {Block.EntityId}");
                return false;
            }

            string rawData;
            if (!Block.Storage.TryGetValue(SettingsID, out rawData))
            {
                Log.Info($"LoadSettings: No data found for {Block.EntityId}");
                return false;
            }

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<FieldGenSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Log.Info($"LoadSettings: Successfully loaded settings for {Block.EntityId}");

                    SiegeMode.Value = loadedSettings.Saved_SiegeMode;
                    SiegeCooldownActive.Value = loadedSettings.Saved_SiegeCooldownActive;
                    SiegeElapsedTime.Value = loadedSettings.Saved_SiegeElapsedTime;
                    SiegeCooldownTime.Value = loadedSettings.Saved_SiegeCooldownTime;
                    FieldPower.Value = loadedSettings.Saved_FieldPower;
                    MaxFieldPower.Value = loadedSettings.Saved_MaxFieldPower;
                    MinFieldPower.Value = loadedSettings.Saved_MinFieldPower;
                    SizeModifier.Value = loadedSettings.Saved_SizeModifier;
                    Stability.Value = loadedSettings.Saved_Stability;


                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error loading settings for {Block.EntityId}!\n{e}");
            }

            return false;
        }

        void SaveSettings()
        {
            if (Block == null)
            {
                Log.Info("SaveSettings called but Block is null.");
                return;
            }

            try
            {
                if (MyAPIGateway.Utilities == null)
                    throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId};");

                if (Block.Storage == null)
                {
                    Log.Info($"Creating new storage for {Block.EntityId}");
                    Block.Storage = new MyModStorageComponent();
                }

                var settings = new FieldGenSettings
                {
                    Saved_SiegeMode = SiegeMode.Value,
                    Saved_SiegeCooldownActive = SiegeCooldownActive.Value,
                    Saved_SiegeElapsedTime = SiegeElapsedTime.Value,
                    Saved_SiegeCooldownTime = SiegeCooldownTime.Value,
                    Saved_FieldPower = FieldPower.Value,
                    Saved_MaxFieldPower = MaxFieldPower.Value,
                    Saved_MinFieldPower = MinFieldPower.Value,
                    Saved_SizeModifier = SizeModifier.Value,
                    Saved_Stability = Stability.Value,
                };

                string serializedData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(settings));
                Block.Storage.SetValue(SettingsID, serializedData);
                Log.Info($"SaveSettings: Successfully saved settings for {Block.EntityId}");
            }
            catch (Exception e)
            {
                Log.Error($"Error saving settings for {Block.EntityId}!\n{e}");
            }
        }
        #endregion

        #region HUD
        private void UpdateHUD()
        {
            if (GeneratorHUDContent == null)
            {
                GeneratorHUDContent = new StringBuilder();
            }
            GeneratorHUDContent.Clear();

            var fieldPower = SiegeMode.Value ? 90 : FieldPower.Value;
            GeneratorHUDContent.Append(GenerateBar("Field Power:", fieldPower, MaxFieldPower.Value, false));

            if (!Config.SimplifiedMode)
            {
                GeneratorHUDContent.Append(GenerateBar("Stability:", Stability.Value, 100, true));
            }

            if (SiegeMode.Value)
            {
                GeneratorHUDContent.Append($"\nSiege Mode Active | {SiegeElapsedTime.Value} / {Config.MaxSiegeTime}");
            }
            else if (!SiegeMode.Value && SiegeCooldownActive.Value)
            {
                GeneratorHUDContent.Append($"\nSiege Mode On Cooldown | {SiegeCooldownTime.Value}");
            }

            if (!Block.IsWorking)
            {
                string reason = Block.IsFunctional ? "Insufficient Power?" : "Block Damaged!";
                GeneratorHUDContent.Append($"\nGenerator Core is Offline! | {reason}");
            }    

            if (GeneratorHUD == null && FieldGeneratorSession.HudAPI.Heartbeat)
            {
                GeneratorHUD = new HUDMessage
                (
                    Message: GeneratorHUDContent,
                    Origin: new Vector2D(-1.2, -0.525),              
                    TimeToLive: -1,
                    Scale: 0.7f,
                    HideHud: false,
                    Blend: BlendTypeEnum.PostPP,
                    Font: "monospace"
                );

                GeneratorHUD.Offset = GeneratorHUD.GetTextLength() / 2;
                GeneratorHUD.Visible = true;
            }
        }

        private string GenerateBar(string label, float value, float maxValue, bool Stability)
        {
            if (maxValue <= 0)
                maxValue = 1;

            var percentage = MathHelper.Clamp(value / maxValue, 0, 1);
            var percentageReal = Math.Max(0, (int)Math.Round(percentage * 40));

            string filledBar = new string('|', percentageReal);
            string emptyBar = new string(' ', 40 - percentageReal);

            var maxPercentage = Stability ? Math.Round(percentage * 100) : FieldPower.Value;

            return $"{label}\n[{filledBar}{emptyBar}] {maxPercentage}%\n";
        }

        private void PurgeHUDMessage()
        {
            if (GeneratorHUD != null)
            {
                GeneratorHUD.Visible = false;
                GeneratorHUD.DeleteMessage();
                GeneratorHUD = null;
            }
        }
        #endregion
    }

    [ProtoContract]
    public class FieldGenSettings
    {
        [ProtoMember(41)]
        public bool Saved_SiegeMode;

        [ProtoMember(42)]
        public bool Saved_SiegeCooldownActive;

        [ProtoMember(43)]
        public int Saved_SiegeElapsedTime;

        [ProtoMember(44)]
        public int Saved_SiegeCooldownTime;

        [ProtoMember(45)]
        public float Saved_FieldPower;
        
        [ProtoMember(46)]
        public float Saved_MaxFieldPower;
        
        [ProtoMember(47)]
        public float Saved_MinFieldPower;
        
        [ProtoMember(48)]
        public float Saved_SizeModifier;
        
        [ProtoMember(49)]
        public float Saved_Stability;
    }
}
