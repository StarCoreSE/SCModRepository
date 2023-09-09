using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draygo.BlockExtensionsAPI
{
	public class GameLogicComponent
	{
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
		public bool Enabled = true;
	}
}
