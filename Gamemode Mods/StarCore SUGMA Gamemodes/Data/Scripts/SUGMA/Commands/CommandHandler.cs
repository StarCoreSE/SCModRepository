using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using SC.SUGMA.Utilities;

namespace SC.SUGMA.Commands
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
                "SUGMA",
                "Displays command help.",
                message => I.ShowHelp()),

            #region Match Commands

            ["start"] = new Command(
                "SUGMA.Match",
                "Begins a new match of type [arg1] with arguments. Run without arguments for a list of available gamemodes.",
                CommandMethods.Start
            ),
            ["end"] = new Command(
                "SUGMA.Match",
                "Ends the current match.",
                CommandMethods.End
            ),
            ["pause"] = new Command(
                "SUGMA.Match",
                "Locks all grids in place and disables weapons.",
                CommandMethods.Pause
            ),
            ["clearboard"] = new Command(
                "SUGMA.Match",
                "Ends the current match, deletes all grids, and returns players to the respawn screen.",
                CommandMethods.ClearBoard
            ),

            #endregion

            #region Util Commands

            ["shields"] = new Command(
                "SUGMA.Utils",
                "Fills shields to full capacity.",
                CommandMethods.Shields
            ),
            ["problem"] = new Command(
                "SUGMA.Utils",
                "Reports a problem with optional message [arg1].",
                CommandMethods.ReportProblem
            ),
            ["fixed"] = new Command(
                "SUGMA.Utils",
                "Marks a problem as fixed.",
                CommandMethods.ReportFixed
            ),
            ["missing"] = new Command(
                "SUGMA.Utils",
                "Manual override missing players.",
                CommandMethods.ResolveMissingPlayers
            ),
            ["auto"] = new Command(
                "SUGMA.Utils",
                "Automatically balance tracked grids",
                CommandMethods.AutoBalance
            ),
            ["clearlcd"] = new Command(
                "SUGMA.Utils",
                "Manually clear all image lcds.",
                args => SUtils.ClearImageLcds()
                )

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

            MyAPIGateway.Utilities.ShowMessage("Universal Gamemodes Help", "");

            foreach (var modName in modNames)
            {
                foreach (var command in _commands)
                    if (command.Value.ModName == modName)
                        helpBuilder.Append($"\n{{/sc {command.Key}}}: " + command.Value.HelpText);

                MyAPIGateway.Utilities.ShowMessage($"[{modName}]", helpBuilder + "\n");
                helpBuilder.Clear();
            }
        }

        public static void Init()
        {
            Close(); // Close existing command handlers.
            I = new CommandHandler();
            MyAPIGateway.Utilities.MessageEnteredSender += I.Command_MessageEnteredSender;
            MyAPIGateway.Utilities.ShowMessage("SUGMA",
                "Run \"/sc help\" for commands.");
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
                if (messageText.Length == 0 || !messageText.ToLower().StartsWith("/sc "))
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
                    MyAPIGateway.Utilities.ShowMessage("Universal Gamemodes",
                        $"Unrecognized command \"{command}\".");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(CommandHandler));
            }
        }

        /// <summary>
        ///     Registers a command for Universal Gamemodes' command handler.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="action"></param>
        /// <param name="modName"></param>
        public static void AddCommand(string command, string helpText, Action<string[]> action,
            string modName = "Universal Gamemodes")
        {
            if (I == null)
                return;

            command = command.ToLower();
            if (I._commands.ContainsKey(command))
            {
                Log.Exception(
                    new Exception("Attempted to add duplicate command " + command + " from [" + modName + "]"),
                    typeof(CommandHandler));
                return;
            }

            I._commands.Add(command, new Command(modName, helpText, action));
            Log.Info($"Registered new chat command \"!{command}\" from [{modName}]");
        }

        /// <summary>
        ///     Removes a command from Universal Gamemodes' command handler.
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
