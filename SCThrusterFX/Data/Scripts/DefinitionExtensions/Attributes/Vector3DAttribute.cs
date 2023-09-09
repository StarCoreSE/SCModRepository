using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRageMath;

namespace Draygo.BlockExtensionsAPI
{
	public class Vector3DAttribute : BaseAttribute 
	{
		public Vector3DAttribute()
		{

		}
		public Vector3DAttribute(Vector3D Value)
		{
			X = Value.X;
			Y = Value.Y;
			Z = Value.Z;
		}
		[XmlAttribute]
		public double X;
		[XmlAttribute]
		public double Y;
		[XmlAttribute]
		public double Z;
	}
}
