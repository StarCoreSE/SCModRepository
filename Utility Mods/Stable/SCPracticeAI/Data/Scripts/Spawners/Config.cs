using ProtoBuf;
using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using ProtoBuf;

namespace Invalid.SCPracticeAI
{

    [ProtoContract]
    public class ModConfig
    {
        [ProtoMember(1)]
        public bool AutomaticSpawnBattle { get; set; }

        [ProtoMember(2)]
        public int AutomaticSpawnBattleAmount { get; set; }

        public ModConfig()
        {
            // Default values
            AutomaticSpawnBattle = false;
            AutomaticSpawnBattleAmount = 10;
        }
    }

    public class ConfigManager
    {
        private const string CONFIG_FILE_NAME = "SCPracticeAIConfig.xml";

        public static ModConfig LoadConfig()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(CONFIG_FILE_NAME, typeof(ConfigManager)))
            {
                try
                {
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(CONFIG_FILE_NAME, typeof(ConfigManager));
                    var xmlText = reader.ReadToEnd();
                    reader.Close();
                    return MyAPIGateway.Utilities.SerializeFromXML<ModConfig>(xmlText);
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole($"Error loading config: {ex.Message}");
                }
            }

            // If file doesn't exist or there's an error, return default config
            return new ModConfig();
        }

        public static void SaveConfig(ModConfig config)
        {
            try
            {
                var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(CONFIG_FILE_NAME, typeof(ConfigManager));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(config));
                writer.Close();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error saving config: {ex.Message}");
            }
        }
    }
}
