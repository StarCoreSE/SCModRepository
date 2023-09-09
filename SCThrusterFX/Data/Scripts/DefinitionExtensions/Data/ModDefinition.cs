using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Draygo.BlockExtensionsAPI
{

	public class PlanetGeneratorDefinition : Definition
	{

	}
	public class PhysicalItem : Definition
	{

	}
	public class Component : Definition 
	{

	}
	public class Definition
	{
		public SerializableDefinitionId Id;
		public ModExtensions ModExtensions;
	}
}
