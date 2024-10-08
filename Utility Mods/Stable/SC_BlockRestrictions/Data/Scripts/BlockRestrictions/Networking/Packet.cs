using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Utils;

namespace SC_BlockRestrictions
{
  [ProtoInclude(1000, typeof(BlockSaveData))]
  [ProtoInclude(2000, typeof(SerializableBlockSetting))]
  [ProtoInclude(3000, typeof(SerializableGroupSetting))]
  [ProtoInclude(4000, typeof(SerialId))]
  [ProtoContract]
  public class Packet
  {
    [ProtoMember(1)]
    public readonly ulong SenderId;

    [ProtoMember(2)]
    public bool CreativeEnabled;

    [ProtoMember(3)]
    public bool CopyPasteEnabled;

    [ProtoMember(4)]
    public bool CreativeUpdateOnly;

    [ProtoMember(5)]
    public string Message;

    [ProtoMember(6)]
    public List<SerializableBlockSetting> BlockSettings;

    [ProtoMember(7)]
    public List<SerializableGroupSetting> GroupSettings;

    [ProtoMember(8)]
    public bool CreativeModeAllowed = true;

    [ProtoMember(9)]
    public bool AdminUpdate;

    public Packet()
    {
      SenderId = MyAPIGateway.Multiplayer.MyId;
    }

    public Packet(bool creativeAllowed)
    {
      SenderId = MyAPIGateway.Multiplayer.MyId;
      CreativeModeAllowed = creativeAllowed;
      AdminUpdate = true;
    }

    public Packet(BlockSaveData modData)
    {
      SenderId = MyAPIGateway.Multiplayer.MyId;
      BlockSettings = modData.Settings;
      GroupSettings = modData.GroupSettings;
      CreativeModeAllowed = modData.CreativeModeAllowed;
    }

    public Packet(bool creativeEnabled, bool copyPasteEnabled, bool creativeOnlyUpdate)
    {
      SenderId = MyAPIGateway.Multiplayer.MyId;
      CreativeEnabled = creativeEnabled;
      CopyPasteEnabled = copyPasteEnabled;
      CreativeUpdateOnly = creativeOnlyUpdate;
    }

    public Packet(string message)
    {
      SenderId = MyAPIGateway.Multiplayer.MyId;
      Message = message;
    }
  }
}
