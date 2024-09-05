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
        private AnimationDef CHP_IonFixed_Animation => new AnimationDef
        {


           
            AnimationSets = new[]
            {
				#region Muzzles Animations
                new PartAnimationSetDef()
                {
                    SubpartId = Names(
										"GatlingBarrel"
										
										),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 0,
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
                                    TicksToMove = 30, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = ExpoGrowth, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 360f), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                
                            },
						
						
                        
						[Firing] =
                            new[]
                            {
                                 
								new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 30, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 540f), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
								
                                
                            },
						
						[StopFiring] =
                            new[]
                            {
                                 
								new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 125, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = ExpoDecay, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 540f), //degrees
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
