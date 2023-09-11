using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using ServerMod;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Digi {
    public static class Log // v1.4
    {
        public static string modName = "UNNAMED";
        public static string modFolder = "UNNAMED";
        public static ulong workshopId = 0;
       
        private static IMyHudNotification notify = null;
        private static readonly List<string> preInitMessages = new List<string>(0);

        public static void Error(Exception e) {
            Error(e.ToString());
        }
        
        public static void ServerError(Exception e) {
            MyLog.Default.WriteLineAndConsole(e.Message + " " + e.StackTrace);
        }

        public static void Error(Exception e, string printText) {
            Error(printText +" "+ e.ToString(), printText);
        }

        public static void Error(string msg) {
            Error(msg, modName + " error - open %AppData%/SpaceEngineers");
        }

        public static void ChatError (String s, Exception e) {
            ChatError (s + " " + e.ToString());
        }
        
        public static void ChatError (Exception e) {
            ChatError (e.ToString());
        }
        

        public static int MAX_LOGS = 5000;
        public static int logged = 0;

        public static void ChatError (String s) {
            MyLog.Default.WriteLine("SLIME:" + s);
            if (!ShouldShowError ()) return;


            Info(s);

            if (logged >= MAX_LOGS) return;
            logged++;

            Common.SendChatMessageToMe (s, "<Logs>");
            if (logged == MAX_LOGS)
            {
                Common.SendChatMessageToMe("Reached limit of messages. Watch logs", "<Logs>");
            }
        }

        public static bool ShouldShowError () {
            return (MyAPIGateway.Session.Player == null || MyAPIGateway.Session.Player.PromoteLevel > MyPromoteLevel.None); //is Server or admin
        }

        public static void Error(string msg, string printText) {
            Info("ERROR: " + msg);
            if (ShouldShowError ()) {
                try {
                    if (MyAPIGateway.Session != null) {
                        //MyAPIGateway.Utilities.CreateNotification(msg, 5000, MyFontEnum.Red).Show();
                        if (notify == null) {
                            notify = MyAPIGateway.Utilities.CreateNotification(msg, 5000, MyFontEnum.Red);
                        } else {
                            notify.Text = msg;
                            notify.ResetAliveTime();
                        }

                        notify.Show();
                    }
                } catch (Exception e) {
                    Info("ERROR: Could not send notification to local client: " + e);
                    MyLog.Default.WriteLineAndConsole(modName + " error/exception: Could not send notification to local client: " + e);
                }
            }
        }

        public static void Info(string msg) {
            MyLog.Default.WriteLineAndConsole(msg);
        }
    }
}