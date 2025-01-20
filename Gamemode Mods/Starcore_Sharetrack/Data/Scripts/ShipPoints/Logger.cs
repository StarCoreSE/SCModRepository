using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ParallelTasks;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace StarCore.ShareTrack
{
    /// <summary>
    ///     <para>Standalone logger, does not require any setup.</para>
    ///     <para>
    ///         Mod name is automatically set from workshop name or folder name. Can also be manually defined using
    ///         <see cref="ModName" />.
    ///     </para>
    ///     <para>Version 1.52 by Digi</para>
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, int.MaxValue)]
    public class Log : MySessionComponentBase
    {
        private const int DefaultTimeInfo = 3000;
        private const int DefaultTimeError = 10000;

        /// <summary>
        ///     Print the generic error info.
        ///     (For use in <see cref="Error(string, string, int)" />'s 2nd arg)
        /// </summary>
        public const string PrintError = "<err>";

        /// <summary>
        ///     Prints the message instead of the generic generated error info.
        ///     (For use in <see cref="Error(string, string, int)" />'s 2nd arg)
        /// </summary>
        public const string PrintMsg = "<msg>";

        private static Log _instance;
        private static Handler _handler;
        private static bool _unloaded;

        public static readonly string File = GenerateTimestampedFileName();

        private static string GenerateTimestampedFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"[{timestamp}]-Sharetrack.log";
        }

        private class Handler
        {
            private readonly StringBuilder _sb = new StringBuilder(64);
            private string _errorPrintText;
            private int _indent;
            private string _modName = string.Empty;
            private IMyHudNotification _notifyError;

            private IMyHudNotification _notifyInfo;

            private List<string> _preInitMessages;
            private Log _sessionComp;

            private TextWriter _writer;

            public bool AutoClose { get; set; } = true;

            public ulong WorkshopId { get; private set; }

            public string ModName
            {
                get { return _modName; }
                set
                {
                    _modName = value;
                    ComputeErrorPrintText();
                }
            }

            public void Init(Log sessionComp)
            {
                if (_writer != null)
                    return; // already initialized

                if (MyAPIGateway.Utilities == null)
                {
                    Error("MyAPIGateway.Utilities is NULL !");
                    return;
                }

                _sessionComp = sessionComp;

                if (string.IsNullOrWhiteSpace(ModName))
                    ModName = sessionComp.ModContext.ModName;

                WorkshopId = GetWorkshopId(sessionComp.ModContext.ModId);

                _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(File, typeof(Log));

                #region Pre-init messages

                if (_preInitMessages != null)
                {
                    var warning = $"{_modName} WARNING: there are log messages before the mod initialized!";

                    Info("--- pre-init messages ---");

                    foreach (var msg in _preInitMessages) Info(msg, warning);

                    Info("--- end pre-init messages ---");

                    _preInitMessages = null;
                }

                #endregion

                #region Init message

                _sb.Clear();
                _sb.Append("Initialized");
                _sb.Append("\nGameMode=").Append(MyAPIGateway.Session.SessionSettings.GameMode);
                _sb.Append("\nOnlineMode=").Append(MyAPIGateway.Session.SessionSettings.OnlineMode);
                _sb.Append("\nServer=").Append(MyAPIGateway.Session.IsServer);
                _sb.Append("\nDS=").Append(MyAPIGateway.Utilities.IsDedicated);
                _sb.Append("\nDefined=");

#if STABLE
                _sb.Append("STABLE, ");
#endif

#if UNOFFICIAL
                _sb.Append("UNOFFICIAL, ");
#endif

#if DEBUG
                _sb.Append("DEBUG, ");
#endif

#if BRANCH_STABLE
                _sb.Append("BRANCH_STABLE, ");
#endif

#if BRANCH_DEVELOP
                _sb.Append("BRANCH_DEVELOP, ");
#endif

#if BRANCH_UNKNOWN
                _sb.Append("BRANCH_UNKNOWN, ");
#endif

                Info(_sb.ToString());
                _sb.Clear();

                #endregion
            }

            public void Close()
            {
                if (_writer != null)
                {
                    Info("Unloaded.");

                    _writer.Flush();
                    _writer.Close();
                    _writer = null;
                }
            }

            private void ComputeErrorPrintText()
            {
                _errorPrintText =
                    $"[ {_modName} ERROR, report contents of: %AppData%/SpaceEngineers/Storage/{MyAPIGateway.Utilities.GamePaths.ModScopeName}/{File} ]";
            }

            public void IncreaseIndent()
            {
                _indent++;
            }

            public void DecreaseIndent()
            {
                if (_indent > 0)
                    _indent--;
            }

            public void ResetIndent()
            {
                _indent = 0;
            }

            public void Error(string message, string printText = PrintError, int printTime = DefaultTimeError)
            {
                MyLog.Default.WriteLineAndConsole(_modName + " error/exception: " + message); // write to game's log

                LogMessage(message, "ERROR: "); // write to custom log

                if (printText != null) // printing to HUD is optional
                    ShowHudMessage(ref _notifyError, message, printText, printTime, MyFontEnum.Red);
            }

            public void Info(string message, string printText = null, int printTime = DefaultTimeInfo)
            {
                LogMessage(message); // write to custom log

                if (printText != null) // printing to HUD is optional
                    ShowHudMessage(ref _notifyInfo, message, printText, printTime, MyFontEnum.White);
            }

            private void ShowHudMessage(ref IMyHudNotification notify, string message, string printText, int printTime,
                string font)
            {
                if (printText == null)
                    return;

                try
                {
                    if (MyAPIGateway.Utilities != null && !MyAPIGateway.Utilities.IsDedicated)
                    {
                        if (printText == PrintError)
                            printText = _errorPrintText;
                        else if (printText == PrintMsg)
                            printText = $"[ {_modName} ERROR: {message} ]";

                        if (notify == null)
                        {
                            notify = MyAPIGateway.Utilities.CreateNotification(printText, printTime, font);
                        }
                        else
                        {
                            notify.Text = printText;
                            notify.AliveTime = printTime;
                            notify.ResetAliveTime();
                        }

                        notify.Show();
                    }
                }
                catch (Exception e)
                {
                    Info("ERROR: Could not send notification to local client: " + e);
                    MyLog.Default.WriteLineAndConsole(_modName +
                                                      " logger error/exception: Could not send notification to local client: " +
                                                      e);
                }
            }

            private void LogMessage(string message, string prefix = null)
            {
                try
                {
                    _sb.Clear();
                    _sb.Append(DateTime.Now.ToString("[HH:mm:ss] "));

                    if (_writer == null)
                        _sb.Append("(PRE-INIT) ");

                    for (var i = 0; i < _indent; i++)
                        _sb.Append(' ', 4);

                    if (prefix != null)
                        _sb.Append(prefix);

                    _sb.Append(message);

                    if (_writer == null)
                    {
                        if (_preInitMessages == null)
                            _preInitMessages = new List<string>();

                        _preInitMessages.Add(_sb.ToString());
                    }
                    else
                    {
                        _writer.WriteLine(_sb);
                        _writer.Flush();
                    }

                    _sb.Clear();
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLineAndConsole(
                        $"{_modName} had an error while logging message = '{message}'\nLogger error: {e.Message}\n{e.StackTrace}");
                }
            }

            private ulong GetWorkshopId(string modId)
            {
                // NOTE workaround for MyModContext not having the actual workshop ID number.
                foreach (var mod in MyAPIGateway.Session.Mods)
                    if (mod.Name == modId)
                        return mod.PublishedFileId;

                return 0;
            }
        }

        #region Handling of handler

        public override void LoadData()
        {
            _instance = this;
            EnsureHandlerCreated();
            _handler.Init(this);
        }

        protected override void UnloadData()
        {
            _instance = null;

            if (_handler != null && _handler.AutoClose) Unload();
        }

        private void Unload()
        {
            try
            {
                if (_unloaded)
                    return;

                _unloaded = true;
                _handler?.Close();
                _handler = null;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(
                    $"Error in {ModContext.ModName} ({ModContext.ModId}): {e.Message}\n{e.StackTrace}");
                throw new ModCrashedException(e, ModContext);
            }
        }

        private static void EnsureHandlerCreated()
        {
            if (_unloaded)
                throw new Exception("Digi.Log accessed after it was unloaded!");

            if (_handler == null)
                _handler = new Handler();
        }

        #endregion

        #region Publicly accessible properties and methods

        /// <summary>
        ///     Manually unload the logger. Works regardless of <see cref="AutoClose" />, but if that property is false then this
        ///     method must be called!
        /// </summary>
        public static void Close()
        {
            _instance?.Unload();
        }

        /// <summary>
        ///     Defines if the component self-unloads next tick or after <see cref="UNLOAD_TIMEOUT_MS" />.
        ///     <para>If set to false, you must call <see cref="Close" /> manually!</para>
        /// </summary>
        public static bool AutoClose
        {
            get
            {
                EnsureHandlerCreated();
                return _handler.AutoClose;
            }
            set
            {
                EnsureHandlerCreated();
                _handler.AutoClose = value;
            }
        }

        /// <summary>
        ///     Sets/gets the mod name.
        ///     <para>This is optional as the mod name is generated from the folder/workshop name, but those can be weird or long.</para>
        /// </summary>
        public static string ModName
        {
            get
            {
                EnsureHandlerCreated();
                return _handler.ModName;
            }
            set
            {
                EnsureHandlerCreated();
                _handler.ModName = value;
            }
        }

        /// <summary>
        ///     Gets the workshop id of the mod.
        ///     <para>Will return 0 if it's a local mod or if it's called before LoadData() executes on the logger.</para>
        /// </summary>
        public static ulong WorkshopId => _handler?.WorkshopId ?? 0;

        /// <summary>
        ///     <para>Increases indentation by 4 spaces.</para>
        ///     Each indent adds 4 space characters before each of the future messages.
        /// </summary>
        public static void IncreaseIndent()
        {
            EnsureHandlerCreated();
            _handler.IncreaseIndent();
        }

        /// <summary>
        ///     <para>Decreases indentation by 4 space characters down to 0 indentation.</para>
        ///     See <seealso cref="IncreaseIndent" />
        /// </summary>
        public static void DecreaseIndent()
        {
            EnsureHandlerCreated();
            _handler.DecreaseIndent();
        }

        /// <summary>
        ///     <para>Resets the indentation to 0.</para>
        ///     See <seealso cref="IncreaseIndent" />
        /// </summary>
        public static void ResetIndent()
        {
            EnsureHandlerCreated();
            _handler.ResetIndent();
        }

        /// <summary>
        ///     Writes an exception to custom log file, game's log file and by default writes a generic error message to player's
        ///     HUD.
        /// </summary>
        /// <param name="exception">The exception to write to custom log and game's log.</param>
        /// <param name="printText">
        ///     HUD notification text, can be set to null to disable, to <see cref="PrintMsg" /> to use the
        ///     exception message, <see cref="PrintError" /> to use the predefined error message, or any other custom string.
        /// </param>
        /// <param name="printTimeMs">How long to show the HUD notification for, in miliseconds.</param>
        public static void Error(Exception exception, string printText = PrintError,
            int printTimeMs = DefaultTimeError)
        {
            EnsureHandlerCreated();
            _handler.Error(exception.ToString(), printText, printTimeMs);
        }

        /// <summary>
        ///     Writes a message to custom log file, game's log file and by default writes a generic error message to player's HUD.
        /// </summary>
        /// <param name="message">The message printed to custom log and game log.</param>
        /// <param name="printText">
        ///     HUD notification text, can be set to null to disable, to <see cref="PrintMsg" /> to use the
        ///     message arg, <see cref="PrintError" /> to use the predefined error message, or any other custom string.
        /// </param>
        /// <param name="printTimeMs">How long to show the HUD notification for, in miliseconds.</param>
        public static void Error(string message, string printText = PrintError, int printTimeMs = DefaultTimeError)
        {
            EnsureHandlerCreated();
            _handler.Error(message, printText, printTimeMs);
        }

        /// <summary>
        ///     Writes a message in the custom log file.
        ///     <para>Optionally prints a different message (or same message) in player's HUD.</para>
        /// </summary>
        /// <param name="message">The text that's written to log.</param>
        /// <param name="printText">
        ///     HUD notification text, can be set to null to disable, to <see cref="PrintMsg" /> to use the
        ///     message arg or any other custom string.
        /// </param>
        /// <param name="printTimeMs">How long to show the HUD notification for, in miliseconds.</param>
        public static void Info(string message, string printText = null, int printTimeMs = DefaultTimeInfo)
        {
            EnsureHandlerCreated();
            _handler.Info(message, printText, printTimeMs);
        }

        /// <summary>
        ///     Iterates task errors and reports them, returns true if any errors were found.
        /// </summary>
        /// <param name="task">The task to check for errors.</param>
        /// <param name="taskName">Used in the reports.</param>
        /// <returns>true if errors found, false otherwise.</returns>
        public static bool TaskHasErrors(Task task, string taskName)
        {
            EnsureHandlerCreated();

            if (task.Exceptions != null && task.Exceptions.Length > 0)
            {
                foreach (var e in task.Exceptions) Error($"Error in {taskName} thread!\n{e}");

                return true;
            }

            return false;
        }

        #endregion
    }
}