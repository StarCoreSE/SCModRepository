using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParallelTasks;
using System.IO;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;
using Sandbox.Game;
using VRage;
using VRage.Input;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Definitions.GUI;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Collections.Concurrent;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using IMyLargeTurretBase = Sandbox.ModAPI.IMyLargeTurretBase;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using IMyMechanicalConnectionBlock = Sandbox.ModAPI.IMyMechanicalConnectionBlock;
using IMyMotorStator = Sandbox.ModAPI.IMyMotorStator;
using IMyPistonBase = Sandbox.ModAPI.IMyPistonBase;

namespace SC_BlockRestrictions
{
  public class Entity
  {
    public HashSet<IMyCubeGrid> GridsToReallocate;
    public GridCollection GridCollection;
    public MyCubeSize GridSize;
    public bool NeedsBaseGridCheck, NeedsGridCheck, NeedsOwnerUpdate, NeedsBlockOwnerUpdate, NeedsCreativeRecalc, CloseAfterRedistrubution, FactionUpdateCheck;
    public bool Unowned, NPCOwned, PlayerOwned;
    public long EntityId, OwnerId, FactionId;

    public bool AdminOwned => IsAdminOwned();
    public bool IsStatic => GridCollection?.IsStatic ?? false;
    public bool MarkedForClose => !(GridCollection?.CubeGridHash?.Count > 0);

    SC_BlockRestrictions _mod;
    Dictionary<MyDefinitionId, int> _gridBlockDict;
    Dictionary<string, int> _gridGroupDict;
    HashSet<IMyCubeGrid> _gridsToAdd, _gridHash;
    List<IMyTerminalBlock> _blockQueue;
    List<MyEntity> _boxList;

    StringBuilder _debug;
    Logger _logger;

    public Entity(MyCubeGrid grid, List<MyEntity> boxList, HashSet<IMyCubeGrid> gridHash, HashSet<IMyCubeGrid> gridAddHash, SC_BlockRestrictions mod, StringBuilder debug = null, Logger logger = null)
    {
      _mod = mod;
      _boxList = boxList;
      _gridHash = gridHash;
      _gridsToAdd = gridAddHash;
      _debug = debug;
      _logger = logger;

      _gridBlockDict = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
      _gridGroupDict = new Dictionary<string, int>();
      _blockQueue = new List<IMyTerminalBlock>();
      GridsToReallocate = new HashSet<IMyCubeGrid>();
      GridCollection = new GridCollection(grid);
      Utilities.GetConnectedGrids(grid, GridCollection.CubeGridHash);
      GridSize = grid.GridSizeEnum;
      SetupGridEvents();
      UpdateOwner();
      EntityId = grid.EntityId;

      foreach (var cubeGrid in GridCollection.CubeGridHash)
      {
        var myGrid = cubeGrid as MyCubeGrid;

        bool _;
        AddGrid(myGrid, out _);

        foreach (var block in myGrid.GetFatBlocks())
        {
          var terminal = block as IMyTerminalBlock;
          if (terminal == null)
            continue;

          AddBlockToCreativeDict(terminal);
        }
      }
    }

    public void Close()
    {
      if (GridsToReallocate != null)
      {
        foreach (var grid in GridsToReallocate)
          CloseGrid((MyCubeGrid)grid);
      }

      if (GridCollection?.CubeGridHash != null)
      {
        foreach (var grid in GridCollection.CubeGridHash)
          CloseGrid((MyCubeGrid)grid);
      }

      GridCollection?.Close();
      GridsToReallocate?.Clear();
      _gridBlockDict?.Clear();
      _gridGroupDict?.Clear();
      _blockQueue?.Clear();

      GridsToReallocate = null;
      GridCollection = null;
      _gridBlockDict = null;
      _gridGroupDict = null;
      _blockQueue = null;
    }

    void SetupGridEvents()
    {
      if (GridCollection?.CubeGridHash == null)
        return;

      foreach (var grid in GridCollection.CubeGridHash)
        SetupGrid((MyCubeGrid)grid);
    }

    void SetupGrid(MyCubeGrid grid)
    {
      if (grid == null)
        return;

      CloseGrid(grid);
      grid.OnHierarchyUpdated += OnHierarchyUpdated;
      grid.OnBlockAdded += OnBlockAdded;
      grid.OnBlockRemoved += OnBlockRemoved;
      grid.OnBlockOwnershipChanged += OnOwnershipChanged;
      grid.OnStaticChanged += OnStaticChanged;
      grid.OnGridSplit += OnGridSplit;
      grid.OnClosing += OnGridClosing;
    }

