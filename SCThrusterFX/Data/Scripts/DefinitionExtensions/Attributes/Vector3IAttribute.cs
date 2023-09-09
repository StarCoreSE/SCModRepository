using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRageMath;

namespace Draygo.BlockExtensionsAPI
{
	public class Vector3IAttribute : BaseAttribute
	{
		public Vector3IAttribute()
		{

		}
		public Vector3IAttribute(Vector3I Value)
		{
			X = Value.X;
			Y = Value.Y;
			Z = Value.Z;
		}
		[XmlAttribute]
		public int X;
		[XmlAttribute]
		public int Y;
		[XmlAttribute]
		public int Z;
	}
}
