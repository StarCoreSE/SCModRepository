using System;
using System.Linq;

namespace SC.SUGMA.Utilities
{
    /// <summary>
    /// Parses a string array and triggers predefined actions.
    /// </summary>
    public class ArgumentParser
    {
        private ArgumentDefinition[] _definitions;

        public ArgumentParser(params ArgumentDefinition[] definitions)
        {
            _definitions = definitions;
            if (definitions == null || definitions.Length == 0)
                throw new ArgumentException("Definitions array should not be null or empty!");
        }

        public void ParseArguments(string[] arguments)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                foreach (ArgumentDefinition argumentDefinition in _definitions)
                {
                    string next = arguments.Length <= i + 1 ? "" : arguments[i + 1];
                    if (argumentDefinition.TryParse(arguments[i], next))
                    {
                        if (next.Length > 0 && next[0] != '-')
                            i++;
                        break;
                    }
                }
            }
        }

        public ArgumentParser AddDefinition(params ArgumentDefinition[] definitions)
        {
            return new ArgumentParser(_definitions.Concat(definitions).ToArray());
        }

        public string HelpText => string.Join("\n", _definitions.Select(d => d.HelpText));

        public static ArgumentParser operator +(ArgumentParser a, ArgumentParser b) => a.AddDefinition(b._definitions);

        public class ArgumentDefinition
        {
            /// <summary>
            /// Single-dash short argument key. Ex: [-a] for "select all". Does not contain the leading [-].
            /// </summary>
            public readonly string ShortKey;

            /// <summary>
            /// Double-dash short argument key. Ex: [--all] for "select all". Does not contain the leading [--].
            /// </summary>
            public readonly string LongKey;

            /// <summary>
            /// Help text displayed in [help cmd]
            /// </summary>
            public readonly string HelpText;

            public Action<string> OnTrigger;

            public ArgumentDefinition(Action<string> onTrigger, string shortKey, string longKey = null, string helpText = "")
            {
                ShortKey = shortKey;
                LongKey = longKey;
                OnTrigger = onTrigger;
                HelpText = $"[-{shortKey}]{(longKey == null ? "" : $" [--{longKey}]")}: {helpText}";

                if (string.IsNullOrWhiteSpace(ShortKey))
                    throw new ArgumentException("ShortKey should not be null or whitespace!");

                if (LongKey != null && string.IsNullOrWhiteSpace(LongKey))
                    throw new ArgumentException("LongKey should not be whitespace!");
            }

            internal bool TryParse(string argument, string next)
            {
                string parsed = null;
                if (argument.StartsWith("-" + ShortKey))
                {
                    parsed = argument.Remove(0, ShortKey.Length + 1);
                }
                else if (LongKey != null && argument.StartsWith("--" + LongKey))
                {
                    parsed = argument.Remove(0, LongKey.Length + 2);
                }

                if (parsed == null || parsed != string.Empty)
                    return false;
                Log.Info("Parsed! Arg: " + argument + " | Next: " + next);
                OnTrigger?.Invoke(next.StartsWith("-") ? "" : next);
                return true;
            }
        }
    }
}
