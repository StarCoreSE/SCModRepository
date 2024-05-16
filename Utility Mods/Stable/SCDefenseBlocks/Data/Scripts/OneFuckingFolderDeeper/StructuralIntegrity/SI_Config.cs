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

namespace StarCore.StructuralIntegrity
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SI_Config : MySessionComponentBase
    {
        readonly Config_Settings Settings = new Config_Settings();

        public override void LoadData()
        {
            Settings.Load();

            // example usage/debug
            Log.Info($"MinFieldPower value={Settings.MinFieldPower}");
            Log.Info($"MaxFieldPower value={Settings.MaxFieldPower}");
            Log.Info($"MinGridModifier value={Settings.MinGridModifier}");
            Log.Info($"MaxGridModifier value={Settings.MaxGridModifier}");

            Log.Info($"BasePowerUsage value={Settings.BasePowerUsage}");
            Log.Info($"MaxModifierPowerPercentage value={Settings.MaxModifierPowerPercentage}");
            Log.Info($"MinModifierPowerPercentage value={Settings.MinModifierPowerPercentage}");

            Log.Info($"SiegeEnabled value={Settings.SiegeEnabled}");
            Log.Info($"SiegeMinPowerReq value={Settings.SiegeMinPowerReq}");
            Log.Info($"SiegeTimer value={Settings.SiegeTimer}");
            Log.Info($"SiegeCooldownTimer value={Settings.SiegeCooldownTimer}");
            Log.Info($"SiegePowerPercentage value={Settings.SiegePowerPercentage}");
        }
    }

    public class Config_Settings
    {
        const string VariableId = nameof(SI_Config); // IMPORTANT: must be unique as it gets written in a shared space (sandbox.sbc)
        const string FileName = "StructuralIntegrity.ini"; // the file that gets saved to world storage under your mod's folder
        const string IniSection = "Config";

        public float MinFieldPower = 0f;
        public float MaxFieldPower = 50f;
        public float MinGridModifier = 1.0f;
        public float MaxGridModifier = 0.5f;

        public float BasePowerUsage = 50f;
        public float MaxModifierPowerPercentage = 0.5f;
        public float MinModifierPowerPercentage = 0f;

        public bool SiegeEnabled = true;
        public float SiegeMinPowerReq = 150f;
        public int SiegeTimer = 9000;
        public int SiegeCooldownTimer = 18000;
        public float SiegePowerPercentage = 0.9f;

        void LoadConfig(MyIni iniParser)
        {
            MinFieldPower = iniParser.Get(IniSection, nameof(MinFieldPower)).ToSingle(MinFieldPower);
            MaxFieldPower = iniParser.Get(IniSection, nameof(MaxFieldPower)).ToSingle(MaxFieldPower);
            MinGridModifier = iniParser.Get(IniSection, nameof(MinGridModifier)).ToSingle(MinGridModifier);
            MaxGridModifier = iniParser.Get(IniSection, nameof(MaxGridModifier)).ToSingle(MaxGridModifier);

            BasePowerUsage = iniParser.Get(IniSection, nameof(BasePowerUsage)).ToSingle(BasePowerUsage);
            MinModifierPowerPercentage = iniParser.Get(IniSection, nameof(MinModifierPowerPercentage)).ToSingle(MinModifierPowerPercentage);
            MaxModifierPowerPercentage = iniParser.Get(IniSection, nameof(MaxModifierPowerPercentage)).ToSingle(MaxModifierPowerPercentage);     

            SiegeEnabled = iniParser.Get(IniSection, nameof(SiegeEnabled)).ToBoolean(SiegeEnabled);
            SiegeMinPowerReq = iniParser.Get(IniSection, nameof(SiegeMinPowerReq)).ToSingle(SiegeMinPowerReq);
            SiegeTimer = (int)iniParser.Get(IniSection, nameof(SiegeTimer)).ToSingle(SiegeTimer);
            SiegeCooldownTimer = (int)iniParser.Get(IniSection, nameof(SiegeCooldownTimer)).ToSingle(SiegeCooldownTimer);
            SiegePowerPercentage = iniParser.Get(IniSection, nameof(SiegePowerPercentage)).ToSingle(SiegePowerPercentage);
        }

        void SaveConfig(MyIni iniParser)
        {
            // repeat for each setting field
            iniParser.Set(IniSection, nameof(MinFieldPower), MinFieldPower);
            iniParser.SetComment(IniSection, nameof(MinFieldPower), "Minimum Value for Power Diversion in Percentage [Default: 0]"); // optional

            iniParser.Set(IniSection, nameof(MaxFieldPower), MaxFieldPower);
            iniParser.SetComment(IniSection, nameof(MaxFieldPower), "Maximum Value for Power Diversion in Percentage [Default: 30]"); // optional

            iniParser.Set(IniSection, nameof(MinGridModifier), MinGridModifier);
            iniParser.SetComment(IniSection, nameof(MinGridModifier), "Minimum Value for Potential Grid Resistence [Default: 1.0]"); // optional

            iniParser.Set(IniSection, nameof(MaxGridModifier), MaxGridModifier);
            iniParser.SetComment(IniSection, nameof(MaxGridModifier), "Maximum Value for Potential Grid Resistence [Default: 0.7]"); // optional

            iniParser.Set(IniSection, nameof(BasePowerUsage), BasePowerUsage);
            iniParser.SetComment(IniSection, nameof(BasePowerUsage), "Base Power Required while Idle in MW [Default: 50.000]"); // optional

            iniParser.Set(IniSection, nameof(MinModifierPowerPercentage), MinModifierPowerPercentage);
            iniParser.SetComment(IniSection, nameof(MinModifierPowerPercentage), "Mnimum Value for Power Usage Percentage at Min Resistence [Default: 0] [Percentage of Grid Max Power]"); // optional

            iniParser.Set(IniSection, nameof(MaxModifierPowerPercentage), MaxModifierPowerPercentage);
            iniParser.SetComment(IniSection, nameof(MaxModifierPowerPercentage), "Maximum Value for Power Usage Percentage at Max Resistence [Default: 0.5] [Percentage of Grid Max Power]"); // optional

            iniParser.Set(IniSection, nameof(SiegeEnabled), SiegeEnabled);
            iniParser.SetComment(IniSection, nameof(SiegeEnabled), "True/False Value to Enable or Disable Siege Mode"); // optional

            iniParser.Set(IniSection, nameof(SiegeMinPowerReq), SiegeMinPowerReq);
            iniParser.SetComment(IniSection, nameof(SiegeMinPowerReq), "Minimum Power Required for Siege Mode Activation in MW [Default: 150] [WARNING!: Setting Lower than 100 MW may break Siege Mode]"); // optional       

            iniParser.Set(IniSection, nameof(SiegeTimer), SiegeTimer);
            iniParser.SetComment(IniSection, nameof(SiegeTimer), "Siege Mode Timer Value in Ticks [Default: 9000]"); // optional

            iniParser.Set(IniSection, nameof(SiegeCooldownTimer), SiegeCooldownTimer);
            iniParser.SetComment(IniSection, nameof(SiegeCooldownTimer), "Siege Mode Cooldown Timer Value in Ticks [Default: 18000]"); // optional

            iniParser.Set(IniSection, nameof(SiegePowerPercentage), SiegePowerPercentage);
            iniParser.SetComment(IniSection, nameof(SiegePowerPercentage), "Power Usage Percentage during Siege Mode [Default: 0.9] [Percentage of Grid Max Power]"); // optional
        }

        // nothing to edit below this point

        public Config_Settings()
        {
        }

        public void Load()
        {
            if (MyAPIGateway.Session.IsServer)
                LoadOnHost();
            else
                LoadOnClient();
        }

        void LoadOnHost()
        {
            MyIni iniParser = new MyIni();

            // load file if exists then save it regardless so that it can be sanitized and updated

            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(Config_Settings)))
            {
                using (TextReader file = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(Config_Settings)))
                {
                    string text = file.ReadToEnd();

                    MyIniParseResult result;
                    if (!iniParser.TryParse(text, out result))
                        throw new Exception($"Config error: {result.ToString()}");

                    LoadConfig(iniParser);
                }
            }

            iniParser.Clear(); // remove any existing settings that might no longer exist

            SaveConfig(iniParser);

            string saveText = iniParser.ToString();

            MyAPIGateway.Utilities.SetVariable(VariableId, saveText);

            using (TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(Config_Settings)))
            {
                file.Write(saveText);
            }
        }

        void LoadOnClient()
        {
            string text;
            if (!MyAPIGateway.Utilities.GetVariable(VariableId, out text))
                throw new Exception("No config found in sandbox.sbc!");

            MyIni iniParser = new MyIni();
            MyIniParseResult result;
            if (!iniParser.TryParse(text, out result))
                throw new Exception($"Config error: {result.ToString()}");

            LoadConfig(iniParser);
        }
    }
}