using static Scripts.Structure.WeaponDefinition;
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
        private AmmoDef AmmoType1 => new AmmoDef // Your ID, for slotting into the Weapon CS
        {
            AmmoMagazine = "LBX5_Slug", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
            AmmoRound = "Ammo 1", // Unique name used in server overrides and in the terminal (default).  Should be different for each ammoDef used by the same weapon.  Referred to for Shrapnel.
            TerminalName = "", // Optional terminal name for this ammo type, used when picking ammo/cycling consumables.  Safe to have duplicates across different ammo defs.
            HybridRound = false, // Use both a physical ammo magazine and energy per shot.
            EnergyCost = 0.1f, // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR.
            BaseDamage = 111f, // Direct damage; one steel plate is worth 100.
            Mass = 0f, // In kilograms; how much force the impact will apply to the target.
            Health = 0, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
            BackKickForce = 0f, // Recoil. This is applied to the Parent Grid.
            DecayPerShot = 0f, // Damage to the firing weapon itself. 
			       //float.MaxValue will drop the weapon to the first build state and destroy all components used for construction
			       //If greater than cube integrity it will remove the cube upon firing, without causing deformation (makes it look like the whole "block" flew away)
            HardPointUsable = true, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            EnergyMagazineSize = 1, // For energy weapons, how many shots to fire before reloading.
            IgnoreWater = false, // Whether the projectile should be able to penetrate water when using WaterMod.
            IgnoreVoxels = false, // Whether the projectile should be able to penetrate voxels.
            HeatModifier = -1f, // Allows this ammo to modify the amount of heat the weapon produces per shot.
            NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
            NoGridOrArmorScaling = true, // If you enable this you can remove the damagescale section entirely.
            Sync = new SynchronizeDef
            {
                Full = false, // Do not use - still in progress
                PointDefense = false, // Server will inform clients of what projectiles have died by PD defense and will trigger destruction.
                OnHitDeath = false, // Server will inform clients when projectiles die due to them hitting something and will trigger destruction.
            },
            Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
            {
                Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                Diameter = 1, // For SphereShape this is diameter.
                              // For LineShape it is total length (double this value when setting up MaximumDiameter for weapon targeting).
                              // Defaults to 1 if left zero or deleted.
            },
            ObjectsHit = new ObjectsHitDef
            {
                MaxObjectsHit = 0, // Limits the number of grids or projectiles that damage can be applied to, useful to limit overpenetration; 0 = unlimited.
                CountBlocks = false, // Counts individual blocks, not just entities hit.  Note that every block touched by primary damage hits will count toward MaxObjectsHit
                SkipBlocksForAOE = false, //If CountBlocks = true this will determine if AOE hits are counted against MaxObjectsHit.  Set true to skip counting for AOE
            },
            Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
            {
                AmmoRound = "", // AmmoRound field of the ammo to spawn.
                Fragments = 0, // Number of projectiles to spawn.
                Degrees = 15, // Cone in which to randomize direction of spawned projectiles.
                Reverse = false, // Spawn projectiles backward instead of forward.
                DropVelocity = false, // fragments will not inherit velocity from parent.
                Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                IgnoreArming = true, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                ArmWhenHit = false, // Setting this to true will arm the projectile when its shot by other projectiles.
                AdvOffset = Vector(x: 0, y: 0, z: 0), // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below, unless ArmWhenHit or Eol ArmOnlyOnHit is set to true then both kinds of frags are active
                {
                    Enable = true, // Enables TimedSpawns mechanism
                    Interval = 0, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                    StartTime = 0, // Time delay to start spawning fragments, in ticks, of total projectile life
                    MaxSpawns = 1, // Max number of fragment children to spawn
                    Proximity = 1000, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                    ParentDies = true, // Parent dies once after it spawns its last child.
                    PointAtTarget = true, // Start fragment direction pointing at Target
                    PointType = Predict, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                    DirectAimCone = 0f, //Aim cone used for Direct fire, in degrees
                    GroupSize = 5, // Number of spawns in each group
                    GroupDelay = 120, // Delay between each group.
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
                HealthHitModifier = 20, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                FallOff = new FallOffDef
                {
                    Distance = 4500f, // Distance at which damage begins falling off.
                    MinMultipler = 0.66f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                },
                Grids = new GridSizeDef //If both of these values are -1, a 4x buff to SG weapons firing at LG and 0.25x debuff to LG weapons firing at SG will apply
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
                    BypassModifier = -1.4f, // 0-1 will bypass shields and apply that damage amount as a scaled %.  -1 is disabled.  -2 to -1 will alter the chance of penning a damaged shield, with -2 being a 100% reduction
                    HeatModifier = 1, // scales how much of the damage is converted to heat, negative values subtract heat.
                },
                DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                {
                    Base = Kinetic, // Base Damage uses this
                    AreaEffect = Kinetic,
                    Detonation = Kinetic,
                    Shield = Kinetic, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                },
                Deform = new DeformDef
                {
                    DeformType = HitBlock, // HitBlock- applies deformation to the block that was hit
					   // AllDamagedBlocks- applies deformation to all blocks damaged (for AOE)
					   // NoDeform- applies no deformation
                    DeformDelay = 60, // Time in ticks to wait before applying another deformation event (prevents excess calls for deformation every tick or from multiple sources)
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
            AreaOfDamage = new AreaOfDamageDef // Note AOE is only applied to the Player/Grid it hit (and nearby projectiles) not nearby grids/players.
            {
                ByBlockHit = new ByBlockHitDef
                {
                    Enable = false,
                    Radius = 0f, // Meters
                    Damage = 0f,
                    Depth = 0f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                    MaxAbsorb = 0f, // Soft cutoff for damage (total, against shields or grids), except for pooled falloff.  If pooled falloff, limits max damage per block.
                    Falloff = Pooled, //.NoFalloff applies the same damage to all blocks in radius
                    //.Linear drops evenly by distance from center out to max radius
                    //.Curve drops off damage sharply as it approaches the max radius
                    //.InvCurve drops off sharply from the middle and tapers to max radius
                    //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                    //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                    //.Exponential drops off exponentially.  Does not scale to max radius
                    Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                },
                EndOfLife = new EndOfLifeDef
                {
                    Enable = false,
                    Radius = 0f, // Radius of AOE effect, in meters.
                    Damage = 0f,
                    Depth = 1f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                    MaxAbsorb = 60f, // Soft cutoff for damage (total, against shields or grids), except for pooled falloff.  If pooled falloff, limits max damage per block.
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
                    CustomParticle = "particleName", // Particle SubtypeID, from your Particle SBC
					             // If you need to set a custom offset, specify it in the "Hit" particle
                    CustomSound = "soundName", // SubtypeID from your Audio SBC, not a filename
                    Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                }, 
            },
            Beams = new BeamDef
            {
                Enable = false, // Enable beam behaviour. Please have 3600 RPM, when this Setting is enabled. Please do not fire Beams into Voxels.
                VirtualBeams = false, // Only one damaging beam, but with the effectiveness of the visual beams combined (better performance).
                ConvergeBeams = false, // When using virtual beams, converge the visual beams to the location of the real beam.
                RotateRealBeam = false, // The real beam is rotated between all visual beams, instead of centered between them.
                OneParticle = false, // Only spawn one particle hit per beam weapon.
                FakeVoxelHitTicks = 0, // If this beam hits/misses a voxel it assumes it will continue to do so for this many ticks at the same hit length and not extend further within this window.  This can save up to n times worth of cpu.
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                TargetLossDegree = 80f, // Degrees, Is pointed forward
                TargetLossTime = 0, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                MaxLifeTime = 300, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                AccelPerSec = 0f, // Acceleration in Meters Per Second. Projectile starts on tick 0 at its parents (weapon/other projectiles) travel velocity.
                DesiredSpeed = 3000f, // voxel phasing if you go above 5100
                MaxTrajectory = 6000f, // Max Distance the projectile or beam can Travel.
                DeaccelTime = 0, // EWAR & Mines only- time to spend slowing down to stop at end of trajectory.  0 is instant stop
                GravityMultiplier = 0f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable. Natural Gravity Only.
                SpeedVariance = Random(start: 0, end: 50), // subtracts value from DesiredSpeed. Be warned, you can make your projectile go backwards.
                RangeVariance = Random(start: 0, end: 50), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                TotalAcceleration = 0, // 0 means no limit, something to do due with a thing called delta and something called v.
                DragPerSecond = 0f, // Amount of drag (m/s) deducted from the projectile's speed, multiplied by age.  Will not go below zero/negative.  Note that turrets will not be able to reliably account for this with non-smart ammo.
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                VisualProbability = 1f, // %
                ShieldHitDraw = false,
                Decals = new DecalDef
                {
                    MaxAge = 600,
                    Map = new[]
                    {
                        new TextureMapDef
                        {
                            HitMaterial = "Metal",
                            DecalMaterial = "GunBullet",
                        },
                        new TextureMapDef
                        {
                            HitMaterial = "Glass",
                            DecalMaterial = "GunBullet",
                        },
                    },
                },
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "", //ShipWelderArc
                        Offset = Vector(x: 0, y: 0, z: 0),
                        DisableCameraCulling = false,// If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        Offset = Vector(x: 0, y: 0, z: 0), // Note you can alter the directionality by passing different options:
			    				   // Vector(double.MinValue, double.MinValue, double.MinValue), will align the "Up" direction of the particle opposite gravity.  Note this is computationally expensive and should not be used with rapid fire weapons
			    				   // Vector(double.MaxValue, double.MaxValue, double.MaxValue), will align the "Forward" direction of the particle opposite the trajectory it was going when it hit
                        DisableCameraCulling = false, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
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
                        DisableCameraCulling = false, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                    WeaponEffect1Override = new ParticleDef //Optional ammo-level override for Effect1 used when a weapon fires.  Delete this section or leave name blank to disable
                    {
                        Name = "", // SubtypeId of muzzle particle effect.
                        Offset = Vector(x: 0, y: 0, z: 0), // Offsets the effect from the muzzle empty.
                        DisableCameraCulling = false, // If not true will not cull when not in view of camera, be careful with this and only use if you know you need it
                        Extras = new ParticleOptionDef
                        {
                            Loop = false, // Set this to the same as in the particle sbc!
                            Restart = false, // Whether to end a looping effect instantly when firing stops.
                            MaxDistance = 800,
                            MaxDuration = 0,
                            Scale = 1f, // Scale of effect.
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0.75f, end: 2f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: -0.25f), // adds random value to default width (negatives shrinks width)
                    DropParentVelocity = false, // If set to true will not take on the parents (grid/player) initial velocity when rendering.

                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 120f, //
                        Width = 1f, //
                        Color = Color(red: 8f, green: 7.8f, blue: 11f, alpha: 1), // RBG 255 is Neon Glowing, 100 is Quite Bright.
                        FactionColor = DontUse, // DontUse, Foreground, Background.
                        VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                        AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "ProjectileTrailLine", // Please always have this Line set, if this Section is enabled.
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
                            FactionColor = DontUse, // DontUse, Foreground, Background.
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
                        AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                        Textures = new[] {
                            "ProjectileTrailLine", "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                        },
                        TextureMode = Cycle,
                        DecayTime = 30, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                        Color = Color(red: 2.585f, green: 2.21f, blue: 2.562f, alpha: 0.4f),
                        FactionColor = DontUse, // DontUse, Foreground, Background.
                        Back = false,
                        CustomWidth = 0.5f,
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
                TravelSound = "", // SubtypeID for your Sound File. Travel, is sound generated around your Projectile in flight
                HitSound = "",
                ShotSound = "",
                ShieldHitSound = "",
                PlayerHitSound = "",
                VoxelHitSound = "",
                FloatingHitSound = "",
                HitPlayChance = 0.5f,
                HitPlayShield = true,
            },
            Ejection = new EjectionDef // Optional Component, allows generation of Particle or Item (Typically magazine), on firing, to simulate Tank shell ejection
            {
                Type = Particle, // Particle or Item (Inventory Component)
                Speed = 100f, // Speed inventory is ejected from in dummy direction in meters per second
                SpawnChance = 0.5f, // chance of triggering effect (0 - 1)
                SpeedVariance = Random(start: 0f, end: 0f), //Random range added to speed of ejected item, in meters per second
                DirectionVariance = Random(start: 0f, end: 0f), //Random range added to each component of the ejector dummy position (try 0-1)
                Rotation = Vector(0, 0, 0), //Rotation amount, radians per second per axis (try 50 in Z axis as a start)
                RotationVariance = Random(start: 0f, end: 0f), //Random range added to each component of the rotation vector

                CompDef = new ComponentDef
                {
                    ItemName = "", //InventoryComponent name
                    ItemLifeTime = 0, // how long item should exist in world
                    Delay = 0, // delay in ticks after shot before ejected
                }
            }, // Don't edit below this line
        };

      

 private AmmoDef AmmoType2 => new AmmoDef // Your ID, for slotting into the Weapon CS --- This ammo has been stripped to a minimal config as an example
        {
            AmmoMagazine = "Energy", 
            AmmoRound = "Ammo 2", 
            HybridRound = false, 
            EnergyCost = 0.1f, 
            BaseDamage = 111f, 
            Health = 1,
            HardPointUsable = true, 
            EnergyMagazineSize = 1, 

            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef
                {
                    Enable = true,
                    Radius = 5f, 
                    Damage = 5f,
                    Depth = 1f,
                    MaxAbsorb = 0f,
                    Falloff = Squeeze, 
                    MinArmingTime = 100, 
                    ParticleScale = 1,
                    CustomParticle = "particleName",
                    CustomSound = "soundName", 
                }, 
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = None, 
                TargetLossDegree = 80f, 
                MaxLifeTime = 1200, 
                AccelPerSec = 120f, 
                DesiredSpeed = 600, 
                MaxTrajectory = 1000f, 
                TotalAcceleration = 1234.5,
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "", 
                VisualProbability = 1f,
                ShieldHitDraw = false,
                Particles = new AmmoParticleDef
                {
                    Hit = new ParticleDef
                    {
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0.75f, end: 2f),
                    WidthVariance = Random(start: 0f, end: 0f), 
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 5f, //
                        Width = 0.1f, //
                        Color = Color(red: 3, green: 2, blue: 1f, alpha: 1), 
                        VisualFadeStart = 0, 
                        VisualFadeEnd = 0, 
                        Textures = new[] {
                            "WeaponLaser", 
                        },
                        TextureMode = Normal, 
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "WeaponLaser", 
                        },
                        TextureMode = Normal,
                        DecayTime = 3, 
                        Color = Color(red: 0, green: 0, blue: 1, alpha: 1),
                        Back = false,
                        CustomWidth = 0,
                        UseWidthVariance = false,
                        UseColorFade = true,
                    },
                },
            },
            AmmoAudio = new AmmoAudioDef
            {
                HitPlayChance = 0.5f,
                HitPlayShield = true,
            },
        };
    }
}

