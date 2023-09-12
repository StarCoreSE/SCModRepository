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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "SI_Field_Gen")]
    public class DynamicResistLogic : MyGameLogicComponent
    {

        public const float MinDivertedPower = 0f;
        public const float MaxDivertedPower = 30f;

        public const float MinResistModifier = 1.0f;
        public const float MaxResistModifier = 0.7f;

        public float finalResistanceModifier = 0f;

        public const string Control_Prefix = "DynaResist.";
        public readonly Guid Settings_GUID = new Guid("9EFDABA1-E705-4F62-BD37-A4B046B60BC0");
        public const int Settings_Change_Countdown = (60 * 1) / 10;

        public int SiegeTimer = 9000;
        public int SiegeCooldownTimer = 18000;
        public int SiegeDisplayTimer = 60;
        public int SiegeVisibleTimer = 150;

        public const float SiegePowerRequirement = 150f;

        public float MaxAvailibleGridPower = 0f;

        public bool SiegeCooldownTimerActive = false;
        public bool SiegeModeActivatedClient = false;
        public bool SiegeBlockShutdown = false;
        public bool SiegeModeResistence = false;

        private MyResourceSinkComponent Sink = null;

        private IMyHudNotification notifPowerDiversion = null;
        private IMyHudNotification notifCountdown = null;

        public float DivertedPower { get; set; }

        public float FieldPower
        {
            get
            { return Settings.FieldPower; }
            set
            {
                Settings.FieldPower = MathHelper.Clamp((float)Math.Floor(value), MinDivertedPower, MaxDivertedPower);

                SettingsChanged();

                if (Settings.FieldPower == 0)
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

        public bool SiegeModeActivated
        {
            get 
            { 
                return Settings.SiegeModeActivated;
            }
            set
            {
                SettingsChanged();

                if (Settings.SiegeModeActivated == false)
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

        DynamicResistenceMod Mod => DynamicResistenceMod.Instance;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
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

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

                Settings.Modifier = 1.0f;
                Settings.FieldPower = MinDivertedPower;

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

                if (SiegeModeActivatedClient)
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
            if (!dynResistBlock.IsWorking)
                return 0f;
            
            else if (DivertedPower == 0f && !SiegeModeActivatedClient)
            {
                return 50.000f;
            }
            else if (SiegeModeActivatedClient)
            {
                CalculateMaxGridPower();    

                float maxPowerUsage = dynResistBlockDef.RequiredPowerInput = MaxAvailibleGridPower * 0.9f;

                return maxPowerUsage;
            }
            else    
            {
                CalculateMaxGridPower();

                float baseUsage = 50.000f;
                float powerPrecentage = dynResistBlockDef.RequiredPowerInput = MaxAvailibleGridPower * 0.3f;
                float sliderValue = DivertedPower;

                float ratio = sliderValue / MaxDivertedPower;

                return baseUsage + ((baseUsage + (powerPrecentage - baseUsage)) * ratio);
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
                    }

                    Log.Info("totalPower Evaluation: " + totalPower);

                    if (MaxAvailibleGridPower == totalPower)
                        return;
                    else
                    {
                        MaxAvailibleGridPower = totalPower;
                    }
                }
            }
        }

        private void SiegeMode()
        {
            var allTerminalBlocks = new List<IMySlimBlock>();
            dynResistBlock.CubeGrid.GetBlocks(allTerminalBlocks);

            Settings.SiegeModeActivated = SiegeModeActivatedClient;

            if (!SiegeModeActivatedClient)
            {
                return;
            }
            else if (dynResistBlock != null && SiegeModeActivatedClient && MaxAvailibleGridPower <= SiegePowerRequirement)
            {
                SetCountdownStatus($"Insufficient Power", 1500, MyFontEnum.Red);
                SiegeModeActivatedClient = false;
                Settings.SiegeModeActivated = SiegeModeActivatedClient;
                return;
            }
            else if (dynResistBlock != null && SiegeModeActivatedClient && !SiegeModeResistence && !dynResistBlock.IsWorking && MaxAvailibleGridPower > SiegePowerRequirement)
            {
                SetCountdownStatus($"Block Disabled", 1500, MyFontEnum.Red);
                SiegeModeActivatedClient = false;
                Settings.SiegeModeActivated = SiegeModeActivatedClient;
                return;
            }
            else if (dynResistBlock != null && SiegeModeActivatedClient && !SiegeModeResistence && dynResistBlock.IsWorking && MaxAvailibleGridPower > 150f)
            {
                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(dynResistBlock.CubeGrid.Name, 0.1f);
                SiegeModeResistence = true;
            }
            else if (dynResistBlock != null && SiegeModeActivatedClient && SiegeModeResistence && dynResistBlock.IsWorking && MaxAvailibleGridPower > 150f)
            {
                /*MyVisualScriptLogicProvider.SetHighlightLocal(dynResistBlock.CubeGrid.Name, thickness: 2, pulseTimeInFrames: 12, color: Color.DarkOrange);*/

                Sink.Update();

                if (SiegeTimer > 0)
                {
                    DivertedPower = 0f;

                    SiegeModeShutdown(allTerminalBlocks);

                    if (dynResistBlock.CubeGrid.Physics.LinearVelocity != Vector3D.Zero)
                    {
                        dynResistBlock.CubeGrid.Physics.LinearVelocity = Vector3D.Zero;
                    }
                    /*else if (dynResistBlock.CubeGrid.Physics.AngularVelocity != Vector3D.Zero)
                    {
                        dynResistBlock.CubeGrid.Physics.AngularVelocity = Vector3D.Zero;
                    }*/

                    SiegeTimer = SiegeTimer - 1;
                    SiegeDisplayTimer = SiegeDisplayTimer - 1;
                    if (SiegeDisplayTimer <= 0)
                    {
                        SiegeDisplayTimer = 60;
                        SiegeVisibleTimer = SiegeVisibleTimer - 1;
                        DisplayMessageToNearPlayers(0);
                    }
                }
                else if (SiegeTimer == 0)
                {
                    SiegeTimer = 6000;
                    SiegeDisplayTimer = 60;
                    SiegeVisibleTimer = 150;

                    /*MyVisualScriptLogicProvider.SetHighlightLocal(dynResistBlock.CubeGrid.Name, thickness: -1);*/

                    ResetBlockResist(dynResistBlock);
                    SiegeModeTurnOn(allTerminalBlocks);
                    DisplayMessageToNearPlayers(1);

                    SiegeModeActivatedClient = false;
                    Settings.SiegeModeActivated = SiegeModeActivatedClient;
                    SiegeModeResistence = false;
                    SiegeCooldownTimerActive = true;

                    Sink.Update();
                }
            }
            else if (dynResistBlock != null && dynResistBlock.IsWorking == false & SiegeModeActivatedClient)
            {                
                SiegeTimer = 6000;
                SiegeDisplayTimer = 60;
                SiegeVisibleTimer = 150;

                /*MyVisualScriptLogicProvider.SetHighlightLocal(dynResistBlock.CubeGrid.Name, thickness: -1);*/

                ResetBlockResist(dynResistBlock);
                SiegeModeTurnOn(allTerminalBlocks);
                DisplayMessageToNearPlayers(2);

                SiegeModeActivatedClient = false;
                Settings.SiegeModeActivated = SiegeModeActivatedClient;
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
                CalculateMaxGridPower();
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
                    Settings.FieldPower = loadedSettings.FieldPower;
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

        private void SetPowerStatus(string text, int aliveTime = 300, string font = MyFontEnum.Green)
        {
            if (notifPowerDiversion == null)
                notifPowerDiversion = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);

            notifPowerDiversion.Hide();
            notifPowerDiversion.Font = font;
            notifPowerDiversion.Text = text;
            notifPowerDiversion.AliveTime = aliveTime;
            notifPowerDiversion.Show();
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

            if (dynResistBlock.IsWorking && !SiegeModeActivatedClient)
            {
                var dynamicResistLogic = dynResistBlock.GameLogic?.GetAs<DynamicResistLogic>();

                if (dynamicResistLogic != null)
                {
                    float divertedPower = dynamicResistLogic.DivertedPower;

                    float t = (divertedPower - MinDivertedPower) / (float)(MaxDivertedPower - MinDivertedPower);
                    float resistanceModifier = MinResistModifier + t * (MaxResistModifier - MinResistModifier);

                    resistanceModifier = (float)Math.Round(resistanceModifier, 2);

                    Settings.Modifier = resistanceModifier;

                    if (Settings.Modifier == finalResistanceModifier)
                        return;
                    else
                    {
                        Sink.Update();

                        MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(dynResistBlock.CubeGrid.Name, resistanceModifier);

                        finalResistanceModifier = resistanceModifier;

                        SetPowerStatus($"Integrity Field Power: " + DivertedPower + "%", 1500, MyFontEnum.Green);
                    }
                }
            }
            else if (!dynResistBlock.IsWorking || !SiegeModeActivatedClient)
            {
                if (DivertedPower > 0f)
                {
                    DivertedPower = 0f;
                    Settings.FieldPower = 0f;
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

            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(obj.CubeGrid.Name, 1f);

        }

        #region Terminal Controls
        static void SetupTerminalControls<T>()
        {
            var mod = DynamicResistenceMod.Instance;

            if (mod.ControlsCreated)
                return;

            mod.ControlsCreated = true;

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

            var integrityFieldPowerValueSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCollector>(Control_Prefix + "IntegrityFieldPower");
            integrityFieldPowerValueSlider.Title = MyStringId.GetOrCompute("Integrity Field Power");
            integrityFieldPowerValueSlider.Tooltip = MyStringId.GetOrCompute("Adjusts the amount of Damage Absorbed by the Block");
            integrityFieldPowerValueSlider.SetLimits(MinDivertedPower, MaxDivertedPower);
            integrityFieldPowerValueSlider.Writer = Control_Power_Writer;
            integrityFieldPowerValueSlider.Visible = Control_Visible;
            integrityFieldPowerValueSlider.Getter = Control_Power_Getter;
            integrityFieldPowerValueSlider.Setter = Control_Power_Setter;
            integrityFieldPowerValueSlider.Enabled = Siege_Enabler;
            integrityFieldPowerValueSlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<T>(integrityFieldPowerValueSlider);

            var increaseFieldPower = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(Control_Prefix + "FieldPowerIncrease");
            increaseFieldPower.Name = new StringBuilder("Increase Field Power");
            increaseFieldPower.ValidForGroups = true;
            increaseFieldPower.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            increaseFieldPower.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    if (logic.SiegeModeActivatedClient)
                    {
                        logic.SetPowerStatus($"Cant Change Field Power in Siege Mode", 1500, MyFontEnum.Red);
                        return;
                    }
                    if (logic.dynResistBlock.IsWorking == false)
                    {
                        logic.SetPowerStatus($"Block Disabled", 1500, MyFontEnum.Red);
                        return;
                    }
                    logic.DivertedPower = logic.DivertedPower + 1;
                    logic.DivertedPower = MathHelper.Clamp(logic.DivertedPower, 0f, 30f);
                    logic.Settings.FieldPower = logic.DivertedPower;
                }
            };
            increaseFieldPower.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    sb.Append("Increase");
                }
            };
            increaseFieldPower.InvalidToolbarTypes = new List<MyToolbarType>()
                {
                    MyToolbarType.ButtonPanel,
                    MyToolbarType.Character,
                };
            /*increasePolarization.Enabled = Siege_Enabler;*/
            MyAPIGateway.TerminalControls.AddAction<T>(increaseFieldPower);

            var decreaseFieldPower = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(Control_Prefix + "FieldPowerDecrease");
            decreaseFieldPower.Name = new StringBuilder("Decrease Field Power");
            decreaseFieldPower.ValidForGroups = true;
            decreaseFieldPower.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            decreaseFieldPower.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    if (logic.SiegeModeActivatedClient)
                    {
                        logic.SetPowerStatus($"Cant Change Field Power in Siege Mode", 1500, MyFontEnum.Red);
                        return;
                    }
                    if (logic.dynResistBlock.IsWorking == false)
                    {
                        logic.SetPowerStatus($"Block Disabled", 1500, MyFontEnum.Red);
                        return;
                    }
                    logic.DivertedPower = logic.DivertedPower - 1;
                    logic.DivertedPower = MathHelper.Clamp(logic.DivertedPower, 0f, 30f);
                    logic.Settings.FieldPower = logic.DivertedPower;
                }
            };
            decreaseFieldPower.Writer = (b, sb) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    sb.Append("Decrease");
                }
            };
            decreaseFieldPower.InvalidToolbarTypes = new List<MyToolbarType>()
                {
                    MyToolbarType.ButtonPanel,
                    MyToolbarType.Character,
                };
            /*decreasePolarization.Enabled = Siege_Enabler;*/
            MyAPIGateway.TerminalControls.AddAction<T>(decreaseFieldPower);

            var siegeModeToggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>(Control_Prefix + "siegeToggleAction");
            siegeModeToggleAction.Name = new StringBuilder("Toggle Siege");
            siegeModeToggleAction.ValidForGroups = true;
            siegeModeToggleAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            siegeModeToggleAction.Action = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<DynamicResistLogic>();
                if (logic != null)
                {
                    if (logic.SiegeModeActivatedClient == true)
                    {
                        logic.SetPowerStatus($"Cant Deactivate Siege Mode", 1500, MyFontEnum.Red);
                        return;
                    }
                    else if (logic.SiegeCooldownTimerActive == true)
                    {
                        logic.SetCountdownStatus($"Siege Mode On Cooldown: " + (logic.SiegeCooldownTimer / 60) + " Seconds", 1500, MyFontEnum.Red);
                        return;
                    }
                    if (logic.SiegeModeActivatedClient == false)
                    {
                        logic.SiegeModeActivatedClient = true;
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
            /*siegeModeToggleAction.Enabled = Siege_Cooldown_Enabler;*/
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

                if (dynamicResistLogic.SiegeModeActivatedClient == true)
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

                if (dynamicResistLogic.SiegeCooldownTimerActive == true || dynamicResistLogic.SiegeModeActivatedClient == true)
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

        static float Control_Power_Getter(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            return logic != null ? logic.DivertedPower : 0f;
        }

        static void Control_Power_Setter(IMyTerminalBlock block, float value)
        {
            var logic = GetLogic(block);
            if (logic != null)
                logic.DivertedPower = MathHelper.Clamp(value, 0f, 30f);
                logic.DivertedPower = (float)Math.Round(logic.DivertedPower, 0);
                logic.Settings.FieldPower = logic.DivertedPower;
        }

        static void Control_Power_Writer(IMyTerminalBlock block, StringBuilder writer)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                float value = logic.DivertedPower;
                writer.Append(Math.Round(value, 0, MidpointRounding.ToEven)).Append("%");
            }
        }

        static bool Control_Siege_Getter(IMyTerminalBlock block)
        {
            var logic = GetLogic(block);
            return logic != null ? logic.SiegeModeActivatedClient : false;
        }

        static void Control_Siege_Setter(IMyTerminalBlock block, bool value)
        {
            var logic = GetLogic(block);
            if (logic != null)
            {
                logic.SiegeModeActivatedClient = value;
            }
        }
        #endregion

        private void DisplayMessageToNearPlayers(int msgId)
        {
            List<IMyCharacter> playerCharacters = new List<IMyCharacter>();

            if (dynResistBlock != null)
            {
                var bound = new BoundingSphereD(dynResistBlock.GetPosition(), 50);
                List<IMyEntity> nearEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref bound);

                foreach (var entity in nearEntities)
                {
                    IMyCharacter character = entity as IMyCharacter;
                    if (character != null && character.IsPlayer && bound.Contains(character.GetPosition()) != ContainmentType.Disjoint)
                    {
                        if (msgId == 0)
                        {
                            SetCountdownStatus($"Siege Mode: " + SiegeVisibleTimer + " Seconds", 1500, MyFontEnum.Green);
                        }
                        else if (msgId == 1)
                        {
                            SetCountdownStatus($"Siege Mode Deactivated", 1500, MyFontEnum.Red);
                        }
                        else if (msgId == 2)
                        {
                            SetCountdownStatus($"Block Inoperative! Siege Mode Deactivated", 1500, MyFontEnum.Red);
                        }
                        else
                        {
                            SetCountdownStatus($"Error! Unknown State!", 1500, MyFontEnum.Red);
                            return;
                        }
                            
                    }
                }
            }
        }
    }
}
