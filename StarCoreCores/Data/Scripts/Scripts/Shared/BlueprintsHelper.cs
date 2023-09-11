using Sandbox.Definitions;
using VRage.ObjectBuilders;

namespace MIG.Shared.SE
{

    public enum BlueprintType
    {
        OreToIngot,
        OresToIngots,
        OresToOres,

        IngotsToIngots,
        IngotsToComponent,
        IngotsToComponents,

        ComponentsToComponents,

        OtherToTools,
        IngotsToTools,

        Other
    }

    public static class BlueprintsHelper
    {
        public static readonly MyObjectBuilderType COMPONENT = MyObjectBuilderType.Parse("MyObjectBuilder_Component");
        public static readonly MyObjectBuilderType ORE = MyObjectBuilderType.Parse("MyObjectBuilder_Ore");
        public static readonly MyObjectBuilderType INGOT = MyObjectBuilderType.Parse("MyObjectBuilder_Ingot");
        public static readonly MyObjectBuilderType TOOL = MyObjectBuilderType.Parse("MyObjectBuilder_PhysicalGunObject");
        public static readonly MyObjectBuilderType TOOL2 = MyObjectBuilderType.Parse("MyObjectBuilder_OxygenContainerObject");

        public static BlueprintType GetBlueprintType(this MyBlueprintDefinitionBase b)
        {
            var hasInputOres = false;
            var hasInputIngots = false;
            var hasInputComponents = false;
            var hasInputOther = false;

            var hasOutputOres = false;
            var hasOutputIngots = false;
            var hasOutputComponents = false;
            var hasOutputTools = false;
            var hasOutputOther = false;

            foreach (var r in b.Prerequisites)
            {
                if (r.Id.TypeId == COMPONENT)
                {
                    hasInputComponents = true;
                    continue;
                }
                if (r.Id.TypeId == ORE)
                {
                    hasInputOres = true;
                    continue;
                }
                if (r.Id.TypeId == INGOT)
                {
                    hasInputIngots = true;
                    continue;
                }

                hasInputOther = true;
            }

            foreach (var r in b.Results)
            {
                if (r.Id.TypeId == COMPONENT)
                {
                    hasOutputComponents = true;
                    continue;
                }
                if (r.Id.TypeId == TOOL || r.Id.TypeId == TOOL2)
                {
                    hasOutputTools = true;
                    continue;
                }
                if (r.Id.TypeId == ORE)
                {
                    hasOutputOres = true;
                    continue;
                }
                if (r.Id.TypeId == INGOT)
                {
                    hasOutputIngots = true;
                    continue;
                }

                hasOutputOther = true;
            }

            var i = (hasInputOres ? 1 : 0) + (hasInputIngots ? 1 : 0) + (hasInputComponents ? 1 : 0) + (hasInputOther ? 1 : 0);
            var o = (hasOutputOres ? 1 : 0) + (hasOutputIngots ? 1 : 0) + (hasOutputComponents ? 1 : 0) + (hasOutputTools ? 1 : 0) + (hasOutputOther ? 1 : 0);

            if (i != 1) return BlueprintType.Other;
            if (o != 1) return BlueprintType.Other;

            if (hasOutputTools) return hasInputIngots ? BlueprintType.IngotsToTools : BlueprintType.OtherToTools;
            if (hasInputOres && hasOutputIngots) return b.Results.Length == 1 && b.Prerequisites.Length == 1 ? BlueprintType.OreToIngot : BlueprintType.OresToIngots;
            if (hasInputIngots && hasOutputComponents) return b.Results.Length > 1 ? BlueprintType.IngotsToComponents : BlueprintType.IngotsToComponent;
            if (hasInputOres && hasOutputOres) return BlueprintType.OresToOres;
            if (hasInputIngots && hasOutputIngots) return BlueprintType.IngotsToIngots;
            if (hasInputComponents && hasOutputComponents) return BlueprintType.ComponentsToComponents;
            if (hasOutputTools) return BlueprintType.IngotsToTools;

            return BlueprintType.Other;
        }
    }
}
