using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC_BlockRestrictions.Settings
{
  public class PlayerData
  {
    public ulong SteamId;
    public bool IsAdmin;
    public bool CreativeEnabled;
    public bool CopyPasteEnabled;

    public PlayerData() { }

    public PlayerData(ulong steamId, bool creativeMode, bool copyPaste)
    {
      SteamId = steamId;
      CreativeEnabled = creativeMode;
      CopyPasteEnabled = copyPaste;
    }
  }

  public class PlayerSaveData
  {
    public List<PlayerData> PlayerSettings = new List<PlayerData>();
  }
}
