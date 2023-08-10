using System.Collections.Generic;
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AnimationDef;
using static Scripts.Structure.WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove.MoveType;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove;
using static Scripts.Structure.WeaponDefinition.AnimationDef.PartAnimationSetDef.ResetConditions;
namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        private AnimationDef ERPPC_Animation => new AnimationDef
        {

            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [Firing] = new[]{ //This particle fires in the Prefire state, during the 10 second windup the gauss cannon has.
                                   //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
                       new EventParticle
                       {
                           EmptyNames = Names(""), //If you want an effect on your own dummy
                           MuzzleNames = Names("muzzle_projectile_1"), //If you want an effect on the muzzle
                           StartDelay = 0, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "ERPPCcoilparticle", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = true, //Should match your particle definition.
                                   Restart = false,
                                Scale = 1, //How chunky the particle is.
                               }
                           }
                       },
                   },
            },

        };
    }
}
