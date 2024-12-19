using System;
using System.Text;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using SC.SUGMA.Utilities;
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
                    $"Unrecognized gamemode \"null\". Available gamemodes:{SUGMA_SessionComponent.ListGamemodes()}");
                return;
            }

            // Remove first two items from arguments
            var startArgs = Array.Empty<string>();
            if (args.Length > 2)
            {
                var newStartArgs = new string[args.Length - 2];
                for (var i = 2; i < args.Length; i++) newStartArgs[i - 2] = args[i];
                startArgs = newStartArgs;
            }

            if (!SUGMA_SessionComponent.I.StartGamemode(args[1].ToLower(), startArgs, true))
            {
                MyAPIGateway.Utilities.ShowMessage("SUGMA",
                    $"Unrecognized gamemode \"{args[1].ToLower()}\". Available gamemodes:{SUGMA_SessionComponent.ListGamemodes()}");
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
                MyAPIGateway.Utilities.ShowMessage("SUGMA", "There isn't a match running, idiot.");
                return;
            }

            MyAPIGateway.Utilities.ShowMessage("SUGMA", "Match ended.");
        }

        public static void Pause(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                new PauseRequestPacket().Received(0);
            else
                HeartNetwork.I.SendToServer(new PauseRequestPacket());
        }

        public static void ClearBoard(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                new ClearBoardRequestPacket(true).Received(0);
            else
                HeartNetwork.I.SendToServer(new ClearBoardRequestPacket(true));
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

        #region Utility Commands

        public static void Shields(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                new ShieldFillRequestPacket().Received(0);
            else
                HeartNetwork.I.SendToServer(new ShieldFillRequestPacket());
        }

        public static void ReportProblem(string[] args)
        {
            var message = "@" + (MyAPIGateway.Session.Player?.DisplayName ?? "ERR") + ":";
            for (var i = 1; i < args.Length; i++) // Skip the first argument as it's always "problem"
                message += ' ' + args[i];

            SUtils.ReportProblem(args.Length > 1 ? message : "");
        }

        public static void ReportFixed(string[] args)
        {
            SUtils.ResolvedProblem();
        }

        public static void ResolveMissingPlayers(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                DisconnectHandler.I.ResolveProblem();
            else
                HeartNetwork.I.SendToServer(new MissingPlayerOverridePacket());
        }

        public static void AutoBalance(string[] args)
        {
            TeamBalancer.PerformBalancing();
        }

        #endregion
    }
}