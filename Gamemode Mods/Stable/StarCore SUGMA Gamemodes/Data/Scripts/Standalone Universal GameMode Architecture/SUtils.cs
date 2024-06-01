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
            MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Damage {(value ? "enabled" : "disabled")}.");
            if (!MyAPIGateway.Session.IsServer)
                return;

            int existing = (int)MySessionComponentSafeZones.AllowedActions;
            MySessionComponentSafeZones.AllowedActions = CastProhibit(MySessionComponentSafeZones.AllowedActions, value ? existing | 0x1 : existing & ~0x1);
        }

        public static void SetWorldPermissionsForMatch(bool matchActive)
        {
            MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Match permissions {(matchActive ? "enabled" : "disabled")}.");
            if (!MyAPIGateway.Session.IsServer)
                return;

            MySessionComponentSafeZones.AllowedActions = CastProhibit(MySessionComponentSafeZones.AllowedActions, matchActive ? MatchPermsInt : FullPermsInt);
        }
    }
}
