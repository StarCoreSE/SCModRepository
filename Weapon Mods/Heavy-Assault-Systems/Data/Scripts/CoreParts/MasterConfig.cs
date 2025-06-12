
namespace Scripts
{
    partial class Parts
    {
        internal Parts()
        {
            // naming convention: WeaponDefinition Name
            //SA_HMI_ERGaussRF
            // Enable your definitions using the follow syntax:
            // PartDefinitions(Your1stDefinition, Your2ndDefinition, Your3rdDefinition);
            // PartDefinitions includes both weapons and phantoms
            PartDefinitions(

            HAS_Vulcan,

            HAS_Crossfield,
            HAS_CrossfieldOne,
            HAS_CrossfieldTwo,
            HAS_CrossfieldThree,


            HAS_Esper,
			HAS_Mammon,
            HAS_MammonMK2B1,
            HAS_MammonMK2B2,
            HAS_MammonMK2B3,
            HAS_MammonMK2B4,
            HAS_MammonMK2B5,

            HAS_Nyx,
            HAS_Thanatos,
            HAS_Cyclops


            );
            ArmorDefinitions();
            SupportDefinitions();
            UpgradeDefinitions();
        }
    }
}
