using Sandbox.Definitions;
using VRage.Game.Components;


namespace Klime.NoDrawH2Thruster
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class NoDrawH2Thruster : MySessionComponentBase
    {
        public override void LoadData()
        {
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                MyThrustDefinition thruster = def as MyThrustDefinition;
                if (thruster != null)
                {
                    if (thruster.FuelConverter.FuelId.SubtypeName == "Hydrogen")
                    {
                        thruster.FuelConverter.Efficiency = 1000000f;
                    }
                }
            }
        }

        protected override void UnloadData()
        {

        }
    }
}