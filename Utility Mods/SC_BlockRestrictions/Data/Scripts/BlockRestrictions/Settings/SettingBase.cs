using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC_BlockRestrictions
{
  public class SettingBase
  {
    public int PlayerMaxCount;
    public int GridMaxCount;
    public int FactionMaxCount;
    public bool OwnershipEnabled;
    public bool AllowedForNPC;
    public bool AllowedForPlayer;
    public bool AllowedForUnowned;
    public bool AllowedForNPCStaticOnly;
    public bool AllowedForPlayerStaticOnly;
    public bool AllowedForUnownedStaticOnly;
  }
}
