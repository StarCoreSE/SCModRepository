﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using ProtoBuf;
using VRage.Utils;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Sync;
using VRage.Network;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender.Models;

using Starcore.FieldGenerator.Networking.Custom;

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
    public class FieldGenerator : MyGameLogicComponent, IMyEventProxy
    {
        private IMyCubeBlock Block;
        private readonly bool IsServer = MyAPIGateway.Session.IsServer;

        private int _damageEventCounter = 0;
        private float _stabilityChange = 0;
        private int _resetCounter = 0;

        #region Sync Properties
        public bool SiegeMode
        {
            get { return _siegeMode; }
            set
            {
                if (_siegeMode != value)
                {
                    _siegeMode = value;
                    OnBoolPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<bool>(nameof(SiegeMode), _siegeMode));
                }
            }
        }
        public bool _siegeMode;

        public bool SiegeCooldownActive
        {
            get { return _siegeCooldownActive; }
            set
            {
                if (_siegeCooldownActive != value)
                {
                    _siegeCooldownActive = value;
                    OnBoolPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<bool>(nameof(SiegeCooldownActive), _siegeCooldownActive));
                }
            }
        }
        public bool _siegeCooldownActive;
        public event PropertyChangedEventHandler<bool> OnBoolPropertyChanged;

        public int SiegeElapsedTime
        {
            get { return _siegeElapsedTime; }
            set
            {
                if (_siegeElapsedTime != value)
                {
                    _siegeElapsedTime = value;
                    OnIntPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<int>(nameof(SiegeElapsedTime), _siegeElapsedTime));
                }
            }
        }
        public int _siegeElapsedTime;

        public int SiegeCooldownTime
        {
            get { return _siegeCooldownTime; }
            set
            {
                if (_siegeCooldownTime != value)
                {
                    _siegeCooldownTime = value;
                    OnIntPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<int>(nameof(SiegeCooldownTime), _siegeCooldownTime));
                }
            }
        }
        public int _siegeCooldownTime;
        public event PropertyChangedEventHandler<int> OnIntPropertyChanged;

        public float FieldPower
        {
            get { return _fieldPower; }
            set
            {
                if (_fieldPower != value)
                {               
                    _fieldPower = MathHelper.Clamp(value, MinFieldPower, MaxFieldPower);
                    OnFloatPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<float>(nameof(FieldPower), _fieldPower));
                }
            }
        }
        public float _fieldPower;

        public float MaxFieldPower
        {
            get { return _maxFieldPower; }
            set
            {
                if (_maxFieldPower != value)
                {
                    _maxFieldPower = value;
                    OnFloatPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<float>(nameof(MaxFieldPower), _maxFieldPower));
                }
            }
        }
        public float _maxFieldPower;

        public float MinFieldPower
        {
            get { return _minFieldPower; }
            set
            {
                if (_minFieldPower != value)
                {
                    _minFieldPower = value;
                    OnFloatPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<float>(nameof(MinFieldPower), _minFieldPower));
                }
            }
        }
        public float _minFieldPower;

        public float SizeModifier
        {
            get { return _sizeModifier; }
            set
            {
                if (_sizeModifier != value)
                {
                    _sizeModifier = value;
                    OnFloatPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<float>(nameof(SizeModifier), _sizeModifier));
                }
            }
        }
        public float _sizeModifier;

        public float Stability
        {
            get { return _stability; }
            set
            {
                if (_stability != value)
                {
                    _stability = value;
                    OnFloatPropertyChanged?.Invoke(this, new PropertyChangedEventArgs<float>(nameof(Stability), _stability));
                }
            }
        }
        public float _stability;
        public event PropertyChangedEventHandler<float> OnFloatPropertyChanged;
        #endregion

        private Dictionary<string, IMyModelDummy> _coreDummies = new Dictionary<string, IMyModelDummy>();
        private HashSet<long> _attachedModuleIds = new HashSet<long>();
        private int _moduleCount = 0;

        private List<IMySlimBlock> _gridBlocks = new List<IMySlimBlock>();
        private int _gridBlockCount;             

        public MySync<bool, SyncDirection.FromServer> GridStopped = null;

        private MyResourceSinkComponent Sink = null;

        private IMyHudNotification notifSiege = null;

        #region Overrides
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Block = (IMyCubeBlock)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null)
                return;

            FieldGeneratorControls.DoOnce(ModContext);

            Sink = Block.Components.Get<MyResourceSinkComponent>();
            Sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, CalculatePowerDraw);

            OnBoolPropertyChanged += HandleBoolPacket;
            OnIntPropertyChanged += HandleIntPacket;
            OnFloatPropertyChanged += HandleFloatPacket;

            if (IsServer)
            {
                Block.Model.GetDummies(_coreDummies);
                InitExistingUpgrades();

                Stability = 100;
                MinFieldPower = 0;
                FieldPower = MinFieldPower;

                Block.CubeGrid.GetBlocks(_gridBlocks);
                _gridBlockCount = _gridBlocks.Count;

                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleResistence);
                Block.CubeGrid.OnBlockAdded += OnBlockAdded;
                Block.CubeGrid.OnBlockRemoved += OnBlockRemoved;
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

            if (!IsServer)
                return;      

            if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0)
            {
                if (Block.IsWorking)
                {
                    Sink.Update();

                    UpdateSiegeState();             

                    if (!Config.SimplifiedMode)
                    {
                        SizeModifier = CalculateSizeModifier();

                        if (_damageEventCounter > Config.DamageEventThreshold)
                        {
                            _stabilityChange = -((1.6666666666667f * SizeModifier) * (FieldPower / 50));
                        }
                        else
                        {
                            _stabilityChange = 3;
                        }

                        _stability = MathHelper.Clamp(_stability + _stabilityChange, 0, 100);

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
                else if (!Block.IsWorking)
                {
                    if (FieldPower > 0)
                        FieldPower = 0;
                    
                    if (SiegeMode)
                        SiegeMode = false;                    
                }
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (!IsClientInShip())
                return;

            if (SiegeMode)
            {
                SetSiegeNotification($"Siege Active | {SiegeElapsedTime} / {Config.MaxSiegeTime}", 3000);
            }
            else if (!SiegeMode && SiegeCooldownActive)
            {
                SetSiegeNotification($"Siege On Cooldown | {SiegeCooldownTime}", 3000, "Red");
            }
        }

        public override void Close()
        {
            base.Close();

            OnBoolPropertyChanged -= HandleBoolPacket;
            OnIntPropertyChanged -= HandleIntPacket;
            OnFloatPropertyChanged -= HandleFloatPacket;

            if (IsServer)
            {
                Block.CubeGrid.OnBlockAdded -= OnBlockAdded;
                Block.CubeGrid.OnBlockRemoved -= OnBlockRemoved;
            }

            if (!IsServer)
            {
                GridStopped.ValueChanged -= OnGridStopValueChange;
            }              

            Block = null;
        }
        #endregion

        #region Subscription Event Handlers
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

        private void HandleResistence(object target, ref MyDamageInformation info)
        {
            if (Block == null || !Block.IsWorking)
                return;

            IMySlimBlock targetBlock = target as IMySlimBlock;

            if (targetBlock.CubeGrid != null && targetBlock != null)
            {
                IMyCubeGrid targetGrid = targetBlock.CubeGrid;

                if (Block != null && targetGrid.EntityId == Block.CubeGrid.EntityId)
                {
                    if (SiegeMode)
                    {
                        info.Amount *= 0.1f;
                        return;
                    }
                    else
                    {
                        if (!Config.SimplifiedMode)
                        {
                            _damageEventCounter++;
                        }                   

                        float roundedModifier = (float)Math.Round(1 - ((double)FieldPower / 100), 3);
                        info.Amount *= roundedModifier;
                        return;
                    }
                }                          
            }
        }

        private void OnGridStopValueChange(MySync<bool, SyncDirection.FromServer> obj)
        {
            if (obj?.Value ?? false)
                Block.CubeGrid.Physics.LinearVelocity = Vector3.Zero;
        }
        #endregion

        #region Terminal Control Packet Handlers
        private void HandleBoolPacket(object sender, PropertyChangedEventArgs<bool> e)
        {
            BoolSyncPacket.SyncBoolProperty(Block.EntityId, e.PropertyName, e.NewValue);
        }

        private void HandleIntPacket(object sender, PropertyChangedEventArgs<int> e)
        {
            IntSyncPacket.SyncIntProperty(Block.EntityId, e.PropertyName, e.NewValue);
        }

        private void HandleFloatPacket(object sender, PropertyChangedEventArgs<float> e)
        {
            FloatSyncPacket.SyncFloatProperty(Block.EntityId, e.PropertyName, e.NewValue);
        }
        #endregion

        #region Siege Mode
        private void UpdateSiegeState()
        {
            if (SiegeMode && !SiegeCooldownActive)
            {
                if (SiegeElapsedTime + 1 <= Config.MaxSiegeTime)
                {
                    SiegeElapsedTime++;
                    SiegeBlockEnabler(_gridBlocks, false);

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
                    SiegeMode = false;
                    return;
                }
            }

            if (!SiegeMode && !SiegeCooldownActive && SiegeElapsedTime > 0)
            {
                EndSiegeMode();
                return;
            }

            if (SiegeCooldownActive)
            {
                if (SiegeCooldownTime > 0)
                {
                    SiegeCooldownTime--;
                }
                else
                {
                    SiegeCooldownActive = false;
                }
            }
        }

        private void EndSiegeMode()
        {
            if (IsServer && GridStopped.Value)
                GridStopped.Value = false;

            SiegeBlockEnabler(_gridBlocks, true);

            SiegeCooldownTime = (SiegeElapsedTime > 5) ? (SiegeElapsedTime * 2) : 5;
            SiegeElapsedTime = 0;
            SiegeCooldownActive = true;
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
            List<IMySlimBlock> neighbours = new List<IMySlimBlock>();
            Block.SlimBlock.GetNeighbours(neighbours);

            foreach (var n in neighbours)
            {
                OnBlockAdded(n);
            }
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
            MaxFieldPower = MinFieldPower + (_moduleCount * Config.PerModuleAmount);

            if (FieldPower > MaxFieldPower)
            {
                FieldPower = MaxFieldPower;
            }
        }

        private float CalculateSizeModifier()
        {
            int clampedBlockCount = MathHelper.Clamp(_gridBlockCount, Config.MinBlockCount, Config.MaxBlockCount);
            float t = (float)(clampedBlockCount - Config.MinBlockCount) / (Config.MaxBlockCount - Config.MinBlockCount);

            return Config.SizeModifierMin + t * (Config.SizeModifierMax - Config.SizeModifierMin);
        }

        private float CalculatePowerDraw()
        {
            if (SiegeMode)
            {
                return Config.SiegePowerDraw;
            }

            float maxPossibleFieldPower = Config.PerModuleAmount * Config.MaxModuleCount;
            float clampedFieldPower = MathHelper.Clamp(FieldPower, 0, maxPossibleFieldPower);
            float t = clampedFieldPower / maxPossibleFieldPower;

            return Config.MinPowerDraw + t * (Config.MaxPowerDraw - Config.MinPowerDraw);
        }

        private bool IsClientInShip()
        {
            if (Block != null)
            {
                var cockpits = Block.CubeGrid.GetFatBlocks<IMyCockpit>();

                foreach (var cockpit in cockpits)
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

        private void SiegeBlockEnabler(List<IMySlimBlock> allTerminalBlocks, bool enabled)
        {
            foreach (var block in allTerminalBlocks)
            {
                if (block.FatBlock != null && block.FatBlock.BlockDefinition.SubtypeId != "FieldGen_Core")
                {
                    var entBlock = block as MyEntity;
                    if (entBlock != null && FieldGeneratorSession.CoreSysAPI.HasCoreWeapon(entBlock))
                    {
                        FieldGeneratorSession.CoreSysAPI.SetFiringAllowed(entBlock, enabled);

                        var functionalBlock = block.FatBlock as IMyFunctionalBlock;
                        if (functionalBlock != null)
                        {
                            functionalBlock.Enabled = enabled;

                        }
                    }
                }
                else
                    continue;             
            }
        }
        #endregion

        #region Notifs
        public void SetSiegeNotification(string text, int aliveTime = 300, string font = MyFontEnum.Green)
        {
            if (notifSiege == null)
                notifSiege = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);

            notifSiege.Hide();
            notifSiege.Font = font;
            notifSiege.Text = text;
            notifSiege.AliveTime = aliveTime;
            notifSiege.Show();
        }
        #endregion
    }

    public delegate void PropertyChangedEventHandler<T>(object sender, PropertyChangedEventArgs<T> e);

    public class PropertyChangedEventArgs<T> : EventArgs
    {
        public string PropertyName { get; }
        public T NewValue { get; }

        public PropertyChangedEventArgs(string propertyName, T newValue)
        {
            PropertyName = propertyName;
            NewValue = newValue;
        }
    }
}
