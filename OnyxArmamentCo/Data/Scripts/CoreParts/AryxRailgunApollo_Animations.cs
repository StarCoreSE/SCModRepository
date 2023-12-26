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
        private AnimationDef AryxRailgunTurretAnims => new AnimationDef
        {

            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [PreFire] = new[]{
                       new EventParticle
                       {
                           EmptyNames = Names("muzzle_projectile_1"),
                           MuzzleNames = Names("muzzle_projectile_1"),
                           StartDelay = 0, //ticks 60 = 1 second
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "Aryx_Railgun_windup_effect",
                               Color = Color(red: 1, green: 1, blue: 1, alpha: 1),
                               Extras = new ParticleOptionDef
                               {
                                   Loop = true,
                                   Restart = true,
                                   MaxDistance = 1000, //meters
                                   MaxDuration = 0, //ticks 60 = 1 second
                                   Scale = 1,
                               }
                           }
                       },
                   },
            },


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
            //        AnimationSets = new[]
            //        {
            //#region Barrels Animations
            //            new PartAnimationSetDef()
            //            {
            //                SubpartId = Names("howitzerrecoil"),
            //                BarrelId = "muzzle_missile_1", //only used for firing, use "Any" for all muzzles
            //                StartupFireDelay = 60,
            //                AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0),//Delay before animation starts
            //                Reverse = Events(),
            //                Loop = Events(),
            //                EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
            //                {

            //                    [Firing] =
            //                        new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire define a new[] for each
            //                        {

            //                           new RelMove
            //                            {
            //                                CenterEmpty = "",
            //                                TicksToMove = 11, //number of ticks to complete motion, 60 = 1 second
            //                                MovementType = ExpoDecay, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
            //                                LinearPoints = new[]
            //                                {
            //                                    Transformation(0, 0, 0.5), //linear movement
            //                                },
            //                                Rotation = Transformation(0, 0, 0), //degrees
            //                                RotAroundCenter = Transformation(0, 0, 0), //degrees
            //                            },
            //                            new RelMove
            //                            {
            //                                CenterEmpty = "",
            //                                TicksToMove = 15, //number of ticks to complete motion, 60 = 1 second
            //                                MovementType = Delay,
            //                                LinearPoints = new XYZ[0],
            //                                Rotation = Transformation(0, 0, 0), //degrees
            //                                RotAroundCenter = Transformation(0, 0, 0), //degrees
            //                            },

            //                            new RelMove
            //                            {
            //                                CenterEmpty = "",
            //                                TicksToMove = 50, //number of ticks to complete motion, 60 = 1 second
            //                                MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
            //                                LinearPoints = new[]
            //                                {
            //                                    Transformation(0, 0, -0.5), //linear movement
            //                                },
            //                                Rotation = Transformation(0, 0, 0), //degrees
            //                                RotAroundCenter = Transformation(0, 0, 0), //degrees
            //                            },
            //                        },

            //                }
            //            },

            //#endregion




            //        }
        };
    }
}
