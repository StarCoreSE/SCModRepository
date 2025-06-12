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
{
    partial class Parts
    {
        private AmmoDef SRBWarheadFragContainer => new AmmoDef
        {
            AmmoMagazine = "Energy",
            AmmoRound = "SRBWarheadFragContainer",
            HardPointUsable = false,
			NoGridOrArmorScaling = true,
            EnergyMagazineSize = 1, // For energy weapons, how many shots to fire before reloading.
            Fragment = new FragmentDef
            {
                AmmoRound = "SRBWarheadFrags",
                Fragments = 120,
                Degrees = 360,
            },
            AreaOfDamage = new AreaOfDamageDef
            {
                EndOfLife = new EndOfLifeDef //With a phantom shell (in this case) not moving at all, there's no "hit" to register and start the AOE spread.  Rely on frags for actual dmg, and this EOL for AV
                {
                    Enable = true,
                    ParticleScale = 1f,
                    CustomParticle = "Muzzle_Flash_Large",
                    CustomSound = "WepSmallWarheadExpl",
                }, 
            },
            Trajectory = new TrajectoryDef
            {
                MaxLifeTime = 1,
            },
            AmmoGraphics = new GraphicDef //Necessary to have AV actions register and draw the explosion custom particle
            {
                VisualProbability = 1f,
                Lines = new LineDef
                {
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                    },
                },
            },
        };

      

		private AmmoDef SRBWarheadFrags => new AmmoDef
		{
			AmmoMagazine = "Energy",
			AmmoRound = "SRBWarheadFrags",
			BaseDamage = 1750,
            Mass = 10,
			DamageScales = new DamageScaleDef
            {
                FallOff = new FallOffDef
                {
                    Distance = 5f,
                    MinMultipler = 0.001f,
                },
                Grids = new GridSizeDef
                {
                    Large = -1f,
                    Small = -1f,
                },
                Armor = new ArmorDef
                {
                    Armor = -1f,
                    Light = -1f,
                    Heavy = -1f,
                    NonArmor = -1f,
                },
                Shields = new ShieldDef
                {
                    Modifier = 1f,
                    Type = Default,
                    BypassModifier = -1f,
                    HeatModifier = 1,
                },
                DamageType = new DamageTypes
                {
                    Base = Kinetic,
                    AreaEffect = Energy,
                    Detonation = Energy,
                    Shield = Energy,
                },
                Deform = new DeformDef
                {
                    DeformType = HitBlock,
                    DeformDelay = 30,
                },
            },
            Beams = new BeamDef
            {
                Enable = false, //Note that invisible beams are recommended for the cloud of frag for performance
            },
			Trajectory = new TrajectoryDef
			{
				MaxLifeTime = 60,
				MaxTrajectory = 15,
				DesiredSpeed = 30,
			},
            AmmoGraphics = new GraphicDef //This whole section can be deleted, but is left in for the trail visual to show it's working
            {
                VisualProbability = 0.75f,
                Lines = new LineDef
                {
                    ColorVariance = Random(start: 0.75f, end: 2f),
                    WidthVariance = Random(start: -0.01f, end: 0.05f),
                    Tracer = new TracerBaseDef
                    {
                        Enable = true,
                        Length = 1f,
                        Width = 0.05f,
                        Color = Color(red: 23, green: 16, blue: 1, alpha: 1),
                        Textures = new[] {
                            "ProjectileTrailLine",
                        },
                        TextureMode = Chaos,
                    },
                    Trail = new TrailDef
                    {
                        Enable = true,
                        Textures = new[] {
                            "WeaponLaser",
                        },
                        DecayTime = 45,
                        Color = Color(red: 5, green: 1, blue: 1, alpha: 1),
                    },
                },
            },
		};
    }
}

