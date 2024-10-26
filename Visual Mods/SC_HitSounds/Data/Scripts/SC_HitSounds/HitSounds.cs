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
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)] // Attempt to load after Weaponcore because adding terminal actions is load-order dependent. :(
    public class HitSounds : MySessionComponentBase
    {
        public static HitSounds I = new HitSounds();

        // If you want to add a new sound, just copy-paste a dictionary entry and fill in the values. The Script:tm: will handle the rest.
        // - aristeas

        public Dictionary<string, MySoundPair> HitSoundEffects = new Dictionary<string, MySoundPair>()
        {
         // ["VISIBLE NAME"] = new MySoundPair("SOUND_SUBTYPE_ID") //
            ["TF2 Hitsound"] = new MySoundPair("SC_HitSound_TF2"),
            ["COD Hitsound"] = new MySoundPair("SC_HitSound_COD"),
        };

        public Dictionary<string, MySoundPair> CritSoundEffects = new Dictionary<string, MySoundPair>()
        {
            ["TF2 Critsound"] = new MySoundPair("SC_CritSound_TF2"),
        };

        public Dictionary<string, MySoundPair> KillSoundEffects = new Dictionary<string, MySoundPair>() // Maybe-Todo: Kill sound for everyone
        {
            ["TF2 Killsound"] = new MySoundPair("SC_KillSound_TF2"),
            ["MWO Arcade Kill"] = new MySoundPair("SC_KillSound_MW"),
            ["Metal Pipe"] = new MySoundPair("SC_KillSound_Pipe"),
        };





        internal HitSounds_Settings Settings = new HitSounds_Settings();

        MyCharacterSoundComponent SoundEmitter = null;

        int Ticks = 0;
        int LastSoundTick = 0;

        #region Base Methods
        public override void LoadData()
        {
            I = this;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            Settings.InitSettings(ModContext);
        }

        protected override void UnloadData()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
                Settings.StoreSettings();
            I = null;
        }

        public override void BeforeStart()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnDamageEvent);
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

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
            if (!(Settings.ForceHitSounds || Settings.PlayCritSounds || Settings.PlayKillSounds))
                return;


            // I wish I could use pattern matching :(
            IMyConveyorSorter attackerWeapon = MyAPIGateway.Entities.GetEntityById(info.AttackerId) as IMyConveyorSorter; // info.AttackerId is an EntityId
            IMySlimBlock target = targetObj as IMySlimBlock;
            if (attackerWeapon == null || target == null)
                return;

            // Complexity is basically just subgrid checking; only trigger hits for weapons on your own grid group.
            var myGrid = (MyAPIGateway.Session.Player?.Controller?.ControlledEntity as IMyCubeBlock)?.CubeGrid;
            if (myGrid == null || !attackerWeapon.CubeGrid.IsInSameLogicalGroupAs(myGrid))
                return;

            if (Settings.PlayKillSounds && target.FatBlock is IMyCockpit && (target.Integrity - info.Amount <= 0)) // Kill sound (cockpit kill)
            {
                SoundEmitter?.PlayActionSound(KillSoundEffects[Settings.CurrentKillSound]);
            }

            if (LastSoundTick + Settings.IntervalBetweenSounds >= Ticks)
                return;

            if (Settings.PlayCritSounds && target.FatBlock is IMyReactor && (target.Integrity - info.Amount <= 0)) // Crit sound (reactor kill)
            {
                SoundEmitter?.PlayActionSound(CritSoundEffects[Settings.CurrentCritSound]);
            }

            if ((Settings.ForceHitSounds || ShouldPlayBlockSounds(attackerWeapon)) && info.Amount >= Settings.MinDamageToPlay) // Hit sound
            {
                SoundEmitter?.PlayActionSound(HitSoundEffects[Settings.CurrentHitSound]);
            }

            LastSoundTick = Ticks;
        }

        public bool ShouldPlayBlockSounds(IMyConveyorSorter block)
        {
            return Settings.validSorterWeapons.Contains(block.BlockDefinition.SubtypeId);
        }

        #endregion
    }
}
