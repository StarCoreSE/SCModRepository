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
        private AnimationDef HexcannonAnimation => new AnimationDef
        {
			
            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [PreFire] = new[]{ //This particle fires in the Prefire state, during the 2 second windup.
                                   //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
                       new EventParticle
                       {
                           EmptyNames = Names("muzzle"), //If you want an effect on your own dummy
                           MuzzleNames = Names("muzzle"), //If you want an effect on the muzzle
                           StartDelay = 0, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "THESUNMUZZLE", //Particle subtypeID
                               Color = Color(red: 25, green: 25, blue: 25, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = false, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 6000, //meters
                                   MaxDuration = 0, //ticks 60 = 1 second
                                   Scale = 5, //How chunky the particle is.
                               }
                           }
                       },
                   },
            },

			AnimationSets = new[]
            {
                new PartAnimationSetDef()
                {
                    SubpartId = Names("antEA"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 60,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [PreFire] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 50, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                        [Firing] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 7000, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, -75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                    }
                },

                new PartAnimationSetDef()
                {
                    SubpartId = Names("antFA"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 60,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [PreFire] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 50, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, -75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                        [Firing] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 7000, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                    }
                },

                new PartAnimationSetDef()
                {
                    SubpartId = Names("antAB"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 60,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [PreFire] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 50, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    //Rotation = Transformation(-59f, -47f, -82f), //degrees
                                    Rotation = Transformation(0, 75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                        [Firing] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 7000, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, -75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                    }
                },

                new PartAnimationSetDef()
                {
                    SubpartId = Names("antBB"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 60,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [PreFire] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 50, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    //Rotation = Transformation(-59f, -47f, -82f), //degrees
                                    Rotation = Transformation(0, -75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                        [Firing] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 7000, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                    }
                },

                new PartAnimationSetDef()
                {
                    SubpartId = Names("antCB"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 60,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [PreFire] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 50, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    //Rotation = Transformation(-59f, -47f, -82f), //degrees
                                    Rotation = Transformation(0, -75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                        [Firing] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 7000, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                    }
                },

                new PartAnimationSetDef()
                {
                    SubpartId = Names("antDB"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 60,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                        [PreFire] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 50, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    //Rotation = Transformation(-59f, -47f, -82f), //degrees
                                    Rotation = Transformation(0, -75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                        [Firing] =
                            new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 7000, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 75, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },

                            },

                    }
                },
            }
        };       
		
		
 }
}
