using Sandbox.ModAPI;
using System;
using System.IO;
using VRage.Game.ModAPI;

namespace StealthSystem
{
    internal class Logs
    {
        internal const string LOG_PREFIX = "StealthMod_";
        internal const string LOG_SUFFIX = ".log";
        internal const int LOGS_TO_KEEP = 5;

        internal static TextWriter TextWriter;

        internal static void InitLogs()
        {
            int last = LOGS_TO_KEEP - 1;
            string lastName = LOG_PREFIX + last + LOG_SUFFIX;
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(lastName, typeof(Logs)))
                MyAPIGateway.Utilities.DeleteFileInLocalStorage(lastName, typeof(Logs));

            if (last > 0)
            {
                for (int i = last; i > 0; i--)
                {
                    string oldName = LOG_PREFIX + (i - 1) + LOG_SUFFIX;
                    string newName = LOG_PREFIX + i + LOG_SUFFIX;
                    RenameFileInLocalStorage(oldName, newName, typeof(Logs));
                }
            }

            string fileName = LOG_PREFIX + 0 + LOG_SUFFIX;
            TextWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(Logs));

            var message = $"{DateTime.Now:dd-MM-yy HH-mm-ss} - Logging Started";
            TextWriter.WriteLine(message);
            TextWriter.WriteLine("  Tick - Log");
            TextWriter.Flush();

        }

        internal static void RenameFileInLocalStorage(string oldName, string newName, Type anyObjectInYourMod)
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(oldName, anyObjectInYourMod))
                return;

            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(newName, anyObjectInYourMod))
                return;

            using (var read = MyAPIGateway.Utilities.ReadFileInLocalStorage(oldName, anyObjectInYourMod))
            {
                using (var write = MyAPIGateway.Utilities.WriteFileInLocalStorage(newName, anyObjectInYourMod))
                {
                    write.Write(read.ReadToEnd());
                    write.Flush();
                    write.Dispose();
                }
            }

            MyAPIGateway.Utilities.DeleteFileInLocalStorage(oldName, anyObjectInYourMod);
        }

        internal static void WriteLine(string text)
        {
            string line = $"{StealthSession.Tick,6} - " + text;
            TextWriter.WriteLine(line);
            TextWriter.Flush();
        }

        internal static void Close()
        {
            var message = $"{DateTime.Now:dd-MM-yy HH-mm-ss} - Logging Stopped";
            TextWriter.WriteLine(message);

            TextWriter.Flush();
            TextWriter.Close();
            TextWriter.Dispose();
        }

        internal static void CheckGrid()
        {
            if (MyAPIGateway.Session.LocalHumanPlayer?.Character == null) return;

            var from = MyAPIGateway.Session.LocalHumanPlayer.Character.PositionComp.WorldMatrixRef.Translation;
            var to = from + MyAPIGateway.Session.LocalHumanPlayer.Character.PositionComp.WorldMatrixRef.Forward * 100;
            IHitInfo info;
            MyAPIGateway.Physics.CastRay(from, to, out info);

            if (info == null) return;

            var grid = info.HitEntity as IMyCubeGrid;
            if (grid == null) return;

            Logs.WriteLine($"Grid: {grid.DisplayName} - has flag: {((uint)grid.Flags & StealthSession.IsStealthedFlag) > 0}");
        }
    }
}
