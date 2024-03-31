using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GUI;
using Sandbox.Game;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Sync;
using VRageMath;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.Game.Components;
using VRage.Game.Components.Interfaces;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace StarCore.AutoRepairModule
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "SELtdLargeNanobotBuildAndRepairSystem", "SELtdSmallNanobotBuildAndRepairSystem")]
    public class AutoRepairModule : MyGameLogicComponent
    {
        private IMyCollector block;

        private const string WeldingParticle = MyParticleEffectsNameEnum.WelderContactPoint;
        private MyParticleEffect ActiveWeldingParticle;

        private MyEntity3DSoundEmitter ActiveWeldSoundEmitter;
        private string WeldSoundId = "ToolLrgWeldMetal";

        public MySoundPair ActiveWeldSoundPair => new MySoundPair(WeldSoundId);

        public AutoRepairModuleMod Mod => AutoRepairModuleMod.Instance;
        public AutoRepairModuleSettings Settings = new AutoRepairModuleSettings();
        public readonly Guid Settings_GUID = new Guid("09E18094-46AE-4F55-8215-A407B49F9CAA");

        int syncCountdown;

        public const int SettingsChangedCountdown = (60 * 1) / 10;

        private bool SortedOnce = false;
        private int WeldingSortTimeout = 15;
        private int WeldNextTargetDelay = 1;

        private List<IMySlimBlock> repairList = new List<IMySlimBlock>();
        private List<IMySlimBlock> priorityRepairList = new List<IMySlimBlock>();

        private IMySlimBlock firstBlock = null;

        private int MinSubsystemPriority = 0;
        private int MaxSubsystemPriority = 5;
        private int SubsystemPriority;

        private bool ExclusiveMode;
        private bool IgnoreArmor;

        public int SettingsSubsystemPriority
        {
            get { return Settings.SubsystemPriority; }
            set
            {
                Settings.SubsystemPriority = MathHelper.Clamp((int)Math.Floor((double)value), MinSubsystemPriority, MaxSubsystemPriority);

                SettingsChanged();

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public bool SettingsExclusiveMode
        {
            get { return Settings.ExclusiveMode; }
            set
            {
                Settings.ExclusiveMode = value;

                SettingsChanged();

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public bool SettingsIgnoreArmor
        {
            get { return Settings.IgnoreArmor; }
            set
            {
                Settings.IgnoreArmor = value;

                SettingsChanged();

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public Vector3I[] SettingsRepairPositionalList
        {
            get { return Settings.RepairPositionalList; }
            set
            {
                Settings.RepairPositionalList = value;

                SettingsChanged();

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public Vector3I[] SettingsPriorityPositionalList
        {
            get { return Settings.PriorityPositionalList; }
            set
            {
                Settings.PriorityPositionalList = value;

                SettingsChanged();

                if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        #region Overrides
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;   
        }

        public override void UpdateOnceBeforeFrame()
        {
            block = (IMyCollector)Entity;

            if (block?.CubeGrid?.Physics == null)
                return;

            SetupTerminalControls<IMyCollector>();

            MyParticlesManager.TryCreateParticleEffect(WeldingParticle, ref MatrixD.Identity, ref Vector3D.Zero, uint.MaxValue, out ActiveWeldingParticle);
            ActiveWeldSoundEmitter = new MyEntity3DSoundEmitter(null);

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

            LoadSettings();

            Settings.IgnoreArmor = true;

            if (SettingsSubsystemPriority != SubsystemPriority)
            {
                SubsystemPriority = SettingsSubsystemPriority;
            }

            if (SettingsExclusiveMode != ExclusiveMode)
            {
                ExclusiveMode = SettingsExclusiveMode;
            }

            if (SettingsIgnoreArmor != IgnoreArmor)
            {
                IgnoreArmor = SettingsIgnoreArmor;
            }

            SaveSettings();

            block.AppendingCustomInfo += AppendingCustomInfo;

        }

        public override void UpdateAfterSimulation()
        {         

            if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0 && block != null && block.IsWorking)
            {
                if (block.CubeGrid != null && block != null && WeldNextTargetDelay <= 0)
                {
                    if (MyAPIGateway.Session.IsServer)
                    {
                        GatherDamagedBlocks(block.CubeGrid, ref repairList, ref priorityRepairList);

                        var positionalList = new Vector3I[0];
                        var priorityPositionalList = new Vector3I[0];
                        CreatePositionalList(ref repairList, ref priorityRepairList, out positionalList, out priorityPositionalList);

                        SettingsRepairPositionalList = positionalList;
                        SettingsPriorityPositionalList = priorityPositionalList;                     
                    }

                    FetchBlocksFromPosition(block.CubeGrid, ref repairList, ref priorityRepairList, SettingsRepairPositionalList, SettingsPriorityPositionalList);

                    DoRepairAction();
                    WeldingSortTimeout = WeldingSortTimeout - 1;                 
                }
                else if (block != null && block.CubeGrid != null)
                {
                    if (ActiveWeldSoundEmitter != null)
                    {
                        ActiveWeldingParticle?.SetTranslation(ref Vector3D.Zero);
                        ActiveWeldSoundEmitter?.StopSound(true);
                        WeldNextTargetDelay = WeldNextTargetDelay - 1;
                    }
                }

                if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
                {
                    block.RefreshCustomInfo();
                    block.SetDetailedInfoDirty();
                }
            }
            else if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0 && block != null && !block.IsWorking)
            {
                ActiveWeldingParticle?.SetTranslation(ref Vector3D.Zero);
                repairList?.Clear();
                priorityRepairList?.Clear();

                if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
                {
                    block.RefreshCustomInfo();
                    block.SetDetailedInfoDirty();
                }
            }

            if (firstBlock != null && !firstBlock.IsFullIntegrity)
            {
                Vector3D firstBlockPosition = Vector3D.Zero;
                firstBlock.ComputeWorldCenter(out firstBlockPosition);

                ActiveWeldingParticle?.SetTranslation(ref firstBlockPosition);
                ActiveWeldSoundEmitter?.SetPosition(firstBlockPosition);
            }
            else
            {
                ActiveWeldingParticle?.SetTranslation(ref Vector3D.Zero);
                ActiveWeldSoundEmitter?.SetPosition(Vector3D.Zero);
                ActiveWeldSoundEmitter?.StopSound(true);
            }

        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                SyncSettings();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public override void Close()
        {
            if (block == null)
                return;

            block = null;

            if (ActiveWeldingParticle == null)
                return;

            ActiveWeldingParticle.SetTranslation(ref Vector3D.Zero);

            if (ActiveWeldSoundEmitter == null)
                return;

            ActiveWeldSoundEmitter.SetPosition(Vector3D.Zero);
            ActiveWeldSoundEmitter.StopSound(true);
        }
        #endregion

        #region Utility
        public bool DetermineRepairPriority(IMySlimBlock block, int SubsystemPriority)
        {
            switch (SubsystemPriority)
            {
                case 1: // Offense
                    if (block.FatBlock != null &&
                        (block.FatBlock is IMyConveyorSorter ||
                        block.FatBlock is IMyLargeTurretBase ||
                        block.FatBlock is IMySmallMissileLauncher ||
                        block.FatBlock is IMySmallMissileLauncherReload ||
                        block.FatBlock is IMySmallGatlingGun))
                    {
                        return true;
                    };
                    break;

                case 2: // Power
                    if (block.FatBlock != null &&
                        (block.FatBlock is IMyReactor ||
                        block.FatBlock is IMyBatteryBlock))
                    {
                        return true;
                    };
                    break;

                case 3: // Thrust
                    if (block.FatBlock != null && (block.FatBlock is IMyThrust))
                    {
                        return true;
                    };
                    break;

                case 4: // Steering
                    if (block.FatBlock != null && (block.FatBlock is IMyGyro || block.FatBlock is IMyCockpit))
                    {
                        return true;
                    };
                    break;
                case 5: //Utility
                    if (block.FatBlock != null && (block.FatBlock is IMyGasTank || block.FatBlock is IMyConveyor || block.FatBlock is IMyConveyorSorter || block.FatBlock is IMyConveyorTube))
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private void UpdateLists(ref List<IMySlimBlock> mainList, HashSet<IMySlimBlock> tempList)
        {
            List<IMySlimBlock> convertHashToList = tempList.ToList();
            convertHashToList.Sort((block1, block2) => block1.Integrity.CompareTo(block2.Integrity));

            if (mainList.Count > 30)
            {
                mainList = mainList.Take(30).ToList();
            }

            var limitedTempList = convertHashToList.Take(30).ToList();

            mainList.RemoveAll(block => block.IsFullIntegrity && !block.HasDeformation || !limitedTempList.Contains(block));

            foreach (var block in limitedTempList)
            {
                if (!mainList.Contains(block))
                {
                    mainList.Add(block);
                }
            }
        }

        private void CreatePositionalList(ref List<IMySlimBlock> repairList, ref List<IMySlimBlock> priorityRepairList, out Vector3I[] positionalList, out Vector3I[] priorityPositionalList)
        {
            positionalList = new Vector3I[repairList.Count];
            priorityPositionalList = new Vector3I[priorityRepairList.Count];

            for (int i = 0; i < repairList.Count; i++)
            {
                positionalList[i] = repairList[i].Position;
            }

            for (int i = 0; i < priorityRepairList.Count; i++)
            {
                priorityPositionalList[i] = priorityRepairList[i].Position;
            }
        }

        private void FetchBlocksFromPosition(IMyCubeGrid grid, ref List<IMySlimBlock> repairList, ref List<IMySlimBlock> priorityRepairList, Vector3I[] positionalList, Vector3I[] priorityPositionalList)
        {
            repairList.Clear();
            priorityRepairList.Clear();

            if (positionalList != null)
            {
                foreach (var item in positionalList)
                {
                    if (grid != null)
                    {
                        var block = grid.GetCubeBlock(item);

                        if (block != null)
                        {
                            repairList.Add(block);
                        }
                    }
                }
            }

            if (priorityPositionalList != null)
            {
                foreach (var item in priorityPositionalList)
                {
                    if (grid != null)
                    {
                        var block = grid.GetCubeBlock(item);

                        if (block != null)
                        {
                            priorityRepairList.Add(block);
                        }
                    }
                }
            }
        }

        private void TimeoutSortAndReset(IMySlimBlock block)
        {
            ActiveWeldingParticle?.SetTranslation(ref Vector3D.Zero);

            block.UpdateVisual();

            if (priorityRepairList.Contains(block))
            {
                priorityRepairList.Sort((block1, block2) => block1.Integrity.CompareTo(block2.Integrity));
            }
            else
            {
                repairList.Sort((block1, block2) => block1.Integrity.CompareTo(block2.Integrity));
            }
            WeldNextTargetDelay = 1;
            WeldingSortTimeout = 15;
            return;
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            try // only for non-critical code
            {
                // NOTE: don't Clear() the StringBuilder, it's the same instance given to all mods.

                string priorityListAsString = string.Join(Environment.NewLine, priorityRepairList.Select(listItem =>
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

                string listAsString = string.Join(Environment.NewLine, repairList.Select(listItem =>
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

                sb.Append("Priority Targets: ").Append("\n").Append($"{priorityListAsString}").Append("\n").Append("\n").Append("Regular Targets: ").Append("\n").Append($"{listAsString}");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
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
        #endregion

        #region Body
        public void GatherDamagedBlocks(IMyCubeGrid grid, ref List<IMySlimBlock> repairList, ref List<IMySlimBlock> priorityRepairList)
        {
            var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Mechanical);
            List<IMyCubeGrid> gridsList = new List<IMyCubeGrid>();
            HashSet<IMySlimBlock> tempRepairList = new HashSet<IMySlimBlock>();
            HashSet<IMySlimBlock> tempPriorityList = new HashSet<IMySlimBlock>();

            if (gridGroup != null)
            {
                gridGroup.GetGrids(gridsList);

                foreach (IMyCubeGrid groupGrid in gridsList)
                {
                    var tempBlockList = new List<IMySlimBlock>();

                    groupGrid.GetBlocks(tempBlockList);

                    foreach (var block in tempBlockList)
                    {
                        if (block.IsFullIntegrity && !block.HasDeformation)
                        {
                            continue;
                        }

                        if (IgnoreArmor && (block.FatBlock == null || block.ToString().Contains("MyCubeBlock")))
                        {
                            continue;
                        }

                        if (DetermineRepairPriority(block, SubsystemPriority))
                        {
                            tempPriorityList.Add(block);
                        }
                        else if (!tempPriorityList.Contains(block) && !ExclusiveMode)
                        {
                            tempRepairList.Add(block);
                        }
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
                    if (block.IsFullIntegrity && !block.HasDeformation)
                    {
                        continue;
                    }

                    if (DetermineRepairPriority(block, SubsystemPriority))
                    {
                        tempPriorityList.Add(block);
                    }
                    else if (!tempPriorityList.Contains(block) && !ExclusiveMode)
                    {
                        tempRepairList.Add(block);
                    }
                }

                tempBlockList.Clear();
            }

            priorityRepairList.RemoveAll(block => !DetermineRepairPriority(block, SubsystemPriority));

            UpdateLists(ref repairList, tempRepairList);

            UpdateLists(ref priorityRepairList, tempPriorityList);
        }

        public void DoRepairAction()
        {
            ActiveWeldingParticle?.SetTranslation(ref Vector3D.Zero);

            ActiveWeldSoundEmitter?.SetPosition(Vector3D.Zero);
            ActiveWeldSoundEmitter?.StopSound(true);

            if (!SortedOnce)
            {
                priorityRepairList.Sort((block1, block2) => block1.Integrity.CompareTo(block2.Integrity));
                repairList.Sort((block1, block2) => block1.Integrity.CompareTo(block2.Integrity));
                SortedOnce = true;
            }

            firstBlock = null;

            if (priorityRepairList.Any())
            {
                firstBlock = priorityRepairList[0];
            }
            else if (!ExclusiveMode && repairList.Any())
            {
                firstBlock = repairList[0];
            }
            else
                return;

            if (firstBlock != null && WeldingSortTimeout > 0 && (!firstBlock.IsFullIntegrity || firstBlock.HasDeformation))
            {
                float repairAmount = 2f;
                firstBlock.IncreaseMountLevel(repairAmount * MyAPIGateway.Session.WelderSpeedMultiplier, firstBlock.OwnerId, null, 1);

                Vector3D firstBlockPosition = Vector3D.Zero;
                firstBlock.ComputeWorldCenter(out firstBlockPosition);

                ActiveWeldingParticle?.SetTranslation(ref firstBlockPosition);

                ActiveWeldSoundEmitter?.SetPosition(firstBlockPosition);
                ActiveWeldSoundEmitter?.PlaySingleSound(ActiveWeldSoundPair, true);

                if (firstBlock != null && firstBlock.IsFullIntegrity)
                {
                    TimeoutSortAndReset(firstBlock);
                    return;
                }
            }
            else if (firstBlock != null && (WeldingSortTimeout <= 0 || firstBlock.IsFullIntegrity || !firstBlock.HasDeformation))
            {
                TimeoutSortAndReset(firstBlock);
                return;
            }
        }
        #endregion

        #region Settings
        bool LoadSettings()
        {
            if (block.Storage == null)
                return false;

            string rawData;
            if (!block.Storage.TryGetValue(Settings_GUID, out rawData))
                return false;

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<AutoRepairModuleSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.SubsystemPriority = loadedSettings.SubsystemPriority;
                    Settings.ExclusiveMode = loadedSettings.ExclusiveMode;
                    Settings.IgnoreArmor = loadedSettings.IgnoreArmor;
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error loading settings!\n{e}");
            }

            return false;
        }

        void SaveSettings()
        {

            if (block == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; modInstance={AutoRepairModuleMod.Instance != null}");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; modInstance={AutoRepairModuleMod.Instance != null}");

            if (block.Storage == null)
                block.Storage = new MyModStorageComponent();

            block.Storage.SetValue(Settings_GUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
        }

        void SyncSettings()
        {
            if (syncCountdown > 0 && --syncCountdown <= 0)
            {
                SaveSettings();

                Mod.CachedPacketSettings.Send(block.EntityId, Settings);
            }
        }

        void SettingsChanged()
        {
            if (syncCountdown == 0)
                syncCountdown = SettingsChangedCountdown;
        }

        public override bool IsSerialized()
        {
            try
            {
                SaveSettings();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return base.IsSerialized();
        }
        #endregion

        #region Terminal Controls
        static void SetupTerminalControls<T>()
        {
            var mod = AutoRepairModuleMod.Instance;

            if (mod.ControlsCreated)
                return;

            mod.ControlsCreated = true;

            #region Terminal Control Handling
            var ARMSubsystemPriorityDropdown = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyCollector>("AutoRepairModule" + "PriorityDropdown");
            ARMSubsystemPriorityDropdown.Title = MyStringId.GetOrCompute("Repair Prioity");
            ARMSubsystemPriorityDropdown.Tooltip = MyStringId.GetOrCompute("Select a Subsystem Group to Prioritize");
            ARMSubsystemPriorityDropdown.SupportsMultipleBlocks = true;
            ARMSubsystemPriorityDropdown.Visible = Control_Visible;
            ARMSubsystemPriorityDropdown.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<AutoRepairModule>();
                if (logic != null)
                {
                    return logic.SubsystemPriority;
                }
                else
                {
                    return 0;
                }
               
            };
            ARMSubsystemPriorityDropdown.Setter = Control_Priority_Setter;
            ARMSubsystemPriorityDropdown.ComboBoxContent = (list) =>
            {
                list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Any") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Offense") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Power") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 3, Value = MyStringId.GetOrCompute("Thrust") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 4, Value = MyStringId.GetOrCompute("Steering") });
                list.Add(new MyTerminalControlComboBoxItem() { Key = 5, Value = MyStringId.GetOrCompute("Utility") });
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(ARMSubsystemPriorityDropdown);


            var ARMExclusiveModeCheckbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyCollector>("AutoRepairModule" + "ExclusiveCheckbox");
            ARMExclusiveModeCheckbox.Title = MyStringId.GetOrCompute("Exclusive Mode");
            ARMExclusiveModeCheckbox.Tooltip = MyStringId.GetOrCompute("Toggle Exclusive Mode - Only Repair Priority Blocks");
            ARMExclusiveModeCheckbox.SupportsMultipleBlocks = true;
            ARMExclusiveModeCheckbox.Visible = Control_Visible;
            ARMExclusiveModeCheckbox.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<AutoRepairModule>();
                if (logic != null)
                {
                    return logic.ExclusiveMode;
                }
                else
                {
                    return false;
                }            
            };
            ARMExclusiveModeCheckbox.Setter = Control_Exclusive_Setter;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(ARMExclusiveModeCheckbox);


            var ARMIgnoreArmorCheckbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyCollector>("AutoRepairModule" + "IgnoreArmorCheckbox");
            ARMIgnoreArmorCheckbox.Title = MyStringId.GetOrCompute("Ignore Armor");
            ARMIgnoreArmorCheckbox.Tooltip = MyStringId.GetOrCompute("Toggle Ignoring Armor - Only Repair Functional Blocks");
            ARMIgnoreArmorCheckbox.SupportsMultipleBlocks = true;
            ARMIgnoreArmorCheckbox.Visible = Control_Visible;
            ARMIgnoreArmorCheckbox.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<AutoRepairModule>();
                if (logic != null)
                {
                    return logic.IgnoreArmor;
                }
                else
                {
                    return false;
                }              
            };
            ARMIgnoreArmorCheckbox.Setter = Control_IgnoreArmor_Setter;
            MyAPIGateway.TerminalControls.AddControl<IMyCollector>(ARMIgnoreArmorCheckbox);
            #endregion

            #region Toolbar Action Handling
            var ARMPriorityDropdownCycle = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("AutoRepairModule" + "PriorityDropdownCycle");
            ARMPriorityDropdownCycle.Name = new StringBuilder("Cycle Priority");
            ARMPriorityDropdownCycle.ValidForGroups = true;
            ARMPriorityDropdownCycle.Icon = @"Textures\GUI\Icons\Actions\SubsystemTargeting_Cycle.dds";
            ARMPriorityDropdownCycle.Action = Control_Priority_Action_Setter;
            ARMPriorityDropdownCycle.Writer = Control_Priority_Action_Writer;
            ARMPriorityDropdownCycle.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            ARMPriorityDropdownCycle.Enabled = Control_Visible;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(ARMPriorityDropdownCycle);


            var ARMExclusiveToggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("AutoRepairModule" + "ExclusiveCheckboxAction");
            ARMExclusiveToggleAction.Name = new StringBuilder("Toggle Exclusive Mode");
            ARMExclusiveToggleAction.ValidForGroups = true;
            ARMExclusiveToggleAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            ARMExclusiveToggleAction.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<AutoRepairModule>();
                if (logic != null)
                {
                    if (logic.ExclusiveMode)
                    {
                        logic.ExclusiveMode = false;
                        logic.Settings.ExclusiveMode = logic.ExclusiveMode;
                    }
                    else
                    {
                        logic.ExclusiveMode = true;
                        logic.Settings.ExclusiveMode = logic.ExclusiveMode;
                    }                      
                }
            };
            ARMExclusiveToggleAction.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<AutoRepairModule>();
                if (logic != null)
                {
                    if (logic.ExclusiveMode)
                    {
                        sb.Append("True");
                    }
                    else
                        sb.Append("False");
                }
            };
            ARMExclusiveToggleAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            ARMExclusiveToggleAction.Enabled = Control_Visible;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(ARMExclusiveToggleAction);


            var ARMIgnoreArmorToggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("AutoRepairModule" + "IgnoreArmorCheckboxAction");
            ARMIgnoreArmorToggleAction.Name = new StringBuilder("Toggle Ignore Armor");
            ARMIgnoreArmorToggleAction.ValidForGroups = true;
            ARMIgnoreArmorToggleAction.Icon = @"Textures\GUI\Icons\Actions\StationToggle.dds";
            ARMIgnoreArmorToggleAction.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<AutoRepairModule>();
                if (logic != null)
                {
                    if (logic.IgnoreArmor)
                    {
                        logic.IgnoreArmor = false;
                        logic.Settings.IgnoreArmor = logic.IgnoreArmor;
                    }
                    else
                    {
                        logic.IgnoreArmor = true;
                        logic.Settings.IgnoreArmor = logic.IgnoreArmor;
                    }
                        
                }
            };
            ARMIgnoreArmorToggleAction.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<AutoRepairModule>();
                if (logic != null)
                {
                    if (logic.IgnoreArmor)
                    {
                        sb.Append("True");
                    }
                    else
                        sb.Append("False");
                }
            };
            ARMIgnoreArmorToggleAction.InvalidToolbarTypes = new List<MyToolbarType>()
            {
                MyToolbarType.ButtonPanel,
                MyToolbarType.Character,
                MyToolbarType.Seat
            };
            ARMIgnoreArmorToggleAction.Enabled = Control_Visible;
            MyAPIGateway.TerminalControls.AddAction<IMyCollector>(ARMIgnoreArmorToggleAction);
            #endregion

        }

        static AutoRepairModule GetLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<AutoRepairModule>();

        static bool Control_Visible(IMyTerminalBlock block)
        {
            return GetLogic(block) != null;
        }

        static void Control_Priority_Setter(IMyTerminalBlock block, long key)
        {
            var logic = GetLogic(block);
            if (logic != null)
                logic.SubsystemPriority = (int)key;
            logic.Settings.SubsystemPriority = logic.SubsystemPriority;
        }

        static void Control_Priority_Action_Setter(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            if (logic != null)
                if (logic.SubsystemPriority < 5)
                {
                    logic.SubsystemPriority = logic.SubsystemPriority + 1;
                }
                else
                {
                    logic.SubsystemPriority = 0;
                }
            logic.Settings.SubsystemPriority = logic.SubsystemPriority;
        }

        static void Control_Priority_Action_Writer(IMyTerminalBlock block, StringBuilder sb)
        {
            var logic = GetLogic(block);
            if (logic != null)
                switch (logic.SubsystemPriority)
                {
                    case 0: // None
                        sb.Append("None");
                        break;

                    case 1: // Offense
                        sb.Append("Offense");
                        break;

                    case 2: // Power
                        sb.Append("Power");
                        break;

                    case 3: // Thrust
                        sb.Append("Thrust");
                        break;

                    case 4: // Steering
                        sb.Append("Steering");
                        break;

                    case 5: // Steering
                        sb.Append("Utility");
                        break;
                }
        }

        static void Control_Exclusive_Setter(IMyTerminalBlock block, bool v)
        {
            var logic = GetLogic(block);
            if (logic != null)
                logic.ExclusiveMode = v;
            logic.Settings.ExclusiveMode = logic.ExclusiveMode;
        }

        static void Control_IgnoreArmor_Setter(IMyTerminalBlock block, bool v)
        {
            var logic = GetLogic(block);
            if (logic != null)
                logic.IgnoreArmor = v;
            logic.Settings.IgnoreArmor = logic.IgnoreArmor;
        }

        #endregion

    }
}