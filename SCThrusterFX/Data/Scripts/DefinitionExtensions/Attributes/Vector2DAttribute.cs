using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRageMath;

namespace Draygo.BlockExtensionsAPI
{
	public class Vector2DAttribute : BaseAttribute
	{
		public Vector2DAttribute()
		{

		}
		public Vector2DAttribute(Vector2D Value)
		{
			X = Value.X;
			Y = Value.Y;
		}
		[XmlAttribute]
		public double X;
		[XmlAttribute]
		public double Y;
		//public Vector2D Value;
	}
}
