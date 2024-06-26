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
        WeaponDefinition Null_Point_Jump_Disruptor_Large => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "Null_Point_Jump_Disruptor_Large", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up
                        DurabilityMod = 0.222f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "TestIcon.dds" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },
                    
                 },
                Muzzles = new[] {
                    "muzzle_projectile_001",
                },
                Ejector = "", // Optional; empty from which to eject "shells" if specified.
                Scope = "muzzle_projectile_001", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Thrust, Utility, Offense, Power, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
                ClosestFirst = true, // Tries to pick closest targets first (blocks on grids, projectiles, etc...).
                IgnoreDumbProjectiles = false, // Don't fire at non-smart projectiles.
                LockedSmartOnly = false, // Only fire at smart projectiles that are locked on to parent grid.
                MinimumDiameter = 0, // Minimum radius of threat to engage.
                MaximumDiameter = 0, // Maximum radius of threat to engage; 0 = unlimited.
                MaxTargetDistance = 0, // Maximum distance at which targets will be automatically shot at; 0 = unlimited.
                MinTargetDistance = 0, // Minimum distance at which targets will be automatically shot at.
                TopTargets = 4, // Maximum number of targets to randomize between; 0 = unlimited.
                CycleTargets = 0, // Number of targets to "cycle" per acquire attempt.
                TopBlocks = 8, // Maximum number of blocks to randomize between; 0 = unlimited.
                CycleBlocks = 0, // Number of blocks to "cycle" per acquire attempt.
                StopTrackingSpeed = 0, // Do not track threats traveling faster than this speed; 0 = unlimited.
                UniqueTargetPerWeapon = false, // only applies to multi-weapon blocks 
                MaxTrackingTime = 0, // After this time has been reached the weapon will stop tracking existing target and scan for a new one, only applies to turreted weapons
                ShootBlanks = false, // Do not generate projectiles when shooting
                FocusOnly = false, // This weapon can only track focus targets.
                EvictUniqueTargets = false, // if this is set it will evict any weapons set to UniqueTargetPerWeapon unless they to have this set
                Communications = new CommunicationDef
                {
                    StoreTargets = false, // Pushes its current target to the grid/construct so that other slaved weapons can fire on it.
                    StorageLimit = 0, // The limit at which this weapon will no longer export targets onto the channel.
                    MaxConnections = 0, // 0 is unlimited, this value determines the maximum number of weapons that can link up to another weapon.
                    StoreLimitPerBlock = false, // Setting this to true will switch the StorageLimit from being per Location to per block per Location.
                    StorageLocation = "", // This location ID is used either by the master weapon (if ExportTargets = true) or the slave weapon (if its false).  This is shared across the conncted grids.
                    Mode = NoComms, // NoComms, BroadCast, LocalNetwork, Repeater, Relay, Jamming
                    TargetPersists = false, // Whether or not the weapon will retain its existing target even if the source of the target releases theirs.
                    Security = Private, // Public, Private, Secure
                    BroadCastChannel = "", // If defined you will broadcast to all other scanners on this channel.
                    BroadCastRange = 0, // This is the range that you will broadcast up too.  Note that this value applies to both the sender and receiver, both range requirements must be met. 
                    JammingStrength = 0, // If Mode is set to jamming, then this value will decrease the "range" of broadcasts.  Strength falls off at sqr of the distance.
                    RelayChannel = "", // If defined this channel will be used to relay any targets it seems on the broadcast channel.
                    RelayRange = 0, // This defines the range that any broadcasts will be relayed.  Note that this channel id is seen as the "broadcast" channel for all receivers, broadcast range requirements apply. 
                },
            },
            HardPoint = new HardPointDef
            {
                PartName = "Large Null-Point Jump Disruptor", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0f, // Projectile inaccuracy in degrees.
                AimingTolerance = 1f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                AimLeadingPrediction = Accurate, // Level of turret aim prediction; Off, Basic, Accurate, Advanced
                DelayCeaseFire = 0, // Measured in game ticks (6 = 100ms, 60 = 1 second, etc..). Length of time the weapon continues firing after trigger is released.
                AddToleranceToTracking = false, // Allows turret to track to the edge of the AimingTolerance cone instead of dead centre.
                CanShootSubmerged = false, // Whether the weapon can be fired underwater when using WaterMod.
                NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
                ScanTrackOnly = false, // This weapon only scans and tracks entities, this disables un-needed functionality and customizes for this purpose. 
                Ui = new UiDef
                {
                    RateOfFire = false, // Enables terminal slider for changing rate of fire.
                    DamageModifier = false, // Enables terminal slider for changing damage per shot.
                    ToggleGuidance = false, // Enables terminal option to disable smart projectile guidance.
                    EnableOverload = false, // Enables terminal option to turn on Overload; this allows energy weapons to double damage per shot, at the cost of quadrupled power draw and heat gain, and 2% self damage on overheat.
                    AlternateUi = false, // This simplifies and customizes the block controls for alternative weapon purposes,   
                    DisableStatus = false, // Do not display weapon status NoTarget, Reloading, NoAmmo, etc..
                },
                Ai = new AiDef
                {
                    TrackTargets = true, // Whether this weapon tracks its own targets, or (for multiweapons) relies on the weapon with PrimaryTracking enabled for target designation.
                    TurretAttached = false, // Whether this weapon is a turret and should have the UI and API options for such.
                    TurretController = false, // Whether this weapon can physically control the turret's movement.
                    PrimaryTracking = false, // For multiweapons: whether this weapon should designate targets for other weapons on the platform without their own tracking.
                    LockOnFocus = false, // If enabled, weapon will only fire at targets that have been HUD selected AND locked onto by pressing Numpad 0.
                    SuppressFire = false, // If enabled, weapon can only be fired manually.
                    OverrideLeads = false, // Disable target leading on fixed weapons, or allow it for turrets.
                    DefaultLeadGroup = 0, // Default LeadGroup setting, range 0-5, 0 is disables lead group.  Only useful for fixed weapons or weapons set to OverrideLeads.
                    TargetGridCenter = false, // Does not target blocks, instead it targets grid center.
                },
                HardWare = new HardwareDef
                {
                    RotateRate = 0.1f, // Max traversal speed of azimuth subpart in radians per tick (0.1 is approximately 360 degrees per second).
                    ElevateRate = 0.1f, // Max traversal speed of elevation subpart in radians per tick.
                    MinAzimuth = -180,
                    MaxAzimuth = 180,
                    MinElevation = -9,
                    MaxElevation = 50,
                    HomeAzimuth = 0, // Default resting rotation angle
                    HomeElevation = 0, // Default resting elevation
                    InventorySize = 0f, // Inventory capacity in kL.
                    IdlePower = 15f, // Constant base power draw in MW.
                    FixedOffset = false, // Deprecated.
                    Offset = Vector(x: 0, y: 0, z: 0), // Offsets the aiming/firing line of the weapon, in metres.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                    CriticalReaction = new CriticalDef
                    {
                        Enable = false, // Enables Warhead behaviour.
                        DefaultArmedTimer = 120, // Sets default countdown duration.
                        PreArmed = false, // Whether the warhead is armed by default when placed. Best left as false.
                        TerminalControls = true, // Whether the warhead should have terminal controls for arming and detonation.
                        AmmoRound = "AmmoType2", // Optional. If specified, the warhead will always use this ammo on detonation rather than the currently selected ammo.
                    },
                },
                Other = new OtherDef
                {
                    ConstructPartCap = 0, // Maximum number of blocks with this weapon on a grid; 0 = unlimited.
                    RotateBarrelAxis = 0, // For spinning barrels, which axis to spin the barrel around; 0 = none.
                    EnergyPriority = 0, // Deprecated.
                    MuzzleCheck = false, // Whether the weapon should check LOS from each individual muzzle in addition to the scope.
                    DisableLosCheck = false, // Do not perform LOS checks at all... not advised for self tracking weapons
                    NoVoxelLosCheck = false, // If set to true this ignores voxels for LOS checking.. which means weapons will fire at targets behind voxels.  However, this can save cpu in some situations, use with caution. 
                    Debug = false, // Force enables debug mode.
                    RestrictionRadius = 0, // Prevents other blocks of this type from being placed within this distance of the centre of the block.
                    CheckInflatedBox = false, // If true, the above distance check is performed from the edge of the block instead of the centre.
                    CheckForAnyWeapon = false, // If true, the check will fail if ANY weapon is present, not just weapons of the same subtype.
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 150, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 5, // Number of projectiles per muzzle per fire event.
                    SkipBarrels = 0, // Number of muzzles to skip after each fire event.
                    ReloadTime = 250, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    DelayUntilFire = 500, // How long the weapon waits before shooting after being told to fire. Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    HeatPerShot = 1, // Heat generated per shot.
                    MaxHeat = 70000, // Max heat before weapon enters cooldown (70% of max heat).
                    Cooldown = .95f, // Percentage of max heat to be under to start firing again after overheat; accepts 0 - 0.95
                    HeatSinkRate = 9000000, // Amount of heat lost per second.
                    DegradeRof = false, // Progressively lower rate of fire when over 80% heat threshold (80% of max heat).
                    ShotsInBurst = 0, // Use this if you don't want the weapon to fire an entire physical magazine in one go. Should not be more than your magazine capacity.
                    DelayAfterBurst = 0, // How long to spend "reloading" after each burst. Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    FireFull = false, // Whether the weapon should fire the full magazine (or the full burst instead if ShotsInBurst > 0), even if the target is lost or the player stops firing prematurely.
                    GiveUpAfter = false, // Whether the weapon should drop its current target and reacquire a new target after finishing its magazine or burst.
                    BarrelSpinRate = 0, // Visual only, 0 disables and uses RateOfFire.
                    DeterministicSpin = false, // Spin barrel position will always be relative to initial / starting positions (spin will not be as smooth).
                    SpinFree = true, // Spin barrel while not firing.
                    StayCharged = false, // Will start recharging whenever power cap is not full.
                    MaxActiveProjectiles = 0, // Maximum number of drones in flight (only works for drone launchers)
                    MaxReloads = 0, // Maximum number of reloads in the LIFETIME of a weapon
                    GoHomeToReload = false, // Tells the weapon it must be in the home position before it can reload.
                    DropTargetUntilLoaded = false, // If true this weapon will drop the target when its out of ammo and until its reloaded.
                },
                Audio = new HardPointAudioDef
                {
                    PreFiringSound = "JDD_Charging_Winding_Mono_v2.0", // Audio for warmup effect.
                    FiringSound = "JDD_Fire_3_Extended", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    ReloadSound = "", // Sound SubtypeID, for when your Weapon is in a reloading state
                    NoAmmoSound = "",
                    HardPointRotationSound = "", // Audio played when turret is moving.
                    BarrelRotationSound = "JDD_Winding_Down",
                    FireSoundEndDelay = 120, // How long the firing audio should keep playing after firing stops. Measured in game ticks(6 = 100ms, 60 = 1 seconds, etc..).
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
                Graphics = new HardPointParticleDef
                {
                    Effect1 = new ParticleDef
                    {
                        Name = "Smoke_Container", // SubtypeId of muzzle particle effect.
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 1), // Deprecated, set color in particle sbc.
                        Offset = Vector(x: 0, y: 0, z: 0), // Offsets the effect from the muzzle empty.
                        DisableCameraCulling = false, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 50,
                            MaxDuration = 3,
                            Scale = 0.5f,
                        },
                    },
                    Effect2 = new ParticleDef
                    {
                        Name = "",
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = false, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Loop = false, // Set this to the same as in the particle sbc!
                            Restart = false,
                            MaxDistance = 800,
                            MaxDuration = 0,
                            Scale = 1f,
                        },
                    },
                },
            },
            Ammos = new[] {
                Large_JDD_Ammo, 
                 // Must list all primary, shrapnel, and pattern ammos.
            },
            Animations = Null_Point_Jump_Disruptor_AdvancedAnimation,
            //Upgrades = UpgradeModules,
        };

        WeaponDefinition K_HSR_Utilities_ShieldRestore => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "K_HSR_Utilities_Reactor_Gel_LG", // Block Subtypeid. Your Cubeblocks contain this information
                        SpinPartId = "None", // For weapons with a spinning barrel such as Gatling Guns.
                        MuzzlePartId = "None", // The subpart where your muzzle empties are located. This is often the elevation subpart.
                        AzimuthPartId = "None", // Your Rotating Subpart, the bit that moves sideways
                        ElevationPartId = "None",// Your Elevating Subpart, that bit that moves up
                        DurabilityMod = 0.25f, // GeneralDamageMultiplier, 0.25f = 25% damage taken.
                        IconName = "TestIcon.dds" // Overlay for block inventory slots, like reactors, refineries, etc.
                    },

                 },
                Muzzles = new[] {
                    "nozzle",
                },
                Ejector = "", // Optional; empty from which to eject "shells" if specified.
                Scope = "", // Where line of sight checks are performed from. Must be clear of block collision.
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, Neutrals, // Types of threat to engage: Grids, Projectiles, Characters, Meteors, Neutrals
                },
                SubSystems = new[] {
                    Thrust, Utility, Offense, Power, Production, Any, // Subsystem targeting priority: Offense, Utility, Power, Production, Thrust, Jumping, Steering, Any
                },
                ClosestFirst = false, // Tries to pick closest targets first (blocks on grids, projectiles, etc...).
                IgnoreDumbProjectiles = false, // Don't fire at non-smart projectiles.
                LockedSmartOnly = false, // Only fire at smart projectiles that are locked on to parent grid.
                MinimumDiameter = 0, // Minimum radius of threat to engage.
                MaximumDiameter = 0, // Maximum radius of threat to engage; 0 = unlimited.
                MaxTargetDistance = 0, // Maximum distance at which targets will be automatically shot at; 0 = unlimited.
                MinTargetDistance = 0, // Minimum distance at which targets will be automatically shot at.
                TopTargets = 1, // Maximum number of targets to randomize between; 0 = unlimited.
                CycleTargets = 0, // Number of targets to "cycle" per acquire attempt.
                TopBlocks = 1, // Maximum number of blocks to randomize between; 0 = unlimited.
                CycleBlocks = 0, // Number of blocks to "cycle" per acquire attempt.
                StopTrackingSpeed = 0, // Do not track threats traveling faster than this speed; 0 = unlimited.
                UniqueTargetPerWeapon = false, // only applies to multi-weapon blocks 
                MaxTrackingTime = 0, // After this time has been reached the weapon will stop tracking existing target and scan for a new one, only applies to turreted weapons
                ShootBlanks = false, // Do not generate projectiles when shooting
                FocusOnly = false, // This weapon can only track focus targets.
                EvictUniqueTargets = false, // if this is set it will evict any weapons set to UniqueTargetPerWeapon unless they to have this set
                Communications = new CommunicationDef
                {
                    StoreTargets = false, // Pushes its current target to the grid/construct so that other slaved weapons can fire on it.
                    StorageLimit = 0, // The limit at which this weapon will no longer export targets onto the channel.
                    MaxConnections = 0, // 0 is unlimited, this value determines the maximum number of weapons that can link up to another weapon.
                    StoreLimitPerBlock = false, // Setting this to true will switch the StorageLimit from being per Location to per block per Location.
                    StorageLocation = "", // This location ID is used either by the master weapon (if ExportTargets = true) or the slave weapon (if its false).  This is shared across the conncted grids.
                    Mode = NoComms, // NoComms, BroadCast, LocalNetwork, Repeater, Relay, Jamming
                    TargetPersists = false, // Whether or not the weapon will retain its existing target even if the source of the target releases theirs.
                    Security = Private, // Public, Private, Secure
                    BroadCastChannel = "", // If defined you will broadcast to all other scanners on this channel.
                    BroadCastRange = 0, // This is the range that you will broadcast up too.  Note that this value applies to both the sender and receiver, both range requirements must be met. 
                    JammingStrength = 0, // If Mode is set to jamming, then this value will decrease the "range" of broadcasts.  Strength falls off at sqr of the distance.
                    RelayChannel = "", // If defined this channel will be used to relay any targets it seems on the broadcast channel.
                    RelayRange = 0, // This defines the range that any broadcasts will be relayed.  Note that this channel id is seen as the "broadcast" channel for all receivers, broadcast range requirements apply. 
                },
            },
            HardPoint = new HardPointDef
            {
                PartName = "Kreeg Pattern Shield Restore", // Name of the weapon in terminal, should be unique for each weapon definition that shares a SubtypeId (i.e. multiweapons).
                DeviateShotAngle = 0f, // Projectile inaccuracy in degrees.
                AimingTolerance = 0f, // How many degrees off target a turret can fire at. 0 - 180 firing angle.
                AimLeadingPrediction = Off, // Level of turret aim prediction; Off, Basic, Accurate, Advanced
                DelayCeaseFire = 0, // Measured in game ticks (6 = 100ms, 60 = 1 second, etc..). Length of time the weapon continues firing after trigger is released.
                AddToleranceToTracking = false, // Allows turret to track to the edge of the AimingTolerance cone instead of dead centre.
                CanShootSubmerged = false, // Whether the weapon can be fired underwater when using WaterMod.
                NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
                ScanTrackOnly = false, // This weapon only scans and tracks entities, this disables un-needed functionality and customizes for this purpose. 
                Ui = new UiDef
                {
                    RateOfFire = false, // Enables terminal slider for changing rate of fire.
                    DamageModifier = false, // Enables terminal slider for changing damage per shot.
                    ToggleGuidance = false, // Enables terminal option to disable smart projectile guidance.
                    EnableOverload = false, // Enables terminal option to turn on Overload; this allows energy weapons to double damage per shot, at the cost of quadrupled power draw and heat gain, and 2% self damage on overheat.
                    AlternateUi = false, // This simplifies and customizes the block controls for alternative weapon purposes,   
                    DisableStatus = true, // Do not display weapon status NoTarget, Reloading, NoAmmo, etc..
                },
                Ai = new AiDef
                {
                    TrackTargets = false, // Whether this weapon tracks its own targets, or (for multiweapons) relies on the weapon with PrimaryTracking enabled for target designation.
                    TurretAttached = false, // Whether this weapon is a turret and should have the UI and API options for such.
                    TurretController = false, // Whether this weapon can physically control the turret's movement.
                    PrimaryTracking = false, // For multiweapons: whether this weapon should designate targets for other weapons on the platform without their own tracking.
                    LockOnFocus = false, // If enabled, weapon will only fire at targets that have been HUD selected AND locked onto by pressing Numpad 0.
                    SuppressFire = false, // If enabled, weapon can only be fired manually.
                    OverrideLeads = true, // Disable target leading on fixed weapons, or allow it for turrets.
                    DefaultLeadGroup = 0, // Default LeadGroup setting, range 0-5, 0 is disables lead group.  Only useful for fixed weapons or weapons set to OverrideLeads.
                    TargetGridCenter = false, // Does not target blocks, instead it targets grid center.
                },
                HardWare = new HardwareDef
                {
                    RotateRate = 0f, // Max traversal speed of azimuth subpart in radians per tick (0.1 is approximately 360 degrees per second).
                    ElevateRate = 0f, // Max traversal speed of elevation subpart in radians per tick.
                    MinAzimuth = 0,
                    MaxAzimuth = 0,
                    MinElevation = 0,
                    MaxElevation = 0,
                    HomeAzimuth = 0, // Default resting rotation angle
                    HomeElevation = 0, // Default resting elevation
                    InventorySize = 0f, // Inventory capacity in kL.
                    IdlePower = 15f, // Constant base power draw in MW.
                    FixedOffset = false, // Deprecated.
                    Offset = Vector(x: 0, y: 0, z: 0), // Offsets the aiming/firing line of the weapon, in metres.
                    Type = BlockWeapon, // What type of weapon this is; BlockWeapon, HandWeapon, Phantom 
                    CriticalReaction = new CriticalDef
                    {
                        Enable = false, // Enables Warhead behaviour.
                        DefaultArmedTimer = 120, // Sets default countdown duration.
                        PreArmed = false, // Whether the warhead is armed by default when placed. Best left as false.
                        TerminalControls = true, // Whether the warhead should have terminal controls for arming and detonation.
                        AmmoRound = "AmmoType2", // Optional. If specified, the warhead will always use this ammo on detonation rather than the currently selected ammo.
                    },
                },
                Other = new OtherDef
                {
                    ConstructPartCap = 0, // Maximum number of blocks with this weapon on a grid; 0 = unlimited.
                    RotateBarrelAxis = 0, // For spinning barrels, which axis to spin the barrel around; 0 = none.
                    EnergyPriority = 0, // Deprecated.
                    MuzzleCheck = false, // Whether the weapon should check LOS from each individual muzzle in addition to the scope.
                    DisableLosCheck = true, // Do not perform LOS checks at all... not advised for self tracking weapons
                    NoVoxelLosCheck = true, // If set to true this ignores voxels for LOS checking.. which means weapons will fire at targets behind voxels.  However, this can save cpu in some situations, use with caution. 
                    Debug = false, // Force enables debug mode.
                    RestrictionRadius = 0, // Prevents other blocks of this type from being placed within this distance of the centre of the block.
                    CheckInflatedBox = false, // If true, the above distance check is performed from the edge of the block instead of the centre.
                    CheckForAnyWeapon = false, // If true, the check will fail if ANY weapon is present, not just weapons of the same subtype.
                },
                Loading = new LoadingDef
                {
                    RateOfFire = 1, // Set this to 3600 for beam weapons. This is how fast your Gun fires.
                    BarrelsPerShot = 1, // How many muzzles will fire a projectile per fire event.
                    TrajectilesPerBarrel = 1, // Number of projectiles per muzzle per fire event.
                    SkipBarrels = 0, // Number of muzzles to skip after each fire event.
                    ReloadTime = 1, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    DelayUntilFire = 120, // How long the weapon waits before shooting after being told to fire. Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    HeatPerShot = 600, // Heat generated per shot.
                    MaxHeat = 800, // Max heat before weapon enters cooldown (70% of max heat).
                    Cooldown = .95f, // Percentage of max heat to be under to start firing again after overheat; accepts 0 - 0.95
                    HeatSinkRate = 1, // Amount of heat lost per second.
                    DegradeRof = false, // Progressively lower rate of fire when over 80% heat threshold (80% of max heat).
                    ShotsInBurst = 0, // Use this if you don't want the weapon to fire an entire physical magazine in one go. Should not be more than your magazine capacity.
                    DelayAfterBurst = 0, // How long to spend "reloading" after each burst. Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    FireFull = false, // Whether the weapon should fire the full magazine (or the full burst instead if ShotsInBurst > 0), even if the target is lost or the player stops firing prematurely.
                    GiveUpAfter = false, // Whether the weapon should drop its current target and reacquire a new target after finishing its magazine or burst.
                    BarrelSpinRate = 0, // Visual only, 0 disables and uses RateOfFire.
                    DeterministicSpin = false, // Spin barrel position will always be relative to initial / starting positions (spin will not be as smooth).
                    SpinFree = false, // Spin barrel while not firing.
                    StayCharged = false, // Will start recharging whenever power cap is not full.
                    MaxActiveProjectiles = 0, // Maximum number of drones in flight (only works for drone launchers)

                    GoHomeToReload = false, // Tells the weapon it must be in the home position before it can reload.
                    DropTargetUntilLoaded = false, // If true this weapon will drop the target when its out of ammo and until its reloaded.
                },
                Audio = new HardPointAudioDef
                {
                    PreFiringSound = "", // Audio for warmup effect.
                    FiringSound = "", // Audio for firing.
                    FiringSoundPerShot = true, // Whether to replay the sound for each shot, or just loop over the entire track while firing.
                    ReloadSound = "", // Sound SubtypeID, for when your Weapon is in a reloading state
                    NoAmmoSound = "",
                    HardPointRotationSound = "", // Audio played when turret is moving.
                    BarrelRotationSound = "",
                    FireSoundEndDelay = 240, // How long the firing audio should keep playing after firing stops. Measured in game ticks(6 = 100ms, 60 = 1 seconds, etc..).
                    FireSoundNoBurst = true, // Don't stop firing sound from looping when delaying after burst.
                },
                Graphics = new HardPointParticleDef
                {
                    Effect1 = new ParticleDef
                    {
                        Name = "Smoke_Container", // SubtypeId of muzzle particle effect.
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 1), // Deprecated, set color in particle sbc.
                        Offset = Vector(x: 0, y: 0, z: 0), // Offsets the effect from the muzzle empty.
                        DisableCameraCulling = false, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 1500,
                            MaxDuration = 3,
                            Scale = 0.5f,
                        },
                    },
                    Effect2 = new ParticleDef
                    {
                        Name = "",
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = false, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Loop = false, // Set this to the same as in the particle sbc!
                            Restart = false,
                            MaxDistance = 800,
                            MaxDuration = 0,
                            Scale = 1f,
                        },
                    },
                },
            },
            Ammos = new[] {
                ShieldRestore, ShieldRestoreFrag, ShieldRestoreShrapForTheFrag,
                 // Must list all primary, shrapnel, and pattern ammos.
            },
            //Animations = Null_Point_Jump_Disruptor_AdvancedAnimation,
            //Upgrades = UpgradeModules,
        };
        // Don't edit below this line.
    }
}
