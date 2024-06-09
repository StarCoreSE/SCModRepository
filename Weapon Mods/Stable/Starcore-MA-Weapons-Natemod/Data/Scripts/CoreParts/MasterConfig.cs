
namespace Scripts
{
    partial class Parts
    {
        internal Parts()
        {
            // naming convention: WeaponDefinition Name
            //
            // Enable your definitions using the follow syntax:
            // PartDefinitions(Your1stDefinition, Your2ndDefinition, Your3rdDefinition);
            // PartDefinitions includes both weapons and phantoms
 //           PartDefinitions(Weapon75, Phantom01);
  //          ArmorDefinitions(Armor1, Armor2);
   //         SupportDefinitions(ArmorEnhancer1A);
    //        UpgradeDefinitions(Upgrade75a, Upgrade75b);
	
		PartDefinitions(
			MA_PDX,
			MA_PDX_T2,
			MA_Gimbal_Laser, 
			MA_Gimbal_Laser_T2, 
			MA_Gladius,
			MA_Gladius_Ion, 
			Meatball_Center, 
			Meatball_Left, 
			Meatball_Right, 
		    MA_Blister,
            MA_Slinger,
            MA_Tiger,
			MA_Crouching_Tiger,
			MA_Fixed_T2,
			MA_Fixed_T3,
			MA_Guardian_T4,
			MA_Derecho
		);	
		
		ArmorDefinitions(
			MA_Heavy_Armor
		);
	
	
	
	
	
        }
    }
}
