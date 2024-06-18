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
        private AnimationDef SA_HMI_ERGaussII_III_Animation => new AnimationDef
        {
			

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
            AnimationSets = new[]
            {
				#region Muzzles Animations
                new PartAnimationSetDef()
                {
                    SubpartId = Names("HGaussII_Recoil_III"),
                    BarrelId = "Muzzle_HeavyGaussIIB_01", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 0,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
					TriggerOnce = Events(Firing, TurnOff),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [Firing] =
                            new[]
                            {
                                 
                               new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 5, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new[]
                                    {
                                        Transformation(0, 0, 1.5), //linear movement
                                    },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 30, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Delay,
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 15, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new[]
                                    {
                                        Transformation(0, 0, -1.5), //linear movement
                                    },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                            },
                        
                    }
                },
               
				#endregion


            }
        };
    }
}
