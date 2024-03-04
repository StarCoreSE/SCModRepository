using CoreSystems.Api;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Jnick_SCModRepository.HitSounds.Data.Scripts.HitSounds
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class HitSounds : MySessionComponentBase
    {
        bool PlayHitSounds = false; // TODO: Save settings
        bool PlayCritSounds = true;
        bool PlayKillSounds = true;
        int IntervalBetweenSounds = 4; // Ticks
        int MinDamageToPlay = 100;

        public static HitSounds I = new HitSounds();
        WcApi wAPI;

        MyCharacterSoundComponent SoundEmitter = null;

        MySoundPair hitSound = new MySoundPair("SC_HitSound_TF2");
        MySoundPair critSound = new MySoundPair("SC_CritSound_TF2");
        MySoundPair killSound = new MySoundPair("SC_KillSound_TF2"); // Maybe-Todo: Kill sound for everyone

        int Ticks = 0;
        int LastSoundTick = 0;

        #region Base Methods
        public override void LoadData() // TODO: Add terminal controls to Sorters for toggling 'ding' noise. Alternatively, opt-in SubtypeId field. Alternatively, toggles for every weapon subtype.
        {
            I = this;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            wAPI = new WcApi();
            wAPI.Load();

            RichHudClient.Init(ModContext.ModName, InitRichHud, null);

            MyAPIGateway.Utilities.ShowMessage("HITSOUNDS", "Loaded...");
        }

        protected override void UnloadData()
        {
            I = null;
            //damageHandlerHelper.RegisterForDamage(modId, EventType.Unregister);
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnDamageEvent);
        }

        public override void UpdateAfterSimulation()
        {
            if (SoundEmitter == null && MyAPIGateway.Session.Player != null)
            {
                SoundEmitter = MyAPIGateway.Session.Player.Character?.Components.Get<MyCharacterSoundComponent>();
            }

            Ticks++;
        }
        #endregion

        #region RichHud Terminal

        void InitRichHud()
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
                CustomValueGetter = () => PlayHitSounds,
            };
            toggleHitSounds.ControlChangedHandler = (sender, args) => { PlayHitSounds = toggleHitSounds.Value; };
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

            TerminalSlider sliderIntervalSounds = new TerminalSlider()
            {
                Name = "Interval Between Sounds",
                ToolTip = "Ticks (1/60s)",
                CustomValueGetter = () => IntervalBetweenSounds,
                Min = 0,
                Max = 60,
            };
            sliderIntervalSounds.ControlChanged += (sender, args) => { IntervalBetweenSounds = (int) sliderIntervalSounds.Value; sliderIntervalSounds.ValueText = IntervalBetweenSounds.ToString(); };
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

        #endregion

        #region Custom Methods

        public void OnDamageEvent(object targetObj, ref MyDamageInformation info)
        {
            if (!(PlayHitSounds || PlayCritSounds || PlayKillSounds))
                return;

            // I wish I could use pattern matching :(
            IMyConveyorSorter attackerWeapon = MyAPIGateway.Entities.GetEntityById(info.AttackerId) as IMyConveyorSorter; // info.AttackerId is an EntityId
            IMySlimBlock target = targetObj as IMySlimBlock;
            if (attackerWeapon == null || target == null)
                return;

            // Complexity is basically just subgrid checking; only trigger hits for weapons on your own grid group.
            if (!attackerWeapon.CubeGrid.IsInSameLogicalGroupAs((MyAPIGateway.Session.Player?.Controller?.ControlledEntity as IMyCubeBlock)?.CubeGrid))
                return;

            if (PlayKillSounds && target.FatBlock is IMyCockpit && (target.Integrity - info.Amount <= 0)) // Kill sound (cockpit kill)
            {
                SoundEmitter?.PlayActionSound(killSound);
            }

            if (LastSoundTick + IntervalBetweenSounds >= Ticks)
                return;

            if (PlayCritSounds && target.FatBlock is IMyReactor && (target.Integrity - info.Amount <= 0)) // Crit sound (reactor kill)
            {
                SoundEmitter?.PlayActionSound(critSound);
            }

            if (PlayHitSounds && info.Amount >= MinDamageToPlay) // Hit sound
            {
                SoundEmitter?.PlayActionSound(hitSound);
            }

            LastSoundTick = Ticks;
        }

        #endregion
    }
}
