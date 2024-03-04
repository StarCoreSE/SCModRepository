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
        private AnimationDef UNN_Heavy_Anim => new AnimationDef
        {
			

            AnimationSets = new[]
            {
				// 1-5
              new PartAnimationSetDef()
              {
                SubpartId = Names("Torpedo"),
                BarrelId = "muzzle_01", //only used for firing, use "Any" for all muzzles
                StartupFireDelay = 60,
                AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                Reverse = Events(),
                Loop = Events(),
                EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                {
                    [Firing] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                    {
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Hide,
                            LinearPoints = new[]
                            {
                                Transformation(0, 0, 0), //linear movement
                            },
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },
                    },

                    [Reloading] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                    {
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 60, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Delay, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                            LinearPoints = new XYZ[0],
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },

                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Show, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                            LinearPoints = new XYZ[0],
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },
                    },

                    [NoMagsToLoad] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                    {
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Hide,
                            LinearPoints = new[]
                            {
                                Transformation(0, 0, 0), //linear movement
                            },
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },
                    },
                  
                    [EmptyOnGameLoad] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                    {
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Show,
                            LinearPoints = new[]
                            {
                                Transformation(0, 0, 0), //linear movement
                            },
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },
                    },
			    }
			  },

              new PartAnimationSetDef()
              {
                SubpartId = Names("Torpedo_Tube"),
                BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                StartupFireDelay = 60,
                AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 60, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                Reverse = Events(),
                Loop = Events(),
                EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                {
                    [Reloading] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                    {
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 60, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                            LinearPoints = new[]
                            {
                                Transformation(-1.51, 0, 0), //linear movement
                            },
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },

                        // Rotate Drum 180
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 240, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Delay, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                            LinearPoints = new XYZ[0],
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },

                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 60, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                            LinearPoints = new[]
                            {
                                Transformation(1.51, 0, 0), //linear movement
                            },
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },
                    },

                    [TurnOff] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                    {
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 60, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                            LinearPoints = new[]
                            {
                                Transformation(-1.51, 0, 0), //linear movement
                            },
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },
                    },

                    [TurnOn] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                    {
                        new RelMove
                        {
                            CenterEmpty = "",
                            TicksToMove = 60, //number of ticks to complete motion, 60 = 1 second
                            MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                            LinearPoints = new[]
                            {
                                Transformation(1.51, 0, 0), //linear movement
                            },
                            Rotation = Transformation(0, 0, 0), //degrees
                            RotAroundCenter = Transformation(0, 0, 0), //degrees
                        },
                    },
                }
              }

            }
        };
    }
}
