using System;
using System.Globalization;
using VRage.Utils;

namespace DefenseShields
{
    using Support;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using System.Text;

    internal static class ModUi
    {
        #region Create UI
        internal static void CreateUi(IMyTerminalBlock modualator)
        {
            Session.Instance.CreateModulatorUi(modualator);
            Session.Instance.ModDamage.Enabled = block => true;
            Session.Instance.ModDamage.Visible = ShowControl;
            Session.Instance.ModVoxels.Enabled = block => true;
            Session.Instance.ModVoxels.Visible = ShowVoxels;
            Session.Instance.ModGrids.Enabled = block => true;
            Session.Instance.ModGrids.Visible = ShowControl;
            Session.Instance.ModAllies.Enabled = block => true;
            Session.Instance.ModAllies.Visible = ShowControl;
            Session.Instance.PassiveModulation.Enabled = block => true;
            Session.Instance.PassiveModulation.Visible = ShowControl;
            Session.Instance.ModEmp.Enabled = block => false;
            Session.Instance.ModEmp.Visible = ShowEMP;
            Session.Instance.ModReInforce.Enabled = block => true;
            Session.Instance.ModReInforce.Visible = ShowReInforce;
            Session.Instance.ModSep1.Visible = ShowControl;
            Session.Instance.ModSep2.Visible = ShowControl;
        }

        internal static bool ShowControl(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            var station = comp != null;
            return station;
        }


        internal static void ModWriter(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp?.ModSet?.Settings == null || MyUtils.IsZero(comp.ModSet.Settings.ModulateDamage, 1E-02F))
            {
                stringBuilder.Append("balanced");
                return;
            }

            var modDamage = comp.ModSet.Settings.ModulateDamage;
            var value = modDamage > 0 ? (comp.ModState.State.ModulateEnergy * 100) : (comp.ModState.State.ModulateKinetic * 100);
            var damageType = MyUtils.IsZero(modDamage, 1E-02F) ? "balanced" : modDamage > 0 ? "kinetic" : "energy";
            var damage = Math.Round(Math.Abs(value), 2);
            stringBuilder.Append(damage.ToString(CultureInfo.InvariantCulture) + "% " + damageType);
        }

        internal static float GetDamage(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            return comp?.ModSet.Settings.ModulateDamage ?? 0;
        }

        internal static void SetDamage(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp == null || newValue > 200 || newValue < -200) return;

            if (Session.Instance.IsServer) 
                ComputeDamage(comp, newValue);
            
            comp.ModSet.Settings.ModulateDamage = (int)newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            ((MyCubeBlock)block).UpdateTerminal();
        }

        internal static void ComputeDamage(Modulators comp, float input)
        {
            var value = Math.Abs(input);
            if (!MyUtils.IsZero(value))
            {
                value = (float) (Math.Round(value / 20.0) * 20);
                var first = (value + 100) / 100;
                var second = 1 / first;
                if (input < 0)
                {
                    comp.ModState.State.ModulateEnergy = -second;
                    comp.ModState.State.ModulateKinetic = -first;
                }
                else if (input > 0)
                {
                    comp.ModState.State.ModulateEnergy = first;
                    comp.ModState.State.ModulateKinetic = second;
                }
            }
            else {
                comp.ModState.State.ModulateEnergy = 1;
                comp.ModState.State.ModulateKinetic = 1;
            }
        }

        internal static bool ShowVoxels(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp?.ShieldComp?.DefenseShields == null || comp.ShieldComp.DefenseShields.IsStatic) return false;

            return comp.ModState.State.Link;
        }

        internal static bool GetVoxels(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            return comp?.ModSet.Settings.ModulateVoxels ?? false;
        }

        internal static void SetVoxels(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp == null) return;
            comp.ModSet.Settings.ModulateVoxels = newValue;
            comp.ModSet.NetworkUpdate();
            comp.ModSet.SaveSettings();
        }

        internal static bool GetGrids(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            return comp?.ModSet.Settings.ModulateGrids ?? false;
        }

        internal static void SetGrids(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp == null) return;
            comp.ModSet.Settings.ModulateGrids = newValue;
            comp.ModSet.NetworkUpdate();
            comp.ModSet.SaveSettings();
        }

        internal static bool GetAllies(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            return comp?.ModSet.Settings.AllowAllies ?? false;
        }

        internal static void SetAllies(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp == null) return;
            comp.ModSet.Settings.AllowAllies = newValue;
            comp.ModSet.NetworkUpdate();
            comp.ModSet.SaveSettings();
        }

        internal static bool GetPassiveModulation(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            return comp?.ModSet.Settings.AggregateModulation ?? false;
        }

        internal static void SetPassiveModulation(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp == null) return;
            comp.ModSet.Settings.AggregateModulation = newValue;
            comp.ModSet.NetworkUpdate();
            comp.ModSet.SaveSettings();
        }

        internal static bool ShowEMP(IMyTerminalBlock block)
        {
            return false;
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp?.ShieldComp?.DefenseShields == null || comp.ShieldComp.DefenseShields.IsStatic) return false;

            return comp.EnhancerLink;
        }

        internal static bool GetEmpProt(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            return comp?.ModSet.Settings.EmpEnabled ?? false;
        }

        internal static void SetEmpProt(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp == null) return;
            comp.ModSet.Settings.EmpEnabled = newValue;
            comp.ModSet.NetworkUpdate();
            comp.ModSet.SaveSettings();
        }

        internal static bool ShowReInforce(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp?.ShieldComp?.DefenseShields == null || comp.ShieldComp.DefenseShields.IsStatic)
                return false;

            return comp.EnhancerLink;
        }

        internal static bool GetReInforceProt(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            return comp?.ModSet.Settings.ReInforceEnabled ?? false;
        }

        internal static void SetReInforceProt(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<Modulators>();
            if (comp == null) return;
            comp.ModSet.Settings.ReInforceEnabled = newValue;
            comp.ModSet.NetworkUpdate();
            comp.ModSet.SaveSettings();
        }
        #endregion
    }
}
