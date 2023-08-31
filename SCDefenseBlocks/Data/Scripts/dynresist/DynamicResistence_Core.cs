using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.Game.Localization;

namespace StarCore.DynamicResistence
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "Collector")]
    public class DynamicResistLogic : MyGameLogicComponent
    {

        public const float MinPolarization = 0f;
        public const float MaxPolarization = 30f;

        public const float MinResistModifier = 1.0f;
        public const float MaxResistModifier = 0.7f;

        public const string Control_Prefix = "DynaResist.";
        public readonly Guid Settings_GUID = new Guid("9EFDABA1-E705-4F62-BD37-A4B046B60BC0");
        public const int Settings_Change_Countdown = (60 * 1) / 10;

        public int SiegeTimer = 9000;
        public int SiegeCooldownTimer = 18000;
        public int SiegeDisplayTimer = 60;
        public int SiegeVisibleTimer = 150;
        public bool SiegeCooldownTimerActive = false;

        public float MaxAvailibleGridPower = 0f;
        public bool SiegeModeActivated = false;
        public bool SiegeBlockShutdown = false;
        public bool SiegeModeResistence = false;

        private MyResourceSinkComponent Sink = null;

        private IMyHudNotification notifPolarization = null;
        private IMyHudNotification notifCountdown = null;

        public float Polarization
        {
            get
            { return Settings.Polarization; }
            set
            {
                Settings.Polarization = MathHelper.Clamp((float)Math.Floor(value), MinPolarization, MaxPolarization);

                SettingsChanged();

                if (Settings.Polarization == 0)
                {
                    NeedsUpdate = MyEntityUpdateEnum.NONE;
                }
                else
                {
                    if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                        NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                }

                dynResistBlock?.Components?.Get<MyResourceSinkComponent>()?.Update();
            }
        }

        public float Modifier
        {
            get
            { return Settings.Modifier; }
            set
            {
                Settings.Modifier = MathHelper.Clamp((float)Math.Floor(value), MinResistModifier, MaxResistModifier);

                SettingsChanged();

                if (Settings.Modifier == 1.0)
                {
                    NeedsUpdate = MyEntityUpdateEnum.NONE;
                }
                else
                {
                    if ((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0)
                        NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                }

                dynResistBlock?.Components?.Get<MyResourceSinkComponent>()?.Update();
            }
        }


        public IMyCollector dynResistBlock;
        public MyPoweredCargoContainerDefinition dynResistBlockDef;

        public readonly DynaResistBlockSettings Settings = new DynaResistBlockSettings();
        int syncCountdown;

        public float finalResistanceModifier = 0f;

        public float HullPolarization { get; set; }

        DynamicResistenceMod Mod => DynamicResistenceMod.Instance;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do calculations serverside
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                SetupTerminalControls<IMyCollector>();

                dynResistBlock = (IMyCollector)Entity;

                if (dynResistBlock.CubeGrid?.Physics == null)
                    return;

                dynResistBlockDef = (MyPoweredCargoContainerDefinition)dynResistBlock.SlimBlock.BlockDefinition;

                Sink = dynResistBlock.Components.Get<MyResourceSinkComponent>();
                Sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, RequiredInput);

                Sink.Update();

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

                Settings.Modifier = 1.0f;
                Settings.Polarization = MinPolarization;


                if (!LoadSettings())
                {
                    ParseLegacyNameStorage();
                }

                SaveSettings(); // required for IsSerialized()
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public override void Close()
        {
            try
            {
                if (dynResistBlock == null)
                    return;

                if (SiegeModeActivated)
                {
                    SetCountdownStatus($"Block Removed! Siege Mode Deactivated", 1500, MyFontEnum.Red);
                }

                ResetBlockResist(dynResistBlock);
                dynResistBlock = null;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private float RequiredInput()
        {
            Log.Info("Reached Compute");
            if (!dynResistBlock.IsWorking)
                return 0f;
            
            else if (HullPolarization == 0f && !SiegeModeActivated)
            {
                return 50.000f;
            }
            else if (SiegeModeActivated)
            {
                CalculateMaxGridPower();

                float baseUsage = 50.000f;
                float quarterOfMax = dynResistBlockDef.RequiredPowerInput = MaxAvailibleGridPower / 4f;
                float maxPowerUsage = dynResistBlockDef.RequiredPowerInput = MaxAvailibleGridPower - quarterOfMax;

                return maxPowerUsage;
            }
            else    
            {
                Log.Info("Reached Enabled");

                CalculateMaxGridPower();

                float baseUsage = 50.000f;
                float maxPowerUsage = dynResistBlockDef.RequiredPowerInput = MaxAvailibleGridPower / 4f;
                float sliderValue = HullPolarization;

                float ratio = sliderValue / MaxPolarization;

                return baseUsage + (baseUsage + (maxPowerUsage - baseUsage) * ratio);
            }                      
        }

        private void CalculateMaxGridPower()
        {
            if (dynResistBlock.IsWorking)
            {
                var dynamicResistLogic = dynResistBlock.GameLogic?.GetAs<DynamicResistLogic>();

                if (dynamicResistLogic != null)
                {
                    float totalPower = 0f;

                    var blocks = new List<IMySlimBlock>();
                    dynResistBlock.CubeGrid.GetBlocks(blocks);

                    foreach (var block in blocks)
                    {
                        var fatBlock = block.FatBlock;
                        if (fatBlock is IMyPowerProducer)
                        {
                            var powerProducer = fatBlock as IMyPowerProducer;
                            totalPower += powerProducer.MaxOutput;
                        }
                        /*else if (fatBlock is )
                        {
                            var powerProducer = fatBlock as IMyBatteryBlock;
                            totalPower += powerProducer.MaxOutput;
                        }*/
                    }

                    Log.Info("totalPower Evaluation: " + totalPower);

                    if (MaxAvailibleGridPower == totalPower)
                        return;
                    else
                    {
                        MaxAvailibleGridPower = totalPower;
                        MyAPIGateway.Utilities.ShowNotification("Current Total Power:" + totalPower, 5000, MyFontEnum.Green);
                    }
                }
            }
        }

        private void SiegeMode()
        {

            CalculateMaxGridPower();

            var allSlimBlocks = new List<IMySlimBlock>();
            dynResistBlock.CubeGrid.GetBlocks(allSlimBlocks);

            var allTerminalBlocks = new List<IMySlimBlock>();
            dynResistBlock.CubeGrid.GetBlocks(allTerminalBlocks);

            var CurrentlyHighlighted = dynResistBlock.CubeGrid;

            if (!SiegeModeActivated)
            {
                return;
            }
            else if (dynResistBlock != null && SiegeModeActivated && MaxAvailibleGridPower <= 150f)
            {
                SetCountdownStatus($"Insufficient Power", 1500, MyFontEnum.Red);
                SiegeModeActivated = false;
                return;
            }
            else if (dynResistBlock != null && SiegeModeActivated && !SiegeModeResistence && dynResistBlock.IsWorking && MaxAvailibleGridPower > 150f)
            {
                foreach (var block in allSlimBlocks)
                {
                    block.BlockGeneralDamageModifier = 0.1f;
                }
                SiegeModeResistence = true;
            }

            else if (dynResistBlock != null && SiegeModeActivated && SiegeModeResistence && dynResistBlock.IsWorking && MaxAvailibleGridPower > 150f)
            {
                MyVisualScriptLogicProvider.SetHighlight(CurrentlyHighlighted.Name, thickness: 2, pulseTimeInFrames: 12, color: Color.DarkOrange);

                Sink.Update();

                if (SiegeTimer > 0)
                {
                    HullPolarization = 0f;

                    SiegeModeShutdown(allTerminalBlocks);

                    dynResistBlock.CubeGrid.Physics.LinearVelocity = Vector3D.Zero;
                    /*dynResistBlock.CubeGrid.Physics.AngularVelocity = Vector3D.Zero;*/

                    SiegeTimer = SiegeTimer - 1;
                    SiegeDisplayTimer = SiegeDisplayTimer - 1;
                    if (SiegeDisplayTimer <= 0)
                    {
                        SiegeDisplayTimer = 60;
                        SiegeVisibleTimer = SiegeVisibleTimer - 1;
                        SetCountdownStatus($"Siege Mode: " + SiegeVisibleTimer + " Seconds", 1500, MyFontEnum.Green);
                    }
                }
                else if (SiegeTimer == 0)
                {
                    SiegeTimer = 6000;
                    SiegeDisplayTimer = 60;

                    ResetBlockResist(dynResistBlock);

                    SiegeModeTurnOn(allTerminalBlocks);

                    MyVisualScriptLogicProvider.SetHighlight(CurrentlyHighlighted.Name, thickness: -1);

                    SetCountdownStatus($"Siege Mode Deactivated", 1500, MyFontEnum.Red);

                    SiegeModeActivated = false;
                    SiegeModeResistence = false;
                    SiegeCooldownTimerActive = true;

                    Sink.Update();
                }
            }
            else if (dynResistBlock != null && dynResistBlock.IsWorking == false & SiegeModeActivated)
            {
                ResetBlockResist(dynResistBlock);

                SiegeTimer = 6000;
                SiegeDisplayTimer = 60;

                SiegeModeTurnOn(allTerminalBlocks);

                MyVisualScriptLogicProvider.SetHighlight(CurrentlyHighlighted.Name, thickness: -1);

                SetCountdownStatus($"Block Damaged! Siege Mode Deactivated", 1500, MyFontEnum.Red);

                SiegeModeActivated = false;
                SiegeModeResistence = false;
                SiegeCooldownTimerActive = true;
            }

            else
                return;
        }

        private void SiegeModeShutdown(List<IMySlimBlock> allTerminalBlocks)
        {
            foreach (var block in allTerminalBlocks)
            {
                if (block.FatBlock is IMyReactor || block.FatBlock is IMyBatteryBlock ||
                    block.FatBlock is IMySolarPanel || block.FatBlock is IMyWindTurbine ||
                    block.FatBlock is IMyCollector || block.FatBlock is IMyCockpit)
                {
                    // Skip certain block types
                    continue;
                }

                var functionalBlock = block.FatBlock as IMyFunctionalBlock;
                if (functionalBlock != null)
                {
                    functionalBlock.Enabled = false;
                }
            }
            SiegeBlockShutdown = true;
        }

        private void SiegeModeTurnOn(List<IMySlimBlock> allTerminalBlocks)
        {
            foreach (var block in allTerminalBlocks)
            {
                if (block.FatBlock is IMyReactor || block.FatBlock is IMyBatteryBlock ||
                    block.FatBlock is IMySolarPanel || block.FatBlock is IMyWindTurbine ||
                    block.FatBlock is IMyCollector || block.FatBlock is IMyCockpit)
                {
                    // Skip certain block types
                    continue;
                }

                var functionalBlock = block.FatBlock as IMyFunctionalBlock;
                if (functionalBlock != null)
                {
                    functionalBlock.Enabled = true;
                }
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                SyncSettings();
                if (SiegeCooldownTimerActive == true && SiegeCooldownTimer > 0)
                {
                    SiegeCooldownTimer = SiegeCooldownTimer - 10;
                }
                else if (SiegeCooldownTimerActive == true && SiegeCooldownTimer <= 0)
                {
                    SiegeCooldownTimer = 18000;
                    SiegeCooldownTimerActive = false;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                SiegeMode();
                ChangeResistanceValue(dynResistBlock);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        bool LoadSettings()
        {
            if (dynResistBlock.Storage == null)
                return false;

            string rawData;
            if (!dynResistBlock.Storage.TryGetValue(Settings_GUID, out rawData))
                return false;

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<DynaResistBlockSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.Polarization = loadedSettings.Polarization;
                    Settings.Modifier = loadedSettings.Modifier;
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error loading settings!\n{e}");
            }

            return false;
        }

        bool ParseLegacyNameStorage()
        {
            string name = dynResistBlock.CustomName.TrimEnd(' ');

            if (!name.EndsWith("]", StringComparison.Ordinal))
                return false;

            int startIndex = name.IndexOf('[');

            if (startIndex == -1)
                return false;

            var settingsStr = name.Substring(startIndex + 1, name.Length - startIndex - 2);

            if (settingsStr.Length == 0)
                return false;

            string[] args = settingsStr.Split(';');

            if (args.Length == 0)
                return false;

            string[] data;

            foreach (string arg in args)
            {
                data = arg.Split('=');

                float f;
                int i;

                if (data.Length == 2)
                {
                    switch (data[0])
                    {
                        case "range":
                            if (int.TryParse(data[1], out i))
                                Polarization = i;
                            break;
                        case "str":
                            if (float.TryParse(data[1], out f))
                                Modifier = f;
                            break;
                    }
                }
            }

            dynResistBlock.CustomName = name.Substring(0, startIndex).Trim();
            return true;
        }

        void SaveSettings()
        {
            if (dynResistBlock == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; modInstance={DynamicResistenceMod.Instance != null}");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; modInstance={DynamicResistenceMod.Instance != null}");

            if (dynResistBlock.Storage == null)
                dynResistBlock.Storage = new MyModStorageComponent();

            dynResistBlock.Storage.SetValue(Settings_GUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
        }

        void SettingsChanged()
        {
            if (syncCountdown == 0)
                syncCountdown = Settings_Change_Countdown;
        }

        void SyncSettings()
        {
            if (syncCountdown > 0 && --syncCountdown <= 0)
            {
                SaveSettings();

                Mod.CachedPacketSettings.Send(dynResistBlock.EntityId, Settings);
            }
        }

        public override bool IsSerialized()
        {
            // called when the game iterates components to check if they should be serialized, before they're actually serialized.
            // this does not only include saving but also streaming and blueprinting.
            // NOTE for this to work reliably the MyModStorageComponent needs to already exist in this block with at least one element.

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

        private void SetPolarizationStatus(string text, int aliveTime = 300, string font = MyFontEnum.Green)
        {
            if (notifPolarization == null)
                notifPolarization = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);

            notifPolarization.Hide();
            notifPolarization.Font = font;
            notifPolarization.Text = text;
            notifPolarization.AliveTime = aliveTime;
            notifPolarization.Show();
        }

        public void SetCountdownStatus(string text, int aliveTime = 300, string font = MyFontEnum.Green)
        {
            if (notifCountdown == null)
                notifCountdown = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);

            notifCountdown.Hide();
            notifCountdown.Font = font;
            notifCountdown.Text = text;
            notifCountdown.AliveTime = aliveTime;
            notifCountdown.Show();
        }

        private void ChangeResistanceValue(IMyTerminalBlock obj)
        {
            if (obj.EntityId != dynResistBlock.EntityId) return;

            if (dynResistBlock.IsWorking && !SiegeModeActivated)
            {
                var dynamicResistLogic = dynResistBlock.GameLogic?.GetAs<DynamicResistLogic>();

                if (dynamicResistLogic != null)
                {
                    float hullPolarization = dynamicResistLogic.HullPolarization;

                    float t = (hullPolarization - MinPolarization) / (float)(MaxPolarization - MinPolarization);
                    float resistanceModifier = MinResistModifier + t * (MaxResistModifier - MinResistModifier);

                    resistanceModifier = (float)Math.Round(resistanceModifier, 2);

                    Settings.Modifier = resistanceModifier;

                    if (Settings.Modifier == finalResistanceModifier)
                        return;
                    else
                    {
                        Sink.Update();

                        var allBlocks = new List<IMySlimBlock>();
                        dynResistBlock.CubeGrid.GetBlocks(allBlocks);

                        foreach (var block in allBlocks)
                        {
                            block.BlockGeneralDamageModifier = resistanceModifier;
                        }

                        finalResistanceModifier = resistanceModifier;

                        SetPolarizationStatus($"Current Polarization: " + HullPolarization + "%", 1500, MyFontEnum.Green);
                    }
                }
            }
            else if (!dynResistBlock.IsWorking || !SiegeModeActivated)
            {
                if (HullPolarization > 0f)
                {
                    MyAPIGateway.Utilities.ShowNotification("Block is disabled", 300, MyFontEnum.Red);
                    HullPolarization = 0f;
                    Settings.Polarization = 0f;
                    finalResistanceModifier = 1.0f;
                    Settings.Modifier = 1.0f;
                    ResetBlockResist(dynResistBlock);
                }
                else
                    return;
            }

        }

        private void ResetBlockResist(IMyTerminalBlock obj)
        {
            if (obj.EntityId != dynResistBlock.EntityId) return;

            var allBlocks = new List<IMySlimBlock>();
            dynResistBlock.CubeGrid.GetBlocks(allBlocks);

            foreach (var block in allBlocks)
            {
                block.BlockGeneralDamageModifier = 1.0f;
            }

        }

        #region Terminal Controls
        static void SetupTerminalControls<T>()
        {
            var mod = DynamicResistenceMod.Instance;

            if (mod.ControlsCreated)
                return;

            mod.ControlsCreated = true;

            var polarizationValueSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCollector>(Control_Prefix + "HullPolarization");
            polarizationValueSlider.Title = MyStringId.GetOrCompute("Hull Polarization");
            polarizationValueSlider.Tooltip = MyStringId.GetOrCompute("Adjusts the mount of Damage Absorbed by the Block");
            polarizationValueSlider.SetLimits(MinPolarization, MaxPolarization);
            polarizationValueSlider.Writer = Control_Polarization_Writer;
            polarizationValueSlider.Visible = Control_Visible;
            polarizationValueSlider.Getter = Control_Polarization_Getter;
            polarizationValueSlider.Setter = Control_Polarization_Setter;
            polarizationValueSlider.Enabled = Siege_Enabler;
            polarizationValueSlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<T>(polarizationValueSlider);

            var siegeModeToggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCollector>(Control_Prefix + "SiegeMode");
            siegeModeToggle.Title = MyStringId.GetOrCompute("Siege Mode");
            siegeModeToggle.Tooltip = MyStringId.GetOrCompute("Toggle Siege Mode"); 
            siegeModeToggle.OnText = MySpaceTexts.SwitchText_On;
            siegeModeToggle.OffText = MyStringId.GetOrCompute("Off");
            siegeModeToggle.Visible = Control_Visible; 
            siegeModeToggle.Getter = Control_Siege_Getter;
            siegeModeToggle.Setter = Control_Siege_Setter;
            siegeModeToggle.Enabled = Siege_Cooldown_Enabler;
            siegeModeToggle.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<T>(siegeModeToggle);

            var increasePolarization = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(Control_Prefix + "PolarizationIncrease");
            increasePolarization.Name = new StringBuilder("Increase Polarization");
            increasePolarization.ValidForGroups = true;
            increasePolarization.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            increasePolarization.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    logic.HullPolarization = logic.HullPolarization + 1;
                    logic.HullPolarization = MathHelper.Clamp(logic.HullPolarization, 0f, 30f);
                    logic.Settings.Polarization = logic.HullPolarization;
                }
            };
            increasePolarization.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    sb.Append("Increase");
                }
            };
            increasePolarization.InvalidToolbarTypes = new List<MyToolbarType>()
                {
                    MyToolbarType.ButtonPanel,
                    MyToolbarType.Character,
                };
            increasePolarization.Enabled = Siege_Enabler;

            MyAPIGateway.TerminalControls.AddAction<T>(increasePolarization);


            var decreasePolarization = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(Control_Prefix + "PolarizationDecrease");
            decreasePolarization.Name = new StringBuilder("Decrease Polarization");
            decreasePolarization.ValidForGroups = true;
            decreasePolarization.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            decreasePolarization.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    logic.HullPolarization = logic.HullPolarization - 1;
                    logic.HullPolarization = MathHelper.Clamp(logic.HullPolarization, 0f, 30f);
                    logic.Settings.Polarization = logic.HullPolarization;
                }
            };
            decreasePolarization.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    sb.Append("Decrease");
                }
            };
            decreasePolarization.InvalidToolbarTypes = new List<MyToolbarType>()
                {
                    MyToolbarType.ButtonPanel,
                    MyToolbarType.Character,
                };
            decreasePolarization.Enabled = Siege_Enabler;

            MyAPIGateway.TerminalControls.AddAction<T>(decreasePolarization);

            var siegeModeToggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(Control_Prefix + "siegeToggleAction");
            siegeModeToggleAction.Name = new StringBuilder("Toggle Siege");
            siegeModeToggleAction.ValidForGroups = true;
            siegeModeToggleAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            siegeModeToggleAction.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    if (logic.SiegeModeActivated == false)
                    {
                        logic.SiegeModeActivated = true;
                    }
                    else
                        return;
                }
            };
            siegeModeToggleAction.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    sb.Append("Siege Mode");
                }
            };
            siegeModeToggleAction.InvalidToolbarTypes = new List<MyToolbarType>()
                {
                    MyToolbarType.ButtonPanel,
                    MyToolbarType.Character,
                };
            siegeModeToggleAction.Enabled = Siege_Enabler;

            MyAPIGateway.TerminalControls.AddAction<T>(siegeModeToggleAction);
        }

        static DynamicResistLogic GetLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<DynamicResistLogic>();

        static bool Control_Visible(IMyTerminalBlock block)
        {
            return GetLogic(block) != null;
        }

        static bool Siege_Enabler(IMyTerminalBlock block)
        {
            if (GetLogic(block) != null)
            {
                // Assuming DynamicResistLogic is the class containing SiegeModeActivated
                DynamicResistLogic dynamicResistLogic = GetLogic(block);

                if (dynamicResistLogic.SiegeModeActivated == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            // Add a default return value here (true or false depending on your logic)
            return false;
        }

        static bool Siege_Cooldown_Enabler(IMyTerminalBlock block)
        {
            if (GetLogic(block) != null)
            {
                // Assuming DynamicResistLogic is the class containing SiegeModeActivated
                DynamicResistLogic dynamicResistLogic = GetLogic(block);

                if (dynamicResistLogic.SiegeCooldownTimerActive == true || dynamicResistLogic.SiegeModeActivated == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            // Add a default return value here (true or false depending on your logic)
            return false;
        }

        static float Control_Polarization_Getter(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            return logic != null ? logic.HullPolarization : 0f;
        }

        static void Control_Polarization_Setter(IMyTerminalBlock block, float value)
        {
            var logic = GetLogic(block);
            if (logic != null)
                logic.HullPolarization = MathHelper.Clamp(value, 0f, 30f);
                logic.HullPolarization = (float)Math.Round(logic.HullPolarization, 0);
                logic.Settings.Polarization = logic.HullPolarization;
        }

        static void Control_Polarization_Writer(IMyTerminalBlock block, StringBuilder writer)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                float value = logic.HullPolarization;
                writer.Append(Math.Round(value, 0, MidpointRounding.ToEven)).Append("%");
            }
        }

        static bool Control_Siege_Getter(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            return logic != null ? logic.SiegeModeActivated : false;
        }

        static void Control_Siege_Setter(IMyTerminalBlock block, bool value)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                logic.SiegeModeActivated = value;
            }
        }
        #endregion

        /*private static void SetupControls()
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyCollector>(out controls);

            foreach (var c in controls)
            {
                switch (c.Id)
                {
                    case "DrainAll":
                    case "blacklistWhitelist":
                    case "CurrentList":
                    case "removeFromSelectionButton":
                    case "candidatesList":
                    case "addToSelectionButton":
                        c.Visible = CombineFunc.Create(c.Visible, Visible);
                        break;
                }
            }
        }*/

        private static bool Visible(IMyTerminalBlock block)
        {
            return block != null && !(block.GameLogic is DynamicResistLogic);
        }

        /*private List<IMyCharacter> FindPlayerNearObject()
        {
            List<IMyCharacter> playerCharacters = new List<IMyCharacter>();

            if (dynResistBlock != null)
            {
                // Define the bounding sphere
                var bound = new BoundingSphereD(dynResistBlock.GetPosition(), 50);
                List<IMyEntity> nearEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref bound);

                // Iterate through entities and find player characters
                foreach (var entity in nearEntities)
                {
                    IMyCharacter character = entity as IMyCharacter;
                    if (character != null && character.IsPlayer && bound.Contains(character.GetPosition()) != ContainmentType.Disjoint)
                    {
                        playerCharacters.Add(character);
                    }
                }
            }
            return playerCharacters;
        }*/

        /*private List<IMyCharacter> FindPlayers()
        {
            List<IMyCharacter> playerCharacters = new List<IMyCharacter>();

            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            foreach (var entity in entities)
            {
                IMyCharacter character = entity as IMyCharacter;
                if (character != null && character.IsPlayer)
                {
                    playerCharacters.Add(character);
                }
            }

            return playerCharacters;
        }*/

    }
}
