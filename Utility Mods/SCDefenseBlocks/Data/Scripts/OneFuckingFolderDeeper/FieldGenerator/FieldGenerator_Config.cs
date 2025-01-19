using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame.Utilities; // this ingame namespace is safe to use in mods as it has nothing to collide with
using VRage.Utils;

namespace Starcore.FieldGenerator
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class FieldGenerator_Config : MySessionComponentBase
    {
        public static Generator_Settings Config { get; private set; } = new Generator_Settings();

        public override void LoadData()
        {
            Config.Load();
        }
    }

    public class Generator_Settings
    {
        const string VariableId = nameof(FieldGenerator_Config); // IMPORTANT: must be unique as it gets written in a shared space (sandbox.sbc)
        const string FileName = "FieldGenerator_Config.ini"; // the file that gets saved to world storage under your mod's folder
        const string IniSection = "FieldGenerator_Config";

        // settings you'd be reading, and their defaults.
        public bool SimplifiedMode = true;
        
        public int MaxModuleCount = 4;
        public float PerModuleAmount = 10f;

        public float MaxPowerDraw = 500.00f;
        public float MinPowerDraw = 50.00f;

        public int MaxSiegeTime = 60;
        public int SiegePowerDraw = 900;
        public float SiegeModeResistence = 0.9f;      
  
        // Stability Related Settings - Locked Behind Simplified
        public int DamageEventThreshold = 6;
        public int ResetInterval = 3;

        public int MinBlockCount = 2500;
        public int MaxBlockCount = 35000;

        public float SizeModifierMin = 1.2f;
        public float SizeModifierMax = 0.8f;    

        void LoadConfig(MyIni iniParser)
        {
            MaxModuleCount = iniParser.Get(IniSection, nameof(MaxModuleCount)).ToInt32(MaxModuleCount);
            PerModuleAmount = iniParser.Get(IniSection, nameof(PerModuleAmount)).ToSingle(PerModuleAmount);

            MaxPowerDraw = iniParser.Get(IniSection, nameof(MaxPowerDraw)).ToSingle(MaxPowerDraw);
            MinPowerDraw = iniParser.Get(IniSection, nameof(MinPowerDraw)).ToSingle(MinPowerDraw);

            MaxSiegeTime = iniParser.Get(IniSection, nameof(MaxSiegeTime)).ToInt32(MaxSiegeTime);
            SiegePowerDraw = iniParser.Get(IniSection, nameof(SiegePowerDraw)).ToInt32(SiegePowerDraw);
            SiegeModeResistence = iniParser.Get(IniSection, nameof(SiegeModeResistence)).ToSingle(SiegeModeResistence);

            SimplifiedMode = iniParser.Get(IniSection, nameof(SimplifiedMode)).ToBoolean(SimplifiedMode);

            DamageEventThreshold = iniParser.Get(IniSection, nameof(DamageEventThreshold)).ToInt32(DamageEventThreshold);
            ResetInterval = iniParser.Get(IniSection, nameof(ResetInterval)).ToInt32(ResetInterval);

            MinBlockCount = iniParser.Get(IniSection, nameof(MinBlockCount)).ToInt32(MinBlockCount);
            MaxBlockCount = iniParser.Get(IniSection, nameof(MaxBlockCount)).ToInt32(MaxBlockCount);

            SizeModifierMin = iniParser.Get(IniSection, nameof(SizeModifierMin)).ToSingle(SizeModifierMin);       
            SizeModifierMax = iniParser.Get(IniSection, nameof(SizeModifierMax)).ToSingle(SizeModifierMax);       
        }

        void SaveConfig(MyIni iniParser)
        {
            iniParser.Set(IniSection, nameof(MaxModuleCount), MaxModuleCount);
            iniParser.SetComment(IniSection, nameof(MaxModuleCount),
                " \n[Maximum number of upgrade modules that can be attached to the Field Generator core.]\n" +
                "[Each core has 4 mounting points by default.]\n" +
                "[Default: 4]");

            iniParser.Set(IniSection, nameof(PerModuleAmount), PerModuleAmount);
            iniParser.SetComment(IniSection, nameof(PerModuleAmount),
                " \n[Amount of resistance each attached upgrade module provides.]\n" +
                "[Default: 10.0]");

            iniParser.Set(IniSection, nameof(MaxPowerDraw), MaxPowerDraw);
            iniParser.SetComment(IniSection, nameof(MaxPowerDraw),
                " \n[The maximum power draw (in MW) when the Field Generator is at full power.]\n" +
                "[Default: 500 MW]");

            iniParser.Set(IniSection, nameof(MinPowerDraw), MinPowerDraw);
            iniParser.SetComment(IniSection, nameof(MinPowerDraw),
                " \n[Baseline power draw (in MW) at minimum field power.]\n" +
                "[Default: 50 MW]");

            iniParser.Set(IniSection, nameof(MaxSiegeTime), MaxSiegeTime);
            iniParser.SetComment(IniSection, nameof(MaxSiegeTime),
                " \n[Maximum duration (in seconds) the Field Generator can remain in Siege mode.]\n" +
                "[Default: 60s]\n");

            iniParser.Set(IniSection, nameof(SiegePowerDraw), SiegePowerDraw);
            iniParser.SetComment(IniSection, nameof(SiegePowerDraw),
                " \n[Power draw (in MW) while Siege mode is active.]\n" +
                "[Overrides normal scaled power draw.]\n" +
                "[Default: 900 MW]");

            iniParser.Set(IniSection, nameof(SiegeModeResistence), SiegeModeResistence);
            iniParser.SetComment(IniSection, nameof(SiegeModeResistence),
                " \n[Amount of damage resistance provided by Siege mode (0.0 to 1.0).]\n" +
                "[Example: 0.9 means 90% damage reduction from normal.]\n" +
                "[Default: 0.9]");

            iniParser.Set(IniSection, nameof(SimplifiedMode), SimplifiedMode);
            iniParser.SetComment(IniSection, nameof(SimplifiedMode),
                " \n[Whether to disable (true) or enable (false) the advanced stability system.]\n" +
                "[Default: true]");

            iniParser.Set(IniSection, nameof(DamageEventThreshold), DamageEventThreshold);
            iniParser.SetComment(IniSection, nameof(DamageEventThreshold),
                " \n[Number of damage events (within ResetInterval) needed to trigger stability reduction.]\n" +
                "[Default: 6]");

            iniParser.Set(IniSection, nameof(ResetInterval), ResetInterval);
            iniParser.SetComment(IniSection, nameof(ResetInterval),
                " \n[Time interval (in seconds) between damage counter resets.]\n" +
                "[Default: 3]");

            iniParser.Set(IniSection, nameof(MinBlockCount), MinBlockCount);
            iniParser.SetComment(IniSection, nameof(MinBlockCount),
                " \n[Minimum grid block count used in the size-based stability calculation.]\n" +
                "[Default: 2500]");

            iniParser.Set(IniSection, nameof(MaxBlockCount), MaxBlockCount);
            iniParser.SetComment(IniSection, nameof(MaxBlockCount),
                " \n[Maximum grid block count used in the size-based stability calculation.]\n" +
                "[Default: 35000]");

            iniParser.Set(IniSection, nameof(SizeModifierMin), SizeModifierMin);
            iniParser.SetComment(IniSection, nameof(SizeModifierMin),
                " \n[The lower bound of the size modifier.]\n" +
                "[Size Modifier can reduce or increase stability change based on the grid size. This Min is the Increase at Min Grid Size]\n" +
                "[Default: 1.2]");

            iniParser.Set(IniSection, nameof(SizeModifierMax), SizeModifierMax);
            iniParser.SetComment(IniSection, nameof(SizeModifierMax),
                " \n[The upper bound of the size modifier.]\n" +
                "[Size Modifier can reduce or increase stability change based on the grid size. This Max is the Reduction at Max Grid Size]\n" +
                "[Default: 0.8]");
        }

        // nothing to edit below this point

        public Generator_Settings()
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

            if(MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(Generator_Settings)))
            {
                using(TextReader file = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(Generator_Settings)))
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

            using(TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(Generator_Settings)))
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