    private void OnGridClosing(MyEntity obj)
    {
      var grid = obj as MyCubeGrid;
      if (grid == null)
        return;

      ResetBlocks(grid);
      GridCollection?.CubeGridHash?.Remove(grid);
      NeedsGridCheck = true;
    }

    void CloseGrid(MyCubeGrid grid)
    {
      if (grid == null)
        return;

      grid.OnHierarchyUpdated -= OnHierarchyUpdated;
      grid.OnBlockAdded -= OnBlockAdded;
      grid.OnBlockRemoved -= OnBlockRemoved;
      grid.OnBlockOwnershipChanged -= OnOwnershipChanged;
      grid.OnStaticChanged -= OnStaticChanged;
      grid.OnGridSplit -= OnGridSplit;
      grid.OnClosing -= OnGridClosing;
    }

    private void OnGridSplit(MyCubeGrid arg1, MyCubeGrid arg2)
    {
      NeedsGridCheck = true;
    }

    private void OnStaticChanged(MyCubeGrid grid, bool isStatic)
    {
      NeedsGridCheck = true;
    }

    void OnHierarchyUpdated(MyCubeGrid obj)
    {
      NeedsGridCheck = true;
    }

    private void OnOwnershipChanged(MyCubeGrid obj)
    {
      NeedsOwnerUpdate = true;
    }

    private void OnBlockRemoved(IMySlimBlock obj)
    {
      var cubeGrid = obj.CubeGrid;
      if (cubeGrid == GridCollection?.BaseGrid)
        NeedsBaseGridCheck = true;

      var terminal = obj?.FatBlock as IMyTerminalBlock;
      if (terminal == null || !_mod.BlockTypeDict.ContainsKey(terminal.BlockDefinition))
        return;

      string name;
      GroupSetting groupSetting;
      if (_mod.DefinitionGroupMap.TryGetValue(terminal.BlockDefinition, out name) && _mod.GroupSettingsDict.TryGetValue(name, out groupSetting))
      {
        RemoveBlockForGroup(groupSetting, terminal);
        return;
      }

      BlockSetting blockSetting;
      if (!_mod.SettingsDict.TryGetValue(terminal.BlockDefinition, out blockSetting))
        return;

      if (blockSetting.PlayerMaxCount == 0 && blockSetting.GridMaxCount == 0 && blockSetting.FactionMaxCount == 0)
        return;

      int count;

      if (_mod.BlockOwnerDictGrid.Remove(terminal.EntityId) && blockSetting.GridMaxCount > 0)
      {
        if (_gridBlockDict.TryGetValue(terminal.BlockDefinition, out count))
        {
          count--;
          _gridBlockDict[terminal.BlockDefinition] = Math.Max(0, count);
        }
      }

      if (OwnerId == 0 || !_mod.BlockOwnerDictPlayer.Remove(terminal.EntityId))
        return;

      Dictionary<MyDefinitionId, int> blockDict;
      if (blockSetting.PlayerMaxCount > 0)
      {
        if (_mod.PlayerBlockDict.TryGetValue(OwnerId, out blockDict) 
          && blockDict.TryGetValue(terminal.BlockDefinition, out count))
        {
          count--;
          blockDict[terminal.BlockDefinition] = Math.Max(0, count);
        }
      }

      if (blockSetting.FactionMaxCount > 0 && FactionId != 0)
      {
        if (_mod.BlockFactionDict.Remove(terminal.EntityId)
          && _mod.FactionBlockDict.TryGetValue(FactionId, out blockDict) 
          && blockDict.TryGetValue(terminal.BlockDefinition, out count))
        {
          count--;
          blockDict[terminal.BlockDefinition] = Math.Max(0, count);
        }
      }
    }

    void RemoveBlockForGroup(GroupSetting groupSetting, IMyTerminalBlock terminal, bool force = false)
    {
      int count;
      var groupName = groupSetting.GroupName;

      if (_mod.BlockOwnerDictGrid.Remove(terminal.EntityId) && groupSetting.GridMaxCount > 0)
      {
        if (_gridGroupDict.TryGetValue(groupName, out count))
        {
          count--;
          _gridGroupDict[groupName] = Math.Max(0, count);
        }
      }

      if (OwnerId == 0 || (!force && !_mod.BlockOwnerDictPlayer.Remove(terminal.EntityId)))
        return;

      Dictionary<string, int> blockDict;
      if (groupSetting.PlayerMaxCount > 0)
      {
        if (_mod.PlayerGroupDict.TryGetValue(OwnerId, out blockDict)
          && blockDict.TryGetValue(groupName, out count))
        {
          count--;
          blockDict[groupName] = Math.Max(0, count);
        }
      }

      if (groupSetting.FactionMaxCount > 0 && FactionId != 0)
      {
        if (_mod.BlockFactionDict.Remove(terminal.EntityId)
          && _mod.FactionGroupDict.TryGetValue(FactionId, out blockDict)
          && blockDict.TryGetValue(groupName, out count))
        {
          count--;
          blockDict[groupName] = Math.Max(0, count);
        }
      }
    }

