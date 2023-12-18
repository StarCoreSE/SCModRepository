using ProtoBuf;
using VRageMath;

namespace NuclearWarheads
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class NuclearWarheadsSettingsConfig
    {
        [ProtoMember(1)]
        public VanillaWarheadBlockConfig VanillaWarheadBlockConfig = new VanillaWarheadBlockConfig();
        [ProtoMember(2)]
        public MiniNukeBlockConfig MiniNukeBlockConfig = new MiniNukeBlockConfig();
        [ProtoMember(3)]
        public NuclearWarheadBlockConfig NuclearWarheadBlockConfig = new NuclearWarheadBlockConfig();
        [ProtoMember(4)]
        public ThermoNuclearWarheadBlockConfig ThermoNuclearWarheadBlockConfig = new ThermoNuclearWarheadBlockConfig();
        [ProtoMember(5)]
        public NuclearMissileLauncherConfig NuclearMissileLauncherConfig = new NuclearMissileLauncherConfig();
        [ProtoMember(6)]
        public ComponentsConfig ComponentsConfig = new ComponentsConfig();
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class VanillaWarheadBlockConfig
    {
        [ProtoMember(1)]
        public bool OverrideVanillaWarhead = true;
        [ProtoMember(2)]
        public float VanillaSmallWarheadExplosionRadius = 5F;
        [ProtoMember(3)]
        public float VanillaLargeWarheadExplosionRadius = 25F;
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class MiniNukeBlockConfig
    {
        [ProtoMember(1)]
        public bool PlayerCanBuildMiniNuke = true;
        [ProtoMember(2)]
        public int MiniNukeUraniumComponentRequirement = 1;
        [ProtoMember(3)]
        public float MiniNukeExplosionRadius = 50F;
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class NuclearWarheadBlockConfig
    {
        [ProtoMember(1)]
        public bool PlayerCanBuildNuclearWarhead = true;
        [ProtoMember(2)]
        public int NuclearWarheadUraniumComponentRequirement = 2;
        [ProtoMember(3)]
        public float NuclearExplosionRadius = 100F;
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class ThermoNuclearWarheadBlockConfig
    {
        [ProtoMember(1)]
        public bool PlayerCanBuildThermoNuclearWarhead = true;
        [ProtoMember(2)]
        public int ThermoNuclearWarheadUraniumComponentRequirement = 8;
        [ProtoMember(3)]
        public int ThermoNuclearWarheadSuperconductorComponentRequirement = 50;
        [ProtoMember(4)]
        public float ThermoNuclearExplosionRadius = 300F;
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class NuclearMissileLauncherConfig
    {
        [ProtoMember(1)]
        public bool PlayerCanBuildNuclearMissileLauncher = true;
        [ProtoMember(2)]
        public bool PlayerCanFireNuclearMissileWithHandheldLauncher = true;
        [ProtoMember(3)]
        public float NuclearMissileExplosionRadius = 100F;
        [ProtoMember(4)]
        public float NuclearMissileLaunchSpeed = 150F;
        [ProtoMember(5)]
        public float NuclearMissileGravityMultiplier = 2F;
        [ProtoMember(6)]
        public float NuclearMissileMaxDistance = 15000F;
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class ComponentsConfig
    {
        [ProtoMember(1)]
        public bool PlayerCanMakeEnrichedUraniumCore = true;
        [ProtoMember(2)]
        public float EnrichedUraniumCoreIngotRequirementMultiplier = 1f;
        [ProtoMember(3)]
        public float EnrichedUraniumCoreProductionTimeMultiplier = 1f;
        [ProtoMember(4)]
        public bool PlayerCanMakeNuclearMissileAmmo = true;
        [ProtoMember(5)]
        public float NuclearMissileAmmoUraniumRequirementMultiplier = 1f;
        [ProtoMember(6)]
        public float NuclearMissileAmmoPlatinumRequirementMultiplier = 1f;
        [ProtoMember(7)]
        public float NuclearMissileAmmoMagnesiumRequirementMultiplier = 1f;
        [ProtoMember(8)]
        public float NuclearMissileProductionTimeMultiplier = 1f;
    }
}
