using System;
using System.Diagnostics;
using System.IO;
using Sandbox.ModAPI;

namespace SC.SUGMA
{
    internal class Log
    {
        private static Log I;
        private readonly TextWriter _writer;

        private Log()
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage("SUGMA_GameMode.log");
            _writer = MyAPIGateway.Utilities
                .WriteFileInGlobalStorage(
                    "SUGMA_GameMode.log"); // Only creating one debug.log to avoid clutter. Might change in the future.
            _writer.WriteLine(
                $"      SUGMA v{SUGMA_SessionComponent.ModVersion} - {(MyAPIGateway.Session.IsServer ? "Server" : "Client")} Debug Log\nThe local datetime is {DateTime.Now:R}\n===========================================\n");
            _writer.Flush();
        }

        public static void Info(string message)
        {
            I._Log(message);
        }

        public static void Exception(Exception ex, Type callingType, string prefix = "")
        {
            I._LogException(ex, callingType, prefix);
        }

        public static void Init()
        {
            Close();
            I = new Log();
        }

        public static void Close()
        {
            if (I != null)
            {
                Info("Closing log writer.");
                I._writer.Close();
            }

            I = null;
        }

        private void _Log(string message)
        {
            _writer.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: {message}");
            _writer.Flush();
        }

        private void _LogException(Exception ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                _Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }
            _Log(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
            MyAPIGateway.Utilities.ShowNotification(
                $"{ex.GetType().Name} in Universal Gamemode! Check logs for more info.", 10000, "Red");
        }
    }
}