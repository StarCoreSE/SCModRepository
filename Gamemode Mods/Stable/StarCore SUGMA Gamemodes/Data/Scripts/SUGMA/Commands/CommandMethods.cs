using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking.Custom;
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
                StringBuilder availableGamemodes = new StringBuilder();

                foreach (var gamemode in SUGMA_SessionComponent.I.GetGamemodes())
                {
                    availableGamemodes.Append($"\n-    {gamemode} ({SUGMA_SessionComponent.I.GetComponent<GamemodeBase>(gamemode).ReadableName})");
                }

                MyAPIGateway.Utilities.ShowMessage("SUGMA",
                    $"Unrecognized gamemode \"{args[1].ToLower()}\". Available gamemodes:{availableGamemodes}");
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

        //public static void SetMatchTime(string[] args)
        //{
        //    if (!PointCheck.AmTheCaptainNow)
        //    {
        //        MyAPIGateway.Utilities.ShowNotification(
        //            "You aren't the captain! Run \"/st takeover\" to take over the match.");
        //        return;
        //    }
        //
        //    try
        //    {
        //        MatchTimer.I.SetMatchTime(double.Parse(args[1]));
        //        MyAPIGateway.Utilities.ShowNotification("Match time changed to " + args[1] + " minutes.");
        //    }
        //    catch (Exception)
        //    {
        //        MyAPIGateway.Utilities.ShowNotification("Win time not changed, try /st setmatchtime xxx (in minutes)");
        //    }
        //}

        //public static void SetWinTime(string[] args)
        //{
        //    if (!PointCheck.AmTheCaptainNow)
        //    {
        //        MyAPIGateway.Utilities.ShowNotification(
        //            "You aren't the captain! Run \"/st takeover\" to take over the match.");
        //        return;
        //    }
        //
        //    try
        //    {
        //        MatchTimer.I.MatchDurationMinutes = int.Parse(args[1]);
        //        MyAPIGateway.Utilities.ShowNotification("Match duration changed to " +
        //                                                MatchTimer.I.MatchDurationMinutes + "m.");
        //        MatchTimerPacket.SendMatchUpdate(MatchTimer.I);
        //    }
        //    catch (Exception)
        //    {
        //        MyAPIGateway.Utilities.ShowNotification("Win time not changed, try /st settime xxx (in seconds)");
        //    }
        //}
    }
}