    void AddBlockToCreativeDict(IMyTerminalBlock block)
    {
      bool creativeMode;
      var builder = block.SlimBlock.BuiltBy;
      var steamId = MyAPIGateway.Players.TryGetSteamId(builder);
      if (steamId > 0 && _mod.PlayerCreativeDict.TryGetValue(steamId, out creativeMode) && creativeMode)
        _mod.BlocksBuiltWithCreativeDict.Add(block.EntityId);
    }

    private void OnBlockAdded(IMySlimBlock obj)
    {
      var cubeGrid = (MyCubeGrid)obj.CubeGrid;
      var baseGrid = GridCollection.BaseGrid;
      if (cubeGrid.GridSize >= baseGrid.GridSize && cubeGrid.BlocksCount > baseGrid.BlocksCount)
        NeedsBaseGridCheck = true;

      var terminal = obj?.FatBlock as IMyTerminalBlock;
      if (terminal == null || terminal.MarkedForClose || !_mod.BlockTypeDict.ContainsKey(terminal.BlockDefinition))
        return;

      AddBlockToCreativeDict(terminal);

      if (!_blockQueue.Contains(terminal))
        _blockQueue.Add(terminal);
    }

    public void Update()
    {
      try
      {
        if (MarkedForClose)
          return;

        if (NeedsBaseGridCheck)
        {
          NeedsBaseGridCheck = false;

          foreach (var grid in GridCollection.CubeGridHash)
          {
            var oldId = EntityId;

            if (CompareGrid((MyCubeGrid)grid))
            {
              Entity _;
              _mod.EntityDict.TryRemove(oldId, out _);
              _mod.EntityDict.TryAdd(EntityId, this);
            }
          }
        }

        if (NeedsOwnerUpdate)
          UpdateOwner();

        if (NeedsBlockOwnerUpdate)
        {
          NeedsBlockOwnerUpdate = false;

          foreach (var grid in GridCollection.CubeGridHash)
            AddBlocksToQueue((MyCubeGrid)grid);
        }

        if (NeedsGridCheck)
          CheckGrids();

        if (_blockQueue.Count > 0)
        {
          bool copyPasteEnabled = false;
          if (PlayerOwned)
          {
            var steamId = MyAPIGateway.Players.TryGetSteamId(OwnerId);
            if (steamId > 0)
              _mod.PlayerCopyPasteDict.TryGetValue(steamId, out copyPasteEnabled);
          }

          bool creativeAllowed = copyPasteEnabled && _mod.ModSaveData.CreativeModeAllowed;

          for (int i = _blockQueue.Count - 1; i >= 0; i--)
          {
            var terminal = _blockQueue[i];
            _blockQueue.RemoveAtFast(i);

            if (terminal == null || terminal.MarkedForClose)
              continue;

            var definition = terminal.BlockDefinition;
            MyCubeBlockDefinition cubeDef;
            if (!_mod.BlockTypeDict.TryGetValue(definition, out cubeDef))
              continue;

            string groupName = null;
            GroupSetting groupSetting = null;
            if (_mod.DefinitionGroupMap.TryGetValue(definition, out groupName) && _mod.GroupSettingsDict.TryGetValue(groupName, out groupSetting))
            {
              HandleGroupDefinition(groupSetting, terminal, creativeAllowed);
              continue;
            }

            BlockSetting blockSetting = null;
            if (!_mod.SettingsDict.TryGetValue(definition, out blockSetting))
            {
              bool ownershipEnabled = cubeDef.ContainsComputer();
              blockSetting = new BlockSetting(definition, true, false, true, false, true, false, ownershipEnabled);
              _mod.AddBlockSetting(definition, blockSetting);
            }

            bool shouldRemove = false;
            bool newBlock = true;
            bool okayToRemove = false;
            bool newGridBlock = false;
            bool factionChecked = false;
            bool factionSwitch = false;
            int playerCount = 0, gridCount = 0, factionCount = 0;
            long prevOwnerId, prevFactionId = 0;
            Dictionary<MyDefinitionId, int> playerDict = null;
            Dictionary<MyDefinitionId, int> factionDict = null;

            if (!_mod.BlockOwnerDictPlayer.TryGetValue(terminal.EntityId, out prevOwnerId))
            {
              newGridBlock = true;
              okayToRemove = true;
              _mod.BlockOwnerDictPlayer[terminal.EntityId] = OwnerId;
              _mod.BlockOwnerDictGrid[terminal.EntityId] = OwnerId;
            }
            else if (prevOwnerId != OwnerId)
            {
              okayToRemove = true;
              _mod.BlockOwnerDictPlayer[terminal.EntityId] = OwnerId;
              _mod.BlockOwnerDictGrid[terminal.EntityId] = OwnerId;

              if (blockSetting.PlayerMaxCount > 0 && _mod.PlayerBlockDict.TryGetValue(prevOwnerId, out playerDict) && playerDict.TryGetValue(definition, out playerCount))
              {
                playerCount--;
                playerDict[definition] = Math.Max(0, playerCount);
              }

              if (blockSetting.FactionMaxCount > 0)
              {
                if (_mod.BlockFactionDict.TryGetValue(terminal.EntityId, out prevFactionId))
                {
                  factionSwitch = prevFactionId != FactionId;
                  factionChecked = true;

                  if (_mod.FactionBlockDict.TryGetValue(prevFactionId, out factionDict) && factionDict.TryGetValue(definition, out factionCount))
                  {
                    factionCount--;
                    factionDict[definition] = Math.Max(0, factionCount);
                  }
                }
              }
            }
            else if (!NeedsCreativeRecalc)
              newBlock = false;

            if (!factionChecked && blockSetting.FactionMaxCount > 0)
            {
              _mod.BlockFactionDict.TryGetValue(terminal.EntityId, out prevFactionId);
              factionSwitch = prevFactionId != FactionId;

              if (_mod.FactionBlockDict.TryGetValue(prevFactionId, out factionDict) && factionDict.TryGetValue(definition, out factionCount))
              {
                factionCount--;
                factionDict[definition] = Math.Max(0, factionCount);
              }
            }

            if (Unowned)
            {
              string msg = null;
              if (!blockSetting.AllowedForUnowned)
              {
                shouldRemove = true;
                msg = $"Unowned grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
              }
              else if (blockSetting.AllowedForUnownedStaticOnly && !IsStatic)
              {
                shouldRemove = true;
                msg = $"Static unowned grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
              }

              if (shouldRemove)
              {
                ulong steamId;
                if (terminal.SlimBlock.BuiltBy.IsPlayer(out steamId))
                {
                  var packet = new Packet(msg);
                  _mod.Network.SendToPlayer(steamId, packet);
                }
              }
            }
            else if (PlayerOwned && !creativeAllowed)
            {
              if (!blockSetting.AllowedForPlayer)
              {
                shouldRemove = true;
                _mod.Network.SendToPlayer(OwnerId, $"Players are not allowed to use {terminal.DefinitionDisplayNameText}.");
              }
              else if (blockSetting.AllowedForPlayerStaticOnly)
              {
                shouldRemove = !IsStatic;

                if (shouldRemove)
                  _mod.Network.SendToPlayer(OwnerId, $"Players may only place {terminal.DefinitionDisplayNameText} on a static grid.");
              }
            }
            else if (NPCOwned)
            {
              string msg = null;
              if (!blockSetting.AllowedForNPC)
              {
                shouldRemove = true;
                msg = $"NPC grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
              }
              else if (blockSetting.AllowedForNPCStaticOnly && !IsStatic)
              {
                shouldRemove = true;
                msg = $"Static NPC grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
              }

              if (shouldRemove)
              {
                ulong steamId;
                if (terminal.SlimBlock.BuiltBy.IsPlayer(out steamId))
                {
                  var packet = new Packet(msg);
                  _mod.Network.SendToPlayer(steamId, packet);
                }
              }
            }

            playerDict = factionDict = null;
            bool addedToPlayer = false;
            bool removedFromPlayer = false;
            bool addedToGrid = false;

            if (!shouldRemove && newBlock && PlayerOwned && blockSetting.PlayerMaxCount > 0)
            {
              if (!_mod.PlayerBlockDict.TryGetValue(OwnerId, out playerDict))
              {
                playerDict = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
                _mod.PlayerBlockDict[OwnerId] = playerDict;
              }

              playerDict.TryGetValue(definition, out playerCount);
              playerCount++;

              if (!creativeAllowed && playerCount > blockSetting.PlayerMaxCount)
              {
                shouldRemove = true;
                removedFromPlayer = true;
                _mod.Network.SendToPlayer(OwnerId, $"Players cannot have more than {blockSetting.PlayerMaxCount} of {terminal.DefinitionDisplayNameText}.");
              }
              else
              {
                addedToPlayer = true;
                playerDict[definition] = playerCount;
              }
            }

            if (!shouldRemove && newGridBlock && blockSetting.GridMaxCount > 0)
            {
              _gridBlockDict.TryGetValue(definition, out gridCount);
              gridCount++;

              if (gridCount > blockSetting.GridMaxCount)
              {
                shouldRemove = true;

                if (addedToPlayer)
                {
                  addedToPlayer = false;
                  removedFromPlayer = true;
                  int count;
                  playerDict.TryGetValue(definition, out count);
                  playerDict[definition] = Math.Max(0, count - 1);
                }

                if (PlayerOwned)
                  _mod.Network.SendToPlayer(OwnerId, $"Grids cannot have more than {blockSetting.GridMaxCount} of {terminal.DefinitionDisplayNameText}.");
              }
              else
              {
                addedToGrid = true;
                _gridBlockDict[definition] = gridCount;
              }
            }

            if (!shouldRemove && (newBlock || factionSwitch) && !Unowned && FactionId != 0 && blockSetting.FactionMaxCount > 0)
            {
              if (!_mod.FactionBlockDict.TryGetValue(FactionId, out factionDict))
              {
                factionDict = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
                _mod.FactionBlockDict[FactionId] = factionDict;
              }

              factionDict.TryGetValue(definition, out factionCount);
              factionCount++;

              if (factionCount > blockSetting.FactionMaxCount)
              {
                shouldRemove = true;
                int count;

                if (!removedFromPlayer && (addedToPlayer || (FactionUpdateCheck && _mod.PlayerBlockDict.TryGetValue(OwnerId, out playerDict))))
                {
                  addedToPlayer = false;
                  removedFromPlayer = true;
                  playerDict.TryGetValue(definition, out count);
                  playerDict[definition] = Math.Max(0, count - 1);
                }

                if (addedToGrid || FactionUpdateCheck)
                {
                  addedToGrid = false;
                  _gridBlockDict.TryGetValue(definition, out count);
                  _gridBlockDict[definition] = Math.Max(0, count - 1);
                }

                if (PlayerOwned)
                  _mod.Network.SendToPlayer(OwnerId, $"Factions cannot have more than {blockSetting.FactionMaxCount} of {terminal.DefinitionDisplayNameText}.");
              }
              else
              {
                _mod.BlockFactionDict[terminal.EntityId] = FactionId;
                factionDict[definition] = factionCount;
              }
            }

            if (shouldRemove)
            {
              if (removedFromPlayer)
                _mod.BlockOwnerDictPlayer.Remove(terminal.EntityId);

              if (okayToRemove)
                _mod.BlockOwnerDictGrid.Remove(terminal.EntityId);

              var ownerKVP = new KeyValuePair<bool, long>(PlayerOwned, OwnerId);
              _mod.RemovalQueue.Enqueue(new KeyValuePair<IMyTerminalBlock, KeyValuePair<bool, long>>(terminal, ownerKVP));
            }
          }

          FactionUpdateCheck = false;
          NeedsCreativeRecalc = false;
        }
      }
      catch (Exception e)
      {
        _logger?.Log($"Exception during Update for '{GridCollection?.BaseGrid?.DisplayName ?? "(null)"} with ID {EntityId}'\nDebug output:\n{_debug.ToString()}\n\n{e.Message}\n\n{e.StackTrace}");
      }
    }

