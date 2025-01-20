using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SC_BlockRestrictions.Settings
{
  public class DefaultSettings
  {
    public bool ForceSetting;

    [XmlElement("DefaultSetting", typeof(SerializableBlockSetting))]
    public List<SerializableBlockSetting> Settings { get; set; } = new List<SerializableBlockSetting>();

    public DefaultSettings() { }
  }
}
