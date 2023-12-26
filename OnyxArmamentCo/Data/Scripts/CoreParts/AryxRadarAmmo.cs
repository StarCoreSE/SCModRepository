using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AmmoDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef.SpawnType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.ShapeDef.Shapes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.ShieldDef.ShieldType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef.AreaEffectType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef.EwarFieldsDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef.EwarFieldsDef.PushPullDef.Force;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.TracerBaseDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.Texture;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DamageTypes.Damage;

namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        private AmmoDef AryxRadarAmmo => new AmmoDef
        {
            AmmoMagazine = "Energy",
            AmmoRound = "Radiowave",
            HybridRound = false, //AmmoMagazine based weapon with energy cost
            EnergyCost = 0f, //(((EnergyCost * DefaultDamage) * ShotsPerSecond) * BarrelsPerShot) * ShotsPerBarrel
            BaseDamage = 0f,
            Mass = 0, // in kilograms
            Health = 0, // 0 = disabled, otherwise how much damage it can take from other trajectiles before dying.
            BackKickForce = 0f,
            DecayPerShot = 0,
            HardPointUsable = true, // set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
            EnergyMagazineSize = 1,
            Shape = new ShapeDef //defines the collision shape of projectile, defaults line and visual Line Length if set to 0
            {
                Shape = LineShape,
                Diameter = 1,
            },
            ObjectsHit = new ObjectsHitDef
            {
                MaxObjectsHit = 0, // 0 = disabled
                CountBlocks = false, // counts gridBlocks and not just entities hit
            },
            Fragment = new FragmentDef
            {
                AmmoRound = "",
                Fragments = 200,
                Degrees = 360,
                Reverse = false,
                //RandomizeDir = true, // randomzie between forward and backward directions
            },
            Pattern = new PatternDef
            {
                Patterns = new[] {
                    "",
                },
                Enable = false,
                TriggerChance = 1f,
                Random = false,
                RandomMin = 1,
                RandomMax = 1,
                SkipParent = false,
            },
            DamageScales = new DamageScaleDef
            {
                MaxIntegrity = 0f, // 0 = disabled, 1000 = any blocks with currently integrity above 1000 will be immune to damage.
                DamageVoxels = false, // true = voxels are vulnerable to this weapon
                SelfDamage = false, // true = allow self damage.
                HealthHitModifier = 0f, // defaults to a value of 1, this setting modifies how much Health is subtracted from a projectile per hit (1 = per hit).
                Characters = 0f,
                DamageType = new DamageTypes
                {
                    Base = Energy,
                    AreaEffect = Energy,
                    Detonation = Energy,
                    Shield = Energy,
                },
                FallOff = new FallOffDef
                {
                    Distance = 10000f, // Distance at which max damage begins falling off.
                    MinMultipler = 0.1f, // value from 0.0f to 1f where 0.1f would be a min damage of 10% of max damage.
                },
                Grids = new GridSizeDef
                {
                    Large = 0.01f,
                    Small = 0.01f,
                },
                Armor = new ArmorDef
                {
                    Armor = 0.01f,
                    Light = 0.01f,
                    Heavy = 0.01f,
                    NonArmor = 0.01f,
                },
                Shields = new ShieldDef
                {
                    Modifier = 0.01f,
                    Type = Default,
                    BypassModifier = 0f,
                },
            },
            AreaEffect = new AreaDamageDef
            {
                AreaEffect = Disabled, // Disabled = do not use area effect at all, Explosive, Radiant, AntiSmart, JumpNullField, JumpNullField, EnergySinkField, AnchorField, EmpField, OffenseField, NavField, DotField.
                AreaEffectDamage = 0f, // 0 = use spillover from BaseDamage, otherwise use this value.
                AreaEffectRadius = 0f,
                Pulse = new PulseDef // interval measured in game ticks (60 == 1 second), pulseChance chance (0 - 100) that an entity in field will be hit
                {
                    Interval = 60,
                    PulseChance = 100,
                    GrowTime = 100,
                    HideModel = false,
                    ShowParticle = false,
                    Particle = new ParticleDef
                    {
                        Name = "", //ShipWelderArc
                        //ShrinkByDistance = false,
                        Color = Color(red: 1, green: 8f, blue: 10f, alpha: 1),
                        Offset = Vector(x: 0, y: -1, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 5000,
                            MaxDuration = 1,
                            Scale = 1,
                        },
                    },
                },
                Explosions = new ExplosionDef
                {
                    NoVisuals = false,
                    NoSound = false,
                    NoShrapnel = false,
                    NoDeformation = false,
                    Scale = 1,
                    CustomParticle = "",
                    CustomSound = "",
                },
                Detonation = new DetonateDef
                {
                    DetonateOnEnd = false,
                    ArmOnlyOnHit = false,
                    DetonationDamage = 0,
                    DetonationRadius = 0,
                    MinArmingTime = 0, //Min time in ticks before projectile will arm for detonation (will also affect shrapnel spawning)
                },
                EwarFields = new EwarFieldsDef
                {
                    Duration = 0,
                    StackDuration = false,
                    Depletable = false,
                    MaxStacks = 0,
                    TriggerRange = 0f,
                    DisableParticleEffect = true,
                    Force = new PushPullDef // AreaEffectDamage is multiplied by target mass.
                    {
                        ForceFrom = ProjectileLastPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                        ForceTo = HitPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                        Position = TargetCenterOfMass, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                    },
                },
            },
            Beams = new BeamDef
            {
                Enable = true,
                VirtualBeams = false, // Only one hot beam, but with the effectiveness of the virtual beams combined (better performace)
                ConvergeBeams = false, // When using virtual beams this option visually converges the beams to the location of the real beam.
                RotateRealBeam = false, // The real (hot beam) is rotated between all virtual beams, instead of centered between them.
                OneParticle = true, // Only spawn one particle hit per beam weapon.
            },
            Trajectory = new TrajectoryDef
            {
                Guidance = None,
                TargetLossDegree = 80f,
                TargetLossTime = 0, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                MaxLifeTime = 0, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                AccelPerSec = 0f,
                DesiredSpeed = 0,
                MaxTrajectory = 50000f,
                //FieldTime = 0, // 0 is disabled, a value causes the projectile to come to rest, spawn a field and remain for a time (Measured in game ticks, 60 = 1 second)
                GravityMultiplier = 0f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable.
                SpeedVariance = Random(start: 0, end: 0), // subtracts value from DesiredSpeed
                RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
                MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                Smarts = new SmartsDef
                {
                    Inaccuracy = 0f, // 0 is perfect, hit accuracy will be a random num of meters between 0 and this value.
                    Aggressiveness = 1f, // controls how responsive tracking is.
                    MaxLateralThrust = 0.5f, // controls how sharp the trajectile may turn
                    TrackingDelay = 1, // Measured in Shape diameter units traveled.
                    MaxChaseTime = 1800, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    OverideTarget = true, // when set to true ammo picks its own target, does not use hardpoint's.
                    MaxTargets = 0, // Number of targets allowed before ending, 0 = unlimited
                    NoTargetExpire = false, // Expire without ever having a target at TargetLossTime
                    Roam = false, // Roam current area after target loss
                },
                Mines = new MinesDef
                {
                    DetectRadius = 200,
                    DeCloakRadius = 100,
                    FieldTime = 1800,
                    Cloak = false,
                    Persist = false,
                },
            },
            AmmoGraphics = new GraphicDef
            {
                ModelName = "",
                VisualProbability = 1f,
                ShieldHitDraw = true,
                Particles = new AmmoParticleDef
                {
                    Ammo = new ParticleDef
                    {
                        Name = "", //ShipWelderArc
                        //ShrinkByDistance = false,
                        Color = Color(red: 128, green: 0, blue: 0, alpha: 32),
                        Offset = Vector(x: 0, y: -1, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 5000,
                            MaxDuration = 1,
                            Scale = 1,
                        },
                    },
                    Hit = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        //ShrinkByDistance = false,
                        Color = Color(red: 1, green: 8f, blue: 10f, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 5000,
                            MaxDuration = 30,
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                    Eject = new ParticleDef
                    {
                        Name = "",
                        ApplyToShield = true,
                        //ShrinkByDistance = false,
                        Color = Color(red: 1, green: 8f, blue: 10f, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),
                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 5000,
                            MaxDuration = 30,
                            Scale = 1,
                            HitPlayChance = 1f,
                        },
                    },
                },
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0, end: 0f), // multiply the color by random values within range.
                    WidthVariance = Random(start: 0f, end: 1.025f), // adds random value to default width (negatives shrinks width)
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 1f,
                        Width = 0.0005f,
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 0),
                        VisualFadeStart = 30, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                        VisualFadeEnd = 30, // How many ticks after fade began before it will be invisible.
                        Textures = new[] {// WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                            "AryxPulseLaserEffectL2",
                        },
                        TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                        Segmentation = new SegmentDef
                        {
                            Enable = true, // If true Tracer TextureMode is ignored
                            Textures = new[] {
                                "AryxPulseLaserEffectL2",
                            },
                            SegmentLength = 20f, // Uses the values below.
                            SegmentGap = 15f, // Uses Tracer textures and values
                            Speed = 120f, // meters per second
                            Color = Color(red: 0, green: 0, blue: 0, alpha: 0),
                            WidthMultiplier = 0.5f,
                            Reverse = false,
                            UseLineVariance = true,
                            WidthVariance = Random(start: -0.05f, end: 0f),
                            ColorVariance = Random(start: 0.3f, end: 1f)
                        }
                    },
                    Trail = new TrailDef
                    {
                        Enable = false,
                        Textures = new[] {
                            "AryxPulseLaserEffectL2",
                        },
                        TextureMode = Normal,
                        DecayTime = 128,
                        Color = Color(red: 1, green: 8f, blue: 10f, alpha: 1),
                        Back = false,
                        CustomWidth = 0,
                        UseWidthVariance = false,
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
                TravelSound = "",
                HitSound = "",
                ShieldHitSound = "",
                PlayerHitSound = "",
                VoxelHitSound = "",
                FloatingHitSound = "",
                HitPlayChance = 0.5f,
                HitPlayShield = true,
            }, // Don't edit below this line
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
            },
        };

    }
}
