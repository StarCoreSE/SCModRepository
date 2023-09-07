using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace KingOfTheHill
{

    public class Tools
    {
        public static void Log(MyLogSeverity level, string message)
        {
            MyLog.Default.Log(level, $"[KingOfTheHill] {message}");
            MyLog.Default.Flush();
        }

        public static bool IsAllowedSpecialOperations(ulong steamId)
        {
            return IsAllowedSpecialOperations(MyAPIGateway.Session.GetUserPromoteLevel(steamId));
        }

        public static bool IsAllowedSpecialOperations(MyPromoteLevel level)
        {
            return level == MyPromoteLevel.SpaceMaster || level == MyPromoteLevel.Admin || level == MyPromoteLevel.Owner;
        }
    }
}
