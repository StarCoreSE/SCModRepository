using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ObjectBuilders;

namespace SC_BlockRestrictions
{
  public class BlockSetting : SettingBase
  {
    public MyDefinitionId Type;

    public BlockSetting(MyDefinitionId type, bool allowPlayer, bool allowPlayerStatic, bool allowNPC, bool allowNPCStatic, bool allowUnowned, bool allowUnownedStatic, bool canBeOwned, int maxCountPlayer = 0, int maxCountGrid = 0, int maxCountFaction = 0)
    {
      Type = type;
      OwnershipEnabled = canBeOwned;
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
