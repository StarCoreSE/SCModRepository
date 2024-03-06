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

namespace Jnick_SCModRepository.SC_HitSounds.Data.Scripts.SC_HitSounds
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class HitSounds : MySessionComponentBase
    {
        public static HitSounds I = new HitSounds();

        public Dictionary<string, MySoundPair> HitSoundEffects = new Dictionary<string, MySoundPair>()
        {
            ["TF2 Hitsound"] = new MySoundPair("SC_HitSound_TF2"),
            ["COD Hitsound"] = new MySoundPair("SC_HitSound_COD"),
        };

        public Dictionary<string, MySoundPair> CritSoundEffects = new Dictionary<string, MySoundPair>()
        {
            ["TF2 CRITICAL HIT"] = new MySoundPair("SC_CritSound_TF2"),
        };

        public Dictionary<string, MySoundPair> KillSoundEffects = new Dictionary<string, MySoundPair>() // Maybe-Todo: Kill sound for everyone
        {
            ["TF2 Killsound"] = new MySoundPair("SC_KillSound_TF2"),
            ["MWO Arcade Kill"] = new MySoundPair("SC_KillSound_MW"),
            ["Metal Pipe"] = new MySoundPair("SC_KillSound_Pipe"),
        };





        internal bool ForceHitSounds = false; // TODO: Save settings
        internal bool PlayCritSounds = true;
        internal bool PlayKillSounds = true;
        internal int IntervalBetweenSounds = 4; // Ticks
        internal int MinDamageToPlay = 100;

        WcApi wAPI;
        HitSounds_TerminalActions terminalActions = new HitSounds_TerminalActions();

        MyCharacterSoundComponent SoundEmitter = null;

        public string CurrentHitSound = "TF2 Hitsound";
        public string CurrentCritSound = "TF2 CRITICAL HIT";
        public string CurrentKillSound = "TF2 Killsound";

        int Ticks = 0;
        int LastSoundTick = 0;

        #region Base Methods
        public override void LoadData()
        {
            I = this;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            wAPI = new WcApi();

            HitSounds_Settings.InitSettings(ModContext, this);
        }

        protected override void UnloadData()
        {
            I = null;
            //damageHandlerHelper.RegisterForDamage(modId, EventType.Unregister);
        }

        public override void BeforeStart()
        {
            wAPI.Load(() => terminalActions.CreateTerminalControls(wAPI));
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

        #region Custom Methods

        public void OnDamageEvent(object targetObj, ref MyDamageInformation info)
        {
            if (!(ForceHitSounds || PlayCritSounds || PlayKillSounds))
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
                SoundEmitter?.PlayActionSound(KillSoundEffects[CurrentKillSound]);
            }

            if (LastSoundTick + IntervalBetweenSounds >= Ticks)
                return;

            if (PlayCritSounds && target.FatBlock is IMyReactor && (target.Integrity - info.Amount <= 0)) // Crit sound (reactor kill)
            {
                SoundEmitter?.PlayActionSound(CritSoundEffects[CurrentCritSound]);
            }

            if ((ForceHitSounds || terminalActions.ShouldPlayBlockSounds(attackerWeapon)) && info.Amount >= MinDamageToPlay) // Hit sound
            {
                SoundEmitter?.PlayActionSound(HitSoundEffects[CurrentHitSound]);
            }

            LastSoundTick = Ticks;
        }

        #endregion
    }
}