    void HandleGroupDefinition(GroupSetting groupSetting, IMyTerminalBlock terminal, bool creativeAllowed)
    {
      var groupName = groupSetting.GroupName;
      bool shouldRemove = false;
      bool newBlock = true;
      bool okayToRemove = false;
      bool newGridBlock = false;
      bool factionChecked = false;
      bool factionSwitch = false;
      int playerCount = 0, gridCount = 0, factionCount = 0;
      long prevOwnerId, prevFactionId = 0;
      Dictionary<string, int> playerDict;
      Dictionary<string, int> factionDict;

      if (!_mod.BlockOwnerDictPlayer.TryGetValue(terminal.EntityId, out prevOwnerId))
      {
        newGridBlock = true;
        okayToRemove = true;
        _mod.BlockOwnerDictPlayer[terminal.EntityId] = OwnerId;
        _mod.BlockOwnerDictGrid[terminal.EntityId] = OwnerId;
      }
      else if (prevOwnerId != OwnerId)
      {
        okayToRemove = true;
        _mod.BlockOwnerDictPlayer[terminal.EntityId] = OwnerId;
        _mod.BlockOwnerDictGrid[terminal.EntityId] = OwnerId;

        if (groupSetting.PlayerMaxCount > 0 && _mod.PlayerGroupDict.TryGetValue(prevOwnerId, out playerDict) && playerDict.TryGetValue(groupName, out playerCount))
        {
          playerCount--;
          playerDict[groupName] = Math.Max(0, playerCount);
        }

        if (groupSetting.FactionMaxCount > 0)
        {
          if (_mod.BlockFactionDict.TryGetValue(terminal.EntityId, out prevFactionId))
          {
            factionSwitch = prevFactionId != FactionId;
            factionChecked = true;

            if (_mod.FactionGroupDict.TryGetValue(prevFactionId, out factionDict) && factionDict.TryGetValue(groupName, out factionCount))
            {
              factionCount--;
              factionDict[groupName] = Math.Max(0, factionCount);
            }
          }
        }
      }
      else if (!NeedsCreativeRecalc)
        newBlock = false;

      if (!factionChecked && groupSetting.FactionMaxCount > 0)
      {
        _mod.BlockFactionDict.TryGetValue(terminal.EntityId, out prevFactionId);
        factionSwitch = prevFactionId != FactionId;

        if (_mod.FactionGroupDict.TryGetValue(prevFactionId, out factionDict) && factionDict.TryGetValue(groupName, out factionCount))
        {
          factionCount--;
          factionDict[groupName] = Math.Max(0, factionCount);
        }
      }

      if (Unowned)
      {
        string msg = null;
        if (!groupSetting.AllowedForUnowned)
        {
          shouldRemove = true;
          msg = $"Unowned grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
        }
        else if (groupSetting.AllowedForUnownedStaticOnly && !IsStatic)
        {
          shouldRemove = true;
          msg = $"Static unowned grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
        }

        if (shouldRemove)
        {
          ulong steamId;
          if (terminal.SlimBlock.BuiltBy.IsPlayer(out steamId))
          {
            var packet = new Packet(msg);
            _mod.Network.SendToPlayer(steamId, packet);
          }
        }
      }
      else if (PlayerOwned && !creativeAllowed)
      {
        if (!groupSetting.AllowedForPlayer)
        {
          shouldRemove = true;
          _mod.Network.SendToPlayer(OwnerId, $"Players are not allowed to use {terminal.DefinitionDisplayNameText}.");
        }
        else if (groupSetting.AllowedForPlayerStaticOnly)
        {
          shouldRemove = !IsStatic;

          if (shouldRemove)
            _mod.Network.SendToPlayer(OwnerId, $"Players may only place {terminal.DefinitionDisplayNameText} on a static grid.");
        }
      }
      else if (NPCOwned)
      {
        string msg = null;
        if (!groupSetting.AllowedForNPC)
        {
          shouldRemove = true;
          msg = $"NPC grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
        }
        else if (groupSetting.AllowedForNPCStaticOnly && !IsStatic)
        {
          shouldRemove = true;
          msg = $"Static NPC grids are not allowed to use {terminal.DefinitionDisplayNameText}.";
        }

        if (shouldRemove)
        {
          ulong steamId;
          if (terminal.SlimBlock.BuiltBy.IsPlayer(out steamId))
          {
            var packet = new Packet(msg);
            _mod.Network.SendToPlayer(steamId, packet);
          }
        }
      }

      playerDict = factionDict = null;
      bool addedToPlayer = false;
      bool removedFromPlayer = false;
      bool addedToGrid = false;

      if (!shouldRemove && newBlock && PlayerOwned && groupSetting.PlayerMaxCount > 0)
      {
        if (!_mod.PlayerGroupDict.TryGetValue(OwnerId, out playerDict))
        {
          playerDict = new Dictionary<string, int>();
          _mod.PlayerGroupDict[OwnerId] = playerDict;
        }

        playerDict.TryGetValue(groupName, out playerCount);
        playerCount++;

        if (!creativeAllowed && playerCount > groupSetting.PlayerMaxCount)
        {
          shouldRemove = true;
          removedFromPlayer = true;
          _mod.Network.SendToPlayer(OwnerId, $"Players cannot have more than {groupSetting.PlayerMaxCount} from the {groupSetting.GroupName} group.");
        }
        else
        {
          addedToPlayer = true;
          playerDict[groupName] = playerCount;
        }
      }

      if (!shouldRemove && newGridBlock && groupSetting.GridMaxCount > 0)
      {
        bool found = _gridGroupDict.TryGetValue(groupName, out gridCount);
        gridCount++;

        if (gridCount > groupSetting.GridMaxCount)
        {
          shouldRemove = true;

          if (addedToPlayer)
          {
            addedToPlayer = false;
            removedFromPlayer = true;
            int count;
            playerDict.TryGetValue(groupName, out count);
            playerDict[groupName] = Math.Max(0, count - 1);
          }

          if (PlayerOwned)
            _mod.Network.SendToPlayer(OwnerId, $"Grids cannot have more than {groupSetting.GridMaxCount} from the {groupSetting.GroupName} group.");
        }
        else
        {
          addedToGrid = true;
          _gridGroupDict[groupName] = gridCount;
        }
      }

      if (!shouldRemove && (newBlock || factionSwitch) && !Unowned && FactionId != 0 && groupSetting.FactionMaxCount > 0)
      {
        if (!_mod.FactionGroupDict.TryGetValue(FactionId, out factionDict))
        {
          factionDict = new Dictionary<string, int>();
          _mod.FactionGroupDict[FactionId] = factionDict;
        }

        factionDict.TryGetValue(groupName, out factionCount);
        factionCount++;

        if (factionCount > groupSetting.FactionMaxCount)
        {
          shouldRemove = true;
          int count;

          if (!removedFromPlayer && (addedToPlayer || (FactionUpdateCheck && _mod.PlayerGroupDict.TryGetValue(OwnerId, out playerDict))))
          {
            addedToPlayer = false;
            removedFromPlayer = true;
            playerDict.TryGetValue(groupName, out count);
            playerDict[groupName] = Math.Max(0, count - 1);
          }

          if (addedToGrid || FactionUpdateCheck)
          {
            addedToGrid = false;
            _gridGroupDict.TryGetValue(groupName, out count);
            _gridGroupDict[groupName] = Math.Max(0, count - 1);
          }

          if (PlayerOwned)
            _mod.Network.SendToPlayer(OwnerId, $"Factions cannot have more than {groupSetting.FactionMaxCount} from the {groupSetting.GroupName} group.");
        }
        else
        {
          _mod.BlockFactionDict[terminal.EntityId] = FactionId;
          factionDict[groupName] = factionCount;
        }
      }

      if (shouldRemove)
      {
        if (removedFromPlayer)
          _mod.BlockOwnerDictPlayer.Remove(terminal.EntityId);

        if (okayToRemove)
          _mod.BlockOwnerDictGrid.Remove(terminal.EntityId);

        var ownerKVP = new KeyValuePair<bool, long>(PlayerOwned, OwnerId);
        _mod.RemovalQueue.Enqueue(new KeyValuePair<IMyTerminalBlock, KeyValuePair<bool, long>>(terminal, ownerKVP));
      }
    }

