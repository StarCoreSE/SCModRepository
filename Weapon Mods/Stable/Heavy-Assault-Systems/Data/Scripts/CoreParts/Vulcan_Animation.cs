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
       private AnimationDef Vulcan_Animation => new AnimationDef
        {
            HeatingEmissiveParts = new[]
            {
                "barrels"
            },          
            AnimationSets = new[]
            {
				#region Muzzles Animations
                new PartAnimationSetDef()
                {
                    SubpartId = Names("barrels"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 40,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(Firing),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [PreFire] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "barrels",
                                    TicksToMove = 40, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = ExpoGrowth,
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 240), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },


                            },


                        [Firing] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "barrels",
                                    TicksToMove = 10, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear,
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 120), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                

                            },
                        [StopFiring] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "barrels",
                                    TicksToMove = 240, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = ExpoDecay,
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 300), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                    }
                },
               
				#endregion


            }

            //Emissives = new []
            //{

            //    Emissive(
            //        EmissiveName: "TurnOn",
            //        Colors: new []
            //        {
            //            Color(red:0, green: 0, blue:0, alpha: 1),//will transitions form one color to the next if more than one
            //            Color(red:0, green: .051f, blue:.051f, alpha: .05f),

            //        },
            //        IntensityFrom:0, //starting intensity, can be 0.0-1.0 or 1.0-0.0, setting both from and to, to the same value will stay at that value
            //        IntensityTo:1,
            //        CycleEmissiveParts: false,//whether to cycle from one part to the next, while also following the Intensity Range, or set all parts at the same time to the same value
            //        LeavePreviousOn: true,//true will leave last part at the last setting until end of animation, used with cycleEmissiveParts
            //        EmissivePartNames: new []
            //        {
            //            "Emissive3"
            //        }),

            //    Emissive(
            //        EmissiveName: "TurnOff",
            //        Colors: new []
            //        {
            //            Color(red:0, green: .051f, blue:.051f, alpha: .05f),//will transitions form one color to the next if more than one
            //            Color(red:0, green: 0, blue: 0, alpha: 1),//will transitions form one color to the next if more than one


            //        },
            //        IntensityFrom:1, //starting intensity, can be 0.0-1.0 or 1.0-0.0, setting both from and to, to the same value will stay at that value
            //        IntensityTo:0,
            //        CycleEmissiveParts: false,//whether to cycle from one part to the next, while also following the Intensity Range, or set all parts at the same time to the same value
            //        LeavePreviousOn: true,//true will leave last part at the last setting until end of animation, used with cycleEmissiveParts
            //        EmissivePartNames: new []
            //        {
            //            "Emissive3"
            //        }),




            //    Emissive(
            //        EmissiveName: "PowerUp", 
            //        Colors: new []
            //        {
            //            Color(red:0, green: .051f, blue:.051f, alpha: .05f),//will transitions form one color to the next if more than one
            //            Color(red:0, green: 1, blue:1, alpha: 1),

            //        }, 
            //        IntensityFrom:0, //starting intensity, can be 0.0-1.0 or 1.0-0.0, setting both from and to, to the same value will stay at that value
            //        IntensityTo:1, 
            //        CycleEmissiveParts: false,//whether to cycle from one part to the next, while also following the Intensity Range, or set all parts at the same time to the same value
            //        LeavePreviousOn: true,//true will leave last part at the last setting until end of animation, used with cycleEmissiveParts
            //        EmissivePartNames: new []
            //        {
            //            "Emissive3"
            //        }),

            //    Emissive(
            //        EmissiveName: "ShootPulse",
            //        Colors: new []
            //        {


            //            Color(red:0, green: 250, blue: 250, alpha: 1),

            //        },
            //        IntensityFrom:1, //starting intensity, can be 0.0-1.0 or 1.0-0.0, setting both from and to, to the same value will stay at that value
            //        IntensityTo:1,
            //        CycleEmissiveParts: false,//whether to cycle from one part to the next, while also following the Intensity Range, or set all parts at the same time to the same value
            //        LeavePreviousOn: true,//true will leave last part at the last setting until end of animation, used with cycleEmissiveParts
            //        EmissivePartNames: new []
            //        {
            //            "Emissive3"
            //        }),
            //    Emissive(
            //        EmissiveName: "PowerDown",
            //        Colors: new []
            //        {

            //            Color(red:0, green: 250, blue:250, alpha: 1),
            //            Color(red:0, green: .051f, blue:.051f, alpha: .05f),

            //        },
            //        IntensityFrom:1, //starting intensity, can be 0.0-1.0 or 1.0-0.0, setting both from and to, to the same value will stay at that value
            //        IntensityTo:1,
            //        CycleEmissiveParts: false,//whether to cycle from one part to the next, while also following the Intensity Range, or set all parts at the same time to the same value
            //        LeavePreviousOn: true,//true will leave last part at the last setting until end of animation, used with cycleEmissiveParts
            //        EmissivePartNames: new []
            //        {
            //            "Emissive3"
            //        }),

            //},

            //EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            //{
            //    [PreFire] = new[]{ //This particle fires in the Prefire state, during the 10 second windup the gauss cannon has.
            //                       //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
            //           new EventParticle
            //           {
            //               EmptyNames = Names("muzzle_projectile_1"), //If you want an effect on your own dummy
            //               MuzzleNames = Names("muzzle_projectile_1"), //If you want an effect on the muzzle
            //               StartDelay = 0, //ticks 60 = 1 second, delay until particle starts.
            //               LoopDelay = 0, //ticks 60 = 1 second
            //               ForceStop = false,
            //               Particle = new ParticleDef
            //               {
            //                   Name = "ShipWelderArc", //Particle subtypeID
            //                   Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
            //                   Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
            //                   {
            //                       Loop = true, //Should match your particle definition.
            //                       Restart = false,
            //                    Scale = 1, //How chunky the particle is.
            //                   }
            //               }
            //           },
            //       },
            //},

        };
    }
}
