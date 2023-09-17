using System;
using System.IO;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using static KillFeed.Core;

// This was just pulled from example script, it's a good enough logger

namespace KillFeed
{
    internal class XmlOutput
    {
        private static XmlOutput _instance;
        private readonly StringBuilder _cache = new StringBuilder();
        private int _indent;

        private readonly TextWriter _writer;

        public XmlOutput(string xmlFile)
        {
            try
            {
                _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(xmlFile, typeof(XmlOutput));
                _instance = this;
            }
            catch
            {
            }
        }

        public static XmlOutput Instance
        {
            get
            {
                if (MyAPIGateway.Utilities == null)
                    return null;

                if (_instance == null)
                    _instance = new XmlOutput(Config.xmlFile);

                return _instance;
            }
        }

        private void IncreaseIndent()
        {
            _indent++;
        }

        private void DecreaseIndent()
        {
            if (_indent > 0) { _indent--; }
        }

        private void WriteLine(string text)
        {
            try
            {
                if (text.StartsWith("</"))
                {
                    DecreaseIndent();
                }

                if (_cache.Length > 0) { _writer.WriteLine(_cache); }
                _cache.Clear();
                for (var i = 0; i < _indent; i++) { _cache.Append("\t"); }
                _writer.WriteLine(_cache.Append(text));
                _writer.Flush();
                _cache.Clear();

                if (text.StartsWith("<") && !text.StartsWith("</") && !text.Contains("</"))
                {
                    IncreaseIndent();
                }
            }
            catch
            {
            }
        }

        public void Write(GridAttack attack, IMyIdentity victim)
        {
            WriteLine("<kill>");

            WriteLine("<time>" + DateTime.UtcNow.ToFileTimeUtc() + "</time>");

            WriteLine("<attacker>");
            WriteLine("<name>" + attack.attacker.DisplayName + "</name>");
            WriteLine("<id>" + attack.attacker.IdentityId + "</id>");
            WriteLine("<faction>");
            IMyFaction attackerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(attack.attacker.IdentityId);
            WriteLine("<tag>" + ((attackerFaction != null) ? attackerFaction.Tag : "") + "</tag>");
            WriteLine("<id>" + ((attackerFaction != null) ? attackerFaction.FactionId.ToString() : "") + "</id>");
            WriteLine("</faction>");
            WriteLine("</attacker>");

            WriteLine("<victim>");
            WriteLine("<name>" + victim.DisplayName + "</name>");
            WriteLine("<id>" + victim.IdentityId + "</id>");
            WriteLine("<steamid>" + Utilities.IdentitySteamId(victim) + "</steamid>");
            WriteLine("<faction>");
            IMyFaction victimFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(victim.IdentityId);
            WriteLine("<tag>" + ((victimFaction != null) ? victimFaction.Tag : "") + "</tag>");
            WriteLine("<id>" + ((victimFaction != null) ? victimFaction.FactionId.ToString() : "") + "</id>");
            WriteLine("</faction>");
            WriteLine("<ship>");
            WriteLine("<size>" + attack.blockCount + "</size>");
            if (attack.gridName != null)
            {
                WriteLine("<name>" + attack.gridName + "</name>");
            }
            WriteLine("</ship>");
            WriteLine("</victim>");

            if (attack.position != null)
            {
                WriteLine("<position>");
                WriteLine("<x>" + attack.position.X + "</x>");
                WriteLine("<y>" + attack.position.Y + "</y>");
                WriteLine("<z>" + attack.position.Z + "</z>");
                WriteLine("</position>");
            }

            WriteLine("</kill>");
        }


        internal void Close()
        {
            if (_cache.Length > 0) { _writer.WriteLine(_cache); }
            _writer.Flush();
            _writer.Close();
        }
    }
}