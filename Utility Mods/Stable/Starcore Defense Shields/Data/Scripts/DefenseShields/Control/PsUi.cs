namespace DefenseShields
{
    using Sandbox.ModAPI;

    internal static class PsUi
    {
        #region Create UI
        internal static void CreateUi(IMyTerminalBlock planetShield)
        {
            Session.Instance.CreatePlanetShieldElements(planetShield);
            Session.Instance.CreateModulatorUi(planetShield);
            Session.Instance.PsToggleShield.Enabled = block => true;
            Session.Instance.PsToggleShield.Visible = ShowControl;
            Session.Instance.PsHideActiveCheckBox.Enabled = block => true;
            Session.Instance.PsHideActiveCheckBox.Visible = ShowControl;
            Session.Instance.PsRefreshAnimationCheckBox.Enabled = block => true;
            Session.Instance.PsRefreshAnimationCheckBox.Visible = ShowControl;
            Session.Instance.PsHitWaveAnimationCheckBox.Enabled = block => true;
            Session.Instance.PsHitWaveAnimationCheckBox.Visible = ShowControl;
            Session.Instance.PsSendToHudCheckBox.Enabled = block => true;
            Session.Instance.PsSendToHudCheckBox.Visible = ShowControl;
            Session.Instance.PsBatteryBoostCheckBox.Enabled = block => true;
            Session.Instance.PsBatteryBoostCheckBox.Visible = ShowControl;
        }

        internal static bool ShowControl(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<PlanetShields>();
            var valid = comp != null;
            return valid;
        }

        internal static bool GetBatteries(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.UseBatteries ?? false;
        }

        internal static void SetBatteries(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.UseBatteries = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetHideActive(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.ActiveInvisible ?? false;
        }

        internal static void SetHideActive(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.ActiveInvisible = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetRefreshAnimation(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.RefreshAnimation ?? false;
        }

        internal static void SetRefreshAnimation(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.RefreshAnimation = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetHitWaveAnimation(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.HitWaveAnimation ?? false;
        }

        internal static void SetHitWaveAnimation(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.HitWaveAnimation = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetSendToHud(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.SendToHud ?? false;
        }

        internal static void SetSendToHud(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.SendToHud = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetRaiseShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.RaiseShield ?? false;
        }

        internal static void SetRaiseShield(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.RaiseShield = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }
        #endregion
    }
}
