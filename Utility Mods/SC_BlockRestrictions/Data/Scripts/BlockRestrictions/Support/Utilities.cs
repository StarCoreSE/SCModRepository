using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace SC_BlockRestrictions
{
  public static class Utilities
  {
    public static void CheckCreativeTools(out bool creativeEnabled, out bool copyPasteEnabled)
    {
      copyPasteEnabled = creativeEnabled = false;

      if (MyAPIGateway.Session == null)
        return;

      copyPasteEnabled = MyAPIGateway.Session.EnableCopyPaste;
      creativeEnabled = MyAPIGateway.Session.CreativeMode || (MyAPIGateway.Session.HasCreativeRights && copyPasteEnabled);
    }

    public static void CheckOwnership(IMyTerminalBlock block, bool ownershipEnabled, out bool unowned, out bool playerOwned, out bool adminOwned, out long ownerId, out long factionId, StringBuilder sb = null)
    {
      unowned = playerOwned = adminOwned = false;
      ownerId = block.OwnerId;
      factionId = 0;

      var grid = block.CubeGrid as MyCubeGrid;
      if (grid == null || (ownershipEnabled && block.OwnerId == 0) || (grid.BigOwners.Count == 0 && grid.SmallOwners.Count == 0))
      {
        unowned = true;
        return;
      }

      if (ownershipEnabled || block.OwnerId != 0)
        ownerId = block.OwnerId;
      else if (grid.BigOwners.Count > 0)
        ownerId = grid.BigOwners[0];
      else if (grid.SmallOwners.Count > 0)
        ownerId = grid.SmallOwners[0];

      ulong steamId;
      playerOwned = ownerId.IsPlayer(out steamId);
      adminOwned = playerOwned && steamId.IsAdmin();
      factionId = MyAPIGateway.Session?.Factions.TryGetPlayerFaction(ownerId)?.FactionId ?? 0;
    }

    public static void ReturnComponentsToPlayer(Dictionary<string, int> missingComps, HashSet<string> completedComps, IMyTerminalBlock block, long ownerId, StringBuilder sb = null)
    {
      var definition = block.SlimBlock.BlockDefinition as MyCubeBlockDefinition;
      if (definition == null)
        return;

      missingComps.Clear();
      completedComps.Clear();
      block.SlimBlock.GetMissingComponents(missingComps);

      for (int i = 0; i < definition.Components.Length; i++)
      {
        var comp = definition.Components[i];
        if (comp == null)
          continue;

        var subtype = comp.Definition.Id.SubtypeName;
        if (completedComps.Contains(subtype))
          continue;

        int num, count = 0;
        completedComps.Add(subtype);

        for (int j = i; j < definition.Components.Length; j++)
        {
          var other = definition.Components[j];
          if (other != null && other.Definition.Id.SubtypeName == subtype)
            count += other.Count;
        }

        if (missingComps.TryGetValue(subtype, out num))
        {
          count -= num;
          if (count <= 0)
            continue;
        }

        try
        {
          if (ownerId != 0)
            MyVisualScriptLogicProvider.AddToPlayersInventory(ownerId, comp.Definition.Id, count);
        }
        catch(Exception e)
        {
          sb?.Append($"ERROR in ReturnCompsToPlayer:\n{e.Message}\n{e.StackTrace}\n");
          MyLog.Default.WriteLineAndConsole($"ERROR in SC_BlockRestrictions.RefundComponents! Unable to refund components to {block.OwnerId}:\n{e.Message}\n{e.StackTrace}");
        }
      }
    }

    public static void GetConnectedGrids(IMyCubeGrid baseGrid, HashSet<IMyCubeGrid> gridHash)
    {
      var cubeGrid = (MyCubeGrid)baseGrid;
      GetConnectedGrids(cubeGrid, gridHash);
    }

    public static void GetConnectedGrids(MyCubeGrid baseGrid, HashSet<IMyCubeGrid> gridHash)
    {
      if (gridHash == null)
        gridHash = new HashSet<IMyCubeGrid>();
      else
        gridHash.Clear();

      if (baseGrid == null || baseGrid.MarkedForClose)
        return;

      MyAPIGateway.GridGroups.GetGroup(baseGrid, GridLinkTypeEnum.Mechanical, gridHash);
    }

    public static bool IsPlayer(this long ownerId, out ulong steamId)
    {
      steamId = MyAPIGateway.Players.TryGetSteamId(ownerId);
      return steamId != 0;
    }

    public static bool IsAdmin(this ulong steamId)
    {
      var level = MyAPIGateway.Session.GetUserPromoteLevel(steamId);
      return level == MyPromoteLevel.Owner || level == MyPromoteLevel.Admin;
    }
  }
}
