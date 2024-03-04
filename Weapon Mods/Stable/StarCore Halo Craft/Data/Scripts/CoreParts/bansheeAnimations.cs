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
        private AnimationDef Banshee_Animation => new AnimationDef
        {


            AnimationSets = new[]
            {

   new PartAnimationSetDef()
                {
                    SubpartId = Names("door1"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 60,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0, StopFiringDelay: 0, StopTrackingDelay:0, InitDelay:0),//Delay before animation starts
                    Reverse = Events(),
                                TriggerOnce = Events(Firing, Reloading),
                    Loop = Events(),
                 EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                [Firing] = new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire, EmptyOnGameLoad define a new[] for each
                        {
                            new RelMove
                            {
                                CenterEmpty = "",
                                TicksToMove = 10, //number of ticks to complete motion, 60 = 1 second
                                MovementType = Linear,
                                LinearPoints = new[]
                                {
                                    Transformation(0, 4, 0), //linear movement
                                },
                                Rotation = Transformation(0, 0, 0), //degrees
                                RotAroundCenter = Transformation(0, 0, 0), //degrees
                            },


                        },

                [StopFiring] = new[] //Firing, Reloading, etc, see Possible Events,  define a new[] for each
                            {
                                new RelMove
                                {
                                CenterEmpty = "",
                                TicksToMove = 60, //number of ticks to complete motion, 60 = 1 second
                                MovementType = Linear,
                                LinearPoints = new[]
                                {
                                    Transformation(0, -4, 0), //linear movement
                                },
                                Rotation = Transformation(0, 0, 0), //degrees
                                RotAroundCenter = Transformation(0, 0, 0), //degrees
                                 },
                            },

                }
                },


            }
        };
}
}
