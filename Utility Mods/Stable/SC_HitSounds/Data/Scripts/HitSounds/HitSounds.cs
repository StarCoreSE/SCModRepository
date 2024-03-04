using CoreSystems.Api;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using static CoreSystems.Api.WcApi;
using static CoreSystems.Api.WcApi.DamageHandlerHelper;

namespace Jnick_SCModRepository.HitSounds.Data.Scripts.HitSounds
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class HitSounds : MySessionComponentBase
    {
        // TODO: Config settings for this!
        const int IntervalBetweenSounds = 4; // Ticks
        const int MinDamageToPlay = 100;

        public static HitSounds I = new HitSounds();
        long modId = 0;
        WcApi wAPI;
        DamageHandlerHelper damageHandlerHelper;
        MyCharacterSoundComponent soundEmitter = null;

        MySoundPair hitSound = new MySoundPair("SC_HitSound_TF2");
        //MySoundPair critSound = new MySoundPair("SC_CritSound_TF2");
        MySoundPair killSound = new MySoundPair("SC_KillSound_TF2");

        int Ticks = 0;
        int LastSoundTick = 0;

        #region Base Methods
        public override void LoadData()
        {
            I = this;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            wAPI = new WcApi();
            damageHandlerHelper = new DamageHandlerHelper(wAPI);
            long.TryParse(ModContext.ModId, out modId);
            wAPI.Load(() => damageHandlerHelper.RegisterForDamage(modId, EventType.SystemWideDamageEvents));
            MyAPIGateway.Utilities.ShowMessage("HITSOUNDS", "Loaded...");
        }

        protected override void UnloadData()
        {
            I = null;
            //damageHandlerHelper.RegisterForDamage(modId, EventType.Unregister);
        }

        public override void UpdateAfterSimulation()
        {
            if (soundEmitter == null)
                soundEmitter = MyAPIGateway.Session.Player?.Character?.Components.Get<MyCharacterSoundComponent>();

            Ticks++;
        }
        #endregion

        #region Custom Methods

        public void OnDamageEvent(ulong projectileId, long playerId, int weaponId, MyEntity weaponEntity, MyEntity weaponParent, List<ProjectileDamageEvent.ProHit> objectsHit)
        {
            if (LastSoundTick + IntervalBetweenSounds >= Ticks)
                return;

            var weaponRelations = (weaponEntity as IMyConveyorSorter)?.GetUserRelationToOwner(MyAPIGateway.Session.Player.IdentityId);

            // Check if gun is faction owned
            if (weaponRelations == null || !(weaponRelations == MyRelationsBetweenPlayerAndBlock.FactionShare || weaponRelations == MyRelationsBetweenPlayerAndBlock.Owner))
                return;

            bool playKillSound = false;
            bool playAnySound = false;
            foreach (var hit in objectsHit)
            {
                if (hit.Damage > MinDamageToPlay)
                    playKillSound = true;

                IMySlimBlock slim = hit.ObjectHit as IMySlimBlock;
                if (slim != null && slim.FatBlock != null && slim.FatBlock is IMyCockpit) // This doesn't actually trigger when a hit is made...
                {
                    playKillSound = true;
                }
            }

            if (!playAnySound)
                return;

            if (playKillSound)
                soundEmitter?.PlayActionSound(killSound);
            else
                soundEmitter?.PlayActionSound(hitSound);

            LastSoundTick = Ticks;
        }

        #endregion
    }
}
