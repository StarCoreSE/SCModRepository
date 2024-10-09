using ProtoBuf;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;

namespace Jnick_SCModRepository.SC_HitSounds.Data.Scripts.SC_HitSounds
{
    [ProtoContract]
    internal class HitSounds_Settings
    {
        const string filename = "HitSounds_Settings.ini";

        public bool ForceHitSounds = false; // TODO: Save settings
        public bool PlayCritSounds = true;
        public bool PlayKillSounds = true;
        public int IntervalBetweenSounds = 4; // Ticks
        public int MinDamageToPlay = 100;

        public string CurrentHitSound
        {
            get
            {
                return currentHitSound;
            }
            set
            {
                if (HitSounds.I.HitSoundEffects.ContainsKey(value))
                    currentHitSound = value;
            }
        }
        public string CurrentCritSound
        {
            get
            {
                return currentCritSound;
            }
            set
            {
                if (HitSounds.I.CritSoundEffects.ContainsKey(value))
                    currentCritSound = value;
            }
        }
        public string CurrentKillSound
        {
            get
            {
                return currentKillSound;
            }
            set
            {
                if (HitSounds.I.KillSoundEffects.ContainsKey(value))
                    currentKillSound = value;
            }
        }

        private string currentHitSound = "TF2 Hitsound";
        private string currentCritSound = "TF2 Critsound";
        private string currentKillSound = "TF2 Killsound";

        public HashSet<string> validSorterWeapons = new HashSet<string>();

        public HitSounds_Settings() { }

        public void InitSettings(IMyModContext ModContext)
        {
            LoadSavedSettings();
            RichHudClient.Init(ModContext.ModName, CreateSettings, null);
        }

        public void LoadSavedSettings()
        {
            MyIni ini = new MyIni();
            LoadSavedSettings(ini);
            StoreSettings();
        }

        private void LoadSavedSettings(MyIni ini)
        {
            MyIniParseResult result;

            if (!MyAPIGateway.Utilities.FileExistsInGlobalStorage(filename) ||
                !ini.TryParse(ReadFileSafe(filename), out result))
            {
                // Load default settings
                LoadDefaults();
                return;
            }

            try
            {
                ForceHitSounds = ini.Get("hitsounds", "forceHitSounds").ToBoolean();
                PlayCritSounds = ini.Get("hitsounds", "playCritSounds").ToBoolean();
                PlayKillSounds = ini.Get("hitsounds", "playKillSounds").ToBoolean();

                IntervalBetweenSounds = ini.Get("hitsounds", "intervalBetweenSounds").ToInt32();
                MinDamageToPlay = ini.Get("hitsounds", "minDamageToPlay").ToInt32();

                CurrentHitSound = ini.Get("hitsounds", "currentHitSound").ToString();
                CurrentCritSound = ini.Get("hitsounds", "currentCritSound").ToString();
                CurrentKillSound = ini.Get("hitsounds", "currentKillSound").ToString();

                validSorterWeapons = ini.Get("allowedWeaponSubtypes", "subtypes").ToString().Split(',').ToHashSet();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("Error parsing HitSounds_Settings!\n" + ex);
                LoadDefaults();
            }
        }

        private void LoadDefaults()
        {
            ForceHitSounds = false;
            PlayCritSounds = true;
            PlayKillSounds = true;
            IntervalBetweenSounds = 4;
            MinDamageToPlay = 100;

            CurrentHitSound = "TF2 Hitsound";
            CurrentCritSound = "TF2 Critsound";
            CurrentKillSound = "TF2 Killsound";
        }

        private static string ReadFileSafe(string fileName)
        {
            var reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(fileName);
            string str = reader.ReadToEnd();
            reader.Close();
            return str;
        }

        public void StoreSettings()
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(filename);
            MyIni ini = new MyIni();
            ini.AddSection("HitSounds");
            ini.SetSectionComment("HitSounds", "HitSounds Config\n - by Aristeas\nSettings are read on session load, and saved on session unload.\nInvalid values are set to default.\n");

            ini.Set("hitsounds", "forceHitSounds", ForceHitSounds);
            ini.SetComment("hitsounds", "forceHitSounds", "If true, play hit sounds on all weapons.");

            ini.Set("hitsounds", "playCritSounds", PlayCritSounds);
            ini.SetComment("hitsounds", "playCritSounds", "If false, disable crit (reactor destroy) sounds.");

            ini.Set("hitsounds", "playKillSounds", PlayKillSounds);
            ini.SetComment("hitsounds", "playKillSounds", "If false, disable kill (cockpit destroy) sounds.");

            ini.Set("hitsounds", "intervalBetweenSounds", IntervalBetweenSounds);
            ini.SetComment("hitsounds", "intervalBetweenSounds", "Minimum interval between sounds, in gameticks (1/60s)");

            ini.Set("hitsounds", "minDamageToPlay", MinDamageToPlay);
            ini.SetComment("hitsounds", "minDamageToPlay", "Minimum single-hit damage to trigger a sound.");



            ini.Set("HitSoundSelection", "currentHitSound", CurrentHitSound);
            ini.SetComment("HitSoundSelection", "currentHitSound", "Available sounds: " + string.Join(", ", HitSounds.I.HitSoundEffects.Keys));

            ini.Set("HitSoundSelection", "currentCritSound", CurrentCritSound);
            ini.SetComment("HitSoundSelection", "currentCritSound", "Available sounds: " + string.Join(", ", HitSounds.I.CritSoundEffects.Keys));

