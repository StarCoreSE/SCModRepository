using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ObjectBuilders.Components;

namespace SC.SUGMA
{
    public static class SUtils
    {
        // Good lord this is horrendous, thanks Digi
        public static T CastProhibit<T>(T ptr, object val) => (T)val;


        private const int DamageToggleInt = 0x1;
        private const int MatchPermsInt = 0x29B;
        private const int FullPermsInt = 0x3FE;

        public static void SetDamageEnabled(bool value)
        {
            MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Global damage {(value ? "enabled" : "disabled")}.");

            int existing = (int)MySessionComponentSafeZones.AllowedActions;
            MySessionComponentSafeZones.AllowedActions = CastProhibit(MySessionComponentSafeZones.AllowedActions, value ? existing | DamageToggleInt : existing & ~DamageToggleInt);
        }

        public static void SetWorldPermissionsForMatch(bool matchActive)
        {
            MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Match global permissions {(matchActive ? "enabled" : "disabled")}.");

            MySessionComponentSafeZones.AllowedActions = CastProhibit(MySessionComponentSafeZones.AllowedActions, matchActive ? MatchPermsInt : FullPermsInt);
        }
    }
}