    public void CheckGrids()
    {
      NeedsGridCheck = false;
      _gridsToAdd.Clear();
      GridsToReallocate.Clear();

      if (GridCollection.BaseGrid == null)
      {
        GridsToReallocate.UnionWith(GridCollection.CubeGridHash);
        CloseAfterRedistrubution = true;
      }
      else
      {
        Utilities.GetConnectedGrids(GridCollection?.BaseGrid, _gridHash);

        _gridsToAdd.UnionWith(_gridHash);
        _gridsToAdd.ExceptWith(GridCollection.CubeGridHash);
        GridsToReallocate.UnionWith(GridCollection.CubeGridHash);
        GridsToReallocate.ExceptWith(_gridHash);
      }

      if (_gridsToAdd.Count == 0 && GridsToReallocate.Count == 0)
        return;

      bool update = false;
      var gridId = EntityId;
      foreach (var grid in _gridsToAdd)
      {
        Entity ent;
        if (_mod.EntityDict.TryRemove(grid.EntityId, out ent))
          ent.Close();

        bool changed;
        AddGrid((MyCubeGrid)grid, out changed);

        if (changed)
          update = true;
      }

      if (update)
      {
        Entity _;
        _mod.EntityDict.TryAdd(EntityId, this);
        _mod.EntityDict.TryRemove(gridId, out _);
      }
      
      foreach (var grid in GridsToReallocate)
      {
        var cubeGrid = grid as MyCubeGrid;
        CloseGrid(cubeGrid);
        ResetBlocks(cubeGrid);
      }

      GridCollection.CubeGridHash.Clear();
      GridCollection.CubeGridHash.UnionWith(_gridHash);

      _gridHash.Clear();
      _gridsToAdd.Clear();
    }

