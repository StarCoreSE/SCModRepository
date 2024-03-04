using static Scripts.Structure;
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.ModelAssignmentsDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef.Prediction;
using static Scripts.Structure.WeaponDefinition.TargetingDef.BlockTypes;
using static Scripts.Structure.WeaponDefinition.TargetingDef.Threat;
using static Scripts.Structure.WeaponDefinition.TargetingDef;
using static Scripts.Structure.WeaponDefinition.TargetingDef.CommunicationDef.Comms;
using static Scripts.Structure.WeaponDefinition.TargetingDef.CommunicationDef.SecurityMode;
using static Scripts.Structure.WeaponDefinition.HardPointDef.HardwareDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef.HardwareDef.HardwareType;

namespace Scripts {   
    partial class Parts {
        // Don't edit above this line
        WeaponDefinition Starcore_AMS_Comm => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "Starcore_AMS_Comm_Block", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "", // For weapons with a spinning barrel such as Gatling Guns.
                        MuzzlePartId = "SC_AMS_I_Ele", // The subpart where your muzzle empties are located. This is often the elevation subpart.
                        AzimuthPartId = "SC_AMS_I_Rot", // Your Rotating Subpart, the bit that moves sideways
                        ElevationPartId = "SC_AMS_I_Ele",// Your Elevating Subpart, that bit that moves up
                        DurabilityMod = 0.25f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "TestIcon.dds" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    
                 },
                Muzzles = new[] {
                    "SC_AMS_I_barrel_01", // Where your Projectiles spawn. Use numbers not Letters. IE Muzzle_01 not Muzzle_A
                   // "SC_AMS_I_barrel_02",
                  //  "SC_AMS_I_barrel_03",
                  //  "SC_AMS_I_barrel_04",
                  //  "SC_AMS_I_barrel_05",
                   // "SC_AMS_I_barrel_06",
					//"SC_AMS_I_barrel_07",
					//"SC_AMS_I_barrel_08",
					//"SC_AMS_I_barrel_09",
					//"SC_AMS_I_barrel_10",
					
                },
                Ejector = "", // Optional; empty from which to eject "shells" if specified.
                Scope = "SC_AMS_I_camera", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[ ]
                {
                    Projectiles, Grids // threats percieved automatically without changing menu settings
                } ,
                SubSystems = new[ ]
                {
                    Any, Thrust, Utility, Offense, Power, Production, // subsystems the gun targets
                } ,
                ClosestFirst = true , // tries to pick closest targets first (blocks on grids, projectiles, etc...).
                IgnoreDumbProjectiles = false , // Don't fire at non-smart projectiles.
                LockedSmartOnly = false , // Only fire at smart projectiles that are locked on to parent grid.
                MinimumDiameter = 0 , // 0 = unlimited, Minimum radius of threat to engage.
                MaximumDiameter = 0 , // 0 = unlimited, Maximum radius of threat to engage.
                MaxTargetDistance = 0 , // 0 = unlimited, Maximum target distance that targets will be automatically shot at.
                MinTargetDistance = 0 , // 0 = unlimited, Min target distance that targets will be automatically shot at.
                TopTargets = 24, // Maximum number of targets to randomize between; 0 = unlimited.
                CycleTargets = 4, // Number of targets to "cycle" per acquire attempt.
                TopBlocks = 24, // Maximum number of blocks to randomize between; 0 = unlimited.
                CycleBlocks = 4, // Number of blocks to "cycle" per acquire attempt.
                StopTrackingSpeed = 0 , // do not track target threats traveling faster than this speed
                MaxTrackingTime = 30 , // After this time has been reached the weapon will stop tracking existing target and scan for a new one, only applies to turreted weapons
                Communications = new CommunicationDef
                {
                    StoreTargets = false, // Pushes its current target to the grid/construct so that other slaved weapons can fire on it.
                    StorageLimit = 0, // The limit at which this weapon will no longer export targets onto the channel.
                    MaxConnections = 0, // 0 is unlimited, this value determines the maximum number of weapons that can link up to another weapon.
                    StoreLimitPerBlock = false, // Setting this to true will switch the StorageLimit from being per Location to per block per Location.
                    StorageLocation = "Projectile_Target_List", // This location ID is used either by the master weapon (if ExportTargets = true) or the slave weapon (if its false).  This is shared across the conncted grids.
                    Mode = LocalNetwork, // NoComms, BroadCast, LocalNetwork, Repeater, Relay, Jamming
                    TargetPersists = false, // Whether or not the weapon will retain its existing target even if the source of the target releases theirs.
                    Security = Private, // Public, Private, Secure
                    BroadCastChannel = "", // If defined you will broadcast to all other scanners on this channel.
                    BroadCastRange = 0, // This is the range that you will broadcast up too.  Note that this value applies to both the sender and receiver, both range requirements must be met. 
                    JammingStrength = 0, // If Mode is set to jamming, then this value will decrease the "range" of broadcasts.  Strength falls off at sqr of the distance.
                    RelayChannel = "", // If defined this channel will be used to relay any targets it seems on the broadcast channel.
                    RelayRange = 0, // This defines the range that any broadcasts will be relayed.  Note that this channel id is seen as the "broadcast" channel for all receivers, broadcast range requirements apply. 
                },
            } ,
            HardPoint = new HardPointDef
            {
                PartName = "Brainless", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0.15f, // Projectile inaccuracy in degrees.
                AimingTolerance = 20f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                AimLeadingPrediction = Advanced, // Level of turret aim prediction; Off, Basic, Accurate, Advanced
                DelayCeaseFire = 5, // Measured in game ticks (6 = 100ms, 60 = 1 second, etc..). Length of time the weapon continues firing after trigger is released.
                AddToleranceToTracking = false, // Allows turret to track to the edge of the AimingTolerance cone instead of dead centre.
                CanShootSubmerged = false, // Whether the weapon can be fired underwater when using WaterMod.

                Ui = new UiDef
                {
                    RateOfFire = false, // Enables terminal slider for changing rate of fire.
                    DamageModifier = false, // Enables terminal slider for changing damage per shot.
                    ToggleGuidance = false, // Enables terminal option to disable smart projectile guidance.
                    EnableOverload = false, // Enables terminal option to turn on Overload; this allows energy weapons to double damage per shot, at the cost of quadrupled power draw and heat gain, and 2% self damage on overheat.
                },
                Ai = new AiDef
                {
                    TrackTargets = true, // Whether this weapon tracks its own targets, or (for multiweapons) relies on the weapon with PrimaryTracking enabled for target designation.
                    TurretAttached = true, // Whether this weapon is a turret and should have the UI and API options for such.
                    TurretController = true, // Whether this weapon can physically control the turret's movement.
                    PrimaryTracking = true, // For multiweapons: whether this weapon should designate targets for other weapons on the platform without their own tracking.
                    LockOnFocus = false, // If enabled, weapon will only fire at targets that have been HUD selected AND locked onto by pressing Numpad 0.
                    SuppressFire = false, // If enabled, weapon can only be fired manually.
                    OverrideLeads = false, // Disable target leading on fixed weapons, or allow it for turrets.
                },
                HardWare = new HardwareDef
                {
                    RotateRate = 0.05f,
                    ElevateRate = 0.06f, // Max traversal speed of elevation subpart in radians per tick.
                    MinAzimuth = -180,
                    MaxAzimuth = 180,
                    MinElevation = -6,
                    MaxElevation = 95,
                    HomeAzimuth = 0, // Default resting rotation angle
                    HomeElevation = 25, // Default resting elevation
                    InventorySize = 1f, // Inventory capacity in kL.
                    IdlePower = 0.02f, // Constant base power draw in MW.
                    FixedOffset = false, // Deprecated.
                    Offset = Vector(x: 0, y: 0, z: 0), // Offsets the aiming/firing line of the weapon, in metres.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                    CriticalReaction = new CriticalDef
                    {
                        Enable = false, // Enables Warhead behaviour.
                        DefaultArmedTimer = 120, // Sets default countdown duration.
                        PreArmed = false, // Whether the warhead is armed by default when placed. Best left as false.
                        TerminalControls = true, // Whether the warhead should have terminal controls for arming and detonation.
                        AmmoRound = "Starcore_AMS_I_BulletBase", // Optional. If specified, the warhead will always use this ammo on detonation rather than the currently selected ammo.
                    },
                },
                Other = new OtherDef
                {
                    ConstructPartCap = 0, // Maximum number of blocks with this weapon on a grid; 0 = unlimited.
                    RotateBarrelAxis = 0, // For spinning barrels, which axis to spin the barrel around; 0 = none.
                    EnergyPriority = 0, // Deprecated.
                    MuzzleCheck = false, // Whether the weapon should check LOS from each individual muzzle in addition to the scope.
                    Debug = false, // Force enables debug mode.
                    RestrictionRadius = 0, // Prevents other blocks of this type from being placed within this distance of the centre of the block.
                    CheckInflatedBox = false, // If true, the above distance check is performed from the edge of the block instead of the centre.
                    CheckForAnyWeapon = false, // If true, the check will fail if ANY weapon is present, not just weapons of the same subtype.
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 1150, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1,
                    TrajectilesPerBarrel = 1, // Number of Trajectiles per barrel per fire event.
                    SkipBarrels = 0,
                    ReloadTime = 60, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    DelayUntilFire = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    HeatPerShot = 26, //heat generated per shot
                    MaxHeat = 6000, //max heat before weapon enters cooldown (70% of max heat)
                    Cooldown = .75f, //percent of max heat to be under to start firing again after overheat accepts .2-.95
                    HeatSinkRate = 130, //amount of heat lost per second
                    DegradeRof = false, // progressively lower rate of fire after 80% heat threshold (80% of max heat)
                    ShotsInBurst = 0,
                    DelayAfterBurst = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    FireFull = false,
                    GiveUpAfter = false,
                    BarrelSpinRate = 2600, // visual only, 0 disables and uses RateOfFire
                    DeterministicSpin = false, // Spin barrel position will always be relative to initial / starting positions (spin will not be as smooth).
                    SpinFree = false, // Spin barrel while not firing.
                    StayCharged = false, // Will start recharging whenever power cap is not full.

                },
                Audio = new HardPointAudioDef
                {
                    PreFiringSound = "", // Audio for warmup effect.
                    FiringSound = "20mmRotaryLoopS", // Audio for firing.
                    FiringSoundPerShot = false, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    ReloadSound = "", // Sound SubtypeID, for when your Weapon is in a reloading state
                    NoAmmoSound = "",
                    HardPointRotationSound = "", // Audio played when turret is moving.
                    BarrelRotationSound = "",
                    FireSoundEndDelay = 0, // How long the firing audio should keep playing after firing stops. Measured in game ticks(6 = 100ms, 60 = 1 seconds, etc..).
                    FireSoundNoBurst = true,
                    
                },
                Graphics = new HardPointParticleDef
                {
                    Effect1 = new ParticleDef
                    {
                        Name = "RERotaryCannonFlash", // SubtypeId of muzzle particle effect.
                        Color = Color(red: 15, green: 2, blue: 1, alpha: 0.8f),  // Deprecated, set color in particle sbc.
                        Offset = Vector(x: 0, y: 0.22, z: -1.5), // Offsets the effect from the muzzle empty.

                        Extras = new ParticleOptionDef
                        {
                            Loop = false, // Deprecated, set this in particle sbc.
                            Restart = true, // Whether to end the previous effect early and spawn a new one.
                            MaxDistance = 1000, // Max distance at which this effect should be visible. NOTE: This will use whichever MaxDistance value is higher across Effect1 and Effect2!
                            MaxDuration = 0, // How many ticks the effect should be ended after, if it's still running.
                            Scale = 1f, // Scale of effect.
                        },
                    },
                    Effect2 = new ParticleDef
                    {
                        Name = "RERotarycannonSmoke",
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),

                        Extras = new ParticleOptionDef
                        {
                            Restart = false,
                            MaxDistance = 50,
                            MaxDuration = 0,
                            Scale = 1f,
                        },
                    },
                },
            },
            Ammos = new[] {
                Starcore_AMS_I_BulletBase,
                Starcore_AMS_I_BulletBaseCheating,
                Starcore_AMS_I_BulletBaseCheating_Frag,
                 // Must list all primary, shrapnel, and pattern ammos.
            },
            Animations = Starcore_AMS_I_Animation,
            //Upgrades = UpgradeModules,
        };
        // Don't edit below this line.
    }
}
