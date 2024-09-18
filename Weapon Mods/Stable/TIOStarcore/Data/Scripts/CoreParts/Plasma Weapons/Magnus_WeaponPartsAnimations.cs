using System.Collections.Generic;
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AnimationDef;
using static Scripts.Structure.WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove.MoveType;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove;
namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        /// Possible Events ///

        //Reloading,
        //Firing,
        //Tracking,
        //Overheated,
        //TurnOn,
        //TurnOff,
        //BurstReload,
        //OutOfAmmo,
        //PreFire,
        //EmptyOnGameLoad,
        //StopFiring,
        //StopTracking

        private AnimationDef Magnus_AdvancedAnimation => new AnimationDef
        {
            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [PreFire] = new[]{ //This particle fires in the Prefire state, during the 2 second windup.
                                   //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
                       new EventParticle
                       {
                           EmptyNames = Names("muzzle_effect_1"), //If you want an effect on your own dummy
                           MuzzleNames = Names("muzzle_effect_1"), //If you want an effect on the muzzle
                           StartDelay = 60, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "SignificantFusionableEvent", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = false, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 1000, //meters
                                   MaxDuration = 120, //ticks 60 = 1 second
                                   Scale = 0.25f, //How chunky the particle is.
                               }
                           }
                       },
                       new EventParticle
                       {
                           EmptyNames = Names("muzzle_effect_2"), //If you want an effect on your own dummy
                           MuzzleNames = Names("muzzle_effect_2"), //If you want an effect on the muzzle
                           StartDelay = 75, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "SignificantFusionableEvent", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = false, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 1000, //meters
                                   MaxDuration = 120, //ticks 60 = 1 second
                                   Scale = 0.25f, //How chunky the particle is.
                               }
                           }
                       },
                       new EventParticle
                       {
                           EmptyNames = Names("muzzle_effect_3"), //If you want an effect on your own dummy
                           MuzzleNames = Names("muzzle_effect_3"), //If you want an effect on the muzzle
                           StartDelay = 90, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "SignificantFusionableEvent", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = false, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 1000, //meters
                                   MaxDuration = 120, //ticks 60 = 1 second
                                   Scale = 0.25f, //How chunky the particle is.
                               }
                           }
                       },
                   },

            },



        };
    }
}