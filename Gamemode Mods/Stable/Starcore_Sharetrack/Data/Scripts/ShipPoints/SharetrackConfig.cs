using Sandbox.ModAPI;
using System;
using System.IO;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;

namespace StarCore.ShareTrack
{
    internal class SharetrackConfig
    {
        const string Filename = "ShareTrack_Settings.ini";

        #region Settings Values

        public bool AllowGridTracking = true;
        public bool AutoTrack = true;

        #endregion

        public SharetrackConfig()
        {
            MyIni ini = new MyIni();
            LoadSavedSettings(ini);
            StoreSettings();
        }

        private void LoadSavedSettings(MyIni ini)
        {
            MyIniParseResult result;

            if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(Filename, typeof(SharetrackConfig)) ||
                !ini.TryParse(ReadFileSafe(Filename), out result))
            {
                // Load default settings
                LoadDefaults();
                return;
            }

            try
            {
                AllowGridTracking = ini.Get("sharetrack", "allowGridTracking").ToBoolean();
                AutoTrack = ini.Get("sharetrack", "autoTrack").ToBoolean();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error parsing {Filename}!\n" + ex);
                LoadDefaults();
            }
        }

        public void StoreSettings()
        {
            MyAPIGateway.Utilities.DeleteFileInWorldStorage(Filename, typeof(SharetrackConfig));
            MyIni ini = new MyIni();
            ini.AddSection("ShareTrack");
            ini.SetSectionComment("ShareTrack", "ShareTrack Config\n - by Aristeas\nSettings are read on session load, and saved on session unload.\nInvalid values are set to default.\n");

            ini.Set("sharetrack", "allowGridTracking", AllowGridTracking);
            ini.SetComment("sharetrack", "allowGridTracking", "If false, disallow players from tracking grids.");

            ini.Set("sharetrack", "autoTrack", AutoTrack);
            ini.SetComment("sharetrack", "autoTrack", "If true, automatically track grids counted as alive.");

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(Filename, typeof(SharetrackConfig));
            writer.Write(ini.ToString());
            writer.Flush();
            writer.Close();
        }

        private void LoadDefaults()
        {
            AllowGridTracking = true;
            AutoTrack = true;
        }

        private static string ReadFileSafe(string fileName)
        {
            var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, typeof(SharetrackConfig));
            string str = reader.ReadToEnd();
            reader.Close();
            return str;
        }
    }
}
