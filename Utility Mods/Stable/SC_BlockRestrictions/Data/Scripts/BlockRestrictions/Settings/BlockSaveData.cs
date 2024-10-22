using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ObjectBuilders;
using ProtoBuf;
using System.Xml.Serialization;

namespace SC_BlockRestrictions
{
  [ProtoContract]
  public class BlockSaveData
  {
    [ProtoMember(1)] public bool CreativeModeAllowed;

    [ProtoMember(2)] public List<SerializableGroupSetting> GroupSettings { get; set; } = new List<SerializableGroupSetting>();

    [ProtoMember(3)] public List<SerializableBlockSetting> Settings { get; set; } = new List<SerializableBlockSetting>();

    public BlockSaveData() { }

    public void Close()
    {
      GroupSettings?.Clear();
      Settings?.Clear();
      GroupSettings = null;
      Settings = null;
    }
  }
}