    void ResetBlocks(MyCubeGrid grid)
    {
      foreach (var block in grid.GetFatBlocks())
      {
        var terminal = block as IMyTerminalBlock;
        if (terminal == null)
          continue;

        var idx = _blockQueue?.IndexOf(terminal) ?? -1;
        if (idx >= 0)
          _blockQueue.RemoveAtFast(idx);

        long blockOwner;
        if (!_mod.BlockOwnerDictPlayer.TryGetValue(terminal.EntityId, out blockOwner))
          continue;

        _mod.BlockOwnerDictPlayer.Remove(terminal.EntityId);

        string name;
        GroupSetting groupSetting;
        if (_mod.DefinitionGroupMap.TryGetValue(terminal.BlockDefinition, out name) && _mod.GroupSettingsDict.TryGetValue(name, out groupSetting))
        {
          RemoveBlockForGroup(groupSetting, terminal, true);
          continue;
        }

        BlockSetting blockSetting;
        if (!_mod.SettingsDict.TryGetValue(terminal.BlockDefinition, out blockSetting))
          continue;

        if (blockSetting.PlayerMaxCount == 0 && blockSetting.GridMaxCount == 0 && blockSetting.FactionMaxCount == 0)
          continue;

        int count;
        if (blockSetting.GridMaxCount > 0)
        {
          if (_gridBlockDict.TryGetValue(terminal.BlockDefinition, out count))
            _gridBlockDict[terminal.BlockDefinition] = Math.Max(0, count - 1);
        }

        if (blockOwner == 0)
          continue;

        Dictionary<MyDefinitionId, int> blockDict;
        if (blockSetting.PlayerMaxCount > 0)
        {
          if (_mod.PlayerBlockDict.TryGetValue(blockOwner, out blockDict))
          {
            if (blockDict.TryGetValue(terminal.BlockDefinition, out count))
              blockDict[terminal.BlockDefinition] = Math.Max(0, count - 1);
          }
        }

        if (blockSetting.FactionMaxCount > 0)
        {
          var factionId = MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(blockOwner)?.FactionId ?? 0;
          if (_mod.FactionBlockDict.TryGetValue(factionId, out blockDict))
          {
            if (blockDict.TryGetValue(terminal.BlockDefinition, out count))
              blockDict[terminal.BlockDefinition] = Math.Max(0, count - 1);
          }
        }
      }
    }

