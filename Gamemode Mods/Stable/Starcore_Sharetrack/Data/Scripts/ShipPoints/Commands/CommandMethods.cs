using ShipPoints.Data.Scripts.ShipPoints.Networking;
using System;
using Math0424.Networking;
using Sandbox.ModAPI;
using ShipPoints.MatchTiming;

namespace ShipPoints.Commands
{
    internal static class CommandMethods
    {
        #region Match Commands

        public static void Start(string[] args)
        {
            MyNetworkHandler.Static.MyNetwork.TransmitToServer(new BasicPacket(6), true, true);
            PointCheck.AmTheCaptainNow = true;
            PointCheck.LocalMatchState = 1;
            MatchTimer.I.Start();
            MyAPIGateway.Utilities.ShowMessage("GM", "You are the captain now.");
            MyAPIGateway.Utilities.ShowNotification("HEY DUMBASS, IS DAMAGE ON?", 10000, "Red");
        }

        public static void End(string[] args)
        {
            MyNetworkHandler.Static.MyNetwork.TransmitToServer(new BasicPacket(8), true, true);
            PointCheck.AmTheCaptainNow = false;
            PointCheck.LocalMatchState = 0;
            MatchTimer.I.Stop();
            MyAPIGateway.Utilities.ShowMessage("GM", "Match Ended.");
        }

        public static void TakeOver(string[] args)
        {
            PointCheck.AmTheCaptainNow = true;
            MyAPIGateway.Utilities.ShowMessage("GM", "You are the captain now.");
        }

        public static void GiveUp(string[] args)
        {
            PointCheck.AmTheCaptainNow = false;
            MyAPIGateway.Utilities.ShowMessage("GM", "You are not the captain now.");
        }

        #endregion

        #region Match Config

        public static void SetMatchTime(string[] args)
        {
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
            try
            {
                PointCheck.I.Team1.Value = args[1].ToUpper();
                PointCheck.I.Team2.Value = args[2].ToUpper();
                PointCheck.I.Team3.Value = args[3].ToUpper();
                //team1_Local = tempdist[1].ToUpper(); team2_Local = tempdist[2].ToUpper(); team3_Local = tempdist[3].ToUpper();
                MyAPIGateway.Utilities.ShowNotification("Teams changed to " + args[1] + " vs " + args[2] +
                                                        " vs " + args[3]); //sendToOthers = true;
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowNotification("Teams not changed, try /st setteams abc xyz");
            }
        }

        public static void SetWinTime(string[] args)
        {
            try
            {
                MatchTimer.I.MatchDurationMinutes = int.Parse(args[1]);
                MyAPIGateway.Utilities.ShowNotification("Match duration changed to " + MatchTimer.I.MatchDurationMinutes + "m.");
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowNotification("Win time not changed, try /st settime xxx (in seconds)");
            }
        }

        public static void SetDelay(string[] args)
        {
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

        public static void SetDecay(string[] args)
        {
            try
            {
                PointCheck.Decaytime = int.Parse(args[1]);
                MyAPIGateway.Utilities.ShowNotification("Decay time changed to " + PointCheck.Decaytime + " seconds.");
                PointCheck.Decaytime *= 60;
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowNotification("Decay time not changed, try /st setdecay xxx (in seconds)");
            }
        }

        public static void SetTwoTeams(string[] args)
        {
            MyAPIGateway.Utilities.ShowMessage("GM", "Teams set to two.");
            PointCheck.I.ThreeTeams.Value = 0;
        }

        public static void SetThreeTeams(string[] args)
        {
            MyAPIGateway.Utilities.ShowMessage("GM", "Teams set to three.");
            PointCheck.I.ThreeTeams.Value = 1;
        }

        #endregion

        #region Utility Commands

        public static void ToggleSphere(string[] args)
        {
            PointCheck.I.SphereVisual = !PointCheck.I.SphereVisual;
        }

        public static void Shields(string[] args)
        {
            MyNetworkHandler.Static.MyNetwork.TransmitToServer(new BasicPacket(5));
        }

        public static void ReportProblem(string[] args)
        {
            MyAPIGateway.Utilities.ShowNotification("A problem has been reported.", 10000);
            PointCheck.LocalProblemSwitch = 1;
            MyNetworkHandler.Static.MyNetwork.TransmitToServer(new BasicPacket(17), true, true);
        }

        public static void ReportFixed(string[] args)
        {
            MyAPIGateway.Utilities.ShowNotification("Fixed :^)", 10000);
            PointCheck.LocalProblemSwitch = 0;
            MyNetworkHandler.Static.MyNetwork.TransmitToServer(new BasicPacket(18), true, true);
        }

        #endregion
    }
}
