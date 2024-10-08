using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VRage.Game;

namespace SC_BlockRestrictions
{
  public class GroupSetting : SettingBase
  {
    public string GroupName;
    public HashSet<MyDefinitionId> Definitions = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);

    public GroupSetting(string name, List<SerialId> defs, bool allowPlayer = true, bool allowPlayerStatic = false, bool allowNPC = true, bool allowNPCStatic = false, bool allowUnowned = true, bool allowUnownedStatic = false, int maxCountPlayer = 0, int maxCountGrid = 0, int maxCountFaction = 0)
    {
      foreach (var def in defs)
        Definitions.Add(def.DefinitionId);

      GroupName = name;
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

    public override string ToString()
    {
      return $"Group Name: {GroupName}\nGroupSettings:\n  Player Max: {PlayerMaxCount}, Grid Max: {GridMaxCount}, Faction Max: {FactionMaxCount}\n  Allowed for Player: {AllowedForPlayer} (Station only = {AllowedForPlayerStaticOnly})\n  Allowed for NPC: {AllowedForNPC} (Station only = {AllowedForNPCStaticOnly})\n  Allowed for Unowned: {AllowedForUnowned} (Station only = {AllowedForUnownedStaticOnly})";
    }
  }
}
