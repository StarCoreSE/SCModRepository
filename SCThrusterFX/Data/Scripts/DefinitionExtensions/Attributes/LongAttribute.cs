using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Draygo.BlockExtensionsAPI
{
	public class LongAttribute : BaseAttribute
	{
		public LongAttribute()
		{

		}
		public LongAttribute(long value)
		{
			Value = value;
		}
		public LongAttribute(int value)
		{
			Value = value;
		}

		[XmlAttribute]
		public long Value = 0;

	}
}
