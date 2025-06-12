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

        private AnimationDef QuadAutoAnim => new AnimationDef
        {
            /*
            Emissives = new[]
            {
                Emissive(
                    EmissiveName: "TurnOn",
                    Colors: new []
                    {
                        Color(red:0, green: 0, blue:0, alpha: 1),//will transitions form one color to the next if more than one
                        Color(red:0.05f, green: .021f, blue:0, alpha: .05f),
                    },
                    IntensityFrom:0, //starting intensity, can be 0.0-1.0 or 1.0-0.0, setting both from and to, to the same value will stay at that value
                    IntensityTo:1,
                    CycleEmissiveParts: false,//whether to cycle from one part to the next, while also following the Intensity Range, or set all parts at the same time to the same value
                    LeavePreviousOn: true,//true will leave last part at the last setting until end of animation, used with cycleEmissiveParts
                    EmissivePartNames: new []
                    {
                        "Emissive3"
                    }),
                Emissive(
                    EmissiveName: "TurnOff",
                    Colors: new []
                    {
                        Color(red:0.05f, green: .021f, blue:0, alpha: .05f),
                        Color(red:0, green: 0, blue:0, alpha: 1),//will transitions form one color to the next if more than one
                    },
                    IntensityFrom:1, //starting intensity, can be 0.0-1.0 or 1.0-0.0, setting both from and to, to the same value will stay at that value
                    IntensityTo:0,
                    CycleEmissiveParts: false,//whether to cycle from one part to the next, while also following the Intensity Range, or set all parts at the same time to the same value
                    LeavePreviousOn: true,//true will leave last part at the last setting until end of animation, used with cycleEmissiveParts
                    EmissivePartNames: new []
                    {
                        "Emissive3"
                    }),
            },
            */


            AnimationSets = new[]
            {
                #region Muzzles Animations
                new PartAnimationSetDef()
                {
                    SubpartId = Names("quadautoelev"),
                    BarrelId = "Any", //only used for firing events, use "Any" for all muzzles, muzzle triggers only this animation if not Any
                    AnimationDelays = Delays(FiringDelay : 0, ReloadingDelay: 0, OverheatedDelay: 0, TrackingDelay: 0, LockedDelay: 0, OnDelay: 0, OffDelay: 0, BurstReloadDelay: 0, OutOfAmmoDelay: 0, PreFireDelay: 0),//Delay before animation starts
                    Reverse = Events(),
                    Loop = Events(),
                    EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                    {
                        
                        [Firing] = new[] //Firing, Reloading, etc, see Possible Events,  define a new[] for each
                            {

                                new RelMove
                                {
                                    CenterEmpty = "",
                                    EmissiveName = "", //EmissiveName: from above Emissives definitions, TurnOn TurnOff
                                    TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = ExpoDecay,
                                    LinearPoints = new[]
                                {
                                    Transformation(0, 0, 0.15f), //linear movement
                                },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees, rotation is around CenterEmpty
                                },
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    EmissiveName = "", //EmissiveName: from above Emissives definitions, TurnOn TurnOff
                                    TicksToMove = 4, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = ExpoGrowth,
                                    LinearPoints = new[]
                                {
                                    Transformation(0, 0, -0.15f), //linear movement
                                },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees, rotation is around CenterEmpty
                                }
                            },
                    }
                },
            }
#endregion
        };   }     }   