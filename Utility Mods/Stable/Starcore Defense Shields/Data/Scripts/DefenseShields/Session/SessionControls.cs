using VRageMath;

namespace DefenseShields
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Control;
    using Support;
    using Sandbox.Game.Localization;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using VRage.ModAPI;
    using VRage.Utils;

    public partial class Session
    {
        #region UI Config
        public static void AppendConditionToAction<T>(Func<IMyTerminalAction, bool> actionFindCondition, Func<IMyTerminalAction, IMyTerminalBlock, bool> actionEnabledAppend)
        {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<T>(out actions);

            foreach (var a in actions)
            {
                if (actionFindCondition(a))
                {
                    var existingAction = a.Enabled;

                    a.Enabled = (b) => (existingAction?.Invoke(b) ?? true) && actionEnabledAppend(a, b);
                }
            }
        }

        public void CreateControllerElements(IMyTerminalBlock block)
        {
            try
            {
                if (DsControl) return;
                var comp = block?.GameLogic?.GetAs<DefenseShields>();
                HeatSink = TerminalHelpers.AddButton(comp?.Shield, "DS-SinkHeat", Localization.GetText("TerminalHeatSink"), Localization.GetText("TerminalHeatSinkTooltip"), DsUi.HeatSinkAction, DsUi.HeatSinkEnable, DsUi.HeatSinkVis);
                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep0");
                ToggleShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_ToggleShield", Localization.GetText("TerminalToggleShieldTitle"), Localization.GetText("TerminalToggleShieldTooltip"), Localization.GetText("TerminalSwitchUp"), Localization.GetText("TerminalSwitchDown"), DsUi.GetRaiseShield, DsUi.SetRaiseShield);
                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep1");
                //ChargeSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_ChargeRate", "Shield Charge Rate", "Percentage Of Power The Shield May Consume", DsUi.GetRate, DsUi.SetRate);
                //ChargeSlider.SetLimits(20, 95);
                PowerScaleSelect = TerminalHelpers.AddCombobox(comp?.Shield, "DS-C_PowerScale", Localization.GetText("TerminalPowerScaleSelectTitle"), Localization.GetText("TerminalPowerScaleSelectTooltip"), DsUi.GetPowerScale, DsUi.SetPowerScale, DsUi.ListPowerScale);

                PowerWatts = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_PowerWatts", Localization.GetText("TerminalPowerWattsTitle"), Localization.GetText("TerminalPowerWattsTooltip"), DsUi.GetPowerWatts, DsUi.SetPowerWatts,  DsUi.EnablePowerWatts);
                PowerWatts.SetLimits(1, 999);
                if (comp != null && comp.GridIsMobile)
                {
                    TerminalHelpers.Separator(comp.Shield, "DS-C_sep2");
                }

                Fit = TerminalHelpers.AddSlider(comp?.Shield, "DS-CFit", Localization.GetText("TerminalFitTitle"), Localization.GetText("TerminalFitTooltip"), DsUi.GetFit, DsUi.SetFit);
                Fit.SetLimits(0, 22);

                SphereFit = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_SphereFit", Localization.GetText("TerminalSphereFitTitle"), Localization.GetText("TerminalSphereFitTooltip"), DsUi.GetSphereFit, DsUi.SetSphereFit);
                FortifyShield = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_ShieldFortify", Localization.GetText("TerminalFortifyShieldTitle"), Localization.GetText("TerminalFortifyShieldTooltip"), DsUi.GetFortify, DsUi.SetFortify);

                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep3");
                
                WidthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_WidthSlider", Localization.GetText("TerminalWidthSliderTitle"), Localization.GetText("TerminalWidthSliderTooltip"), DsUi.GetWidth, DsUi.SetWidth);
                WidthSlider.SetLimits(30, 1000);

                HeightSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_HeightSlider", Localization.GetText("TerminalHeightSliderTitle"), Localization.GetText("TerminalHeightSliderTooltip"), DsUi.GetHeight, DsUi.SetHeight);
                HeightSlider.SetLimits(30, 1000);

                DepthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_DepthSlider", Localization.GetText("TerminalDepthSliderTitle"), Localization.GetText("TerminalDepthSliderTooltip"), DsUi.GetDepth, DsUi.SetDepth);
                DepthSlider.SetLimits(30, 1000);

                OffsetWidthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_OffsetWidthSlider", Localization.GetText("TerminalOffsetWidthSliderTitle"), Localization.GetText("TerminalOffsetWidthSliderTooltip"), DsUi.GetOffsetWidth, DsUi.SetOffsetWidth);
                OffsetWidthSlider.SetLimits(-69, 69);

                OffsetHeightSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_OffsetHeightSlider", Localization.GetText("TerminalOffsetHeightSliderTitle"), Localization.GetText("TerminalOffsetHeightSliderTooltip"), DsUi.GetOffsetHeight, DsUi.SetOffsetHeight);
                OffsetHeightSlider.SetLimits(-69, 69);

                OffsetDepthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_OffsetDepthSlider", Localization.GetText("TerminalOffsetDepthSliderTitle"), Localization.GetText("TerminalOffsetDepthSliderTooltip"), DsUi.GetOffsetDepth, DsUi.SetOffsetDepth);
                OffsetDepthSlider.SetLimits(-69, 69);

                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep4");

                BatteryBoostCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_UseBatteries", Localization.GetText("TerminalBatteryBoostCheckBoxTitle"), Localization.GetText("TerminalBatteryBoostCheckBoxTooltip"), DsUi.GetBatteries, DsUi.SetBatteries);
                SendToHudCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_HideIcon", Localization.GetText("TerminalSendToHudCheckBoxTitle"), Localization.GetText("TerminalSendToHudCheckBoxTooltip"), DsUi.GetSendToHud, DsUi.SetSendToHud);
                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep5");
                ShellSelect = TerminalHelpers.AddCombobox(comp?.Shield, "DS-C_ShellSelect", Localization.GetText("TerminalShellSelectTitle"), Localization.GetText("TerminalShellSelectTooltip"), DsUi.GetShell, DsUi.SetShell, DsUi.ListShell);

                ShellVisibility = TerminalHelpers.AddCombobox(comp?.Shield, "DS-C_ShellSelect", Localization.GetText("TerminalShellVisibilityTitle"), Localization.GetText("TerminalShellVisibilityTooltip"), DsUi.GetVisible, DsUi.SetVisible, DsUi.ListVisible);

                HideActiveCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_HideActive", Localization.GetText("TerminalHideActiveCheckBoxTitle"), Localization.GetText("TerminalHideActiveCheckBoxTooltip"), DsUi.GetHideActive, DsUi.SetHideActive);

                //RefreshAnimationCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_RefreshAnimation", "Show Refresh Animation  ", "Show Random Refresh Animation", DsUi.GetRefreshAnimation, DsUi.SetRefreshAnimation);
                //HitWaveAnimationCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_HitWaveAnimation", "Show Hit Wave Animation", "Show Wave Effect On Shield Damage", DsUi.GetHitWaveAnimation, DsUi.SetHitWaveAnimation);
                NoWarningSoundsCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_NoWarningSounds", Localization.GetText("TerminalNoWarningSoundsCheckBoxTitle"), Localization.GetText("TerminalNoWarningSoundsCheckBoxTooltip"), DsUi.GetNoWarningSounds, DsUi.SetNoWarningSounds);
                DimShieldHitsCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_DimShieldHits", Localization.GetText("TerminalDimShieldHitsCheckBoxTitle"), Localization.GetText("TerminalDimShieldHitsCheckBoxTooltip"), DsUi.GetDimShieldHits, DsUi.SetDimShieldHits);

                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep6");
                SideShunting = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_SideRedirect", Localization.GetText("TerminalSideShuntingTitle"), Localization.GetText("TerminalSideShuntingTooltip"), DsUi.GetSideShunting, DsUi.SetSideShunting);
                ShowShunting = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_ShowRedirect", Localization.GetText("TerminalShowShuntingTitle"), Localization.GetText("TerminalShowShuntingTooltip"), DsUi.GetShowShunting, DsUi.SetShowShunting);

                TopShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_TopShield", Localization.GetText("TerminalTopShieldTitle"), Localization.GetText("TerminalTopShieldTooltip"), Localization.GetText("TerminalSwitchPush"), Localization.GetText("TerminalSwitchPull"), DsUi.GeTopShield, DsUi.SetTopShield, DsUi.RedirectEnabled);
                BottomShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_BottomShield", Localization.GetText("TerminalBottomShieldTitle"), Localization.GetText("TerminalBottomShieldTooltip"), Localization.GetText("TerminalSwitchPush"), Localization.GetText("TerminalSwitchPull"), DsUi.GetBottomShield, DsUi.SetBottomShield, DsUi.RedirectEnabled);
                LeftShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_LeftShield", Localization.GetText("TerminalLeftShieldTitle"), Localization.GetText("TerminalLeftShieldTooltip"), Localization.GetText("TerminalSwitchPush"), Localization.GetText("TerminalSwitchPull"), DsUi.GetLeftShield, DsUi.SetLeftShield, DsUi.RedirectEnabled);
                RightShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_RightShield", Localization.GetText("TerminalRightShieldTitle"), Localization.GetText("TerminalRightShieldTooltip"), Localization.GetText("TerminalSwitchPush"), Localization.GetText("TerminalSwitchPull"), DsUi.GetRightShield, DsUi.SetRightShield, DsUi.RedirectEnabled);
                FrontShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_FrontShield", Localization.GetText("TerminalFrontShieldTitle"), Localization.GetText("TerminalFrontShieldTooltip"), Localization.GetText("TerminalSwitchPush"), Localization.GetText("TerminalSwitchPull"), DsUi.GetFrontShield, DsUi.SetFrontShield, DsUi.RedirectEnabled);
                BackShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_BackShield", Localization.GetText("TerminalBackShieldTitle"), Localization.GetText("TerminalBackShieldTooltip"), Localization.GetText("TerminalSwitchPush"), Localization.GetText("TerminalSwitchPull"), DsUi.GetBackShield, DsUi.SetBackShield, DsUi.RedirectEnabled);


                ControllerCreateAction<IMyUpgradeModule>(HeatSink);


                ControllerCreateAction<IMyUpgradeModule>(ToggleShield);

                ControllerCreateAction<IMyUpgradeModule>(SphereFit);
                ControllerCreateFitAction<IMyUpgradeModule>(Fit);
                ControllerCreateAction<IMyUpgradeModule>(FortifyShield);


                ControllerCreateAction<IMyUpgradeModule>(HideActiveCheckBox);
                //CreateAction<IMyUpgradeModule>(RefreshAnimationCheckBox);
                //CreateAction<IMyUpgradeModule>(HitWaveAnimationCheckBox);
                ControllerCreateAction<IMyUpgradeModule>(SendToHudCheckBox);
                ControllerCreateAction<IMyUpgradeModule>(BatteryBoostCheckBox);
                CreateDepthAction<IMyUpgradeModule>(DepthSlider);
                CreateWidthAction<IMyUpgradeModule>(WidthSlider);
                CreateHeightAction<IMyUpgradeModule>(HeightSlider);

                CreateShuntAction<IMyUpgradeModule>(TopShield);
                CreateShuntAction<IMyUpgradeModule>(BottomShield);
                CreateShuntAction<IMyUpgradeModule>(LeftShield);
                CreateShuntAction<IMyUpgradeModule>(RightShield);
                CreateShuntAction<IMyUpgradeModule>(FrontShield);
                CreateShuntAction<IMyUpgradeModule>(BackShield);

                ControllerCreateAction<IMyUpgradeModule>(SideShunting);

                DsControl = true;
            }
            catch (Exception ex) { Log.Line($"Exception in CreateControlerUi: {ex}"); }
        }

        internal static void TerminalDepthIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetDepth(blk);
            var nextValue = currentValue + 1 < max ? currentValue + 1 : currentValue; 
            DsUi.SetOffsetDepth(blk, nextValue);
        }

        internal static void TerminalActionDepthDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetDepth(blk);
            var nextValue = currentValue - 1 > -max ? currentValue - 1 : currentValue;
            DsUi.SetOffsetDepth(blk, nextValue);
        }

        internal static void TerminalActioWidthIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;
            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetWidth(blk);
            var nextValue = currentValue + 1 < max ? currentValue + 1 : currentValue;
            DsUi.SetOffsetWidth(blk, nextValue);
        }

        internal static void TerminalActionWidthDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetWidth(blk);
            var nextValue = currentValue - 1 > -max ? currentValue - 1 : currentValue;
            DsUi.SetOffsetWidth(blk, nextValue);
        }

        internal static void TerminalActioHeightIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetHeight(blk);
            var nextValue = currentValue + 1 < max ? currentValue + 1 : currentValue;
            DsUi.SetOffsetHeight(blk, nextValue);
        }

        internal static void TerminalActionHeightDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetHeight(blk);
            var nextValue = currentValue - 1 > -max ? currentValue - 1 : currentValue;
            DsUi.SetOffsetHeight(blk, nextValue);
        }

        internal static void TerminalActioFitSizeIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var currentValue = DsUi.GetFit(blk);
            var nextValue = currentValue + 1 < Instance.Fits.Length ? currentValue + 1 : currentValue;
            DsUi.SetFit(blk, nextValue);
        }

        internal static void TerminalActionFitSizeDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var currentValue = DsUi.GetFit(blk);
            var nextValue = currentValue - 1 >= 0 ? currentValue - 1 : currentValue;
            DsUi.SetFit(blk, nextValue);
        }

        internal static bool HasShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null;
        }

        internal static void FitSizeWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(comp.DsSet.Settings.Fit);
        }

        internal static void DepthWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(DsUi.GetOffsetDepth(blk));
        }

        internal static void WidthWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(DsUi.GetOffsetWidth(blk));
        }

        internal static void HeightWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(DsUi.GetOffsetHeight(blk));
        }

        internal static bool IsController(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            return comp != null;
        }

        internal static bool IsModulator(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<Modulators>();
            return comp != null;
        }

        public void CreateDepthAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_DepthIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionDepthIncrease"));
            action0.Action = TerminalDepthIncrease;
            action0.Writer = DepthWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_DepthDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionDepthDecrease"));
            action1.Action = TerminalActionDepthDecrease;
            action1.Writer = DepthWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }

        public void CreateWidthAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_WidthIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionWidthIncrease"));
            action0.Action = TerminalActioWidthIncrease;
            action0.Writer = WidthWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_WidthDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionWidthDecrease"));
            action1.Action = TerminalActionWidthDecrease;
            action1.Writer = WidthWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }

        public void CreateHeightAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_HeightIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionHeightIncrease"));
            action0.Action = TerminalActioHeightIncrease;
            action0.Writer = HeightWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_HeightDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionHeightDecrease"));
            action1.Action = TerminalActionHeightDecrease;
            action1.Writer = HeightWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }

        public void ControllerCreateFitAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_FitIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder(Localization.GetText("ActionFitIncrease"));
            action0.Action = TerminalActioFitSizeIncrease;
            action0.Writer = FitSizeWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_FitDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder(Localization.GetText("ActionFitDecrease"));
            action1.Action = TerminalActionFitSizeDecrease;
            action1.Writer = FitSizeWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }


        public void CreatePlanetShieldElements(IMyTerminalBlock block)
        {
            try
            {
            }
            catch (Exception ex) { Log.Line($"Exception in CreateControlerUi: {ex}"); }
        }

        public void CreateModulatorUi(IMyTerminalBlock block)
        {
            try
            {
                if (ModControl) return;
                var comp = block?.GameLogic?.GetAs<Modulators>();
                ModSep1 = TerminalHelpers.Separator(comp?.Modulator, "DS-M_sep1");
                ModDamage = TerminalHelpers.AddModSlider(comp?.Modulator, "DS-M_DamageModulation", Localization.GetText("TerminalModDamageTitle"), Localization.GetText("TerminalModDamageTooltip"), ModUi.GetDamage, ModUi.SetDamage, null, null, ModUi.ModWriter);
                ModDamage.SetLimits(-200, 200);
                ModSep2 = TerminalHelpers.Separator(comp?.Modulator, "DS-M_sep2");
                ModReInforce = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateReInforceProt", Localization.GetText("TerminalModReInforceTitle"), Localization.GetText("TerminalModReInforceTooltip"), ModUi.GetReInforceProt, ModUi.SetReInforceProt);
                PassiveModulation = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_PassiveModulation", Localization.GetText("TerminalPassiveModulationTitle"), Localization.GetText("TerminalPassiveModulationTooltip"), ModUi.GetPassiveModulation, ModUi.SetPassiveModulation);
                ModVoxels = TerminalHelpers.AddCheckbox(comp?.Modulator, " DS-M_ModulateVoxels", Localization.GetText("TerminalModVoxelsTitle"), Localization.GetText("TerminalModVoxelsTooltip"), ModUi.GetVoxels, ModUi.SetVoxels);
                ModGrids = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateGrids", Localization.GetText("TerminalModGridsTitle"), Localization.GetText("TerminalModGridsTooltip"), ModUi.GetGrids, ModUi.SetGrids);
                ModAllies = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateAllies", Localization.GetText("TerminalModAlliesTitle"), Localization.GetText("TerminalModAlliesTooltip"), ModUi.GetAllies, ModUi.SetAllies);
                ModEmp = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateEmpProt", Localization.GetText("TerminalModEmpTitle"), Localization.GetText("TerminalModEmpTooltip"), ModUi.GetEmpProt, ModUi.SetEmpProt);

                CreateActionDamageModRate<IMyUpgradeModule>(ModDamage);

                CreateAction<IMyUpgradeModule>(ModVoxels);
                CreateAction<IMyUpgradeModule>(ModGrids);
                CreateAction<IMyUpgradeModule>(PassiveModulation);
                CreateAction<IMyUpgradeModule>(ModEmp);
                CreateAction<IMyUpgradeModule>(ModReInforce);
                ModControl = true;
            }
            catch (Exception ex) { Log.Line($"Exception in CreateModulatorUi: {ex}"); }
        }

        public void CreateO2GeneratorUi(IMyTerminalBlock block)
        {
            try
            {
                if (O2Control) return;
                var comp = block?.GameLogic?.GetAs<O2Generators>();
                O2DoorFix = TerminalHelpers.AddCheckbox(comp?.O2Generator, "DS-FixRoomPressure", "Keen-Bug, Fix Room Pressure", "Keen-Bug, Fix Room Pressure", O2Ui.FixStatus, O2Ui.FixRooms);
                O2Control = true;
            }
            catch (Exception ex) { Log.Line($"Exception in CreateO2GeneratorUi: {ex}"); }
        }

        public void ControllerCreateAction<T>(IMyTerminalControlOnOffSwitch c)
        {
            try
            {
                var id = ((IMyTerminalControl)c).Id;
                var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                Action<IMyTerminalBlock, StringBuilder> writer = (b, s) => s.Append(c.Getter(b) ? c.OnText : c.OffText);
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Toggle");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append(c.OnText.String).Append("/").Append(c.OffText.String);

                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\SmallShipToggle.dds";

                    a.ValidForGroups = true;
                    a.Enabled = IsController;
                    a.Action = (b) => c.Setter(b, !c.Getter(b));
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_On");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append(c.OnText.String);
                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\SmallShipSwitchOn.dds";
                    a.ValidForGroups = true;
                    a.Enabled = IsController;
                    a.Action = (b) => c.Setter(b, true);
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Off");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append(c.OffText.String);
                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\LargeShipSwitchOn.dds";
                    a.ValidForGroups = true;
                    a.Enabled = IsController;
                    a.Action = (b) => c.Setter(b, false);
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateAction: {ex}"); }
        }

        public void CreateShuntAction<T>(IMyTerminalControlOnOffSwitch c)
        {
            try
            {
                var id = ((IMyTerminalControl)c).Id;
                var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                Action<IMyTerminalBlock, StringBuilder> writer = (b, s) => s.Append(c.Getter(b) ? c.OnText : c.OffText);
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_ShuntToggle");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append($" {Localization.GetText("TerminalSwitchPush")}").Append(" /").Append($" {Localization.GetText("TerminalSwitchPull")}");

                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\SmallShipToggle.dds";

                    a.ValidForGroups = true;
                    a.Enabled = IsController;
                    a.Action = (b) => c.Setter(b, !c.Getter(b));
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_ShuntOn");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append($" {Localization.GetText("TerminalSwitchPush")}");
                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\SmallShipSwitchOn.dds";
                    a.ValidForGroups = true;
                    a.Enabled = IsController;
                    a.Action = (b) => c.Setter(b, true);
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_ShuntOff");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append($" {Localization.GetText("TerminalSwitchPull")}");
                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\LargeShipSwitchOn.dds";
                    a.ValidForGroups = true;
                    a.Enabled = IsController;
                    a.Action = (b) => c.Setter(b, false);
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateAction: {ex}"); }
        }


        private void CustomControls(IMyTerminalBlock tBlock, List<IMyTerminalControl> myTerminalControls)
        {
            try
            {
                LastTerminalId = tBlock.EntityId;
                switch (tBlock.BlockDefinition.SubtypeId)
                {
                    case "LargeShieldModulator":
                    case "SmallShieldModulator":
                        SetCustomDataToPassword(myTerminalControls);
                        break;
                    case "DSControlLarge":
                    case "DSControlSmall":
                    case "DSControlTable":
                        SetCustomDataToShieldFreq(myTerminalControls);
                        break;
                    default:
                        if (!CustomDataReset) ResetCustomData(myTerminalControls);
                        break;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CustomControls: {ex}"); }
        }


        public void BlockTagActive(IMyTerminalBlock tBlock)
        {
            var customName = tBlock.CustomName;
            if (customName.StartsWith("[A] ")) return;
            if (customName.StartsWith("[B] "))
            {
                customName = customName.Remove(0, 4);
                customName = "[A] " + customName;
            }
            else
            {
                customName = "[A] " + customName;
            }
            tBlock.CustomName = customName;
        }

        public void BlockTagBackup(IMyTerminalBlock tBlock)
        {
            var customName = tBlock.CustomName;
            if (customName.StartsWith("[B] ")) return;
            if (customName.StartsWith("[A] "))
            {
                customName = customName.Remove(0, 4);
                customName = "[B] " + customName;
            }
            else
            {
                customName = "[B] " + customName;
            }
            tBlock.CustomName = customName;
        }

        private void SetCustomDataToPassword(IEnumerable<IMyTerminalControl> controls)
        {
            var customData = controls.First((x) => x.Id.ToString() == "CustomData");
            ((IMyTerminalControlTitleTooltip)customData).Title = Password;
            ((IMyTerminalControlTitleTooltip)customData).Tooltip = PasswordTooltip;
            customData.RedrawControl();
            CustomDataReset = false;
        }

        private void SetCustomDataToShieldFreq(IEnumerable<IMyTerminalControl> controls)
        {
            var customData = controls.First((x) => x.Id.ToString() == "CustomData");
            ((IMyTerminalControlTitleTooltip)customData).Title = ShieldFreq;
            ((IMyTerminalControlTitleTooltip)customData).Tooltip = ShieldFreqTooltip;
            customData.RedrawControl();
            CustomDataReset = false;
        }

        private void ResetCustomData(IEnumerable<IMyTerminalControl> controls)
        {
            var customData = controls.First((x) => x.Id.ToString() == "CustomData");
            ((IMyTerminalControlTitleTooltip)customData).Title = MySpaceTexts.Terminal_CustomData;
            ((IMyTerminalControlTitleTooltip)customData).Tooltip = MySpaceTexts.Terminal_CustomDataTooltip;
            customData.RedrawControl();
            CustomDataReset = true;
        }

        private void CreateAction<T>(IMyTerminalControlCheckbox c,
            bool addToggle = true,
            bool addOnOff = false,
            string iconPack = null,
            string iconToggle = null,
            string iconOn = null,
            string iconOff = null)
        {
            try
            {

                var id = ((IMyTerminalControl)c).Id;
                var name = c.Title.String;
                Action<IMyTerminalBlock, StringBuilder> writer = (b, s) => s.Append(c.Getter(b) ? c.OnText : c.OffText);

                if (iconToggle == null && iconOn == null && iconOff == null)
                {
                    var pack = iconPack ?? string.Empty;
                    var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                    iconToggle = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "Toggle.dds";
                    iconOn = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "SwitchOn.dds";
                    iconOff = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "SwitchOff.dds";
                }

                if (addToggle)
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Toggle");
                    a.Name = new StringBuilder(name).Append(" On/Off");
                    a.Icon = iconToggle;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;

                    a.Action = (b) => c.Setter(b, !c.Getter(b));
                    if (writer != null)
                        a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }

                if (addOnOff)
                {
                    {
                        var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_On");
                        a.Name = new StringBuilder(name).Append(" On");
                        a.Icon = iconOn;
                        a.ValidForGroups = true;
                        a.Action = (b) => c.Setter(b, true);
                        a.Enabled = IsModulator;
                        if (writer != null)
                            a.Writer = writer;

                        MyAPIGateway.TerminalControls.AddAction<T>(a);
                    }
                    {
                        var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Off");
                        a.Name = new StringBuilder(name).Append(" Off");
                        a.Icon = iconOff;
                        a.ValidForGroups = true;
                        a.Enabled = IsModulator;
                        a.Action = (b) => c.Setter(b, false);
                        if (writer != null)
                            a.Writer = writer;

                        MyAPIGateway.TerminalControls.AddAction<T>(a);
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateAction<T>(IMyTerminalControlCheckbox: {ex}"); }
        }


        private void ControllerCreateAction<T>(IMyTerminalControlCheckbox c,
    bool addToggle = true,
    bool addOnOff = false,
    string iconPack = null,
    string iconToggle = null,
    string iconOn = null,
    string iconOff = null)
        {
            try
            {

                var id = ((IMyTerminalControl)c).Id;
                var name = c.Title.String;
                Action<IMyTerminalBlock, StringBuilder> writer = (b, s) => s.Append(c.Getter(b) ? c.OnText : c.OffText);

                if (iconToggle == null && iconOn == null && iconOff == null)
                {
                    var pack = iconPack ?? string.Empty;
                    var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                    iconToggle = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "Toggle.dds";
                    iconOn = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "SwitchOn.dds";
                    iconOff = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "SwitchOff.dds";
                }

                if (addToggle)
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Toggle");
                    a.Name = new StringBuilder(name).Append(" On/Off");
                    a.Icon = iconToggle;
                    a.ValidForGroups = true;
                    a.Enabled = IsController;
                    a.Action = (b) => c.Setter(b, !c.Getter(b));
                    if (writer != null)
                        a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }

                if (addOnOff)
                {
                    {
                        var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_On");
                        a.Name = new StringBuilder(name).Append(" On");
                        a.Icon = iconOn;
                        a.ValidForGroups = true;
                        a.Enabled = IsController;
                        a.Action = (b) => c.Setter(b, true);
                        if (writer != null)
                            a.Writer = writer;

                        MyAPIGateway.TerminalControls.AddAction<T>(a);
                    }
                    {
                        var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Off");
                        a.Name = new StringBuilder(name).Append(" Off");
                        a.Icon = iconOff;
                        a.ValidForGroups = true;
                        a.Enabled = IsController;
                        a.Action = (b) => c.Setter(b, false);
                        if (writer != null)
                            a.Writer = writer;

                        MyAPIGateway.TerminalControls.AddAction<T>(a);
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateAction<T>(IMyTerminalControlCheckbox: {ex}"); }
        }

        private void ControllerCreateAction<T>(IMyTerminalControlButton c, bool addToggle = true)
        {
            try
            {

                var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                var iconToggle = gamePath + @"\Textures\GUI\Icons\Actions\" + "Toggle.dds";

                var id = c.Id;
                var name = c.Title.String;
                //Action<IMyTerminalBlock, StringBuilder> writer = (b, s) => s.Append(c.Getter(b) ? c.OnText : c.OffText);

                if (addToggle)
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Toggle");
                    a.Name = new StringBuilder(name);

                    a.ValidForGroups = true;
                    a.Enabled = DsUi.HeatSinkEnable;
                    a.Action = DsUi.HeatSinkAction;
                    a.Icon = iconToggle;
                    //a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }

            }
            catch (Exception ex) { Log.Line($"Exception in CreateAction<T>(IMyTerminalControlButton {ex}"); }
        }

        private void CreateActionChargeRate<T>(IMyTerminalControlSlider c,
            float defaultValue = 50f, // HACK terminal controls don't have a default value built in...
            float modifier = 1f,
            string iconReset = null,
            string iconIncrease = null,
            string iconDecrease = null,
            bool gridSizeDefaultValue = false) // hacky quick way to get a dynamic default value depending on grid size)
        {
            try
            {
                var id = ((IMyTerminalControl)c).Id;
                var name = c.Title.String;

                if (iconReset == null && iconIncrease == null && iconDecrease == null)
                {
                    var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                    iconReset = gamePath + @"\Textures\GUI\Icons\Actions\Reset.dds";
                    iconIncrease = gamePath + @"\Textures\GUI\Icons\Actions\Increase.dds";
                    iconDecrease = gamePath + @"\Textures\GUI\Icons\Actions\Decrease.dds";
                }

                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Reset");
                    a.Name = new StringBuilder("Default ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append(defaultValue.ToString("0.###")).Append(")");
                    a.Icon = iconReset;
                    a.ValidForGroups = true;
                    a.Action = (b) => c.Setter(b, (gridSizeDefaultValue ? b.CubeGrid.GridSize : defaultValue));
                    a.Writer = (b, s) => s.Append(c.Getter(b));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Increase");
                    a.Name = new StringBuilder("Increase ").Append(name).Append(" (+").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconIncrease;
                    a.ValidForGroups = true;
                    a.Action = ActionAddChargeRate;
                    a.Writer = (b, s) => s.Append(c.Getter(b));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Decrease");
                    a.Name = new StringBuilder("Decrease ").Append(name).Append(" (-").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconDecrease;
                    a.ValidForGroups = true;
                    a.Action = ActionSubtractChargeRate;
                    a.Writer = (b, s) => s.Append(c.Getter(b).ToString("0.###"));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateActionChargeRate: {ex}"); }
        }

        private void ActionAddChargeRate(IMyTerminalBlock b)
        {
            try
            {
                List<IMyTerminalControl> controls;
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var chargeRate = controls.First((x) => x.Id.ToString() == "DS-C_ChargeRate");
                var c = (IMyTerminalControlSlider)chargeRate;
                if (c.Getter(b) > 94)
                {
                    c.Setter(b, 95f);
                    return;
                }
                c.Enabled = IsModulator;
                c.Setter(b, c.Getter(b) + 5f);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionSubtractChargeRate: {ex}"); }
        }

        private void ActionSubtractChargeRate(IMyTerminalBlock b)
        {
            try
            {
                var controls = new List<IMyTerminalControl>();
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var chargeRate = controls.First((x) => x.Id.ToString() == "DS-C_ChargeRate");
                var c = (IMyTerminalControlSlider)chargeRate;
                if (c.Getter(b) < 21)
                {
                    c.Setter(b, 20f);
                    return;
                }
                c.Enabled = IsModulator;
                c.Setter(b, c.Getter(b) - 5f);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionSubtractChargeRate: {ex}"); }
        }

        private void CreateActionDamageModRate<T>(IMyTerminalControlSlider c,
        float defaultValue = 0, // HACK terminal controls don't have a default value built in...
        float modifier = 20f,
        string iconReset = null,
        string iconIncrease = null,
        string iconDecrease = null,
        bool gridSizeDefaultValue = false) // hacky quick way to get a dynamic default value depending on grid size)
        {
            try
            {
                var id = ((IMyTerminalControl)c).Id;
                var name = c.Title.String;

                if (iconReset == null && iconIncrease == null && iconDecrease == null)
                {
                    var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                    iconReset = gamePath + @"\Textures\GUI\Icons\Actions\Reset.dds";
                    iconIncrease = gamePath + @"\Textures\GUI\Icons\Actions\Increase.dds";
                    iconDecrease = gamePath + @"\Textures\GUI\Icons\Actions\Decrease.dds";
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_KiFull");
                    a.Name = new StringBuilder($"{Localization.GetText("FullKinetic")} ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append(300.ToString("0.###")).Append(")");
                    a.Icon = iconReset;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;
                    a.Action = (b) => c.Setter(b,  200);
                    a.Writer = ModUi.ModWriter;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_EnFull");
                    a.Name = new StringBuilder($"{Localization.GetText("FullEnergy")} ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append((-300).ToString("0.###")).Append(")");
                    a.Icon = iconIncrease;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;
                    a.Action = (b) => c.Setter(b,  -200);
                    a.Writer = ModUi.ModWriter;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }

                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_KiHalf");
                    a.Name = new StringBuilder($"{Localization.GetText("HalfKinetic")} ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append(200.ToString("0.###")).Append(")");
                    a.Icon = iconReset;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;
                    a.Action = (b) => c.Setter(b, 100);
                    a.Writer = ModUi.ModWriter;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_EnHalf");
                    a.Name = new StringBuilder($"{Localization.GetText("HalfEnergy")} ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append((-200).ToString("0.###")).Append(")");
                    a.Icon = iconIncrease;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;
                    a.Action = (b) => c.Setter(b, -100);
                    a.Writer = ModUi.ModWriter;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }

                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Reset");
                    a.Name = new StringBuilder($"{Localization.GetText("ActionDefault")} ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append(defaultValue.ToString("0.###")).Append(")");
                    a.Icon = iconIncrease;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;
                    a.Action = (b) => c.Setter(b, defaultValue);
                    a.Writer = ModUi.ModWriter;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Increase");
                    a.Name = new StringBuilder($"{Localization.GetText("ActionIncrease")} ").Append(name).Append(" (+").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconIncrease;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;
                    a.Action = ActionAddDamageMod;
                    a.Writer = ModUi.ModWriter;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Decrease");
                    a.Name = new StringBuilder($"{Localization.GetText("ActionDecrease")} ").Append(name).Append(" (-").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconDecrease;
                    a.ValidForGroups = true;
                    a.Enabled = IsModulator;
                    a.Action = ActionSubtractDamageMod;
                    a.Writer = ModUi.ModWriter;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateActionDamageModRate: {ex}"); }
        }

        internal void ActionAddDamageMod(IMyTerminalBlock b)
        {
            try
            {
                List<IMyTerminalControl> controls;
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var damageMod = controls.First((x) => x.Id.ToString() == "DS-M_DamageModulation");
                var c = (IMyTerminalControlSlider)damageMod;
                var newValue = MathHelper.Clamp((Math.Round((c.Getter(b) + 20) / 20) * 20), -200, 200);
                c.Enabled = IsModulator;
                c.Setter(b, (float) newValue);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionAddDamageMod: {ex}"); }
        }

        internal void ActionSubtractDamageMod(IMyTerminalBlock b)
        {
            try
            {
                List<IMyTerminalControl> controls;
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var chargeRate = controls.First((x) => x.Id.ToString() == "DS-M_DamageModulation");
                var c = (IMyTerminalControlSlider)chargeRate;
                var newValue = MathHelper.Clamp((Math.Round((c.Getter(b) - 20) / 20) * 20), -200, 200);;
                c.Enabled = IsModulator;
                c.Setter(b, (float) newValue);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionSubtractDamageMod: {ex}"); }
        }

        private void CreateActionCombobox<T>(IMyTerminalControlCombobox c,
            string[] itemIds = null,
            string[] itemNames = null,
            string icon = null)
        {
            var items = new List<MyTerminalControlComboBoxItem>();
            c.ComboBoxContent.Invoke(items);

            foreach (var item in items)
            {
                var id = itemIds == null ? item.Value.String : itemIds[item.Key];

                if (id == null)
                    continue; // item id is null intentionally in the array, this means "don't add action".

                var a = MyAPIGateway.TerminalControls.CreateAction<T>(id);
                a.Name = new StringBuilder(itemNames == null ? item.Value.String : itemNames[item.Key]);
                if (icon != null)
                    a.Icon = icon;
                a.ValidForGroups = true;
                a.Action = (b) => c.Setter(b, item.Key);
                //if(writer != null)
                //    a.Writer = writer;

                MyAPIGateway.TerminalControls.AddAction<T>(a);
            }
        }
        #endregion
    }
}
