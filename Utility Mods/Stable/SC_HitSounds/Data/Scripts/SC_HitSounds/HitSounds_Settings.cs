using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Jnick_SCModRepository.SC_HitSounds.Data.Scripts.SC_HitSounds
{
    internal class HitSounds_Settings
    {
        public static void InitSettings(IMyModContext ModContext, HitSounds hitSounds)
        {
            RichHudClient.Init(ModContext.ModName, () => CreateSettings(hitSounds), null);
        }

        private static void CreateSettings(HitSounds hitSounds)
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
                    CustomValueGetter = () => hitSounds.ForceHitSounds,
                };
                toggleHitSounds.ControlChangedHandler = (sender, args) => { hitSounds.ForceHitSounds = toggleHitSounds.Value; };
                tileToggles.Add(toggleHitSounds);

                TerminalOnOffButton toggleCritSounds = new TerminalOnOffButton()
                {
                    Name = "Play CritSounds",
                    CustomValueGetter = () => hitSounds.PlayCritSounds,

                };
                toggleCritSounds.ControlChangedHandler = (sender, args) => { hitSounds.PlayCritSounds = toggleCritSounds.Value; };
                tileToggles.Add(toggleCritSounds);

                TerminalOnOffButton toggleKillSounds = new TerminalOnOffButton()
                {
                    Name = "Play KillSounds",
                    CustomValueGetter = () => hitSounds.PlayKillSounds,

                };
                toggleKillSounds.ControlChanged += (sender, args) => { hitSounds.PlayKillSounds = toggleKillSounds.Value; };
                tileToggles.Add(toggleKillSounds);
            }

            ControlTile tileSliders = new ControlTile();
            categoryTop.Add(tileSliders);
            {
                TerminalSlider sliderIntervalSounds = new TerminalSlider()
                {
                    Name = "Interval Between Sounds",
                    ToolTip = "Ticks (1/60s)",
                    CustomValueGetter = () => hitSounds.IntervalBetweenSounds,
                    Min = 0,
                    Max = 60,
                };
                sliderIntervalSounds.ControlChanged += (sender, args) => { hitSounds.IntervalBetweenSounds = (int)sliderIntervalSounds.Value; sliderIntervalSounds.ValueText = hitSounds.IntervalBetweenSounds.ToString(); };
                tileSliders.Add(sliderIntervalSounds);

                TerminalSlider sliderMinDamage = new TerminalSlider()
                {
                    Name = "Minimum Hit Damage",
                    ToolTip = "Counted per-block per-hit",
                    CustomValueGetter = () => hitSounds.MinDamageToPlay,
                    Min = 0,
                    Max = 16501,
                };
                sliderMinDamage.ControlChanged += (sender, args) => { hitSounds.MinDamageToPlay = (int)sliderMinDamage.Value; sliderMinDamage.ValueText = hitSounds.MinDamageToPlay.ToString(); };
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
                fxList_Hit.List.SetSelection(0);
                fxList_Hit.ControlChanged += (sender, args) => { hitSounds.CurrentHitSound = fxList_Hit.Value.AssocObject; };

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
                fxList_Crit.List.SetSelection(0);
                fxList_Crit.ControlChanged += (sender, args) => { hitSounds.CurrentCritSound = fxList_Crit.Value.AssocObject; };

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
                fxList_Kill.List.SetSelection(0);
                fxList_Kill.ControlChanged += (sender, args) => { hitSounds.CurrentKillSound = fxList_Kill.Value.AssocObject; };

                fxListKillTile.Add(fxList_Kill);
            }
            categorySounds.Add(fxListKillTile);
        }
    }
}