    public void AddGrid(MyCubeGrid grid, out bool gridChanged)
    {
      gridChanged = false;
      if (grid != null && grid == GridCollection?.BaseGrid)
        return;

      GridCollection.AddGrid(grid);
      SetupGrid(grid);
      gridChanged = CompareGrid(grid);

      if (gridChanged)
      {
        foreach (var cubeGrid in GridCollection.CubeGridHash)
          AddBlocksToQueue((MyCubeGrid)cubeGrid);
      }
      else
        AddBlocksToQueue(grid);
    }

    void AddBlocksToQueue(MyCubeGrid grid)
    {
      if (grid == null || grid.MarkedAsTrash || grid.MarkedForClose)
        return;

      foreach (var block in grid.GetFatBlocks())
      {
        var terminal = block as IMyTerminalBlock;
        if (terminal == null || terminal.MarkedForClose || _blockQueue.Contains(terminal))
          continue;

        _blockQueue.Add(terminal);
      }
    }

    bool CompareGrid(MyCubeGrid gridToCompare)
    {
      if (gridToCompare == GridCollection.BaseGrid)
        return false;

      var gridChanged = false;
      var newGridHasOwner = gridToCompare.BigOwners.Count > 0 || gridToCompare.SmallOwners.Count > 0;

      if (Unowned || newGridHasOwner)
      {
        if (gridToCompare.GridSize > GridCollection.BaseGrid.GridSize)
          gridChanged = true;
        else if (GridCollection.BaseGrid.GridSizeEnum == gridToCompare.GridSizeEnum && gridToCompare.BlocksCount > GridCollection.BaseGrid.BlocksCount)
          gridChanged = true;
      }

      if (gridChanged)
      {
        GridSize = gridToCompare.GridSizeEnum;
        EntityId = gridToCompare.EntityId;
        GridCollection.BaseGrid = gridToCompare;
        UpdateOwner();
      }

      return gridChanged;
    }

    public void UpdateOwner()
    {
      NeedsOwnerUpdate = false;

      long newOwner = OwnerId;
      if (GridCollection?.BaseGrid != null)
      {
        var owners = GridCollection.BaseGrid.BigOwners.Count > 0 ? GridCollection.BaseGrid.BigOwners : GridCollection.BaseGrid.SmallOwners;
        newOwner = owners.Count > 0 ? owners[0] : 0;
      }

      if (newOwner != OwnerId)
      {
        NeedsGridCheck = true;
        NeedsBlockOwnerUpdate = true;
      }

      OwnerId = newOwner;
      Unowned = (OwnerId == 0);
      ulong steamId;
      PlayerOwned = !Unowned && OwnerId.IsPlayer(out steamId);
      NPCOwned = !Unowned && !PlayerOwned;
      FactionId = Unowned ? 0 : MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(OwnerId)?.FactionId ?? 0;
    }

    bool IsAdminOwned()
    {
      if (!PlayerOwned)
        return false;

      var steamId = MyAPIGateway.Players.TryGetSteamId(OwnerId);
      return steamId.IsAdmin();
    }
  }
}
