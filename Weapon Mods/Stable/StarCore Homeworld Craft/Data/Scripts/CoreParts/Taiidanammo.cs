﻿using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AmmoDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef.SpawnType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.ShapeDef.Shapes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.CustomScalesDef.SkipMode;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.FragmentDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.PatternDef.PatternModes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.FragmentDef.TimedSpawnDef.PointTypes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.Conditions;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.UpRelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.FwdRelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.ReInitCondition;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.RelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.ConditionOperators;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.StageEvents;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.ShieldDef.ShieldType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DeformDef.DeformTypes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef.Falloff;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef.AoeShape;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.EwarMode;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.EwarType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.PushPullDef.Force;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.FactionColor;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.TracerBaseDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.Texture;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.DecalDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DamageTypes.Damage;

namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {

        private AmmoDef taiidan_interceptor_Main => new AmmoDef // StarCore AMS I
        {
            AmmoMagazine = "TaiidanCraft", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Taiidani Interceptor", // Name of ammo in terminal, should be different for each ammo type used by the same weapon.
            HybridRound = true, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0.001f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 1000f, // Direct damage; one steel plate is worth 100. 
            Mass = 5, // In kilograms; how much force the impact will apply to the target.
            Health = 250, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            BackKickForce = 10, // Recoil.
            DecayPerShot = 0f, // Damage to the firing weapon itself.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            EnergyMagazineSize = 5, // For energy weapons, how many shots to fire before reloading.
            IgnoreWater = false, // Whether the projectile should be able to penetrate water when using WaterMod.
            IgnoreVoxels = true, // Whether the projectile should be able to penetrate voxels.
            HeatModifier = 1f, // Allows this ammo to modify the amount of heat the weapon produces per shot.
            Sync = new SynchronizeDef
            {
                Full = false, // Be careful, do not use on high fire rate weapons. Do not use with other sync options. Only works on drones and Smart projectiles.Will only work on chained / staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
                PointDefense = true, // Server will inform clients of what projectiles have died by PD defense and will trigger destruction.
                OnHitDeath = true, // Server will inform clients when projectiles die due to them hitting something and will trigger destruction.
            },

            Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
            {
                Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                Diameter = 10, // Diameter is minimum length of LineShape or minimum diameter of SphereShape.
            },
            ObjectsHit = new ObjectsHitDef
            {
                MaxObjectsHit = 0, // Limits the number of entities (grids, players, projectiles) the projectile can penetrate; 0 = unlimited.
                CountBlocks = false, // Counts individual blocks, not just entities hit.
            },
            Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
            {
                AmmoRound = "Taiidan Interceptor Attack", // AmmoRound field of the ammo to spawn.
                Fragments = 1, // Number of projectiles to spawn.
                Degrees = 1f, // Cone in which to randomize direction of spawned projectiles.
                Reverse = false, // Spawn projectiles backward instead of forward.
                DropVelocity = false, // fragments will not inherit velocity from parent.
                Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                IgnoreArming = false, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                ArmWhenHit = false, // Setting this to true will arm the projectile when its shot by other projectiles.
                AdvOffset = Vector(x: 0, y: 0, z: 0), // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below
                {
                    Enable = true, // Enables TimedSpawns mechanism
                    Interval = 2, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                    StartTime = 0, // Time delay to start spawning fragments, in ticks, of total projectile life
                    MaxSpawns = 400, // Max number of fragment children to spawn
                    Proximity = 1500, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                    ParentDies = false, // Parent dies once after it spawns its last child.
                    PointAtTarget = true, // Start fragment direction pointing at Target
                    PointType = Predict, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                    DirectAimCone = 30f, //Aim cone used for Direct fire, in degrees
                    GroupSize = 30, // Number of spawns in each group
                    GroupDelay = 15, // Delay between each group.
                },
            },
            Pattern = new PatternDef
            {
                Patterns = new[] { // If enabled, set of multiple ammos to fire in order instead of the main ammo.
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Attack", "Taiidan Interceptor Attack", "Taiidan Interceptor Attack",
                    "Taiidan Interceptor Missile Attack",
                },
                Mode = Fragment, // Select when to activate this pattern, options: Never, Weapon, Fragment, Both 
                TriggerChance = 1f, // This is %
                Random = false, // This randomizes the number spawned at once, NOT the list order.
                RandomMin = 1,
                RandomMax = 1,
                SkipParent = false, // Skip the Ammo itself, in the list
                PatternSteps = 1, // Number of Ammos activated per round, will progress in order and loop. Ignored if Random = true.
            },
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 1, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                FallOff = new FallOffDef
                {
                    Distance = 3000f, // Distance at which damage begins falling off.
                    MinMultipler = 1f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                },
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = 1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = 0.5f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = 1f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Custom = new CustomScalesDef
                {
                    SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                    Types = new[] // List of blocks to apply custom damage multipliers to.
                    {
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test1",
                            Modifier = -1f,
                        },
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test2",
                            Modifier = -1f,
                        },
                    },
                },
            },
            AreaOfDamage = new AreaOfDamageDef
            {
                ByBlockHit = new ByBlockHitDef
                {
                    Enable = false,
                    Radius = 4f, // Meters
                    Damage = 50f,
                    Depth = 1f, // Meters
                    MaxAbsorb = 0f,
                    Falloff = Linear, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    Shape = Diamond, // Round or Diamond
                },
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 6f, // Meters
                    Damage = 1000f,
                    Depth = 4f,
                    MaxAbsorb = 0f,
                    Falloff = NoFalloff, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    ArmOnlyOnHit = false, // Detonation only is available, After it hits something, when this is true. IE, if shot down, it won't explode.
                    MinArmingTime = 0, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    NoVisuals = false,
                    NoSound = false,
                    ParticleScale = 5,
                    CustomParticle = "Grid_Destruction", // Particle SubtypeID, from your Particle SBC
                    CustomSound = "hwshipexplode", // SubtypeID from your Audio SBC, not a filename
                    Shape = Diamond, // Round or Diamond
                },
            },
            Ewar = new EwarDef
            {
                Enable = false, // Enables the EWAR , Electronic-Warfare System
                Type = Offense, // EnergySink, Emp, Offense, Nav, Dot, AntiSmart, JumpNull, Anchor, Tractor, Pull, Push, 
                Mode = Field, // Effect , Field
                Strength = 1000000000,
                Radius = 8f, // Meters
                Duration = 240, // In Ticks
                StackDuration = true, // Combined Durations
                Depletable = false,
                MaxStacks = 2, // Max Debuffs at once
                NoHitParticle = false,
                /*
                EnergySink : Targets & Shutdowns Power Supplies, such as Batteries & Reactor
                Emp : Targets & Shutdown any Block capable of being powered
                Offense : Targets & Shutdowns Weaponry
                Nav : Targets & Shutdown Gyros, Thrusters, or Locks them down
                Dot : Deals Damage to Blocks in radius
                AntiSmart : Effects & Scrambles the Targeting List of Affected Missiles
                JumpNull : Shutdown & Stops any Active Jumps, or JumpDrive Units in radius
                Tractor : Affects target with Physics
                Pull : Affects target with Physics
                Push : Affects target with Physics
                Anchor : Affects target with Physics
                
                */
                Force = new PushPullDef
                {
                    ForceFrom = ProjectileLastPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    ForceTo = HitPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    Position = TargetCenterOfMass, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    DisableRelativeMass = false,
                    TractorRange = 0,
                    ShooterFeelsForce = false,
                },
                Field = new FieldDef
                {
                    Interval = 0, // Time between each pulse, in game ticks (60 == 1 second).
                    PulseChance = 0, // Chance from 0 - 100 that an entity in the field will be hit by any given pulse.
                    GrowTime = 0, // How many ticks it should take the field to grow to full size.
                    HideModel = false, // Hide the projectile model if it has one.
                    ShowParticle = true, // Show Block damage effect.
                    TriggerRange = 250f, //range at which fields are triggered
                    Particle = new ParticleDef // Particle effect to generate at the field's position.
                    {
                        Name = "", // SubtypeId of field particle effect.
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1, // Scale of effect.
                        },
                    },
                },
            },
            Beams = new BeamDef
            {
                Enable = false, // Enable beam behaviour. Please have 3600 RPM, when this Setting is enabled. Please do not fire Beams into Voxels.
                VirtualBeams = false, // Only one damaging beam, but with the effectiveness of the visual beams combined (better performance).
                ConvergeBeams = false, // When using virtual beams, converge the visual beams to the location of the real beam.
                RotateRealBeam = false, // The real beam is rotated between all visual beams, instead of centered between them.
                OneParticle = false, // Only spawn one particle hit per beam weapon.
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 180f, // Degrees, Is pointed forward
                TargetLossTime = 0, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                MaxLifeTime = 36000, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 300f, // Acceleration in Meters Per Second. Projectile starts on tick 0 at its parents (weapon/other projectiles) travel velocity.
                DesiredSpeed = 500f, // voxel phasing if you go above 5100
                MaxTrajectory = 1000000f, // Max Distance the projectile or beam can Travel.
                DeaccelTime = 0, // 0 is disabled, a value causes the projectile to come to rest overtime, (Measured in game ticks, 60 = 1 second)
                GravityMultiplier = 0f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable. Natural Gravity Only.
                SpeedVariance = Random(start: -150, end: 250), // subtracts value from DesiredSpeed. Be warned, you can make your projectile go backwards.
                RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                TotalAcceleration = 0, // 0 means no limit, something to do due with a thing called delta and something called v.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 160, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Inaccuracy = 0f, // 0 is perfect, hit accuracy will be a random num of meters between 0 and this value.
                    Aggressiveness = 3f, // controls how responsive tracking is.
                    MaxLateralThrust = 1, // controls how sharp the projectile may turn, this is the cheaper but less realistic version of SteeringLimit, cost of 2 on a scale of 1-5, 0 being basic smart.
                    NavAcceleration = -1, // helps influence how the projectile steers. Negative disables feature.
                    TrackingDelay = 0, // Measured in Shape diameter units traveled.
                    AccelClearance = false, // Setting this to true will prevent smart acceleration until it is clear of the grid and tracking delay has been met (free fall).
                    MaxChaseTime = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    OverideTarget = false, // when set to true ammo picks its own target, does not use hardpoint's.
                    CheckFutureIntersection = true, // Utilize obstacle avoidance for drones
                    FutureIntersectionRange = 500,
                    MaxTargets = 0, // Number of targets allowed before ending, 0 = unlimited
                    NoTargetExpire = false, // Expire without ever having a target at TargetLossTime
                    Roam = false, // Roam current area after target loss
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    OffsetRatio = 0f, // The ratio to offset the random direction (0 to 1) 
                    OffsetTime = 0, // how often to offset degree, measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..)
                    OffsetMinRange = 0, // The range from target at which offsets are no longer active
                    FocusOnly = false, // only target the constructs Ai's focus target
                    ScanRange = 5000,
                    MinTurnSpeed = 40, // set this to a reasonable value to avoid projectiles from spinning in place or being too aggressive turing at slow speeds 
                    NoTargetApproach = true, // If true approaches can begin prior to the projectile ever having had a target.
                    AltNavigation = false, // If true this will swap the default navigation algorithm from ProNav to ZeroEffort Miss.  Zero effort is more direct/precise but less cinematic 
                },
                Approaches = new[] // These approaches move forward and backward in order, once the end condition of the last one is reached it will revert to default behavior. Cost level of 4+, or 5+ if used with steering.
                {
                    //0
                    new ApproachDef // Launch
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        OnRestartRevertTo = -1, // This applies if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = Spawn, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=]
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = MinTravelRequired,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 0,
                        Start2Value = 0,
                        End1Value = 200,
                        End2Value = 0,
                        End3Value = 0, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = StorePositionC,

                        Forward = ForwardOriginDirection, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpOriginDirection, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = PositionA,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = StoredEndLocalPosition,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 3.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 3.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = false, // Orbit the target
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from target.  
                        OffsetMaxRadius = 0, // Max Radius to offset from target.  
                        OffsetTime = 0, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_High",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "" // if blank it will use default, must be a default version for this to be useable. 
                    },
                    //1
                    new ApproachDef // Travel
                    {
                        // Start/End behaviors 
                        RestartCondition = ForceRestart, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        RestartList = new[]
                        { // This list is used if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                            new WeightedIdListDef
                            {// If all valid entries (below MaxRuns) role a 0 (i.e. weights are disabled), then the entry with the lowest current "Runs" will be selected, if two or more share lowest runs then the winner is decided by the order below.
                                ApproachId = 3,
                                MaxRuns = 0, // 0 means unlimited, defines how many times this entry can return true. 
                                Weight = Random(1, 1), // The approachId that rolls the highest number will be selected
                                End1WeightMod = 11, // multiplies the weight Start and End value by this number, if both End conditions were true the highest roll between them wins, 0 means disabled
                                End2WeightMod = 0,
                                End3WeightMod = 0,
                            },
                            new WeightedIdListDef
                            {
                                //rtb
                                ApproachId = 5,
                                MaxRuns = 1,
                                Weight = Random(1, 1),
                                End1WeightMod = 1,
                                End2WeightMod = 11,
                                End3WeightMod = 0,
                            },
                        },
                        Operators = StartAnd_EndOr, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=]
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = RelativeHealthLost,
                        EndCondition3 = Ignore,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 1501,
                        Start2Value = 0,
                        End1Value = 1500,
                        End2Value = 20,
                        End3Value = 0, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Relative positions and directions
                        Forward = ForwardTargetDirection, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpTargetDirection, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = Shooter, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartDestination, StoredEndDestination
                        PositionC = Target,
                        Elevation = MidPoint, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0.5, // value 0 - 1, rotates the Updir
                        AngleVariance = Random(-0.4f, 0.4f), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 500, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 2.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 1.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = false, // Orbit the target
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from target.  
                        OffsetMaxRadius = 0, // Max Radius to offset from target.  
                        OffsetTime = 0, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_High",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "" // if blank it will use default, must be a default version for this to be useable. 
                    },
                    //2
                    new ApproachDef // Orbit
                    {
                        RestartCondition = ForceRestart, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        RestartList = new[]
                        { // This list is used if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                            new WeightedIdListDef
                            {// If all valid entries (below MaxRuns) role a 0 (i.e. weights are disabled), then the entry with the lowest current "Runs" will be selected, if two or more share lowest runs then the winner is decided by the order below.
                                ApproachId = 4,
                                MaxRuns = 0, // 0 means unlimited, defines how many times this entry can return true. 
                                Weight = Random(0, 1), // The approachId that rolls the highest number will be selected
                                End1WeightMod = 11, // multiplies the weight Start and End value by this number, if both End conditions were true the highest roll between them wins, 0 means disabled
                                End2WeightMod = 11,
                                End3WeightMod = 1,
                            },
                            new WeightedIdListDef
                            {
                                //rtb
                                ApproachId = 1,
                                MaxRuns = 0,
                                Weight = Random(0, 1),
                                End1WeightMod = 1,
                                End2WeightMod = 1,
                                End3WeightMod = 11,
                            },
                        },
                        Operators = StartAnd_EndOr, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = DistanceFromPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=]
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = RelativeLifetime,
                        EndCondition2 = EnemyTargetLoss,
                        EndCondition3 = RelativeHealthLost,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 600,
                        Start2Value = 0,
                        End1Value = 300,
                        End2Value = 60,
                        End3Value = 10, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Relative positions and directions
                        Forward = ForwardRelativeToBlock, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartDestination, StoredEndDestination
                        PositionC = Target,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        AngleVariance = Random(-0.25f, 0.25f), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 0.5, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).


                        // Target navigation behavior 
                        Orbit = true, // Orbit the target
                        OrbitRadius = 500, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 100, // Min Radius to offset from target.  
                        OffsetMaxRadius = 250, // Max Radius to offset from target.  
                        OffsetTime = 480, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = false, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_High",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "" // if blank it will use default, must be a default version for this to be useable. 
                    },
                    //3
                    new ApproachDef // Turn
                    {
                        // Start/End behaviors 
                        RestartCondition = ForceRestart, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        RestartList = new[]
                        { // This list is used if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                            new WeightedIdListDef
                            {// If all valid entries (below MaxRuns) role a 0 (i.e. weights are disabled), then the entry with the lowest current "Runs" will be selected, if two or more share lowest runs then the winner is decided by the order below.
                                ApproachId = 4,
                                MaxRuns = 0, // 0 means unlimited, defines how many times this entry can return true. 
                                Weight = Random(1, 1), // The approachId that rolls the highest number will be selected
                                End1WeightMod = 11, // multiplies the weight Start and End value by this number, if both End conditions were true the highest roll between them wins, 0 means disabled
                                End2WeightMod = 1,
                                End3WeightMod = 1,
                            },
                            new WeightedIdListDef
                            {
                                //rtb
                                ApproachId = 1,
                                MaxRuns = 0,
                                Weight = Random(1, 1),
                                End1WeightMod = 1,
                                End2WeightMod = 11,
                                End3WeightMod = 1,
                            },
                        },
                        Operators = StartAnd_EndOr, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = SinceTimedSpawn, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=]
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = RelativeLifetime,
                        EndCondition2 = RelativeHealthLost,
                        EndCondition3 = Ignore,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 300,
                        Start2Value = 0,
                        End1Value = 90,
                        End2Value = 10,
                        End3Value = 0, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Relative positions and directions
                        Forward = ForwardRelativeToBlock, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartDestination, StoredEndDestination
                        PositionC = Target,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 0.5, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = true, // Orbit the target
                        OrbitRadius = 750, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 500, // Min Radius to offset from target.  
                        OffsetMaxRadius = 1000, // Max Radius to offset from target.  
                        OffsetTime = 480, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = false, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_Low",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 60, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },
                    //4
                    new ApproachDef // Strafe
                    {
                        // Start/End behaviors 
                        RestartCondition = ForceRestart, // Wait, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        RestartList = new[]
                        { // This list is used if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                            new WeightedIdListDef
                            {// If all valid entries (below MaxRuns) role a 0 (i.e. weights are disabled), then the entry with the lowest current "Runs" will be selected, if two or more share lowest runs then the winner is decided by the order below.
                                ApproachId = 2,
                                MaxRuns = 5, // 0 means unlimited, defines how many times this entry can return true. 
                                Weight = Random(3, 10),
                                End1WeightMod = 1,
                                End2WeightMod = 1,
                                End3WeightMod = 11,

                            },
                            new WeightedIdListDef
                            {// If all valid entries (below MaxRuns) role a 0 (i.e. weights are disabled), then the entry with the lowest current "Runs" will be selected, if two or more share lowest runs then the winner is decided by the order below.
                                ApproachId = 5,
                                MaxRuns = 1, // 0 means unlimited, defines how many times this entry can return true. 
                                Weight = Random(0, 3),
                                End1WeightMod = 0,
                                End2WeightMod = 0,
                                End3WeightMod = 11,
                            },
                        },
                        Operators = StartAnd_EndOr, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = SinceTimedSpawn, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=]
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = RelativeLifetime,
                        EndCondition2 = EnemyTargetLoss,
                        EndCondition3 = RelativeHealthLost,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 300,
                        Start2Value = 0,
                        End1Value = 230,
                        End2Value = 60,
                        End3Value = 10, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Relative positions and directions
                        Forward = ForwardRelativeToBlock, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartDestination, StoredEndDestination
                        PositionC = Target,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 3.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 2, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = true, // Orbit the target
                        OrbitRadius = 200, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 150, // Min Radius to offset from target.  
                        OffsetMaxRadius = 350, // Max Radius to offset from target.  
                        OffsetTime = 480, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = false, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = true, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_Low",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 59, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },
                    //5
                    new ApproachDef // RTB
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        OnRestartRevertTo = -1, // This applies if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance)
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 1001,
                        Start2Value = 0,
                        End1Value = 1000,
                        End2Value = 0,
                        End3Value = 0, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Relative positions and directions
                        Forward = ForwardRelativeToBlock, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Shooter,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 2.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = false, // Orbit the target
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from target.  
                        OffsetMaxRadius = 0, // Max Radius to offset from target.  
                        OffsetTime = 0, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_High",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "" // if blank it will use default, must be a default version for this to be useable. 
                    },
                    //6
                    new ApproachDef // Recover Orbit
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        OnRestartRevertTo = -1, // This applies if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = DistanceFromPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance)
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = RelativeLifetime,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 1000,
                        Start2Value = 0,
                        End1Value = 300,
                        End2Value = 0,
                        End3Value = 0, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Relative positions and directions
                        Forward = ForwardRelativeToShooter, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToShooter, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartDestination, StoredEndDestination
                        PositionC = Shooter,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 1.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = true, // Orbit the target
                        OrbitRadius = 1000, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from target.  
                        OffsetMaxRadius = 0, // Max Radius to offset from target.  
                        OffsetTime = 0, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_High",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "" // if blank it will use default, must be a default version for this to be useable. 
                    },
                    //7
                    new ApproachDef // Recover
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        OnRestartRevertTo = -1, // This applies if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance)
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 2,
                        Start2Value = 0,
                        End1Value = 1,
                        End2Value = 0,
                        End3Value = 0, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = Refund,  
                        
                        // Relative positions and directions
                        Forward = ForwardRelativeToBlock, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = StoredEndLocalPosition,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 3.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 1.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = false, // Orbit the target
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from target.  
                        OffsetMaxRadius = 0, // Max Radius to offset from target.  
                        OffsetTime = 0, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 100, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_Low",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "" // if blank it will use default, must be a default version for this to be useable. 
                    },
                    //8
                    new ApproachDef // Dock
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        OnRestartRevertTo = -1, // This applies if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // DistanceFromDestination[<=], DistanceToDestination[>=], Lifetime[>=], DeadTime[<=], MinTravelRequired[>=], MaxTravelRequired[<=],
                                                    // Ignore(skip this condition), Spawn(works per stage), DesiredElevation(tolerance can be set with ElevationTolerance)
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,

                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 500,
                        Start2Value = 0,
                        End1Value = 200,
                        End2Value = 0,
                        End3Value = 0, 
                        
                        // Special triggers when the start/end conditions are met (DoNothing, EndProjectile, EndProjectileOnRestart, StoreDestination)
                        StartEvent = DoNothing,
                        EndEvent = EndProjectile,  
                        
                        // Relative positions and directions
                        Forward = ForwardRelativeToShooter, // ForwardDestinationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToShooter, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpDestinationDirection
                        
                        PositionB = PositionA, // Origin, Shooter, Target, Surface, MidPoint, Current, Nothing, StoredStartDestination, StoredEndDestination
                        PositionC = Shooter,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the source position overtime.
                        AdjustPositionC = true, // Update destination overtime
                        LeadRotateElevatePositionB = false, // Add lead and rotation to Source Position
                        LeadRotateElevatePositionC = false, // Add lead and rotation to Destination Position
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to target
                        DesiredElevation = 0, // The desired elevation relative to source 
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA,
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 3.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 1.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // Target navigation behavior 
                        Orbit = false, // Orbit the target
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the target (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from target.  
                        OffsetMaxRadius = 0, // Max Radius to offset from target.  
                        OffsetTime = 0, // How often to change the offset direction.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = true, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "BSG_Viper_MK_I_Exhaust_Low",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "" // if blank it will use default, must be a default version for this to be useable. 
                    },
                },
                Mines = new MinesDef  // Note: This is being investigated. Please report to Github, any issues.
                {
                    DetectRadius = 0,
                    DeCloakRadius = 0,
                    FieldTime = 0,
                    Cloak = false,
                    Persist = false,
                },
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "\\Models\\Taiidan_Interceptor", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "CraftExplodeDebris",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                    Eject = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0f, end: 0f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                    DropParentVelocity = true, // If set to true the tracer will not take on the parents (grid/player) initial velocity when rendering.
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 5f, //
                        Width = 1f, //
                        Color = Color(red: 32, green: 8, blue: 9f, alpha: 1), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                        Segmentation = new SegmentDef
                        {
                            Enable = false, // If true Tracer TextureMode is ignored
                            Textures = new[] {
                                "", // Please always have this Line set, if this Section is enabled.
                            },
                            SegmentLength = 0f, // Uses the values below.
                            SegmentGap = 0f, // Uses Tracer textures and values
                            Speed = 1f, // meters per second
                            Color = Color(red: 1, green: 2, blue: 2.5f, alpha: 1),
                            WidthMultiplier = 1f,
                            Reverse = false,
                            UseLineVariance = true,
                            WidthVariance = Random(start: 0f, end: 0f),
                            ColorVariance = Random(start: 0f, end: 0f)
                        }
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal,
                        DecayTime = 60, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                        Color = Color(red: 15f, green: 8f, blue: 0f, alpha: 0.10f),
                        FactionColor = Background, // Tracer uses faction color if it exists.
                        Back = true,
                        CustomWidth = 2f,
                        UseWidthVariance = true,
                        UseColorFade = true,
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "hwshiploop", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "",
                ShotSound = "hwshiplaunchtalk",
                ShieldHitSound = "",
                PlayerHitSound = "",
                VoxelHitSound = "",
                FloatingHitSound = "",
                HitPlayChance = 1f,
                HitPlayShield = true,
            },
            Ejection = new EjectionDef // Optional Component, allows generation of Particle or Item (Typically magazine), on firing, to simulate Tank shell ejection
            {
                Type = Particle, // Particle or Item (Inventory Component)
                Speed = 100f, // Speed inventory is ejected from in dummy direction
                SpawnChance = 0.5f, // chance of triggering effect (0 - 1)
                CompDef = new ComponentDef
                {
                    ItemName = "", //InventoryComponent name
                    ItemLifeTime = 0, // how long item should exist in world
                    Delay = 0, // delay in ticks after shot before ejected
                }
            }, // Don't edit below this line
        };

        private AmmoDef taiidan_interceptor_attack => new AmmoDef
        {
            AmmoMagazine = "Energy", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Taiidan Interceptor Attack", // Name of ammo in terminal, should be different for each ammo type used by the same weapon.
            HybridRound = false, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0.001f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 500f, // Direct damage; one steel plate is worth 100.
            Mass = 0f, // In kilograms; how much force the impact will apply to the target.
            Health = 0, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            BackKickForce = 0f, // Recoil. This is applied to the Parent Grid.
            DecayPerShot = 0f, // Damage to the firing weapon itself. 
                               //float.MaxValue will drop the weapon to the first build state and destroy all components used for construction
                               //If greater than cube integrity it will remove the cube upon firing, without causing deformation (makes it look like the whole "block" flew away)
            HardPointUsable = false, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            EnergyMagazineSize = 1, // For energy weapons, how many shots to fire before reloading.
            IgnoreWater = false, // Whether the projectile should be able to penetrate water when using WaterMod.
            IgnoreVoxels = false, // Whether the projectile should be able to penetrate voxels.
            Synchronize = false, // Be careful, do not use on high fire rate weapons.  Only works on drones and Smart projectiles.  Will only work on chained/staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
            HeatModifier = -1f, // Allows this ammo to modify the amount of heat the weapon produces per shot.
            NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
            Sync = new SynchronizeDef
            {
                Full = false, // Be careful, do not use on high fire rate weapons. Do not use with other sync options. Only works on drones and Smart projectiles.Will only work on chained / staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
                PointDefense = false, // Server will inform clients of what projectiles have died by PD defense and will trigger destruction.
                OnHitDeath = false, // Server will inform clients when projectiles die due to them hitting something and will trigger destruction.
            },
            Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
            {
                Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                Diameter = 1, // Diameter is minimum length of LineShape or minimum diameter of SphereShape.
            },
            ObjectsHit = new ObjectsHitDef
            {
                MaxObjectsHit = 0, // Limits the number of entities (grids, players, projectiles) the projectile can penetrate; 0 = unlimited.
                CountBlocks = false, // Counts individual blocks, not just entities hit.
            },
            Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
            {
                AmmoRound = "", // AmmoRound field of the ammo to spawn.
                Fragments = 1, // Number of projectiles to spawn.
                Degrees = 0, // Cone in which to randomize direction of spawned projectiles.
                Reverse = false, // Spawn projectiles backward instead of forward.
                DropVelocity = false, // fragments will not inherit velocity from parent.
                Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                IgnoreArming = false, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                AdvOffset = Vector(x: 0, y: 0, z: 0), // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below
                {
                    Enable = false, // Enables TimedSpawns mechanism
                    Interval = 15, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                    StartTime = 30, // Time delay to start spawning fragments, in ticks, of total projectile life
                    MaxSpawns = 8, // Max number of fragment children to spawn
                    Proximity = 4000, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                    ParentDies = true, // Parent dies once after it spawns its last child.
                    PointAtTarget = true, // Start fragment direction pointing at Target
                    PointType = Direct, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                    DirectAimCone = 10f, //Aim cone used for Direct fire, in degrees
                    GroupSize = 0, // Number of spawns in each group
                    GroupDelay = 0, // Delay between each group.
                },
            },
            Pattern = new PatternDef
            {
                Patterns = new[] { // If enabled, set of multiple ammos to fire in order instead of the main ammo.
                    "",
                },
                Mode = Fragment, // Select when to activate this pattern, options: Never, Weapon, Fragment, Both 
                TriggerChance = 1f, // This is %
                Random = false, // This randomizes the number spawned at once, NOT the list order.
                RandomMin = 1,
                RandomMax = 1,
                SkipParent = false, // Skip the Ammo itself, in the list
                PatternSteps = 1, // Number of Ammos activated per round, will progress in order and loop. Ignored if Random = true.
            },
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 250f, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                FallOff = new FallOffDef
                {
                    Distance = 0f, // Distance at which damage begins falling off.
                    MinMultipler = -1f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                },
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = 1f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic,
                    AreaEffect = Kinetic, // Kinetic , Energy, are your Options.
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile.
                },
                Custom = new CustomScalesDef
                {
                    SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                    Types = new[] // List of blocks to apply custom damage multipliers to.
                    {
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test1",
                            Modifier = -1f,
                        },
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test2",
                            Modifier = -1f,
                        },
                    },
                },
            },
            AreaOfDamage = new AreaOfDamageDef
            {
                ByBlockHit = new ByBlockHitDef
                {
                    Enable = false,
                    Radius = 0f,
                    Damage = 0f,
                    Depth = 0f,
                    MaxAbsorb = 0f,
                    Falloff = Pooled, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the midAdle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                },
                EndOfLife = new EndOfLifeDef
                {
                    Enable = false,
                    Radius = 1f, // Meters
                    Damage = 0.4f,
                    Depth = 1f,
                    MaxAbsorb = 0f,
                    Falloff = Pooled, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    //.Exponential drops off exponentially.  Does not scale to max radius
                    ArmOnlyOnHit = false, // Detonation only is available, After it hits something, when this is true. IE, if shot down, it won't explode.
                    MinArmingTime = 0, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    NoVisuals = false,
                    NoSound = false,
                    ParticleScale = 1,
                    CustomParticle = "BangShooty", // Particle SubtypeID, from your Particle SBC
                    CustomSound = "", // SubtypeID from your Audio SBC, not a filename
                    Shape = Diamond, // Round or Diamond
                },
            },
            Ewar = new EwarDef
            {
                Enable = false,
                Type = Offense, //AntiSmart,JumpNull,EnergySink, Anchor,Emp,Offense,Nav,Dot,Push,Pull,Tractor,
                Mode = Effect,
                Strength = 1f,
                Radius = 1f,
                Duration = 120,
                StackDuration = false,
                Depletable = false,
                MaxStacks = 0,
                NoHitParticle = false,
                Force = new PushPullDef
                {
                    ForceFrom = ProjectileLastPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    ForceTo = HitPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    Position = TargetCenterOfMass, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    DisableRelativeMass = false,
                    TractorRange = 0,
                    ShooterFeelsForce = false,
                },
                Field = new FieldDef
                {
                    Interval = 0, // Time between each pulse, in game ticks (60 == 1 second).
                    PulseChance = 0, // Chance from 0 - 100 that an entity in the field will be hit by any given pulse.
                    GrowTime = 0, // How many ticks it should take the field to grow to full size.
                    HideModel = false, // Hide the projectile model if it has one.
                    ShowParticle = false, // Deprecated.
                    Particle = new ParticleDef // Particle effect to generate at the field's position.
                    {
                        Name = "", // SubtypeId of field particle effect.
                                   // Deprecated.
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 0), // Deprecated, set color in particle sbc.
                        Extras = new ParticleOptionDef
                        {
                            Loop = false, // Deprecated, set this in particle sbc.
                            Restart = false, // Not used.
                            MaxDistance = 5000, // Not used.
                            MaxDuration = 1, // Not used.
                            Scale = 1, // Scale of effect.
                        },
                    },
                },
            },
            Beams = new BeamDef
            {
                Enable = false, // Enable beam behaviour.
                VirtualBeams = false, // Only one damaging beam, but with the effectiveness of the visual beams combined (better performance).
                ConvergeBeams = false, // When using virtual beams, converge the visual beams to the location of the real beam.
                RotateRealBeam = false, // The real beam is rotated between all visual beams, instead of centered between them.
                OneParticle = false, // Only spawn one particle hit per beam weapon.
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 0,
                TargetLossTime = 0, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                MaxLifeTime = 900, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                AccelPerSec = 0,
                DesiredSpeed = 3000, // voxel phasing if you go above 5100
                MaxTrajectory = 2500f,
                DeaccelTime = 0, // 0 is disabled, a value causes the projectile to come to rest overtime, (Measured in game ticks, 60 = 1 second)
                GravityMultiplier = 0f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable.
                SpeedVariance = Random(start: -500, end: 500), // subtracts value from DesiredSpeed
                RangeVariance = Random(start: -550, end: 550), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                TotalAcceleration = 0, // 0 means no limit, something to do due with a thing called delta and something called v.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 0, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Inaccuracy = 0f, // 0 is perfect, hit accuracy will be a random num of meters between 0 and this value.
                    Aggressiveness = 0f, // controls how responsive tracking is.
                    MaxLateralThrust = 0f, // controls how sharp the trajectile may turn. Cap is 1, and this is % of your Accel.
                    NavAcceleration = 0, // helps influence how the projectile steers. 
                    TrackingDelay = 0, // Measured in Shape diameter units traveled.
                    AccelClearance = false, // Setting this to true will prevent smart acceleration until it is clear of the grid and tracking delay has been met (free fall).
                    CheckFutureIntersection = false, // Utilize obstacle avoidance for drones
                    MaxChaseTime = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    OverideTarget = false, // when set to true ammo picks its own target, does not use hardpoint's.
                    MaxTargets = 0, // Number of targets allowed before ending, 0 = unlimited
                    NoTargetExpire = false, // Expire without ever having a target at TargetLossTime
                    Roam = false, // Roam current area after target loss
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    OffsetRatio = 0f, // The ratio to offset the random direction (0 to 1) 
                    OffsetTime = 0, // how often to offset degree, measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..)
                    FocusOnly = false, // only target the constructs Ai's focus
                },
                Mines = new MinesDef
                {
                    DetectRadius = 0,
                    DeCloakRadius = 0,
                    FieldTime = 0,
                    Cloak = false,
                    Persist = false,
                },
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "",
                VisualProbability = 1f,
                ShieldHitDraw = false,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: 0f),
                        DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1f,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "BangShooty",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = true, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                    Eject = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = true, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0f, end: 0f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 0.5f,
                        Width = 0.2f,
                        Color = Color(red: 9, green: 2, blue: 2f, alpha: 0.1f),
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "WeaponLaser",
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                        Segmentation = new SegmentDef
                        {
                            Enable = false, // If true Tracer TextureMode is ignored
                            Textures = new[] {
                                "",
                            },
                            SegmentLength = 0f, // Uses the values below.
                            SegmentGap = 0f, // Uses Tracer textures and values
                            Speed = 1f, // meters per second
                            Color = Color(red: 30, green: 15, blue: 15, alpha: 1),
                            WidthMultiplier = 1f,
                            Reverse = false,
                            UseLineVariance = true,
                            WidthVariance = Random(start: 0f, end: 0f),
                            ColorVariance = Random(start: 0f, end: 0f)
                        }
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "ProjectileTrailLine",
                        },
                        TextureMode = Normal,
                        DecayTime = 2,
                        Color = Color(red: 3, green: 1, blue: 1f, alpha: 0.1f),
                        Back = false,
                        CustomWidth = 0.2f,
                        UseWidthVariance = false,
                        UseColorFade = true,
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0f,
                        MaxLength = 0,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "hwshiphit",
                ShotSound = "hwshipfire",
                ShieldHitSound = "hwshiphit",
                PlayerHitSound = "",
                VoxelHitSound = "hwshiphit",
                FloatingHitSound = "",
                HitPlayChance = 0.5f,
                HitPlayShield = true,
            },
            Ejection = new EjectionDef
            {
                Type = Particle, // Particle or Item (Inventory Component)
                Speed = 100f, // Speed inventory is ejected from in dummy direction
                SpawnChance = 0.5f, // chance of triggering effect (0 - 1)
                CompDef = new ComponentDef
                {
                    ItemName = "", //InventoryComponent name
                    ItemLifeTime = 0, // how long item should exist in world
                    Delay = 0, // delay in ticks after shot before ejected
                }
            }, // Don't edit below this line
        };

        private AmmoDef taiidan_interceptor_missile_attack => new AmmoDef
        {
            AmmoMagazine = "", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Taiidan Interceptor Missile Attack", // Name of ammo in terminal, should be different for each ammo type used by the same weapon.
            HybridRound = false, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 1f, // Direct damage; one steel plate is worth 100.
            Mass = 0f, // In kilograms; how much force the impact will apply to the target.
            Health = 1, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            BackKickForce = 0f, // Recoil. This is applied to the Parent Grid.
            DecayPerShot = 0f, // Damage to the firing weapon itself. 
                               //float.MaxValue will drop the weapon to the first build state and destroy all components used for construction
                               //If greater than cube integrity it will remove the cube upon firing, without causing deformation (makes it look like the whole "block" flew away)
            HardPointUsable = false, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            EnergyMagazineSize = 0, // For energy weapons, how many shots to fire before reloading.
            IgnoreWater = false, // Whether the projectile should be able to penetrate water when using WaterMod.
            IgnoreVoxels = false, // Whether the projectile should be able to penetrate voxels.
            Synchronize = false, // Be careful, do not use on high fire rate weapons.  Only works on drones and Smart projectiles.  Will only work on chained/staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
            HeatModifier = -1f, // Allows this ammo to modify the amount of heat the weapon produces per shot.
            NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
            Sync = new SynchronizeDef
            {
                Full = false, // Be careful, do not use on high fire rate weapons. Do not use with other sync options. Only works on drones and Smart projectiles.Will only work on chained / staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
                PointDefense = true, // Server will inform clients of what projectiles have died by PD defense and will trigger destruction.
                OnHitDeath = true, // Server will inform clients when projectiles die due to them hitting something and will trigger destruction.
            },
            Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
            {
                Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                Diameter = 3, // Diameter is minimum length of LineShape or minimum diameter of SphereShape.
            },
            ObjectsHit = new ObjectsHitDef
            {
                MaxObjectsHit = 0, // Limits the number of entities (grids, players, projectiles) the projectile can penetrate; 0 = unlimited.
                CountBlocks = false, // Counts individual blocks, not just entities hit.
            },
            Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
            {
                AmmoRound = "", // AmmoRound field of the ammo to spawn.
                Fragments = 180, // Number of projectiles to spawn.
                Degrees = 360, // Cone in which to randomize direction of spawned projectiles.
                Reverse = false, // Spawn projectiles backward instead of forward.
                DropVelocity = true, // fragments will not inherit velocity from parent.
                Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                IgnoreArming = true, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                AdvOffset = Vector(x: 0, y: 0, z: 0), // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below
                {
                    Enable = false, // Enables TimedSpawns mechanism
                    Interval = 1, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                    StartTime = 1, // Time delay to start spawning fragments, in ticks, of total projectile life
                    MaxSpawns = 1, // Max number of fragment children to spawn
                    Proximity = 15, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                    ParentDies = true, // Parent dies once after it spawns its last child.
                    PointAtTarget = false, // Start fragment direction pointing at Target
                    PointType = Direct, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                    DirectAimCone = 180f, //Aim cone used for Direct fire, in degrees
                    GroupSize = 0, // Number of spawns in each group
                    GroupDelay = 0, // Delay between each group.
                },
            },
            Pattern = new PatternDef
            {
                Patterns = new[] { // If enabled, set of multiple ammos to fire in order instead of the main ammo.
                    "",
                },
                Mode = Fragment, // Select when to activate this pattern, options: Never, Weapon, Fragment, Both 
                TriggerChance = 1f, // This is %
                Random = false, // This randomizes the number spawned at once, NOT the list order.
                RandomMin = 1,
                RandomMax = 1,
                SkipParent = false, // Skip the Ammo itself, in the list
                PatternSteps = 1, // Number of Ammos activated per round, will progress in order and loop. Ignored if Random = true.
            },
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 2001f, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                FallOff = new FallOffDef
                {
                    Distance = 0f, // Distance at which damage begins falling off.
                    MinMultipler = 1f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                },
                Grids = new GridSizeDef
                {
                    Large = 0.01f, // Multiplier for damage against large grids.
                    Small = 0.01f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = 1f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic,
                    AreaEffect = Kinetic, // Kinetic , Energy, are your Options.
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile.
                },
                Custom = new CustomScalesDef
                {
                    SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                    Types = new[] // List of blocks to apply custom damage multipliers to.
            {
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test1",
                            Modifier = -1f,
                        },
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test2",
                            Modifier = -1f,
                        },
                    },
                },
            },
            AreaOfDamage = new AreaOfDamageDef
            {
                ByBlockHit = new ByBlockHitDef
                {
                    Enable = false,
                    Radius = 30f,
                    Damage = 1000f,
                    Depth = 0f,
                    MaxAbsorb = 0f,
                    Falloff = NoFalloff, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the midAdle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                },
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 5f, // Meters
                    Damage = 1000f,
                    Depth = 0,
                    MaxAbsorb = 0f,
                    Falloff = Linear, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    //.Exponential drops off exponentially.  Does not scale to max radius
                    ArmOnlyOnHit = false, // Detonation only is available, After it hits something, when this is true. IE, if shot down, it won't explode.
                    MinArmingTime = 1, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    NoVisuals = false,
                    NoSound = false,
                    ParticleScale = 12,
                    CustomParticle = "GO AWAY KEEN", // Particle SubtypeID, from your Particle SBC
                    CustomSound = "", // SubtypeID from your Audio SBC, not a filename
                    Shape = Diamond, // Round or Diamond
                },
            },
            Ewar = new EwarDef
            {
                Enable = false,
                Type = Offense, //AntiSmart,JumpNull,EnergySink, Anchor,Emp,Offense,Nav,Dot,Push,Pull,Tractor,
                Mode = Effect,
                Strength = 1f,
                Radius = 1f,
                Duration = 120,
                StackDuration = false,
                Depletable = false,
                MaxStacks = 0,
                NoHitParticle = false,
                Force = new PushPullDef
                {
                    ForceFrom = ProjectileLastPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    ForceTo = HitPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    Position = TargetCenterOfMass, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    DisableRelativeMass = false,
                    TractorRange = 0,
                    ShooterFeelsForce = false,
                },
                Field = new FieldDef
                {
                    Interval = 0, // Time between each pulse, in game ticks (60 == 1 second).
                    PulseChance = 0, // Chance from 0 - 100 that an entity in the field will be hit by any given pulse.
                    GrowTime = 0, // How many ticks it should take the field to grow to full size.
                    HideModel = false, // Hide the projectile model if it has one.
                    ShowParticle = false, // Deprecated.
                    Particle = new ParticleDef // Particle effect to generate at the field's position.
                    {
                        Name = "", // SubtypeId of field particle effect.
                                   // Deprecated.
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 0), // Deprecated, set color in particle sbc.
                        Extras = new ParticleOptionDef
                        {
                            Loop = false, // Deprecated, set this in particle sbc.
                            Restart = false, // Not used.
                            MaxDistance = 5000, // Not used.
                            MaxDuration = 1, // Not used.
                            Scale = 1, // Scale of effect.
                        },
                    },
                },
            },
            Beams = new BeamDef
            {
                Enable = false, // Enable beam behaviour.
                VirtualBeams = false, // Only one damaging beam, but with the effectiveness of the visual beams combined (better performance).
                ConvergeBeams = false, // When using virtual beams, converge the visual beams to the location of the real beam.
                RotateRealBeam = false, // The real beam is rotated between all visual beams, instead of centered between them.
                OneParticle = false, // Only spawn one particle hit per beam weapon.
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 180,
                TargetLossTime = 1, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                MaxLifeTime = 1900, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                AccelPerSec = 1500,
                DesiredSpeed = 1800, // voxel phasing if you go above 5100
                MaxTrajectory = 5000f,
                DeaccelTime = 0, // 0 is disabled, a value causes the projectile to come to rest overtime, (Measured in game ticks, 60 = 1 second)
                GravityMultiplier = 0f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable.
                SpeedVariance = Random(start: 0, end: 0), // subtracts value from DesiredSpeed
                RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                TotalAcceleration = 4500, // 0 means no limit, something to do due with a thing called delta and something called v.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 0, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Inaccuracy = 0f, // 0 is perfect, hit accuracy will be a random num of meters between 0 and this value.
                    Aggressiveness = 7f, // controls how responsive tracking is.
                    MaxLateralThrust = 1f, // controls how sharp the trajectile may turn. Cap is 1, and this is % of your Accel.
                    NavAcceleration = 0, // helps influence how the projectile steers. 
                    TrackingDelay = 0, // Measured in Shape diameter units traveled.
                    AccelClearance = false, // Setting this to true will prevent smart acceleration until it is clear of the grid and tracking delay has been met (free fall).
                    CheckFutureIntersection = false, // Utilize obstacle avoidance for drones
                    MaxChaseTime = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    OverideTarget = false, // when set to true ammo picks its own target, does not use hardpoint's.
                    MaxTargets = 1, // Number of targets allowed before ending, 0 = unlimited
                    NoTargetExpire = false, // Expire without ever having a target at TargetLossTime
                    Roam = false, // Roam current area after target loss
                    KeepAliveAfterTargetLoss = false, // Whether to stop early death of projectile on target loss
                    OffsetMinRange = 15,
                    OffsetRatio = 0.46f, // The ratio to offset the random direction (0 to 1) 
                    OffsetTime = 36, // how often to offset degree, measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..)
                    FocusOnly = false, // only target the constructs Ai's focus
                    MinTurnSpeed = 10,
                    AltNavigation = true,
                },
                Mines = new MinesDef
                {
                    DetectRadius = 0,
                    DeCloakRadius = 0,
                    FieldTime = 0,
                    Cloak = false,
                    Persist = false,
                },
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "",
                VisualProbability = 1f,
                ShieldHitDraw = false,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "MRMTRAIL", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: 0f),
                        DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 0.25f,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "BangShooty",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = true, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 3,
                            HitPlayChance = 1f,
                        },
                    },
                    Eject = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = true, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0f, end: 0f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 0.75f,
                        Width = 0.5f,
                        Color = Color(red: 9, green: 2, blue: 12f, alpha: 0.5f),
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "WeaponLaser",
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                        Segmentation = new SegmentDef
                        {
                            Enable = false, // If true Tracer TextureMode is ignored
                            Textures = new[] {
                                "",
                            },
                            SegmentLength = 0f, // Uses the values below.
                            SegmentGap = 0f, // Uses Tracer textures and values
                            Speed = 1f, // meters per second
                            Color = Color(red: 30, green: 15, blue: 15, alpha: 1),
                            WidthMultiplier = 1f,
                            Reverse = false,
                            UseLineVariance = true,
                            WidthVariance = Random(start: 0f, end: 0f),
                            ColorVariance = Random(start: 0f, end: 0f)
                        }
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "ProjectileTrailLine",
                        },
                        TextureMode = Normal,
                        DecayTime = 2,
                        Color = Color(red: 3, green: 1, blue: 1f, alpha: 0.1f),
                        Back = false,
                        CustomWidth = 0.52f,
                        UseWidthVariance = false,
                        UseColorFade = true,
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0f,
                        MaxLength = 0,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "hwshiphit",
                ShotSound = "hwshipfire",
                ShieldHitSound = "hwshiphit",
                PlayerHitSound = "",
                VoxelHitSound = "hwshiphit",
                FloatingHitSound = "",
                HitPlayChance = 0.1f,
                HitPlayShield = true,
            },
            Ejection = new EjectionDef
            {
                Type = Particle, // Particle or Item (Inventory Component)
                Speed = 100f, // Speed inventory is ejected from in dummy direction
                SpawnChance = 0.5f, // chance of triggering effect (0 - 1)
                CompDef = new ComponentDef
                {
                    ItemName = "", //InventoryComponent name
                    ItemLifeTime = 0, // how long item should exist in world
                    Delay = 0, // delay in ticks after shot before ejected
                }
            }, // Don't edit below this line
        };

        private AmmoDef taiidan_bomber_Main => new AmmoDef // StarCore AMS I
        {
            AmmoMagazine = "TaiidanCraft", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Taiidani Bomber", // Name of ammo in terminal, should be different for each ammo type used by the same weapon.
            HybridRound = true, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0.001f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 50000f, // Direct damage; one steel plate is worth 100. 
            Mass = 5, // In kilograms; how much force the impact will apply to the target.
            Health = 4000, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            BackKickForce = 10, // Recoil.
            DecayPerShot = 0f, // Damage to the firing weapon itself.
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            EnergyMagazineSize = 5, // For energy weapons, how many shots to fire before reloading.
            IgnoreWater = false, // Whether the projectile should be able to penetrate water when using WaterMod.
            IgnoreVoxels = true, // Whether the projectile should be able to penetrate voxels.
            HeatModifier = 2f, // Allows this ammo to modify the amount of heat the weapon produces per shot.
            Sync = new SynchronizeDef
            {
                Full = false, // Be careful, do not use on high fire rate weapons. Do not use with other sync options. Only works on drones and Smart projectiles.Will only work on chained / staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
                PointDefense = true, // Server will inform clients of what projectiles have died by PD defense and will trigger destruction.
                OnHitDeath = true, // Server will inform clients when projectiles die due to them hitting something and will trigger destruction.
            },
            Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
            {
                Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                Diameter = 10, // Diameter is minimum length of LineShape or minimum diameter of SphereShape.
            },
            ObjectsHit = new ObjectsHitDef
            {
                MaxObjectsHit = 0, // Limits the number of entities (grids, players, projectiles) the projectile can penetrate; 0 = unlimited.
                CountBlocks = false, // Counts individual blocks, not just entities hit.
            },
            Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
            {
                AmmoRound = "Taiidan Bomber Attack", // AmmoRound field of the ammo to spawn.
                Fragments = 1, // Number of projectiles to spawn.
                Degrees = 0.2f, // Cone in which to randomize direction of spawned projectiles.
                Reverse = false, // Spawn projectiles backward instead of forward.
                DropVelocity = false, // fragments will not inherit velocity from parent.
                Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                IgnoreArming = false, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                ArmWhenHit = false, // Setting this to true will arm the projectile when its shot by other projectiles.
                AdvOffset = Vector(x: 0, y: 0, z: 0), // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below
                {
                    Enable = true, // Enables TimedSpawns mechanism
                    Interval = 20, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                    StartTime = 60, // Time delay to start spawning fragments, in ticks, of total projectile life
                    MaxSpawns = 20, // Max number of fragment children to spawn
                    Proximity = 3000, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                    ParentDies = false, // Parent dies once after it spawns its last child.
                    PointAtTarget = true, // Start fragment direction pointing at Target
                    PointType = Predict, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                    DirectAimCone = 10f, //Aim cone used for Direct fire, in degrees
                    GroupSize = 5, // Number of spawns in each group
                    GroupDelay = 300, // Delay between each group.
                },
            },
            Pattern = new PatternDef
            {
                Patterns = new[] { // If enabled, set of multiple ammos to fire in order instead of the main ammo.
                   "",
                },
                Mode = Fragment, // Select when to activate this pattern, options: Never, Weapon, Fragment, Both 
                TriggerChance = 1f, // This is %
                Random = false, // This randomizes the number spawned at once, NOT the list order.
                RandomMin = 1,
                RandomMax = 1,
                SkipParent = false, // Skip the Ammo itself, in the list
                PatternSteps = 1, // Number of Ammos activated per round, will progress in order and loop. Ignored if Random = true.
            },
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 0.5, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                FallOff = new FallOffDef
                {
                    Distance = 3000f, // Distance at which damage begins falling off.
                    MinMultipler = 1f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                },
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = 1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = 0.5f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = 6f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Custom = new CustomScalesDef
                {
                    SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                    Types = new[] // List of blocks to apply custom damage multipliers to.
                    {
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test1",
                            Modifier = -1f,
                        },
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test2",
                            Modifier = -1f,
                        },
                    },
                },
            },
            AreaOfDamage = new AreaOfDamageDef
            {
                ByBlockHit = new ByBlockHitDef
                {
                    Enable = false,
                    Radius = 4f, // Meters
                    Damage = 50f,
                    Depth = 1f, // Meters
                    MaxAbsorb = 0f,
                    Falloff = Linear, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    Shape = Diamond, // Round or Diamond
                },
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 6f, // Meters
                    Damage = 1000f,
                    Depth = 4f,
                    MaxAbsorb = 0f,
                    Falloff = Linear, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    ArmOnlyOnHit = false, // Detonation only is available, After it hits something, when this is true. IE, if shot down, it won't explode.
                    MinArmingTime = 0, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    NoVisuals = true,
                    NoSound = false,
                    ParticleScale = 8,
                    CustomParticle = "Grid_Destruction", // Particle SubtypeID, from your Particle SBC
                    CustomSound = "hwshipexplode", // SubtypeID from your Audio SBC, not a filename
                    Shape = Diamond, // Round or Diamond
                },
            },
            Ewar = new EwarDef
            {
                Enable = false, // Enables the EWAR , Electronic-Warfare System
                Type = Offense, // EnergySink, Emp, Offense, Nav, Dot, AntiSmart, JumpNull, Anchor, Tractor, Pull, Push, 
                Mode = Field, // Effect , Field
                Strength = 1000000000,
                Radius = 8f, // Meters
                Duration = 240, // In Ticks
                StackDuration = true, // Combined Durations
                Depletable = false,
                MaxStacks = 2, // Max Debuffs at once
                NoHitParticle = false,
                /*
                EnergySink : Targets & Shutdowns Power Supplies, such as Batteries & Reactor
                Emp : Targets & Shutdown any Block capable of being powered
                Offense : Targets & Shutdowns Weaponry
                Nav : Targets & Shutdown Gyros, Thrusters, or Locks them down
                Dot : Deals Damage to Blocks in radius
                AntiSmart : Effects & Scrambles the Targeting List of Affected Missiles
                JumpNull : Shutdown & Stops any Active Jumps, or JumpDrive Units in radius
                Tractor : Affects target with Physics
                Pull : Affects target with Physics
                Push : Affects target with Physics
                Anchor : Affects target with Physics
                
                */
                Force = new PushPullDef
                {
                    ForceFrom = ProjectileLastPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    ForceTo = HitPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    Position = TargetCenterOfMass, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    DisableRelativeMass = false,
                    TractorRange = 0,
                    ShooterFeelsForce = false,
                },
                Field = new FieldDef
                {
                    Interval = 0, // Time between each pulse, in game ticks (60 == 1 second).
                    PulseChance = 0, // Chance from 0 - 100 that an entity in the field will be hit by any given pulse.
                    GrowTime = 0, // How many ticks it should take the field to grow to full size.
                    HideModel = false, // Hide the projectile model if it has one.
                    ShowParticle = true, // Show Block damage effect.
                    TriggerRange = 250f, //range at which fields are triggered
                    Particle = new ParticleDef // Particle effect to generate at the field's position.
                    {
                        Name = "", // SubtypeId of field particle effect.
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1, // Scale of effect.
                        },
                    },
                },
            },
            Beams = new BeamDef
            {
                Enable = false, // Enable beam behaviour. Please have 3600 RPM, when this Setting is enabled. Please do not fire Beams into Voxels.
                VirtualBeams = false, // Only one damaging beam, but with the effectiveness of the visual beams combined (better performance).
                ConvergeBeams = false, // When using virtual beams, converge the visual beams to the location of the real beam.
                RotateRealBeam = false, // The real beam is rotated between all visual beams, instead of centered between them.
                OneParticle = false, // Only spawn one particle hit per beam weapon.
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 180f, // Degrees, Is pointed forward
                TargetLossTime = 0, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                MaxLifeTime = 36000, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 450f, // Acceleration in Meters Per Second. Projectile starts on tick 0 at its parents (weapon/other projectiles) travel velocity.
                DesiredSpeed = 500f, // voxel phasing if you go above 5100
                MaxTrajectory = 1000000f, // Max Distance the projectile or beam can Travel.
                DeaccelTime = 0, // 0 is disabled, a value causes the projectile to come to rest overtime, (Measured in game ticks, 60 = 1 second)
                GravityMultiplier = 0f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable. Natural Gravity Only.
                SpeedVariance = Random(start: -20, end: 20), // subtracts value from DesiredSpeed. Be warned, you can make your projectile go backwards.
                RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                TotalAcceleration = 0, // 0 means no limit, something to do due with a thing called delta and something called v.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 150, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Inaccuracy = 0f, // 0 is perfect, hit accuracy will be a random num of meters between 0 and this value.
                    Aggressiveness = 1f, // controls how responsive tracking is.
                    MaxLateralThrust = 1.5, // controls how sharp the projectile may turn, this is the cheaper but less realistic version of SteeringLimit, cost of 2 on a scale of 1-5, 0 being basic smart.
                    NavAcceleration = 0, // helps influence how the projectile steers. 
                    TrackingDelay = 0, // Measured in Shape diameter units traveled.
                    AccelClearance = false, // Setting this to true will prevent smart acceleration until it is clear of the grid and tracking delay has been met (free fall).
                    MaxChaseTime = 36000, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    OverideTarget = false, // when set to true ammo picks its own target, does not use hardpoint's.
                    CheckFutureIntersection = true, // Utilize obstacle avoidance for drones
                    FutureIntersectionRange = 500,
                    MaxTargets = 0, // Number of targets allowed before ending, 0 = unlimited
                    NoTargetExpire = false, // Expire without ever having a target at TargetLossTime
                    Roam = false, // Roam current area after target loss
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    OffsetRatio = 0f, // The ratio to offset the random direction (0 to 1) 
                    OffsetTime = 0, // how often to offset degree, measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..)
                    OffsetMinRange = 0, // The range from target at which offsets are no longer active
                    FocusOnly = false, // only target the constructs Ai's focus target
                    ScanRange = 4000,
                    MinTurnSpeed = 10, // set this to a reasonable value to avoid projectiles from spinning in place or being too aggressive turing at slow speeds 
                    NoTargetApproach = true, // If true approaches can begin prior to the projectile ever having had a target.
                    AltNavigation = false, // If true this will swap the default navigation algorithm from ProNav to ZeroEffort Miss.  Zero effort is more direct/precise but less cinematic 
                },
                Approaches = new[] // These approaches move forward and backward in order, once the end condition of the last one is reached it will revert to default behavior. Cost level of 4+, or 5+ if used with steering.
                {
                    new ApproachDef // Launch
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = Spawn, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = MinTravelRequired,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 0,
                        Start2Value = 0,
                        End1Value = 300,
                        End2Value = 0,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = StorePositionC,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardOriginDirection, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpOriginDirection, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = PositionA,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = StoredEndLocalPosition,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 1.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = false, // Orbit the Position
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = true, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = true, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // Travel to target
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 2001,
                        Start2Value = 0,
                        End1Value = 2000,
                        End2Value = 0,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardTargetDirection, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpTargetDirection, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = Shooter, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Target,
                        Elevation = MidPoint, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0.5, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(-0.4f, 0.4f), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 500, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 2.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = false, // Orbit the Position
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = false, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // Orbit
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartAnd_EndOr, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceFromPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = RelativeLifetime,
                        EndCondition2 = EnemyTargetLoss,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 2500,
                        Start2Value = 0,
                        End1Value = 450,
                        End2Value = 60,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardRelativeToBlock, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Target,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(-0.25f, 0.25f), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 1.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = true, // Orbit the Position
                        OrbitRadius = 2500, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = false, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // Turn to target
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceFromPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 5000,
                        Start2Value = 0,
                        End1Value = 2000,
                        End2Value = 0,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardRelativeToBlock, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Target,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 0.5, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = false, // Orbit the Position
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = false, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // Attack run
                    {
                        // Start/End behaviors 
                        RestartCondition = ForceRestart, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        RestartList = new[]
                        { // This list is used if RestartCondition is set to ForceRestart and trigger requirement was met. -1 to reset to BEFORE the for approach stage was activated.  First stage is 0, second is 1, etc...
                            new WeightedIdListDef
                            {// If all valid entries (below MaxRuns) role a 0 (i.e. weights are disabled), then the entry with the lowest current "Runs" will be selected, if two or more share lowest runs then the winner is decided by the order below.
                                ApproachId = 2,
                                MaxRuns = 10, // 0 means unlimited, defines how many times this entry can return true. 
                                Weight = Random(3, 10), // The approachId that rolls the highest number will be selected
                            },
                            new WeightedIdListDef
                            {
                                ApproachId = 5,
                                MaxRuns = 1,
                                Weight = Random(0, 2),
                            },
                        },
                        Operators = StartAnd_EndOr, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = true, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceFromPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = SinceTimedSpawn,
                        EndCondition2 = EnemyTargetLoss,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 750,
                        Start2Value = 0,
                        End1Value = 30,
                        End2Value = 60,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardRelativeToBlock, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Target,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 0.5, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = false, // Orbit the Position
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = false, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = false, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // RTB
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 1001,
                        Start2Value = 0,
                        End1Value = 1000,
                        End2Value = 0,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardRelativeToBlock, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Shooter,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 2.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = false, // Orbit the Position
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = false, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // Recover orbit
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceFromPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = RelativeLifetime,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 1000,
                        Start2Value = 0,
                        End1Value = 300,
                        End2Value = 0,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardRelativeToShooter, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToShooter, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Shooter,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 1.0, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = true, // Orbit the Position
                        OrbitRadius = 1000, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = false, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // Recover
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 2,
                        Start2Value = 0,
                        End1Value = 1,
                        End2Value = 0,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = DoNothing,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardRelativeToBlock, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToBlock, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = StoredEndLocalPosition,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 0.5, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = false, // Orbit the Position
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = false, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = true, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = false, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = false, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },

                    new ApproachDef // Dock
                    {
                        // Start/End behaviors 
                        RestartCondition = MoveToNext, // Wait*, MoveToPrevious, MoveToNext, ForceRestart -- A restart condition is when the end condition is reached without having met the start condition. 
                        Operators = StartEnd_And, // Controls how the start and end conditions are matched:  StartEnd_And*, StartEnd_Or, StartAnd_EndOr,StartOr_EndAnd,
                        CanExpireOnceStarted = false, // This stages values will continue to apply until the end conditions are met.
                        ForceRestart = false, // This forces the ReStartCondition when the end condition is met no matter if the start condition was met or not.  

                        // Start/End conditions
                        StartCondition1 = DistanceToPositionC, // Each condition type is either >= or <= the corresponding value defined below.
                                                    // Ignore(skip this condition)*, DistanceFromPositionC[<=], DistanceToPositionC[>=], DistanceFromPositionB[<=], DistanceToPositionB[>=]
                                                    // DistanceFromTarget[<=], DistanceToTarget[>=], DistanceFromEndTrajectory[<=], DistanceToEndTrajectory[>=], Lifetime[>=], DeadTime[<=],
                                                    // MinTravelRequired[>=], MaxTravelRequired[<=], Spawn(per stage), DesiredElevation(tolerance can be set with ElevationTolerance),
                                                    // NextTimedSpawn[<=], SinceTimedSpawn[>=], RelativeLifetime[>=], RelativeDeadTime[<=], RelativeSpawns[>=], EnemyTargetLoss[>=],
                                                    // RelativeHealthLost[>=], HealthRemaining[<=],
                                                    // *NOTE* DO NOT set start1 and start2 or end1 and end2 to same condition
                        StartCondition2 = Ignore,
                        EndCondition1 = DistanceFromPositionC,
                        EndCondition2 = Ignore,
                        EndCondition3 = Ignore,
                        // Start/End thresholds -- both conditions are evaluated before activation, use Ignore to skip
                        Start1Value = 2,
                        Start2Value = 0,
                        End1Value = 1,
                        End2Value = 0,
                        End3Value = 0, 
                        // Special triggers when the start/end conditions are met (DoNothing*, EndProjectile, EndProjectileOnRestart, StorePositionA, StorePositionB, StorePositionC, Refund)
                        StartEvent = DoNothing,
                        EndEvent = EndProjectile,  
                        
                        // Stored "Local" positions are always relative to the shooter and will remain true even if the shooter moves or rotates.

                        // Relative positions and directions (relative to projectile current position aka PositionA)
                        Forward = ForwardRelativeToShooter, // ForwardElevationDirection*, ForwardRelativeToBlock, ForwardRelativeToShooter, ForwardRelativeToGravity, ForwardTargetDirection, ForwardTargetVelocity, ForwardStoredStartPosition, ForwardStoredEndPosition, ForwardStoredStartLocalPosition, ForwardStoredEndLocalPosition, ForwardOriginDirection    
                        Up = UpRelativeToShooter, // UpRelativeToBlock*, UpRelativeToShooter, UpRelativeToGravity, UpTargetDirection, UpTargetVelocity, UpStoredStartPosition, UpStoredEndPosition, UpStoredStartLocalPosition, UpStoredEndLocalPosition, UpOriginDirection, UpElevationDirection
                        PositionB = PositionA, // Origin*, Shooter, Target, Surface, MidPoint, PositionA, Nothing, StoredStartPosition, StoredEndPosition, StoredStartLocalPosition, StoredEndLocalPosition
                        PositionC = Shooter,
                        Elevation = Nothing, 
                        
                        //
                        // Control if the vantagepoints update every frame or only at start.
                        //
                        AdjustForward = true, // adjust forwardDir overtime.
                        AdjustUp = true, // adjust upDir overtime
                        AdjustPositionB = true, // Updated the position overtime.
                        AdjustPositionC = true, // Update the position overtime.
                        LeadRotateElevatePositionB = false, // Add Lead, Rotation and DesiredElevation to PositionB
                        LeadRotateElevatePositionC = false, // Add Lead, Rotation and DesiredElevation to PositionC
                        TrajectoryRelativeToB = false, // If true the projectiles immediate trajectory will be relative to PositionB instead of PositionC (e.g. quick response to elevation changes relative to PositionB position assuming that position is closer to PositionA)
                        ElevationRelativeToC = false, // If true the projectiles desired elevation will be relative to PositionC instead of PositionB (e.g. quick response to elevation changes relative to PositionC position assuming that position is closer to PositionA)
                        // Tweaks to vantagepoint behavior
                        AngleOffset = 0, // value 0 - 1, rotates the Updir and ForwardDir
                        AngleVariance = Random(0, 0), // added to AngleOffset above, values of 0,0 disables feature
                        ElevationTolerance = 0, // adds additional tolerance (in meters) to meet the Elevation condition requirement.  *note* collision size is also added to the tolerance
                        TrackingDistance = 0, // Minimum travel distance before projectile begins racing to heading
                        DesiredElevation = 0, // The desired elevation relative to reference position 
                        // Storage Values
                        StoredStartId = 0, // Which approach id the the start storage was saved in, if any.
                        StoredEndId = 0, // Which approach id the the end storage was saved in, if any.
                        StoredStartType = PositionA, // Uses same values as PositionB/PositionC/Elevation
                        StoredEndType = Target,
                        // Controls the leading behavior
                        LeadDistance = 0, // Add additional "lead" in meters to the trajectory (project in the future), this will be applied even before TrackingDistance is met. 
                        PushLeadByTravelDistance = false, // the follow lead position will move in its point direction by an amount equal to the projectiles travel distance.

                        // Modify speed and acceleration ratios while this approach is active
                        AccelMulti = 1.0, // Modify default acceleration by this factor
                        DeAccelMulti = 0, // Modifies your default deacceleration by this factor
                        TotalAccelMulti = 0, // Modifies your default totalacceleration by this factor
                        SpeedCapMulti = 0.2, // Limit max speed to this factor, must keep this value BELOW default maxspeed (1).

                        // navigation behavior 
                        Orbit = false, // Orbit the Position
                        OrbitRadius = 0, // The orbit radius to extend between the projectile and the Position (target volume + this value)
                        OffsetMinRadius = 0, // Min Radius to offset from Position.  
                        OffsetMaxRadius = 0, // Max Radius to offset from Position.  
                        OffsetTime = 0, // How often to change the offset radius.
                        
                        // Other
                        NoTimedSpawns = true, // When true timedSpawns will not be triggered while this approach is active.
                        DisableAvoidance = true, // Disable futureIntersect.
                        IgnoreAntiSmart = true, // If set to true, antismart cannot change this approaches target.
                        HeatRefund = 0, // how much heat to refund when related EndEvent/StartEvent is met.
                        ReloadRefund = false, // Refund a reload (for max reload).
                        ToggleIngoreVoxels = false, // Toggles whatever the default IgnoreVoxel value to its opposite. 
                        SelfAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the parent grids.
                        TargetAvoidance = false, // If this and FutureIntersect is enabled then projectiles will actively avoid the target.
                        SelfPhasing = true, // If enabled the projectiles can phase through the parent grids without doing damage or dying.
                        SwapNavigationType = true, // This will swap to other navigation  (i.e. the alternate of what is set in smart, ProNav vs ZeroEffort) 
                        // Audio/Visual Section
                        AlternateParticle = new ParticleDef // if blank it will use default, must be a default version for this to be useable. 
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        StartParticle = new ParticleDef // Optional particle to play when this stage begins
                        {
                            Name = "",
                            Offset = Vector(x: 0, y: 0, z: 0),
                            DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1,
                            },
                        },
                        AlternateModel = "", // Define only if you want to switch to an alternate model in this phase
                        AlternateSound = "", // if blank it will use default, must be a default version for this to be useable. 
                        ModelRotateTime = 0, // If this value is greater than 0 then the projectile model will rotate to face the target, a value of 1 is instant (in ticks).
                    },
                },
                Mines = new MinesDef  // Note: This is being investigated. Please report to Github, any issues.
                {
                    DetectRadius = 0,
                    DeCloakRadius = 0,
                    FieldTime = 0,
                    Cloak = false,
                    Persist = false,
                },
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "\\Models\\Taiidan_Attack_Bomber", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "CraftExplodeDebris",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                    Eject = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0f, end: 0f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                    DropParentVelocity = true, // If set to true the tracer will not take on the parents (grid/player) initial velocity when rendering.
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 5f, //
                        Width = 1f, //
                        Color = Color(red: 32, green: 8, blue: 9f, alpha: 1), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                        Segmentation = new SegmentDef
                        {
                            Enable = false, // If true Tracer TextureMode is ignored
                            Textures = new[] {
                                "", // Please always have this Line set, if this Section is enabled.
                            },
                            SegmentLength = 0f, // Uses the values below.
                            SegmentGap = 0f, // Uses Tracer textures and values
                            Speed = 1f, // meters per second
                            Color = Color(red: 1, green: 2, blue: 2.5f, alpha: 1),
                            WidthMultiplier = 1f,
                            Reverse = false,
                            UseLineVariance = true,
                            WidthVariance = Random(start: 0f, end: 0f),
                            ColorVariance = Random(start: 0f, end: 0f)
                        }
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Normal,
                        DecayTime = 60, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                        Color = Color(red: 15f, green: 8f, blue: 0f, alpha: 0.10f),
                        FactionColor = Background, // Tracer uses faction color if it exists.
                        Back = true,
                        CustomWidth = 2f,
                        UseWidthVariance = true,
                        UseColorFade = true,
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0.2f,
                        MaxLength = 3,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "hwshiploop", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "",
                ShotSound = "hwshiplaunchtalk",
                ShieldHitSound = "",
                PlayerHitSound = "",
                VoxelHitSound = "",
                FloatingHitSound = "",
                HitPlayChance = 1f,
                HitPlayShield = true,
            },
            Ejection = new EjectionDef // Optional Component, allows generation of Particle or Item (Typically magazine), on firing, to simulate Tank shell ejection
            {
                Type = Particle, // Particle or Item (Inventory Component)
                Speed = 100f, // Speed inventory is ejected from in dummy direction
                SpawnChance = 0.5f, // chance of triggering effect (0 - 1)
                CompDef = new ComponentDef
                {
                    ItemName = "", //InventoryComponent name
                    ItemLifeTime = 0, // how long item should exist in world
                    Delay = 0, // delay in ticks after shot before ejected
                }
            }, // Don't edit below this line
        };

        private AmmoDef taiidan_bomber_attack => new AmmoDef
        {
            AmmoMagazine = "Energy", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Taiidan Bomber Attack", // Name of ammo in terminal, should be different for each ammo type used by the same weapon.
            HybridRound = false, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0.001f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 10000f, // Direct damage; one steel plate is worth 100.
            Mass = 0f, // In kilograms; how much force the impact will apply to the target.
            Health = 0, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            BackKickForce = 0f, // Recoil. This is applied to the Parent Grid.
            DecayPerShot = 0f, // Damage to the firing weapon itself. 
                               //float.MaxValue will drop the weapon to the first build state and destroy all components used for construction
                               //If greater than cube integrity it will remove the cube upon firing, without causing deformation (makes it look like the whole "block" flew away)
            HardPointUsable = false, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            EnergyMagazineSize = 1, // For energy weapons, how many shots to fire before reloading.
            IgnoreWater = false, // Whether the projectile should be able to penetrate water when using WaterMod.
            IgnoreVoxels = false, // Whether the projectile should be able to penetrate voxels.
            Synchronize = false, // Be careful, do not use on high fire rate weapons.  Only works on drones and Smart projectiles.  Will only work on chained/staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
            HeatModifier = -1f, // Allows this ammo to modify the amount of heat the weapon produces per shot.
            NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
            Sync = new SynchronizeDef
            {
                Full = false, // Be careful, do not use on high fire rate weapons. Do not use with other sync options. Only works on drones and Smart projectiles.Will only work on chained / staged fragments with a frag count of 1, will no longer sync once frag chain > 1.
                PointDefense = false, // Server will inform clients of what projectiles have died by PD defense and will trigger destruction.
                OnHitDeath = false, // Server will inform clients when projectiles die due to them hitting something and will trigger destruction.
            },
            Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
            {
                Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                Diameter = 1, // Diameter is minimum length of LineShape or minimum diameter of SphereShape.
            },
            ObjectsHit = new ObjectsHitDef
            {
                MaxObjectsHit = 0, // Limits the number of entities (grids, players, projectiles) the projectile can penetrate; 0 = unlimited.
                CountBlocks = false, // Counts individual blocks, not just entities hit.
            },
            Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
            {
                AmmoRound = "", // AmmoRound field of the ammo to spawn.
                Fragments = 5, // Number of projectiles to spawn.
                Degrees = 0, // Cone in which to randomize direction of spawned projectiles.
                Reverse = false, // Spawn projectiles backward instead of forward.
                DropVelocity = false, // fragments will not inherit velocity from parent.
                Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                IgnoreArming = false, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                AdvOffset = Vector(x: 0, y: 0, z: 0), // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below
                {
                    Enable = false, // Enables TimedSpawns mechanism
                    Interval = 15, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                    StartTime = 30, // Time delay to start spawning fragments, in ticks, of total projectile life
                    MaxSpawns = 8, // Max number of fragment children to spawn
                    Proximity = 4000, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                    ParentDies = true, // Parent dies once after it spawns its last child.
                    PointAtTarget = true, // Start fragment direction pointing at Target
                    PointType = Lead, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                    DirectAimCone = 10f, //Aim cone used for Direct fire, in degrees
                    GroupSize = 0, // Number of spawns in each group
                    GroupDelay = 0, // Delay between each group.
                },
            },
            Pattern = new PatternDef
            {
                Patterns = new[] { // If enabled, set of multiple ammos to fire in order instead of the main ammo.
                    "",
                },
                Mode = Fragment, // Select when to activate this pattern, options: Never, Weapon, Fragment, Both 
                TriggerChance = 1f, // This is %
                Random = false, // This randomizes the number spawned at once, NOT the list order.
                RandomMin = 1,
                RandomMax = 1,
                SkipParent = false, // Skip the Ammo itself, in the list
                PatternSteps = 1, // Number of Ammos activated per round, will progress in order and loop. Ignored if Random = true.
            },
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                DamageVoxels = false, // Whether to damage voxels.
                SelfDamage = false, // Whether to damage the weapon's own grid.
                HealthHitModifier = 5f, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                FallOff = new FallOffDef
                {
                    Distance = 0f, // Distance at which damage begins falling off.
                    MinMultipler = -1f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                },
                Grids = new GridSizeDef
                {
                    Large = -1f, // Multiplier for damage against large grids.
                    Small = -1f, // Multiplier for damage against small grids.
                },
                Armor = new ArmorDef
                {
                    Armor = -1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                    Light = -1f, // Multiplier for damage against light armor.
                    Heavy = -1f, // Multiplier for damage against heavy armor.
                    NonArmor = -1f, // Multiplier for damage against every else.
                },
                Shields = new ShieldDef
                {
                    Modifier = 5f, // Multiplier for damage against shields.
                    Type = Default, // Damage vs healing against shields; Default, Heal
                    BypassModifier = -1f, // If greater than zero, the percentage of damage that will penetrate the shield.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Energy,
                    AreaEffect = Energy, // Kinetic , Energy, are your Options.
                    Detonation = Energy,
                    Shield = Energy, // Damage against shields is currently all of one type per projectile.
                },
                Custom = new CustomScalesDef
                {
                    SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                    Types = new[] // List of blocks to apply custom damage multipliers to.
                    {
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test1",
                            Modifier = -1f,
                        },
                        new CustomBlocksDef
                        {
                            SubTypeId = "Test2",
                            Modifier = -1f,
                        },
                    },
                },
            },
            AreaOfDamage = new AreaOfDamageDef
            {
                ByBlockHit = new ByBlockHitDef
                {
                    Enable = false,
                    Radius = 0f,
                    Damage = 0f,
                    Depth = 0f,
                    MaxAbsorb = 0f,
                    Falloff = Pooled, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the midAdle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                },
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 1.5f, // Meters
                    Damage = 10000f,
                    Depth = 1.5f,
                    MaxAbsorb = 30000f,
                    Falloff = Pooled, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    //.Exponential drops off exponentially.  Does not scale to max radius
                    ArmOnlyOnHit = false, // Detonation only is available, After it hits something, when this is true. IE, if shot down, it won't explode.
                    MinArmingTime = 0, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                    NoVisuals = false,
                    NoSound = false,
                    ParticleScale = 1,
                    CustomParticle = "HWBlueExplosion", // Particle SubtypeID, from your Particle SBC
                    CustomSound = "hwshipexplode", // SubtypeID from your Audio SBC, not a filename
                    Shape = Diamond, // Round or Diamond
                },
            },
            Ewar = new EwarDef
            {
                Enable = false,
                Type = Offense, //AntiSmart,JumpNull,EnergySink, Anchor,Emp,Offense,Nav,Dot,Push,Pull,Tractor,
                Mode = Effect,
                Strength = 1f,
                Radius = 1f,
                Duration = 120,
                StackDuration = false,
                Depletable = false,
                MaxStacks = 0,
                NoHitParticle = false,
                Force = new PushPullDef
                {
                    ForceFrom = ProjectileLastPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    ForceTo = HitPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    Position = TargetCenterOfMass, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    DisableRelativeMass = false,
                    TractorRange = 0,
                    ShooterFeelsForce = false,
                },
                Field = new FieldDef
                {
                    Interval = 0, // Time between each pulse, in game ticks (60 == 1 second).
                    PulseChance = 0, // Chance from 0 - 100 that an entity in the field will be hit by any given pulse.
                    GrowTime = 0, // How many ticks it should take the field to grow to full size.
                    HideModel = false, // Hide the projectile model if it has one.
                    ShowParticle = false, // Deprecated.
                    Particle = new ParticleDef // Particle effect to generate at the field's position.
                    {
                        Name = "", // SubtypeId of field particle effect.
                                   // Deprecated.
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 0), // Deprecated, set color in particle sbc.
                        Extras = new ParticleOptionDef
                        {
                            Loop = false, // Deprecated, set this in particle sbc.
                            Restart = false, // Not used.
                            MaxDistance = 5000, // Not used.
                            MaxDuration = 1, // Not used.
                            Scale = 1, // Scale of effect.
                        },
                    },
                },
            },
            Beams = new BeamDef
            {
                Enable = false, // Enable beam behaviour.
                VirtualBeams = false, // Only one damaging beam, but with the effectiveness of the visual beams combined (better performance).
                ConvergeBeams = false, // When using virtual beams, converge the visual beams to the location of the real beam.
                RotateRealBeam = false, // The real beam is rotated between all visual beams, instead of centered between them.
                OneParticle = false, // Only spawn one particle hit per beam weapon.
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = Smart, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 180,
                TargetLossTime = 1, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                MaxLifeTime = 1900, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                AccelPerSec = 1500,
                DesiredSpeed = 1000, // voxel phasing if you go above 5100
                MaxTrajectory = 5000f,
                DeaccelTime = 0, // 0 is disabled, a value causes the projectile to come to rest overtime, (Measured in game ticks, 60 = 1 second)
                GravityMultiplier = 0f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable.
                SpeedVariance = Random(start: 0, end: 0), // subtracts value from DesiredSpeed
                RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                TotalAcceleration = 4500, // 0 means no limit, something to do due with a thing called delta and something called v.
                Smarts = new SmartsDef
                {
                    SteeringLimit = 0, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                    Inaccuracy = 20f, // 0 is perfect, hit accuracy will be a random num of meters between 0 and this value.
                    Aggressiveness = 7f, // controls how responsive tracking is.
                    MaxLateralThrust = 1f, // controls how sharp the trajectile may turn. Cap is 1, and this is % of your Accel.
                    NavAcceleration = 0, // helps influence how the projectile steers. 
                    TrackingDelay = 0, // Measured in Shape diameter units traveled.
                    AccelClearance = false, // Setting this to true will prevent smart acceleration until it is clear of the grid and tracking delay has been met (free fall).
                    CheckFutureIntersection = false, // Utilize obstacle avoidance for drones
                    MaxChaseTime = 30, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    OverideTarget = false, // when set to true ammo picks its own target, does not use hardpoint's.
                    MaxTargets = 1, // Number of targets allowed before ending, 0 = unlimited
                    NoTargetExpire = false, // Expire without ever having a target at TargetLossTime
                    Roam = false, // Roam current area after target loss
                    KeepAliveAfterTargetLoss = true, // Whether to stop early death of projectile on target loss
                    OffsetMinRange = 0,
                    OffsetRatio = 0f, // The ratio to offset the random direction (0 to 1) 
                    OffsetTime = 0, // how often to offset degree, measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..)
                    FocusOnly = false, // only target the constructs Ai's focus
                    MinTurnSpeed = 10,
                    AltNavigation = true,
                },
                Mines = new MinesDef
                {
                    DetectRadius = 0,
                    DeCloakRadius = 0,
                    FieldTime = 0,
                    Cloak = false,
                    Persist = false,
                },
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "",
                VisualProbability = 1f,
                ShieldHitDraw = false,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: -0.21f),
                        DisableCameraCulling = true,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1f,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = true, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                    Eject = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = true, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0f, end: 0f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 2f,
                        Width = 0.5f,
                        Color = Color(red: 30, green: 30, blue: 60f, alpha: 4),
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "WeaponLaser",
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                        Segmentation = new SegmentDef
                        {
                            Enable = false, // If true Tracer TextureMode is ignored
                            Textures = new[] {
                                "",
                            },
                            SegmentLength = 0f, // Uses the values below.
                            SegmentGap = 0f, // Uses Tracer textures and values
                            Speed = 1f, // meters per second
                            Color = Color(red: 30, green: 15, blue: 15, alpha: 1),
                            WidthMultiplier = 1f,
                            Reverse = false,
                            UseLineVariance = true,
                            WidthVariance = Random(start: 0f, end: 0f),
                            ColorVariance = Random(start: 0f, end: 0f)
                        }
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "ProjectileTrailLine",
                        },
                        TextureMode = Normal,
                        DecayTime = 2,
                        Color = Color(red: 30, green: 30, blue: 60f, alpha: 4),
                        Back = false,
                        CustomWidth = 1f,
                        UseWidthVariance = false,
                        UseColorFade = true,
                    },
                    OffsetEffect = new OffsetEffectDef
                    {
                        MaxOffset = 0,// 0 offset value disables this effect
                        MinLength = 0f,
                        MaxLength = 0,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                TravelSound = "", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "",
                ShotSound = "hwbomberfire",
                ShieldHitSound = "hwshiphit",
                PlayerHitSound = "",
                VoxelHitSound = "hwshiphit",
                FloatingHitSound = "",
                HitPlayChance = 0.1f,
                HitPlayShield = true,
            },
            Ejection = new EjectionDef
            {
                Type = Particle, // Particle or Item (Inventory Component)
                Speed = 100f, // Speed inventory is ejected from in dummy direction
                SpawnChance = 0.5f, // chance of triggering effect (0 - 1)
                CompDef = new ComponentDef
                {
                    ItemName = "", //InventoryComponent name
                    ItemLifeTime = 0, // how long item should exist in world
                    Delay = 0, // delay in ticks after shot before ejected
                }
            }, // Don't edit below this line
        };

    
        
        }
}

