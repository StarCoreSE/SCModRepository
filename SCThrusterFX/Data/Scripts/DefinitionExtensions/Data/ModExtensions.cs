using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Draygo.BlockExtensionsAPI
{
	public class ModExtensions
	{
		[XmlElement("Group")]
		public Group[] Group;
		//[XmlElement("ModComponents")]
		public GameLogicComponent[] ModComponents;
	}
}