            ini.Set("HitSoundSelection", "currentKillSound", CurrentKillSound);
            ini.SetComment("HitSoundSelection", "currentKillSound", "Available sounds: " + string.Join(", ", HitSounds.I.KillSoundEffects.Keys));



            ini.Set("allowedWeaponSubtypes", "subtypes", string.Join(",", validSorterWeapons));

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(filename);
            writer.Write(ini.ToString());
            writer.Flush();
            writer.Close();
        }

        private void CreateSettings()
        {
            RichHudTerminal.Root.Enabled = true;
            ControlPage controlPage = new ControlPage
            {
                Name = "Settings"
            };
            RichHudTerminal.Root.Add(controlPage);

            ControlCategory categoryTop = new ControlCategory
            {
                HeaderText = "",
                SubheaderText = ""
            };
            controlPage.Add(categoryTop);

            ControlTile tileToggles = new ControlTile();
            categoryTop.Add(tileToggles);
            {
                TerminalOnOffButton toggleHitSounds = new TerminalOnOffButton()
                {
                    Name = "Override HitSounds",
                    ToolTip = "Force-enables hitsounds on all weapons.",
                    CustomValueGetter = () => ForceHitSounds,
                };
                toggleHitSounds.ControlChangedHandler = (sender, args) => { ForceHitSounds = toggleHitSounds.Value; };
                tileToggles.Add(toggleHitSounds);

                TerminalOnOffButton toggleCritSounds = new TerminalOnOffButton()
                {
                    Name = "Play CritSounds",
                    CustomValueGetter = () => PlayCritSounds,

                };
                toggleCritSounds.ControlChangedHandler = (sender, args) => { PlayCritSounds = toggleCritSounds.Value; };
                tileToggles.Add(toggleCritSounds);

                TerminalOnOffButton toggleKillSounds = new TerminalOnOffButton()
                {
                    Name = "Play KillSounds",
                    CustomValueGetter = () => PlayKillSounds,

                };
                toggleKillSounds.ControlChanged += (sender, args) => { PlayKillSounds = toggleKillSounds.Value; };
                tileToggles.Add(toggleKillSounds);
            }

            ControlTile tileSliders = new ControlTile();
            categoryTop.Add(tileSliders);
            {
                TerminalSlider sliderIntervalSounds = new TerminalSlider()
                {
                    Name = "Interval Between Sounds",
                    ToolTip = "Ticks (1/60s)",
                    CustomValueGetter = () => IntervalBetweenSounds,
                    Min = 0,
                    Max = 60,
                };
                sliderIntervalSounds.ControlChanged += (sender, args) => { IntervalBetweenSounds = (int)sliderIntervalSounds.Value; sliderIntervalSounds.ValueText = IntervalBetweenSounds.ToString(); };
                tileSliders.Add(sliderIntervalSounds);

                TerminalSlider sliderMinDamage = new TerminalSlider()
                {
                    Name = "Minimum Hit Damage",
                    ToolTip = "Counted per-block per-hit",
                    CustomValueGetter = () => MinDamageToPlay,
                    Min = 0,
                    Max = 16501,
                };
                sliderMinDamage.ControlChanged += (sender, args) => { MinDamageToPlay = (int)sliderMinDamage.Value; sliderMinDamage.ValueText = MinDamageToPlay.ToString(); };
                tileSliders.Add(sliderMinDamage);
            }

            ControlCategory categorySounds = new ControlCategory
            {
                HeaderText = "",
                SubheaderText = ""
            };
            controlPage.Add(categorySounds);

            ControlTile fxListHitTile = new ControlTile();
            {
                TerminalList<string> fxList_Hit = new TerminalList<string>
                {
                    Name = "Hit Sound",
                    ToolTip = "Sound to play on hit.",
                };
                foreach (var value in HitSounds.I.HitSoundEffects.Keys)
                    fxList_Hit.List.Add(value, value);
                fxList_Hit.List.SetSelection(CurrentHitSound);
                fxList_Hit.ControlChanged += (sender, args) => { CurrentHitSound = fxList_Hit.Value.AssocObject; };

                fxListHitTile.Add(fxList_Hit);
            }
            categorySounds.Add(fxListHitTile);


            ControlTile fxListCritTile = new ControlTile();
            {
                TerminalList<string> fxList_Crit = new TerminalList<string>
                {
                    Name = "Crit Sound",
                    ToolTip = "Sound to play on crit.",
                };
                foreach (var value in HitSounds.I.CritSoundEffects.Keys)
                    fxList_Crit.List.Add(value, value);
                fxList_Crit.List.SetSelection(CurrentCritSound);
                fxList_Crit.ControlChanged += (sender, args) => { CurrentCritSound = fxList_Crit.Value.AssocObject; };

                fxListCritTile.Add(fxList_Crit);
            }
            categorySounds.Add(fxListCritTile);


            ControlTile fxListKillTile = new ControlTile();
            {
                TerminalList<string> fxList_Kill = new TerminalList<string>
                {
                    Name = "Kill Sound",
                    ToolTip = "Sound to play on kill.",
                };
                foreach (var value in HitSounds.I.KillSoundEffects.Keys)
                    fxList_Kill.List.Add(value, value);
                fxList_Kill.List.SetSelection(CurrentKillSound);
                fxList_Kill.ControlChanged += (sender, args) => { CurrentKillSound = fxList_Kill.Value.AssocObject; };

                fxListKillTile.Add(fxList_Kill);
            }
            categorySounds.Add(fxListKillTile);
        }
    }
}
