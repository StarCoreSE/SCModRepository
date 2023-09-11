using System.Collections.Generic;
using VRage.Game;
using VRage.ObjectBuilders;

namespace MIG.Shared.SE
{
    public static class UnitFormatter
    {
        private static Dictionary<MyObjectBuilderType, string> alliases = new Dictionary<MyObjectBuilderType, string>() {
            { MyObjectBuilderType.Parse("MyObjectBuilder_Ingot"), "i/" },
            { MyObjectBuilderType.Parse("MyObjectBuilder_Ore"), "o/" },
            { MyObjectBuilderType.Parse("MyObjectBuilder_Component"), "" }
        };
        
        public static string toHumanString (this MyDefinitionId id) {
            if (alliases.ContainsKey (id.TypeId)) {
                return alliases[id.TypeId] +id.SubtypeName;
            } else {
                return id.TypeId.ToString().Substring (16) + "/"+id.SubtypeName;
            }
        }
    }
}