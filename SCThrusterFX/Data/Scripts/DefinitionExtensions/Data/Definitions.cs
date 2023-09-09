using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage;
using VRage.Game;

namespace Draygo.BlockExtensionsAPI
{
	public class Definitions
	{
		public Definition[] CubeBlocks;
		public Component[] Components;
		public PhysicalItem[] PhysicalItems;
		public PlanetGeneratorDefinition[] PlanetGeneratorDefinitions;
		[XmlElement("Definition")]
		public Definition[] Definition;
	}
}
