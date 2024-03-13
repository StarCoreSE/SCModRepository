using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;


namespace Klime.BoostedThrustAndFlame
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class BoostedThrustAndFlame : MySessionComponentBase
    {
        public override void LoadData()
        {
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                MyThrustDefinition thruster = def as MyThrustDefinition;
                
                if (thruster != null)
                {
                    thruster.ForceMagnitude *= 4.0f; //S3 was 3.5
                    thruster.MaxPowerConsumption *= 3.0f; //S3 was 3.5
					thruster.FlameDamage *= 5f;
					//thruster.FlameDamageLengthScale *= 1.5f;
					thruster.FlameLengthScale *= 2.0f;
					//thruster.FlameVisibilityDistance = 100f;
                }
            }
        }

        protected override void UnloadData()
        {
        
        }
    }
}