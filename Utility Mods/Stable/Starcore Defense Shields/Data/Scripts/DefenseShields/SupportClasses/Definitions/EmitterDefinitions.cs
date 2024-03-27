using System.Collections.Generic;

namespace DefenseShields.Support
{
    public static class DefinitionManager
    {
        private static readonly Dictionary<string, Definition> Def = new Dictionary<string, Definition>
        {
            ["EmitterL"] = new Definition { Name = "EmitterL", ParticleScale = 10f, ParticleDist = 1.5d, HelperDist = 5.0d, FieldDist = 3.0d, BlockMoveTranslation = 0.0018f },
            ["EmitterS"] = new Definition { Name = "EmitterS", ParticleScale = 2.5f, ParticleDist = 1.25d, HelperDist = 3d, FieldDist = 0.8d, BlockMoveTranslation = 0.00032f },
            ["EmitterLA"] = new Definition { Name = "EmitterLA", ParticleScale = 10f, ParticleDist = 1.5d, HelperDist = 5.0d, FieldDist = 2.8d, BlockMoveTranslation = 0.0018f },
            ["EmitterSA"] = new Definition { Name = "EmitterSA", ParticleScale = 2.5f, ParticleDist = 1.25d, HelperDist = 3d, FieldDist = 0.8d, BlockMoveTranslation = 0.00032f },
            ["EmitterST"] = new Definition { Name = "EmitterST", ParticleScale = 20f, ParticleDist = 3.5d, HelperDist = 7.5d, FieldDist = 8.0d, BlockMoveTranslation = 0.005f },
            ["NPCEmitterLB"] = new Definition { Name = "NPCEmitterLB", ParticleScale = 10f, ParticleDist = 1.5d, HelperDist = 5.0d, FieldDist = 2.8d, BlockMoveTranslation = 0.0018f },
            ["NPCEmitterSB"] = new Definition { Name = "NPCEmitterSB", ParticleScale = 2.5f, ParticleDist = 1.25d, HelperDist = 3d, FieldDist = 0.8d, BlockMoveTranslation = 0.00032f },
        };


        public static Definition Get(string subtype)
        {
            return Def.GetValueOrDefault(subtype);
        }
    }

    public class Definition
    {
        public string Name;
        public float BlockMoveTranslation;
        public float ParticleScale;
        public double ParticleDist;
        public double HelperDist;
        public double FieldDist;
    }
}
