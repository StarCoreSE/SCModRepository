using RichHudFramework.Client;
using RichHudFramework.UI.Client;
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

            ControlCategory category = new ControlCategory();
            category.HeaderText = "";
            category.SubheaderText = "";
            ControlTile tileToggles = new ControlTile();
            ControlTile tileSliders = new ControlTile();

            controlPage.Add(category);

            category.Add(tileToggles);
            category.Add(tileSliders);

            TerminalOnOffButton toggleHitSounds = new TerminalOnOffButton()
            {
                Name = "Play HitSounds",
                CustomValueGetter = () => hitSounds.PlayHitSounds,
            };
            toggleHitSounds.ControlChangedHandler = (sender, args) => { hitSounds.PlayHitSounds = toggleHitSounds.Value; };
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
    }
}
