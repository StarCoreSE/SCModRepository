
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
            HAS_Assault,
            HAS_Thanatos,
            HAS_Alecto,
            HAS_Mammon,
           // HAS_MammonMK2,
           // HAS_MammonMK21,
           // HAS_MammonMK22,
           // HAS_MammonMK23,
           // HAS_MammonMK24,
            HAS_Twin,
            HAS_Esper,
            HAS_Vulcan,
            HAS_Cyclops,
            HAS_Crossfield,
            HAS_CrossfieldOne,
            HAS_CrossfieldTwo,
            HAS_CrossfieldThree
            );
            ArmorDefinitions();
            SupportDefinitions();
            UpgradeDefinitions();
        }
    }
}
