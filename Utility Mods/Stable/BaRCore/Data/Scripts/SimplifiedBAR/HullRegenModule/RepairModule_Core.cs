using System;
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
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.Sync;
using VRage.Network;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using StarCore.RepairModule.Networking.Custom;

namespace StarCore.RepairModule
{
    public enum RepairPriority
    {
        Offense,
        Power,
        Thrust,
        Steering,
        Utility,
        None
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "SELtdLargeNanobotBuildAndRepairSystem", "SELtdSmallNanobotBuildAndRepairSystem")]
    public class RepairModule : MyGameLogicComponent, IMyEventProxy
    {
        public IMyCollector Block;
        private bool IsServer = MyAPIGateway.Session.IsServer;
        private bool IsDedicated = MyAPIGateway.Utilities.IsDedicated;
        private bool ClientSettingsLoaded = false;

        // Block Settings
        public bool IgnoreArmor
        {
            get { return ignoreArmor;  }
            set
            {
                if (ignoreArmor != value)
                {
                    ignoreArmor = value;

                    if (IsServer)
                    {
                        Log.Info("Processing Repair Targets on Event Trigger: IgnoreArmor");
                        ProcessRepairTargets(Block.CubeGrid, false);
                    }

                    OnIgnoreArmorChanged?.Invoke(ignoreArmor);
                }
            }
        }
        public bool ignoreArmor;
        private event Action<bool> OnIgnoreArmorChanged;

        public bool PriorityOnly
        {
            get { return priorityOnly; }
            set
            {
                if (priorityOnly != value)
                {
                    priorityOnly = value;
                       
                    if (IsServer)
                    {
                        Log.Info("Processing Repair Targets on Event Trigger: PriorityOnly");
                        ProcessRepairTargets(Block.CubeGrid, false);
                    }

                    OnPriorityOnlyChanged?.Invoke(priorityOnly);
                }
            }
        }
        public bool priorityOnly;
        private event Action<bool> OnPriorityOnlyChanged;

        public long SubsystemPriority
        {
            get { return GetLongFromPriority(subsystemPriority); }
            set
            {
                var newPriority = GetPriorityFromLong(value);
                if (subsystemPriority != newPriority)
                {
                    subsystemPriority = newPriority;

                    if (IsServer)
                    {
                        Log.Info("Processing Repair Targets on Event Trigger: SubsystemPriority");
                        ProcessRepairTargets(Block.CubeGrid, false);
                    }

                    OnSubsystemPriorityChanged?.Invoke(value);
                }
            }
        }
        public RepairPriority subsystemPriority = RepairPriority.None;
        private event Action<long> OnSubsystemPriorityChanged;

        // General Settings     
        float RepairAmount = 4f;
        public readonly Guid SettingsID = new Guid("09E18094-46AE-4F55-8215-A407B49F9CAA");

        // Timed Sort
        private int UpdateCounter = 0;
        private const int UpdateInterval = 100;
        private int SortCounter = 0;
        private const int SortInterval = 48;
        private bool NeedsSorting = false;

        // Target Lists
        private List<IMyCubeGrid> AssociatedGrids = new List<IMyCubeGrid>();
        private List<IMySlimBlock> RepairTargets = new List<IMySlimBlock>();
        private List<IMySlimBlock> PriorityRepairTargets = new List<IMySlimBlock>();

        // Client-Side Particle Effects
        public MySync<Vector3D, SyncDirection.FromServer> TargetPosition = null;
        public MySync<long, SyncDirection.FromServer> TargetBlock = null;
        public MySync<bool, SyncDirection.FromServer> ShowWeldEffects = null;

        private const string WeldParticle = MyParticleEffectsNameEnum.WelderContactPoint;
        private MyParticleEffect WeldParticleEmitter;      
        private const string WeldSound = "ToolLrgWeldMetal";
        private MyEntity3DSoundEmitter WeldSoundEmitter;

        public MySoundPair WeldSoundPair => new MySoundPair(WeldSound);

        #region Update Methods
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            
            Block = (IMyCollector)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            RepairModuleControls.DoOnce(ModContext);

            if (Block?.CubeGrid?.Physics == null)
                return;

            MyParticlesManager.TryCreateParticleEffect(WeldParticle, ref MatrixD.Identity, ref Vector3D.Zero, uint.MaxValue, out WeldParticleEmitter);
            WeldSoundEmitter = new MyEntity3DSoundEmitter(null);

            OnIgnoreArmorChanged += IgnoreArmor_Update;
            OnPriorityOnlyChanged += PriorityOnly_Update;
            OnSubsystemPriorityChanged += SubsystemPriority_Update;          

            Block.AppendingCustomInfo += AppendCustomInfo;

            if (IsServer)
            {
                ProcessRepairTargets(Block.CubeGrid, true);

                Block.CubeGrid.OnBlockIntegrityChanged += HandleBlocks;
                Block.CubeGrid.OnBlockAdded += HandleBlocks;
                Block.CubeGrid.OnBlockRemoved += HandleRemovedBlocks;                                        

                if (AssociatedGrids.Any())
                {
                    foreach (IMyCubeGrid grid in AssociatedGrids)
                    {
                        grid.OnBlockIntegrityChanged += HandleBlocks;
                        grid.OnBlockAdded += HandleBlocks;
                        grid.OnBlockRemoved += HandleRemovedBlocks;                  
                    }
                }
            }

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Repair Function
            if (IsServer)
            {
                if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0 && Block.IsWorking)
                {
                    Vector3D targetBlockPosition = Vector3D.Zero;
                    IMySlimBlock targetBlock = PriorityRepairTargets.FirstOrDefault() ?? (!PriorityOnly ? RepairTargets.FirstOrDefault() : null);

                    if (targetBlock != null)
                    {
                        if (targetBlock.FatBlock != null)
                        {
                            TargetBlock.Value = targetBlock.FatBlock.EntityId;
                        }
                        else
                        {
                            targetBlock.ComputeWorldCenter(out targetBlockPosition);
                            TargetPosition.Value = targetBlockPosition;
                        }

                        ShowWeldEffects.Value = true;
                        RepairTarget(targetBlock);
                    }
                    else
                    {
                        ShowWeldEffects.Value = false;
                    }
                }
            }

            // Entity ID based Update Spreading for Reacquisition
            if (IsServer)
            {
                UpdateCounter++;

                if (Block.CubeGrid != null)
                {
                    int updateCount = (int)(Block.CubeGrid.EntityId % UpdateInterval);

                    if (UpdateCounter % UpdateInterval == updateCount)
                    {
                        ProcessRepairTargets(Block.CubeGrid, false);
                    }
                }

                if (UpdateCounter >= int.MaxValue - UpdateInterval)
                {
                    UpdateCounter = 0;
                }
            }

            if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0 && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                Block.RefreshCustomInfo();
                Block.SetDetailedInfoDirty();
            }

            if (ShowWeldEffects.Value && (TargetPosition != null || TargetBlock != null))
            {
                SpawnWeldEffects(TargetPosition.Value);
            }
            else
            {
                ResetWeldEffects();
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (IsServer)
            {
                if (SortCounter > 0 && !NeedsSorting)
                {
                    SortCounter--;
                    return;
                }

                if (SortCounter == 0 || NeedsSorting)
                {
                    RepairTargets = RepairTargets.OrderBy(block => block.Integrity).ToList();
                    PriorityRepairTargets = PriorityRepairTargets.OrderBy(block => block.Integrity).ToList();

                    SortCounter = SortInterval;
                    NeedsSorting = false;
                }
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (IsDedicated)
            {
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_100TH_FRAME;
                return;
            }            

            if (!LoadSettings())
            {               
                IgnoreArmor = true;
                PriorityOnly = false;
                SubsystemPriority = 0;
            }

            ClientSettingsLoaded = true;
            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_100TH_FRAME;
            return;
        }

        public override void Close()
        {
            base.Close();
            if (Block == null)
                return;

            if (IsServer)
            {
                Block.CubeGrid.OnBlockIntegrityChanged -= HandleBlocks;
                Block.CubeGrid.OnBlockAdded -= HandleBlocks;
                Block.CubeGrid.OnBlockRemoved -= HandleRemovedBlocks;

                if (AssociatedGrids.Any())
                {
                    foreach (IMyCubeGrid grid in AssociatedGrids)
                    {
                        grid.OnBlockIntegrityChanged -= HandleBlocks;
                        grid.OnBlockAdded -= HandleBlocks;
                        grid.OnBlockRemoved -= HandleRemovedBlocks;                     
                    }
                }
            }

            AssociatedGrids.Clear();
            RepairTargets.Clear();
            PriorityRepairTargets.Clear();

            if (WeldParticleEmitter != null)
            {
                WeldParticleEmitter.Close();
                WeldParticleEmitter = null;
            }          

            if (WeldSoundEmitter != null)
            {
                WeldSoundEmitter?.Cleanup();
                WeldSoundEmitter = null;
            }        

            Block = null;
        }

        public override bool IsSerialized()
        {
            Log.Info($"IsSerialized called for {Block.EntityId}");
            try
            {
                //SaveSettings(); NO NO NO DON'T CALL IT HERE IT SYNCS ELSEWHERE THIS KILLS EVERYTHING FOR SOME REASON
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return base.IsSerialized();
        }
        #endregion

        #region Event Handlers
        public void HandleBlocks(IMySlimBlock block)
        {
            if (IgnoreArmor && (block.FatBlock == null || block.ToString().Contains("MyCubeBlock") || block.FatBlock.BlockDefinition.SubtypeId.Contains("AQD_LA") || block.FatBlock.BlockDefinition.SubtypeId.Contains("AQD_HA")))
            {
                HandleRemovedBlocks(block);
                return;
            }

            List<IMySlimBlock> newTargetList = IsPriority(block) ? PriorityRepairTargets : RepairTargets;
            List<IMySlimBlock> oldTargetList = newTargetList == PriorityRepairTargets ? RepairTargets : PriorityRepairTargets;

            if (oldTargetList.Contains(block))
            {
                oldTargetList.Remove(block);
            }

            if (block.Integrity != block.MaxIntegrity)
            {
                if (!newTargetList.Contains(block))
                {
                    newTargetList.Add(block);
                }
            }
        }

        public void HandleRemovedBlocks(IMySlimBlock block)
        {
            if (RepairTargets.Contains(block))
            {
                RepairTargets.Remove(block);
            }

            if (PriorityRepairTargets.Contains(block))
            {
                PriorityRepairTargets.Remove(block);
            }
        }

        private void IgnoreArmor_Update(bool _bool)
        {
            SaveSettings();

            IgnoreArmorPacket.UpdateIgnoreArmor(Block.EntityId);                
        }

        private void PriorityOnly_Update(bool _bool)
        {
            SaveSettings();

            PriorityOnlyPacket.UpdatePriorityOnly(Block.EntityId);
        }

        private void SubsystemPriority_Update(long _long)
        {
            SaveSettings();

            SubsystemPriorityPacket.UpdateSubsystemPriority(Block.EntityId);
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
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<RepairSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Log.Info($"LoadSettings: Successfully loaded settings for {Block.EntityId}");
                    Log.Info($"Loaded values: IgnoreArmor={loadedSettings.Stored_IgnoreArmor}, PriorityOnly={loadedSettings.Stored_PriorityOnly}, SubsystemPriority={loadedSettings.Stored_SubsystemPriority}");

                    IgnoreArmor = loadedSettings.Stored_IgnoreArmor;
                    PriorityOnly = loadedSettings.Stored_PriorityOnly;
                    SubsystemPriority = loadedSettings.Stored_SubsystemPriority;

                    Log.Info($"After assignment: IgnoreArmor={IgnoreArmor}, PriorityOnly={PriorityOnly}, SubsystemPriority={SubsystemPriority}");
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

                var settings = new RepairSettings
                {
                    Stored_IgnoreArmor = IgnoreArmor,
                    Stored_PriorityOnly = PriorityOnly,
                    Stored_SubsystemPriority = SubsystemPriority
                };

                string serializedData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(settings));
                Block.Storage.SetValue(SettingsID, serializedData);
                Log.Info($"SaveSettings: Successfully saved settings for {Block.EntityId}");
                Log.Info($"Saved values: IgnoreArmor={IgnoreArmor}, PriorityOnly={PriorityOnly}, SubsystemPriority={SubsystemPriority}");
            }
            catch (Exception e)
            {
                Log.Error($"Error saving settings for {Block.EntityId}!\n{e}");
            }
        }
        #endregion

        #region Utility Methods
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

        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            try
            {
                // NOTE: don't Clear() the StringBuilder, it's the same instance given to all mods.
                string priorityListAsString = ProcessTargetsToString(PriorityRepairTargets);
                string listAsString = ProcessTargetsToString(RepairTargets);

                sb.Append("Priority Targets: ").Append("\n").Append(priorityListAsString).Append("\n\n")
                  .Append("Regular Targets: ").Append("\n").Append(listAsString);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e);
            }
        }

        private string ProcessTargetsToString(List<IMySlimBlock> list)
        {
            return string.Join(Environment.NewLine, list.Select(listItem =>
            {
                string listItemString = listItem.ToString();
                int lastBraceIndex = listItemString.LastIndexOf("}");
                int firstBraceIndex = listItemString.IndexOf("{");

                if (lastBraceIndex != -1)
                {
                    string afterLastBrace = listItemString.Substring(lastBraceIndex + 1);
                    if (!string.IsNullOrWhiteSpace(afterLastBrace))
                    {
                        return afterLastBrace;
                    }
                }

                if (firstBraceIndex != -1)
                {
                    string beforeFirstBrace = listItemString.Substring(0, firstBraceIndex).Trim();

                    if (beforeFirstBrace.Contains("MyCubeBlock") && listItem.FatBlock != null)
                    {
                        string modelName = GetModelNameFromFatBlock(listItem.FatBlock);
                        return modelName;
                    }

                    return beforeFirstBrace;
                }

                return " " + listItemString; // Default return
            }));
        }

        private string GetModelNameFromFatBlock(IMyEntity fatBlock)
        {
            string itemModelName = fatBlock.Model.AssetName.ToString();
            int lastBackslashIndex = itemModelName.LastIndexOf("\\");
            if (lastBackslashIndex != -1)
            {
                string afterLastBackslash = itemModelName.Substring(lastBackslashIndex + 1);
                int firstPeriodIndex = afterLastBackslash.IndexOf("_");
                if (firstPeriodIndex != -1)
                {
                    return " " + afterLastBackslash.Substring(0, firstPeriodIndex).Trim();
                }
                return " " + afterLastBackslash;

            }
            return " " + itemModelName; // Default if no "_" found
        }
        
        public RepairPriority GetPriorityFromLong(long priority)
        {
            switch (priority)
            {
                case 0:
                    return RepairPriority.None;
                case 1:
                    return RepairPriority.Offense;
                case 2:
                    return RepairPriority.Power;
                case 3:
                    return RepairPriority.Thrust;
                case 4:
                    return RepairPriority.Steering;
                case 5:
                    return RepairPriority.Utility;
                default:
                    return RepairPriority.None;
            }
        }

        public long GetLongFromPriority(RepairPriority priority)
        {
            switch (priority)
            {
                case RepairPriority.None:
                    return 0;
                case RepairPriority.Offense:
                    return 1;
                case RepairPriority.Power:
                    return 2;
                case RepairPriority.Thrust:
                    return 3;
                case RepairPriority.Steering:
                    return 4;
                case RepairPriority.Utility:
                    return 5;
                default:
                    return 0;
            }
        }

        private bool IsPriority(IMySlimBlock block)
        {
            if (block.FatBlock == null)
            {
                return false;
            }

            switch (subsystemPriority)
            {
                case RepairPriority.Offense:
                    return block.FatBlock is IMyConveyorSorter ||
                           block.FatBlock is IMyLargeTurretBase ||
                           block.FatBlock is IMySmallMissileLauncher ||
                           block.FatBlock is IMySmallMissileLauncherReload ||
                           block.FatBlock is IMySmallGatlingGun;

                case RepairPriority.Power:
                    return block.FatBlock is IMyPowerProducer;

                case RepairPriority.Thrust:
                    return block.FatBlock is IMyThrust;

                case RepairPriority.Steering:
                    return block.FatBlock is IMyGyro || block.FatBlock is IMyCockpit;

                case RepairPriority.Utility:
                    return block.FatBlock is IMyGasTank ||
                           block.FatBlock is IMyConveyor ||
                           block.FatBlock is IMyConveyorTube;

                default:
                    return false;
            }
        }

        private void SpawnWeldEffects(Vector3D position)
        {
            if (WeldParticleEmitter == null)
                return;
        
            if (TargetBlock.Value != 0)
            {
                IMyEntity entity;
                if (MyAPIGateway.Entities.TryGetEntityById(TargetBlock.Value, out entity))
                {
                    IMyCubeBlock targetBlock = entity as IMyCubeBlock;

                    if (targetBlock != null)
                    {
                        WeldParticleEmitter.WorldMatrix = targetBlock.WorldMatrix;
                        return;
                    }
                    else
                        return;
                }
            }

            WeldParticleEmitter.WorldMatrix = MatrixD.Identity;
            WeldParticleEmitter?.SetTranslation(ref position);

            WeldSoundEmitter?.SetPosition(position);
            WeldSoundEmitter?.PlaySingleSound(WeldSoundPair, true);
        }

        private void ResetWeldEffects()
        {
            if (WeldParticleEmitter == null)
                return;
        
            WeldParticleEmitter.WorldMatrix = MatrixD.Identity;
            WeldParticleEmitter?.SetTranslation(ref Vector3D.Zero);

            WeldSoundEmitter?.SetPosition(Vector3D.Zero);
            WeldSoundEmitter?.StopSound(true);
        }
        #endregion

        #region Main
        public void ProcessRepairTargets(IMyCubeGrid grid, bool init)
        {
            var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Mechanical);
            List<IMyCubeGrid> gridsList = new List<IMyCubeGrid>();

            if (gridGroup != null)
            {
                gridGroup.GetGrids(gridsList);
                if (init)
                {
                    AssociatedGrids = gridsList;
                }               

                foreach (IMyCubeGrid groupGrid in gridsList)
                {
                    var tempBlockList = new List<IMySlimBlock>();
                    groupGrid.GetBlocks(tempBlockList);

                    foreach (var block in tempBlockList)
                    {
                        HandleBlocks(block);
                    }

                    tempBlockList.Clear();
                }
            }
            else
            {
                var tempBlockList = new List<IMySlimBlock>();
                grid.GetBlocks(tempBlockList);

                foreach (var block in tempBlockList)
                {
                    HandleBlocks(block);
                }

                tempBlockList.Clear();
            }
        }

        private void RepairTarget(IMySlimBlock block)
        {
            if (block == null || block.CubeGrid.Physics == null)
                return;

            if(!block.IsFullIntegrity || block.HasDeformation)
            {             
                block.IncreaseMountLevel(RepairAmount * MyAPIGateway.Session.WelderSpeedMultiplier, block.OwnerId, null, 1);
            }
            else
            {
                RepairTargets.Remove(block);
                PriorityRepairTargets.Remove(block);

                TargetPosition.Value = Vector3D.Zero;
                ShowWeldEffects.Value = false;

                NeedsSorting = true;
            }

            // Double Check Existence
            if (block.CubeGrid.GetCubeBlock(block.Position) == null)
            {
                RepairTargets.Remove(block);
                PriorityRepairTargets.Remove(block);

                NeedsSorting = true;
            }
        }      
        #endregion
    }

    [ProtoContract]
    public class RepairSettings
    {
        [ProtoMember(41)]
        public bool Stored_IgnoreArmor { get; set; }

        [ProtoMember(42)]
        public bool Stored_PriorityOnly { get; set; }

        [ProtoMember(43)]
        public long Stored_SubsystemPriority { get; set; }
    }
}
