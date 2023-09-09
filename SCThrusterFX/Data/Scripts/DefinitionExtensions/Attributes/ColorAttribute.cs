using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRageMath;

namespace Draygo.BlockExtensionsAPI
{
	public class ColorAttribute : BaseAttribute 
	{
		public ColorAttribute()
		{

		}
		public ColorAttribute(Color Value)
		{
			R = Value.R;
			G = Value.G;
			B = Value.B;
			A = Value.A;
		}
		[XmlAttribute]
		public byte R;
		[XmlAttribute]
		public byte G;
		[XmlAttribute]
		public byte B;
		[XmlAttribute]
		public byte A;
	}
}
