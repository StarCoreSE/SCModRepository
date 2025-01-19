using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using ProtoBuf;

using VRage.Game;
using VRage.ObjectBuilders;

namespace SC_BlockRestrictions
{
  [ProtoContract]
  public class SerialId
  {
    [XmlAttribute("Type")]
    [ProtoMember(1)] public string TypeId;

    [XmlAttribute("SubtypeId")]
    [ProtoMember(2)] public string SubtypeId;

    [XmlIgnore]
    MyDefinitionId _definitionId = new MyDefinitionId();

    public SerialId() { }

    public SerialId(SerializableDefinitionId id)
    {
      bool isNull = id.IsNull();
      TypeId = isNull ? "MyObjectBuilder_TypeGoesHere" : id.TypeIdString;
      SubtypeId = isNull ? "SubtypeGoesHere" : id.SubtypeId;

      _definitionId = id;
    }

    public MyDefinitionId DefinitionId
    {
      get
      {
        if (_definitionId.TypeId.IsNull)
        {
          MyObjectBuilderType typeId;
          if (!MyObjectBuilderType.TryParse(TypeId, out typeId))
            return new MyDefinitionId();

          if (SubtypeId == "SubtypeGoesHere")
            return new MyDefinitionId();

          var subtype = SubtypeId.IndexOf("null", StringComparison.OrdinalIgnoreCase) >= 0 ? string.Empty : SubtypeId;
          _definitionId = new MyDefinitionId(typeId, subtype);
        }

        return _definitionId;
      }
    }
  }

  [ProtoContract]
  public class SerializableGroupSetting
  {
    [ProtoMember(1)] public string GroupName;

    [XmlArrayItem("DefinitionId")]
    [ProtoMember(2)] public List<SerialId> Definitions { get; set; } = new List<SerialId>();

    [ProtoMember(3)] public int PlayerMaxCount;
    [ProtoMember(4)] public int GridMaxCount;
    [ProtoMember(5)] public int FactionMaxCount;
    [ProtoMember(6)] public bool AllowedForNPC;
    [ProtoMember(7)] public bool AllowedForPlayer;
    [ProtoMember(8)] public bool AllowedForUnowned;
    [ProtoMember(9)] public bool AllowedForNPCStaticOnly;
    [ProtoMember(10)] public bool AllowedForPlayerStaticOnly;
    [ProtoMember(11)] public bool AllowedForUnownedStaticOnly;

    public SerializableGroupSetting() { }

    public SerializableGroupSetting(string name, List<SerialId> defs, bool allowPlayer = true, bool allowPlayerStatic = false, bool allowNPC = true, bool allowNPCStatic = false, bool allowUnowned = true, bool allowUnownedStatic = false, int maxCountPlayer = 0, int maxCountGrid = 0, int maxCountFaction = 0)
    {
      GroupName = name;
      Definitions = defs;
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
