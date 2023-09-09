using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRageMath;

namespace Draygo.BlockExtensionsAPI
{
	public class Vector2IAttribute : BaseAttribute
	{

		public Vector2IAttribute()
		{

		}
		public Vector2IAttribute(Vector2I Value)
		{
			X = Value.X;
			Y = Value.Y;
		}
		[XmlAttribute]
		public int X;
		[XmlAttribute]
		public int Y;
	}
}
