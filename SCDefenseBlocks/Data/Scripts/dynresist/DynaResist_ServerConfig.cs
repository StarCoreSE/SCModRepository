using System;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace StarCore.DynamicResistence
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DynaResist_ServerConfig : MySessionComponentBase
    {
        Config_Settings Settings = new Config_Settings();

        public override void LoadData()
        {
            Settings.Load();           

            // example usage/debug
            Log.Info($"MinDivertedPower value={Settings.MinDivertedPower}");
            Log.Info($"MaxDivertedPower value={Settings.MaxDivertedPower}");
            Log.Info($"MinResistModifier value={Settings.MinResistModifier}");
            Log.Info($"MaxResistModifier value={Settings.MaxResistModifier}");
            Log.Info($"SiegePowerRequirement value={Settings.SiegePowerMinimumRequirement}");
            Log.Info($"SiegeTimer value={Settings.SiegeTimer}");
            Log.Info($"SiegeCooldownTimer value={Settings.SiegeCooldownTimer}");
        }
    }

    public class Config_Settings
    {
        const string VariableId = nameof(DynaResist_ServerConfig); // IMPORTANT: must be unique as it gets written in a shared space (sandbox.sbc)
        const string FileName = "DynaResist_Config.ini"; // the file that gets saved to world storage under your mod's folder
        const string IniSection = "Config";

        public float MinDivertedPower = 0f;
        public float MaxDivertedPower = 30f;
        public float MinResistModifier = 1.0f;
        public float MaxResistModifier = 0.7f;
        public float SiegePowerMinimumRequirement = 150f;
        public int SiegeTimer = 9000;
        public int SiegeCooldownTimer = 18000;

        void LoadConfig(MyIni iniParser)
        {
            MinDivertedPower = iniParser.Get(IniSection, nameof(MinDivertedPower)).ToSingle(MinDivertedPower);
            MaxDivertedPower = iniParser.Get(IniSection, nameof(MaxDivertedPower)).ToSingle(MaxDivertedPower);
            MinResistModifier = iniParser.Get(IniSection, nameof(MinResistModifier)).ToSingle(MinResistModifier);
            MaxResistModifier = iniParser.Get(IniSection, nameof(MaxResistModifier)).ToSingle(MaxResistModifier);
            SiegePowerMinimumRequirement = iniParser.Get(IniSection, nameof(SiegePowerMinimumRequirement)).ToSingle(SiegePowerMinimumRequirement);
            SiegeTimer = (int)iniParser.Get(IniSection, nameof(SiegeTimer)).ToSingle(SiegeTimer);
            SiegeCooldownTimer = (int)iniParser.Get(IniSection, nameof(SiegeCooldownTimer)).ToSingle(SiegeCooldownTimer);                  
        }

        void SaveConfig(MyIni iniParser)
        {
            // repeat for each setting field
            iniParser.Set(IniSection, nameof(MinDivertedPower), MinDivertedPower);
            iniParser.SetComment(IniSection, nameof(MinDivertedPower), "Minimum Value for Power Diversion in Percentage [Default: 0]"); // optional

            iniParser.Set(IniSection, nameof(MaxDivertedPower), MaxDivertedPower);
            iniParser.SetComment(IniSection, nameof(MaxDivertedPower), "Maximum Value for Power Diversion in Percentage [Default: 30]"); // optional

            iniParser.Set(IniSection, nameof(MinResistModifier), MinResistModifier);
            iniParser.SetComment(IniSection, nameof(MinResistModifier), "Minimum Value for Potential Grid Resistence [Default: 1.0]"); // optional

            iniParser.Set(IniSection, nameof(MaxResistModifier), MaxResistModifier);
            iniParser.SetComment(IniSection, nameof(MaxResistModifier), "Maximum Value for Potential Grid Resistence [Default: 0.7]"); // optional

            iniParser.Set(IniSection, nameof(SiegePowerMinimumRequirement), SiegePowerMinimumRequirement);
            iniParser.SetComment(IniSection, nameof(SiegePowerMinimumRequirement), "Minimum Power Required for Siege Mode Activation in MW [Default: 150] [WARNING!: Setting Lower than 100 MW may break Siege Mode]"); // optional

            iniParser.Set(IniSection, nameof(SiegeTimer), SiegeTimer);
            iniParser.SetComment(IniSection, nameof(SiegeTimer), "Siege Mode Timer Value in Ticks [Default: 9000]"); // optional

            iniParser.Set(IniSection, nameof(SiegeCooldownTimer), SiegeCooldownTimer);
            iniParser.SetComment(IniSection, nameof(SiegeCooldownTimer), "Siege Mode Cooldown Timer Value in Ticks [Default: 18000]"); // optional
        }

        // nothing to edit below this point

        public Config_Settings()
        {
        }

        public void Load()
        {
            if(MyAPIGateway.Session.IsServer)
                LoadOnHost();
            else
                LoadOnClient();
        }

        void LoadOnHost()
        {
            MyIni iniParser = new MyIni();

            // load file if exists then save it regardless so that it can be sanitized and updated

            if(MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(Config_Settings)))
            {
                using(TextReader file = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(Config_Settings)))
                {
                    string text = file.ReadToEnd();

                    MyIniParseResult result;
                    if(!iniParser.TryParse(text, out result))
                        throw new Exception($"Config error: {result.ToString()}");

                    LoadConfig(iniParser);
                }
            }

            iniParser.Clear(); // remove any existing settings that might no longer exist

            SaveConfig(iniParser);

            string saveText = iniParser.ToString();

            MyAPIGateway.Utilities.SetVariable<string>(VariableId, saveText);

            using(TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(Config_Settings)))
            {
                file.Write(saveText);
            }
        }

        void LoadOnClient()
        {
            string text;
            if(!MyAPIGateway.Utilities.GetVariable<string>(VariableId, out text))
                throw new Exception("No config found in sandbox.sbc!");

            MyIni iniParser = new MyIni();
            MyIniParseResult result;
            if(!iniParser.TryParse(text, out result))
                throw new Exception($"Config error: {result.ToString()}");

            LoadConfig(iniParser);
        }
    }
}