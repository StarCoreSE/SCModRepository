using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;

namespace ShipPoints.Commands
{
    /// <summary>
    ///     Parses commands from chat and triggers relevant methods.
    /// </summary>
    public class CommandHandler
    {
        public static CommandHandler I;

        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>
        {
            ["help"] = new Command(
                "ShareTrack",
                "Displays command help.",
                message => I.ShowHelp()),

            #region Match Commands

            ["start"] = new Command(
                "ShareTrack.Match",
                "Begins a new match.",
                CommandMethods.Start
            ),
            ["end"] = new Command(
                "ShareTrack.Match",
                "Ends the current match.",
                CommandMethods.End
            ),
            ["takeover"] = new Command(
                "ShareTrack.Match",
                "Takes over control of the match.",
                CommandMethods.TakeOverControl
            ),
            ["giveup"] = new Command(
                "ShareTrack.Match",
                "Gives up control of the match.",
                CommandMethods.GiveUpControl
            ),

            #endregion

            #region Match Config

            ["setmatchtime"] = new Command(
                "ShareTrack.Match.Config",
                "Sets the current match timer to [arg1] in minutes.",
                CommandMethods.SetMatchTime
            ),
            //["setteams"] = new Command( // TODO this doesn't actually do anything.
            //    "ShareTrack.Match.Config",
            //    "Assigns teams to specified. Minimum two, maximum infinite.",
            //    CommandMethods.SetTeams
            //),
            ["setwintime"] = new Command( // TODO this isn't synced.
                "ShareTrack.Match.Config",
                "Sets the current win time to [arg1] in minutes.",
                CommandMethods.SetWinTime
            ),
            ["setdelay"] = new Command(
                "ShareTrack.Match.Config",
                "Sets the current delay time to [arg1] in minutes.",
                CommandMethods.SetDelay
            ),

            #endregion

            #region Util Commands

            ["shields"] = new Command(
                "ShareTrack.Utils",
                "Fills shields to full capacity.",
                CommandMethods.Shields
            ),
            ["problem"] = new Command(
                "ShareTrack.Utils",
                "Reports a problem with optional message [arg1].",
                CommandMethods.ReportProblem
            ),
            ["fixed"] = new Command(
                "ShareTrack.Utils",
                "Marks a problem as fixed.",
                CommandMethods.ReportFixed
            ),

            #endregion
        };

        private CommandHandler()
        {
        }

        private void ShowHelp()
        {
            var helpBuilder = new StringBuilder();
            var modNames = new List<string>();
            foreach (var command in _commands.Values)
                if (!modNames.Contains(command.ModName))
                    modNames.Add(command.ModName);

            MyAPIGateway.Utilities.ShowMessage("ShareTrack Help", "");

            foreach (var modName in modNames)
            {
                foreach (var command in _commands)
                    if (command.Value.ModName == modName)
                        helpBuilder.Append($"\n{{/st {command.Key}}}: " + command.Value.HelpText);

                MyAPIGateway.Utilities.ShowMessage($"[{modName}]", helpBuilder + "\n");
                helpBuilder.Clear();
            }
        }

        public static void Init()
        {
            Close(); // Close existing command handlers.
            I = new CommandHandler();
            MyAPIGateway.Utilities.MessageEnteredSender += I.Command_MessageEnteredSender;
            MyAPIGateway.Utilities.ShowMessage("StarCore ShareTrack",
                "Chat commands registered - run \"/st help\" for help.");
        }

        public static void Close()
        {
            if (I != null)
            {
                MyAPIGateway.Utilities.MessageEnteredSender -= I.Command_MessageEnteredSender;
                I._commands.Clear();
            }

            I = null;
        }

        private void Command_MessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            try
            {
                // Only register for commands
                if (messageText.Length == 0 || !messageText.ToLower().StartsWith("/st "))
                    return;

                sendToOthers = false;

                var parts = messageText.Substring(4).Trim(' ').Split(' '); // Convert commands to be more parseable

                if (parts[0] == "")
                {
                    ShowHelp();
                    return;
                }

                var command = parts[0].ToLower();

                // Really basic command handler
                if (_commands.ContainsKey(command))
                    _commands[command].Action.Invoke(parts);
                else
                    MyAPIGateway.Utilities.ShowMessage("ShareTrack",
                        $"Unrecognized command \"{command}\".");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        ///     Registers a command for ShareTrack' command handler.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="action"></param>
        /// <param name="modName"></param>
        public static void AddCommand(string command, string helpText, Action<string[]> action,
            string modName = "ShareTrack")
        {
            if (I == null)
                return;

            command = command.ToLower();
            if (I._commands.ContainsKey(command))
            {
                Log.Error(new Exception("Attempted to add duplicate command " + command + " from [" + modName + "]"));
                return;
            }

            I._commands.Add(command, new Command(modName, helpText, action));
            Log.Info($"Registered new chat command \"!{command}\" from [{modName}]");
        }

        /// <summary>
        ///     Removes a command from ShareTrack' command handler.
        /// </summary>
        /// <param name="command"></param>
        public static void RemoveCommand(string command)
        {
            command = command.ToLower();
            if (I == null || command == "help" || command == "debug") // Debug and Help should never be removed.
                return;
            if (I._commands.Remove(command))
                Log.Info($"De-registered chat command \"!{command}\".");
        }

        private class Command
        {
            public readonly Action<string[]> Action;
            public readonly string HelpText;
            public readonly string ModName;

            public Command(string modName, string helpText, Action<string[]> action)
            {
                ModName = modName;
                HelpText = helpText;
                Action = action;
            }
        }
    }
}