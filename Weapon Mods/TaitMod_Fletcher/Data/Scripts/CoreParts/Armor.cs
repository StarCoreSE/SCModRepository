using static Scripts.Structure;
using static Scripts.Structure.ArmorDefinition.ArmorType;
namespace Scripts {   
    partial class Parts {
        // Don't edit above this line
        ArmorDefinition Barbetteset => new ArmorDefinition
        {
            SubtypeIds = new[] {
                "Barbette5x5",
                "Barbette3x3",
                "Barbette1x1"
            },
            EnergeticResistance = 3f, //Resistance to Energy damage. 0.5f = 200% damage, 2f = 50% damage
            KineticResistance = 3f, //Resistance to Kinetic damage. Leave these as 1 for no effect
            Kind = Heavy, //Heavy, Light, NonArmor - which ammo damage multipliers to apply
        };
    }
}
