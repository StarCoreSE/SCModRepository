using static Scripts.Structure;
using static Scripts.Structure.ArmorDefinition.ArmorType;
namespace Scripts {   
    partial class Parts {
        // Don't edit above this line
        ArmorDefinition MA_Heavy_Armor => new ArmorDefinition
        {
            SubtypeIds = new[] {
 			 "MA_Laser_Armor01", 
			 "MA_Laser_Armor02",
			 "MA_Laser_Armor03",
			 "MA_Laser_Armor04",
			 "MA_Laser_Armor05",
			 "MA_Laser_Armor06",
			 "MA_Laser_Armor07",
			 "MA_Laser_Armor08",
			 "MA_Laser_Armor09",
			 "MA_Laser_Armor10",
			 "MA_Laser_Armor11",
			 "MA_Laser_Armor12",
			 "MA_Laser_Armor13",
			 "MA_Laser_Armor14",
			 "MA_Laser_Armor15",
			 "MA_Laser_Armor16"
            },
            EnergeticResistance = 1.0f, //Resistance to Energy damage. 0.5f = 200% damage, 2f = 50% damage
            KineticResistance = 1.0f, //Resistance to Kinetic damage. Leave these as 1 for no effect
            Kind = Heavy, //Heavy, Light, NonArmor - which ammo damage multipliers to apply
        };
/*        ArmorDefinition Armor2 => new ArmorDefinition
        {
            SubtypeIds = new[] {
                "Beskar"
            },
            EnergeticResistance = 1f,
            KineticResistance = 0.96f,
            Kind = Light,
        };
		*/
    }
}
