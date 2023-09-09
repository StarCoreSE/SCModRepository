using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draygo.BlockExtensionsAPI
{
	public class BooleanAttribute : BaseAttribute
	{

		public BooleanAttribute()
		{

		}
		public BooleanAttribute(bool value)
		{
			Value = value;
		}
		[XmlAttribute]
		public bool Value;
	}
}
