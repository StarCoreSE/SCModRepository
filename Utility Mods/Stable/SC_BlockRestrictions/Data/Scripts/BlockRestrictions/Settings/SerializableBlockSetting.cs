using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProtoBuf;

namespace SC_BlockRestrictions
{

  [ProtoContract]
  public class SerializableBlockSetting
  {
    [ProtoMember(1)] public string Type;
    [ProtoMember(2)] public int PlayerMaxCount;
    [ProtoMember(3)] public int GridMaxCount;
    [ProtoMember(4)] public int FactionMaxCount;
    [ProtoMember(5)] public bool AllowedForNPC;
    [ProtoMember(6)] public bool AllowedForPlayer;
    [ProtoMember(7)] public bool AllowedForUnowned;
    [ProtoMember(8)] public bool AllowedForNPCStaticOnly;
    [ProtoMember(9)] public bool AllowedForPlayerStaticOnly;
    [ProtoMember(10)] public bool AllowedForUnownedStaticOnly;

    public SerializableBlockSetting() { }

    public SerializableBlockSetting(string type, bool allowPlayer = true, bool allowPlayerStatic = false, bool allowNPC = true, bool allowNPCStatic = false, bool allowUnowned = true, bool allowUnownedStatic = false, int maxCountPlayer = 0, int maxCountGrid = 0, int maxCountFaction = 0)
    {
      Type = type;
      PlayerMaxCount = maxCountPlayer;
      GridMaxCount = maxCountGrid;
      FactionMaxCount = maxCountFaction;
      AllowedForNPC = allowNPC;
      AllowedForPlayer = allowPlayer;
      AllowedForUnowned = allowUnowned;
      AllowedForNPCStaticOnly = allowNPCStatic;
      AllowedForPlayerStaticOnly = allowPlayerStatic;
      AllowedForUnownedStaticOnly = allowUnownedStatic;
    }
  }
}
