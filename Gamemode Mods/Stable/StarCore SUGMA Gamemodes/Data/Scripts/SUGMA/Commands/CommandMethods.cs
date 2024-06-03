using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using MyAPIGateway = Sandbox.ModAPI.MyAPIGateway;

namespace SC.SUGMA.Commands
{
    internal static class CommandMethods
    {
        #region Match Commands

        public static void Start(string[] args)
        {
            if (args.Length < 2)
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA",
                    $"Unrecognized gamemode \"null\". Available gamemodes:\n  {string.Join("\n  ", SUGMA_SessionComponent.I.GetGamemodes())}");
                return;
            }

            string[] startArgs = Array.Empty<string>();
            //if (args.Length > 2)
            //    startArgs = args.RemoveIndices(new List<int>() { 0, 1 }); // TODO remove first two objects from args array

            if (!SUGMA_SessionComponent.I.StartGamemode(args[1].ToLower(), startArgs, true))
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA",
                    $"Unrecognized gamemode \"{args[1].ToLower()}\". Available gamemodes:\n-    {string.Join("\n-    ", SUGMA_SessionComponent.I.GetGamemodes())}");
                return;
            }

            if (SUGMA_SessionComponent.I.CurrentGamemode == null)
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA", "Internal exception encountered - failed to start match.");
                return;
            }


            MyAPIGateway.Utilities.ShowMessage("SUGMA",
                "Now starting: " + SUGMA_SessionComponent.I.CurrentGamemode.ReadableName + ".");
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