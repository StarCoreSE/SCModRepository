using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draygo.BlockExtensionsAPI
{
	public class DecimalAttribute : BaseAttribute
	{
		public DecimalAttribute()
		{

		}
		public DecimalAttribute(double value)
		{
			Value = value;
		}
		public DecimalAttribute(float value)
		{
			Value = value;
		}
		[XmlAttribute]
		public double Value = 0d;
	}
}
