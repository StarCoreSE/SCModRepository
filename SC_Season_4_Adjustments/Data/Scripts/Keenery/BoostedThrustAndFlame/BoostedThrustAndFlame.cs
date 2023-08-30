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
                    thruster.ForceMagnitude *= 5f; //S3 was 3.5
                    thruster.MaxPowerConsumption *= 5f; //S3 was 3.5
					thruster.FlameDamage *= 5f;
					//thruster.FlameDamageLengthScale *= 1f;
					//thruster.FlameLengthScale *= 2.0f;
					thruster.FlameVisibilityDistance *= 1f;
                }
            }
        }

        protected override void UnloadData()
        {
        
        }
    }
}