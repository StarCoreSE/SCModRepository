using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Utils;

namespace KingOfTheHill.Descriptions
{
    [ProtoContract]
    public class Session
    {
        public const string Filename = "Scores.data";

        [ProtoMember(1)]
        public List<PlanetDescription> PlanetScores { get; set; } = new List<PlanetDescription>();

        public static Session Load()
        {
            Session settings = new Session();
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(Filename, typeof(Session)))
                {
                    Tools.Log(MyLogSeverity.Info, "Loading saved settings");
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(Filename, typeof(Session));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    settings = MyAPIGateway.Utilities.SerializeFromXML<Session>(text);
                }
                else
                {
                    Tools.Log(MyLogSeverity.Info, "Config file not found. Loading default settings");
                    Save(settings);
                }
            }
            catch (Exception e)
            {
                Tools.Log(MyLogSeverity.Warning, $"Failed to load saved configuration. Loading defaults\n {e.ToString()}");
                Save(settings);
            }

            return settings;
        }

        public static void Save(Session settings)
        {
            try
            {
                Tools.Log(MyLogSeverity.Info, "Saving Settings");
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(Filename, typeof(Session));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                writer.Close();
            }
            catch (Exception e)
            {
                Tools.Log(MyLogSeverity.Error, $"Failed to save settings\n{e.ToString()}");
            }
        }
    }
}
