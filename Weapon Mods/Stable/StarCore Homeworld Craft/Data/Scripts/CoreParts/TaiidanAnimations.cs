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
        private AnimationDef Rail_Animation1 => new AnimationDef
        {


            AnimationSets = new[]
            {

                new PartAnimationSetDef()
                {
                    SubpartId = Names("launchbox"),
                    BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                    StartupFireDelay = 0,
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0),//Delay before animation starts
                    Reverse = Events(),
                    TriggerOnce = Events(),
                    Loop = Events(),
                    ResetEmissives = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {


                        [Firing] =
                            new[] //Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire define a new[] for each
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 60, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear,
                                    LinearPoints = new[]
                                    {
                                        Transformation(0, 0, -45), //linear movement
                                    },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 20, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Delay,
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 20, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Linear,
                                    LinearPoints = new[]
                                    {
                                        Transformation(0, 0, 45), //linear movement
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
