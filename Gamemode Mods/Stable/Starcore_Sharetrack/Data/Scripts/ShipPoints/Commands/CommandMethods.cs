using System;
using Sandbox.ModAPI;
using SCModRepository_Dev.Gamemode_Mods.Development.Starcore_Sharetrack_Dev.Data.Scripts.ShipPoints.HeartNetworking.
    Custom;
using ShipPoints.HeartNetworking;
using ShipPoints.HeartNetworking.Custom;
using ShipPoints.MatchTiming;

namespace ShipPoints.Commands
{
    internal static class CommandMethods
    {
        #region Match Commands

        public static void Start(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                new GameStatePacket(true).Received(0);
            else
                HeartNetwork.I.SendToServer(new GameStatePacket(true));
            TakeOverControl(null);
            MyAPIGateway.Utilities.ShowNotification("HEY DUMBASS, IS DAMAGE ON?", 10000, "Red");
        }

        public static void End(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                new GameStatePacket(false).Received(0);
            else
                HeartNetwork.I.SendToServer(new GameStatePacket(true));
            GiveUpControl(null);
        }

        public static void TakeOverControl(string[] args)
        {
            PointCheck.AmTheCaptainNow = true;
            MyAPIGateway.Utilities.SendMessage(MyAPIGateway.Session.Player?.DisplayName + " has match control.");
            MyAPIGateway.Utilities.ShowMessage("ShareTrack", "You are the captain now.");
        }

        public static void GiveUpControl(string[] args)
        {
            PointCheck.AmTheCaptainNow = false;
            MyAPIGateway.Utilities.SendMessage(MyAPIGateway.Session.Player?.DisplayName + " released match control.");
            MyAPIGateway.Utilities.ShowMessage("ShareTrack", "You are a deckhand now.");
        }

        #endregion

        #region Match Config

        public static void SetMatchTime(string[] args)
        {
            if (!PointCheck.AmTheCaptainNow)
            {
                MyAPIGateway.Utilities.ShowNotification(
                    "You aren't the captain! Run \"/st takeover\" to take over the match.");
                return;
            }

            try
            {
                MatchTimer.I.SetMatchTime(double.Parse(args[1]));
                MyAPIGateway.Utilities.ShowNotification("Match time changed to " + args[1] + " minutes.");
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowNotification("Win time not changed, try /st setmatchtime xxx (in minutes)");
            }
        }

        public static void SetTeams(string[] args)
        {
            if (!PointCheck.AmTheCaptainNow)
            {
                MyAPIGateway.Utilities.ShowNotification(
                    "You aren't the captain! Run \"/st takeover\" to take over the match.");
                return;
            }

            if (args?.Length < 2)
            {
                MyAPIGateway.Utilities.ShowNotification("Teams not changed, make sure to have two or more arguments.");
                return;
            }

            try
            {
                var teamNames = new string[args.Length - 1];
                for (var i = 1; i < args.Length; i++) // Skip the first argument as it's always "setteams"
                    teamNames[i - 1] = args[i].ToUpper();

                PointCheck.I.TeamNames = teamNames;
                MyAPIGateway.Utilities.ShowNotification("Teams changed to " + string.Join(" v. ", teamNames));
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowNotification("Teams not changed, try /st setteams abc xyz");
            }
        }

        public static void SetWinTime(string[] args)
        {
            if (!PointCheck.AmTheCaptainNow)
            {
                MyAPIGateway.Utilities.ShowNotification(
                    "You aren't the captain! Run \"/st takeover\" to take over the match.");
                return;
            }

            try
            {
                MatchTimer.I.MatchDurationMinutes = int.Parse(args[1]);
                MyAPIGateway.Utilities.ShowNotification("Match duration changed to " +
                                                        MatchTimer.I.MatchDurationMinutes + "m.");
                MatchTimerPacket.SendMatchUpdate(MatchTimer.I);
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowNotification("Win time not changed, try /st settime xxx (in seconds)");
            }
        }

        public static void SetDelay(string[] args) // TODO these aren't synced
        {
            if (!PointCheck.AmTheCaptainNow)
            {
                MyAPIGateway.Utilities.ShowNotification(
                    "You aren't the captain! Run \"/st takeover\" to take over the match.");
                return;
            }

            try
            {
                PointCheck.Delaytime = int.Parse(args[1]);
                MyAPIGateway.Utilities.ShowNotification("Delay time changed to " + PointCheck.Delaytime + " minutes.");
                PointCheck.Delaytime *= 60 * 60;
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowNotification("Delay time not changed, try /st setdelay x (in minutes)");
            }
        }

        #endregion

        #region Utility Commands

        public static void Shields(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                new ShieldFillRequestPacket().Received(0);
            else
                HeartNetwork.I.SendToEveryone(new ShieldFillRequestPacket());
        }

        public static void ReportProblem(string[] args)
        {
            var message = "@" + (MyAPIGateway.Session.Player?.DisplayName ?? "ERR") + ":";
            for (var i = 1; i < args.Length; i++) // Skip the first argument as it's always "problem"
                message += ' ' + args[i];

            PointCheck.I.ReportProblem(args.Length > 1 ? message : "");
        }

        public static void ReportFixed(string[] args)
        {
            PointCheck.I.ResolvedProblem();
        }

        #endregion
    }
}