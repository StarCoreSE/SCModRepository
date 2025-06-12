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
        private AnimationDef CoilgunAnim => new AnimationDef
        {

            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [PreFire] = new[]{ //This particle fires in the Prefire state, during the 10 second windup the gauss cannon has.
                                   //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
                       new EventParticle
                       {
                           EmptyNames = Names("muzzle_projectile_1","muzzle_projectile_2"), //If you want an effect on your own dummy
                           MuzzleNames = Names("muzzle_projectile_1","muzzle_projectile_2"), //If you want an effect on the muzzle
                           StartDelay = 0, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "TestCoilParticle", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = true, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 3000, //meters
                                   MaxDuration = 0, //ticks 60 = 1 second
                                   Scale = 1, //How chunky the particle is.
                               }
                           }
                       },
                   },
            },


            //These are the animation sets the weapon uses in various states.
    //        WeaponAnimationSets = new[]
    //        {   //Region is used for organisation as it creates a collapsible tag.
				//#region Barrels Animations
    //            //new PartAnimationSetDef()
    //            //{
    //            //    SubpartId = Names("leftRail"), //Remember to remove subpart_ from these names!
    //            //    BarrelId = "muzzle_projectile_1", //Trigger anim when this muzzle does something.
    //            //    StartupFireDelay = 0, //Delay in ticks until anim starts.
    //            //    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0),//Delay before animation starts
    //            //    Reverse = Events(),
    //            //    Loop = Events(), //Don't touch these.
    //            //    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
    //            //    {

    //            //        [PreFire] =
    //            //            new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire define a new[] for each
    //            //            {

    //            //               new RelMove //This is an animation block, and moves a subpart named subpart_leftRail -0.2 on the X axis.
    //            //                {
    //            //                    CenterEmpty = "",
    //            //                    TicksToMove = 300, //number of ticks to complete motion, 60 = 1 second
    //            //                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
    //            //                    LinearPoints = new[]
    //            //                    {
    //            //                        Transformation(-0.2f, 0, 0f), //linear movement in XYZ axes.
    //            //                    },
    //            //                    Rotation = Transformation(0, 0, 0), //degrees
    //            //                    RotAroundCenter = Transformation(0, 0, 0), //degrees
    //            //                },
    //            //            },

    //            //    }

    //            //},

    //            //new PartAnimationSetDef()
    //            //{
    //            //    SubpartId = Names("rightRail"),
    //            //    BarrelId = "muzzle_projectile_1", //only used for firing, use "Any" for all muzzles
    //            //    StartupFireDelay = 0,
    //            //    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0),//Delay before animation starts
    //            //    Reverse = Events(),
    //            //    Loop = Events(),
    //            //    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
    //            //    {

    //            //        [PreFire] =
    //            //            new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire define a new[] for each
    //            //            {

    //            //               new RelMove
    //            //                {
    //            //                    CenterEmpty = "",
    //            //                    TicksToMove = 300, //number of ticks to complete motion, 60 = 1 second
    //            //                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
    //            //                    LinearPoints = new[]
    //            //                    {
    //            //                        Transformation(0.2f, 0, 0f), //linear movement
    //            //                    },
    //            //                    Rotation = Transformation(0, 0, 0), //degrees
    //            //                    RotAroundCenter = Transformation(0, 0, 0), //degrees
    //            //                },
    //            //            },

    //            //    }
    //            //},
    //            ////Below are the "reset" positions called on reload state.
    //            //new PartAnimationSetDef()
    //            //{
    //            //    SubpartId = Names("leftRail"),
    //            //    BarrelId = "muzzle_projectile_1", //only used for firing, use "Any" for all muzzles
    //            //    StartupFireDelay = 0,
    //            //    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0),//Delay before animation starts
    //            //    Reverse = Events(),
    //            //    Loop = Events(),
    //            //    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
    //            //    {

    //            //        [Firing] =
    //            //            new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire define a new[] for each
    //            //            {

    //            //                new RelMove
    //            //                {
    //            //                    CenterEmpty = "",
    //            //                    TicksToMove = 300, //number of ticks to complete motion, 60 = 1 second
    //            //                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
    //            //                    LinearPoints = new[]
    //            //                    {
    //            //                        Transformation(0.2f, 0, 0), //linear movement
    //            //                    },
    //            //                    Rotation = Transformation(0, 0, 0), //degrees
    //            //                    RotAroundCenter = Transformation(0, 0, 0), //degrees
    //            //                },
    //            //            },

    //            //    }
    //            //},

    //            //new PartAnimationSetDef()
    //            //{
    //            //    SubpartId = Names("rightRail"),
    //            //    BarrelId = "muzzle_projectile_1", //only used for firing, use "Any" for all muzzles
    //            //    StartupFireDelay = 0,
    //            //    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0),//Delay before animation starts
    //            //    Reverse = Events(),
    //            //    Loop = Events(),
    //            //    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
    //            //    {

    //            //        [Firing] =
    //            //            new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire define a new[] for each
    //            //            {


    //            //                new RelMove
    //            //                {
    //            //                    CenterEmpty = "",
    //            //                    TicksToMove = 300, //number of ticks to complete motion, 60 = 1 second
    //            //                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
    //            //                    LinearPoints = new[]
    //            //                    {
    //            //                        Transformation(-0.2f, 0, 0f), //linear movement
    //            //                    },
    //            //                    Rotation = Transformation(0, 0, 0), //degrees
    //            //                    RotAroundCenter = Transformation(0, 0, 0), //degrees
    //            //                },
    //            //            },

    //            //    }
    //            //},
    //            #endregion
    //        }
        };
    }
}