using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draygo.BlockExtensionsAPI
{
	public class Group
	{
		[XmlAttribute]
		public string Name;
		[XmlElement("Text")]
		public StringAttribute[] Text;
		[XmlElement("Integer")]
		public LongAttribute[] Integer;
		[XmlElement("Boolean")]
		public BooleanAttribute[] Boolean;
		[XmlElement("Decimal")]
		public DecimalAttribute[] Decimal;
		[XmlElement("Vector2I")]
		public Vector2IAttribute[] Vector2I;
		[XmlElement("Vector2D")]
		public Vector2DAttribute[] Vector2D;
		[XmlElement("Vector3I")]
		public Vector3IAttribute[] Vector3I;
		[XmlElement("Vector3D")]
		public Vector3DAttribute[] Vector3D;
		[XmlElement("Color")]
		public ColorAttribute[] Color;
	}
}
