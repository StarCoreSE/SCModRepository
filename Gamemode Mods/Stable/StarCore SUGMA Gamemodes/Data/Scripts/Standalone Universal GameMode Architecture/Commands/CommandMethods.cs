using System;
using Sandbox.ModAPI;

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

            GamemodeBase gamemode = SUGMA_SessionComponent.I.GetComponent<GamemodeBase>(args[1].ToLower());

            if (gamemode == null)
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA", $"Unrecognized gamemode \"{args[1].ToLower()}\". Available gamemodes:\n  {string.Join("\n  ", SUGMA_SessionComponent.I.GetGamemodes())}");
                return;
            }

            MyAPIGateway.Utilities.ShowMessage("SUGMA", "Attempting to start match of type " + args[1].ToLower());
            gamemode.StartRound();

            MyAPIGateway.Utilities.ShowNotification("HEY DUMBASS, IS DAMAGE ON?", 10000, "Red");
        }

        public static void End(string[] args)
        {
            GamemodeBase gamemode = SUGMA_SessionComponent.I.CurrentGamemode;

            if (gamemode == null)
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA", $"There isn't a game running, idiot.");
                return;
            }

            MyAPIGateway.Utilities.ShowMessage("SUGMA", "Attempting to end match.");


            gamemode.StopRound();
        }

        #endregion
    }
}