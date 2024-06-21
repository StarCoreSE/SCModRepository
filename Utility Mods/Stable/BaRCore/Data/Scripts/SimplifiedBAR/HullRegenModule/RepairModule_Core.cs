using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
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
        private IMyCollector Block;
        private bool IsServer = MyAPIGateway.Session.IsServer;      

        // Block Settings
        public bool IgnoreArmor
        {
            get { return ignoreArmor;  }
            set
            {
                if (ignoreArmor != value)
                {
                    ignoreArmor = value;
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
                    OnSubsystemPriorityChanged?.Invoke(value);
                }
            }
        }
        public RepairPriority subsystemPriority = RepairPriority.None;
        private event Action<long> OnSubsystemPriorityChanged;

        // General Settings     
        float RepairAmount = 2f;
        bool defaultsSet = false;

        // Timed Sort
        private int SortTimer = 0;
        private const int SortInterval = 48;
        private bool NeedsSorting = false;

        // Target Lists
        private List<IMyCubeGrid> AssociatedGrids = new List<IMyCubeGrid>();
        private List<IMySlimBlock> RepairTargets = new List<IMySlimBlock>();
        private List<IMySlimBlock> PriorityRepairTargets = new List<IMySlimBlock>();

        // Client-Side Particle Effects
        public MySync<Vector3D, SyncDirection.FromServer> TargetPosition = null;
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

            IgnoreArmor = true;
            PriorityOnly = false;
            SubsystemPriority = 0;

            Block.AppendingCustomInfo += AppendCustomInfo;

            if (IsServer)
            {
                InitRepairTargets(Block.CubeGrid);

                Block.CubeGrid.OnBlockIntegrityChanged += HandleDamagedBlocks;
                Block.CubeGrid.OnBlockRemoved += HandleRemovedBlocks;              
                Block.CubeGrid.OnBlockAdded += HandleAddedBlocks;              

                if (AssociatedGrids.Any())
                {
                    foreach (IMyCubeGrid grid in AssociatedGrids)
                    {
                        grid.OnBlockIntegrityChanged += HandleDamagedBlocks;
                        grid.OnBlockRemoved += HandleRemovedBlocks;
                        grid.OnBlockAdded += HandleAddedBlocks;
                    }
                }
            }         

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (Block != null && !defaultsSet)
            {
                IgnoreArmor = true;
                PriorityOnly = false;
                SubsystemPriority = 0;

                defaultsSet = true;
            }

            if (IsServer)
            {
                if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0 && Block.IsWorking)
                {                                        
                    Vector3D targetBlockPosition = Vector3D.Zero;
                    if (PriorityRepairTargets.Any())
                    {
                        PriorityRepairTargets[0].ComputeWorldCenter(out targetBlockPosition);
                        TargetPosition.Value = targetBlockPosition;
                        ShowWeldEffects.Value = true;

                        RepairTarget(PriorityRepairTargets[0]);
                    }
                    else if (RepairTargets.Any() && !PriorityOnly)
                    {
                        RepairTargets[0].ComputeWorldCenter(out targetBlockPosition);
                        TargetPosition.Value = targetBlockPosition;
                        ShowWeldEffects.Value = true;

                        RepairTarget(RepairTargets[0]);
                    }
                    else
                    {
                        ShowWeldEffects.Value = false;
                    }
                }                            
            }

            if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0 && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                Block.RefreshCustomInfo();
                Block.SetDetailedInfoDirty();
            }

            if (ShowWeldEffects.Value && TargetPosition != null)
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
                if (SortTimer > 0 && !NeedsSorting)
                {
                    SortTimer--;
                    return;
                }

                if (SortTimer == 0 || NeedsSorting)
                {
                    RepairTargets = RepairTargets.OrderBy(block => block.Integrity).ToList();
                    PriorityRepairTargets = PriorityRepairTargets.OrderBy(block => block.Integrity).ToList();

                    SortTimer = SortInterval;
                    NeedsSorting = false;
                }                        
            }
        }

        public override void Close()
        {
            base.Close();
            if (Block == null)
                return;

            if (IsServer)
            {
                Block.CubeGrid.OnBlockIntegrityChanged -= HandleDamagedBlocks;
                Block.CubeGrid.OnBlockRemoved -= HandleRemovedBlocks;
                Block.CubeGrid.OnBlockAdded -= HandleAddedBlocks;

                if (AssociatedGrids.Any())
                {
                    foreach (IMyCubeGrid grid in AssociatedGrids)
                    {
                        grid.OnBlockIntegrityChanged -= HandleDamagedBlocks;
                        grid.OnBlockRemoved -= HandleRemovedBlocks;
                        grid.OnBlockAdded -= HandleAddedBlocks;
                    }
                }
            }

            AssociatedGrids.Clear();
            RepairTargets.Clear();
            PriorityRepairTargets.Clear();

            if (WeldParticleEmitter == null)
                return;

            WeldParticleEmitter.SetTranslation(ref Vector3D.Zero);

            if (WeldSoundEmitter == null)
                return;

            WeldSoundEmitter.SetPosition(Vector3D.Zero);
            WeldSoundEmitter.StopSound(true);

            Block = null;
        }
        #endregion

        #region Event Handlers
        public void HandleDamagedBlocks(IMySlimBlock block)
        {
            if (IgnoreArmor && (block.FatBlock == null || block.ToString().Contains("MyCubeBlock")))
            {
                HandleRemovedBlocks(block);
                return;
            }
                

            List<IMySlimBlock> targetList = IsPriority(block) ? PriorityRepairTargets : RepairTargets;

            if (block.Integrity != block.MaxIntegrity)
            {
                if (!targetList.Contains(block))
                {
                    targetList.Add(block);
                }
            }
            else
            {
                if (targetList.Contains(block))
                {
                    targetList.Remove(block);
                }
            }
        }

        public void HandleAddedBlocks(IMySlimBlock block)
        {
            if (IgnoreArmor && (block.FatBlock == null || block.ToString().Contains("MyCubeBlock")))
                return;

            List<IMySlimBlock> targetList = IsPriority(block) ? PriorityRepairTargets : RepairTargets;

            if (block.Integrity != block.MaxIntegrity)
            {
                if (!targetList.Contains(block))
                {
                    targetList.Add(block);
                }
            }
            else
            {
                if (targetList.Contains(block))
                {
                    targetList.Remove(block);
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
            IgnoreArmorPacket.UpdateIgnoreArmor(Block.EntityId);
            ScanRepairTargets(Block.CubeGrid);
        }

        private void PriorityOnly_Update(bool _bool)
        {
            PriorityOnlyPacket.UpdatePriorityOnly(Block.EntityId);
        }

        private void SubsystemPriority_Update(long _long)
        {
            SubsystemPriorityPacket.UpdateSubsystemPriority(Block.EntityId);
            ScanRepairTargets(Block.CubeGrid);
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
            try // only for non-critical code
            {
                // NOTE: don't Clear() the StringBuilder, it's the same instance given to all mods.

                // Process both priority and regular lists
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
                           block.FatBlock is IMyConveyorSorter ||
                           block.FatBlock is IMyConveyorTube;

                default:
                    return false;
            }
        }

        private void SpawnWeldEffects(Vector3D position)
        {
            WeldParticleEmitter?.SetTranslation(ref position);

            WeldSoundEmitter?.SetPosition(position);
            WeldSoundEmitter?.PlaySingleSound(WeldSoundPair, true);
        }

        private void ResetWeldEffects()
        {
            WeldParticleEmitter?.SetTranslation(ref Vector3D.Zero);

            WeldSoundEmitter?.SetPosition(Vector3D.Zero);
            WeldSoundEmitter?.StopSound(true);
        }
        #endregion

        #region Main
        private void InitRepairTargets(IMyCubeGrid grid)
        {
            var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Mechanical);
            List<IMyCubeGrid> gridsList = new List<IMyCubeGrid>();

            if (gridGroup != null)
            {
                gridGroup.GetGrids(gridsList);
                AssociatedGrids = gridsList;

                foreach (IMyCubeGrid groupGrid in gridsList)
                {
                    var tempBlockList = new List<IMySlimBlock>();

                    groupGrid.GetBlocks(tempBlockList);

                    foreach (var block in tempBlockList)
                    {
                        HandleDamagedBlocks(block);
                    }

                    tempBlockList.Clear();
                }

            }
            else if (gridGroup == null)
            {
                var tempBlockList = new List<IMySlimBlock>();

                grid.GetBlocks(tempBlockList);

                foreach (var block in tempBlockList)
                {
                    HandleDamagedBlocks(block);
                }

                tempBlockList.Clear();
            }
        }

        private void ScanRepairTargets(IMyCubeGrid grid)
        {
            var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Mechanical);

            if (AssociatedGrids.Any())
            {
                foreach (IMyCubeGrid groupGrid in AssociatedGrids)
                {
                    var tempBlockList = new List<IMySlimBlock>();

                    groupGrid.GetBlocks(tempBlockList);

                    foreach (var block in tempBlockList)
                    {
                        HandleDamagedBlocks(block);
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
                    HandleDamagedBlocks(block);
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
}