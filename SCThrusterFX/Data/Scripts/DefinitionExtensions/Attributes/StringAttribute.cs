using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draygo.BlockExtensionsAPI
{
	public class StringAttribute : BaseAttribute
	{
		public StringAttribute()
		{

		}
		public StringAttribute(string value)
		{
			Value = value;
		}

		[XmlAttribute]
		public string Value = string.Empty;
	}
}
