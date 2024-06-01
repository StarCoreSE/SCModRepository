using System;
using Sandbox.ModAPI;
using VRage.Game;

namespace SC.SUGMA.Commands
{
    internal static class CommandMethods
    {
        #region Match Commands

        public static void Start(string[] args)
        {
            if (args.Length < 2)
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Unrecognized gamemode \"null\". Available gamemodes:\n  {string.Join("\n  ", SUGMA_SessionComponent.I.GetGamemodes())}");
                return;
            }

            if (!SUGMA_SessionComponent.I.StartGamemode(args[1].ToLower(), true))
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Unrecognized gamemode \"{args[1].ToLower()}\". Available gamemodes:\n-    {string.Join("\n-    ", SUGMA_SessionComponent.I.GetGamemodes())}");
                return;
            }

            MyAPIGateway.Utilities.ShowMessage("SUGMA", "Starting match of type " + args[1].ToLower() + ".");
        }

        public static void End(string[] args)
        {
            if (!SUGMA_SessionComponent.I.StopGamemode(true))
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA", $"There isn't a match running, idiot.");
                return;
            }

            MyAPIGateway.Utilities.ShowMessage("SUGMA", "Match ended.");
        }

        #endregion
    }
}