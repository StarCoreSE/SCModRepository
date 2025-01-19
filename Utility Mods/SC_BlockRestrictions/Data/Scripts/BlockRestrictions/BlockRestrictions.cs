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
using System.Runtime.InteropServices;
using SC_BlockRestrictions.Settings;

namespace SC_BlockRestrictions
{
  [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
  public class SC_BlockRestrictions : MySessionComponentBase
  {
    HashSet<MyObjectBuilderType> _ignoreTypes = new HashSet<MyObjectBuilderType>()
    {
      typeof(MyObjectBuilder_DebugSphere1),
      typeof(MyObjectBuilder_DebugSphere2),
      typeof(MyObjectBuilder_DebugSphere3),
      typeof(MyObjectBuilder_CubeBlock),
      typeof(MyObjectBuilder_LCDPanelsBlock),
    };

    HashSet<MyDefinitionId> _typesThatCannotBeLocked = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer)
    {
      new MyDefinitionId(typeof(MyObjectBuilder_Assembler), "BasicAssembler")
    };

    public ConcurrentDictionary<long, Entity> EntityDict = new ConcurrentDictionary<long, Entity>();
    public HashSet<long> BlocksBuiltWithCreativeDict = new HashSet<long>();
    public Dictionary<long, long> BlockOwnerDictPlayer = new Dictionary<long, long>(); // from block entity id to owner id
    public Dictionary<long, long> BlockOwnerDictGrid = new Dictionary<long, long>(); // from block entity id to grid id
    public Dictionary<long, long> BlockFactionDict = new Dictionary<long, long>(); // from block entity id to faction id
    public Dictionary<ulong, bool> PlayerCreativeDict = new Dictionary<ulong, bool>();
    public Dictionary<ulong, bool> PlayerCopyPasteDict = new Dictionary<ulong, bool>();
    public Dictionary<string, GroupSetting> GroupSettingsDict = new Dictionary<string, GroupSetting>(); // from group name to group setting
    public Dictionary<MyDefinitionId, string> DefinitionGroupMap = new Dictionary<MyDefinitionId, string>(MyDefinitionId.Comparer); // from def to group setting name
    public Dictionary<MyDefinitionId, BlockSetting> SettingsDict = new Dictionary<MyDefinitionId, BlockSetting>(MyDefinitionId.Comparer);
    public Dictionary<MyDefinitionId, MyCubeBlockDefinition> BlockTypeDict = new Dictionary<MyDefinitionId, MyCubeBlockDefinition>(MyDefinitionId.Comparer);
    public Dictionary<long, Dictionary<MyDefinitionId, int>> PlayerBlockDict = new Dictionary<long, Dictionary<MyDefinitionId, int>>();
    public Dictionary<long, Dictionary<string, int>> PlayerGroupDict = new Dictionary<long, Dictionary<string, int>>();
    public Dictionary<long, Dictionary<MyDefinitionId, int>> FactionBlockDict = new Dictionary<long, Dictionary<MyDefinitionId, int>>(); // from faction id to faction block dictionary
    public Dictionary<long, Dictionary<string, int>> FactionGroupDict = new Dictionary<long, Dictionary<string, int>>();

    public Queue<KeyValuePair<IMyTerminalBlock, KeyValuePair<bool, long>>> RemovalQueue = new Queue<KeyValuePair<IMyTerminalBlock, KeyValuePair<bool, long>>>();
    public BlockSaveData ModSaveData;
    public PlayerSaveData PlayerData;
    public Logger Logger;
    public Networking Network;
    public bool IsServer, IsDedicatedServer;

    List<Entity> _newEnts = new List<Entity>();
    List<MyEntity> _boxList = new List<MyEntity>();
    Queue<MyEntity> _entityQueue = new Queue<MyEntity>();
    HashSet<long> _removals = new HashSet<long>();
    HashSet<string> _compHash = new HashSet<string>();
    HashSet<IMyCubeGrid> _gridHash = new HashSet<IMyCubeGrid>();
    HashSet<IMyCubeGrid> _gridAddHash = new HashSet<IMyCubeGrid>();
    HashSet<SerializableDefinitionId> _restrictedDefinitions = new HashSet<SerializableDefinitionId>();
    HashSet<MyDefinitionId> _terminalBlockDefinitions = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
    HashSet<IMyEntity> _startUpEnts = new HashSet<IMyEntity>();
    Dictionary<string, int> _missingComps = new Dictionary<string, int>();
    StringBuilder _debug = new StringBuilder();
    IMyHudNotification hudMsg;
    Task _addTask, _updateTask;
    bool _isSetup, _creativeEnabled, _creativeAllowed, _copyPasteEnabled, _updateConfig, _researchFrameworkInstalled;
    int _simCount = 0;

    protected override void UnloadData()
    {
      try
      {
        //MyEntities.OnEntityCreate -= MyEntities_OnEntityCreate;
        MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;
        MyEntities.OnEntityRemove -= MyEntities_OnEntityRemove;
        MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;

        if (MyAPIGateway.Session?.Factions != null)
        {
          MyAPIGateway.Session.Factions.FactionStateChanged -= OnFactionStateChanged;
          MyAPIGateway.Session.Factions.FactionCreated -= OnFactionCreated;
        }

        if (EntityDict != null)
        {
          foreach (var ent in EntityDict.Values)
            ent?.Close();
        }

        Logger?.Close();
        Network?.Unregister();
        ModSaveData?.Close();
        _debug?.Clear();
        _ignoreTypes?.Clear();
        _boxList?.Clear();
        _gridHash?.Clear();
        _startUpEnts?.Clear();
        _missingComps?.Clear();
        _compHash?.Clear();
        _removals?.Clear();
        _entityQueue?.Clear();
        _restrictedDefinitions?.Clear();
        _terminalBlockDefinitions?.Clear();
        _typesThatCannotBeLocked?.Clear();
        _cli?.Clear();
        _newEnts?.Clear();
        _gridAddHash?.Clear();
        RemovalQueue?.Clear();
        PlayerBlockDict?.Clear();
        PlayerGroupDict?.Clear();
        FactionBlockDict?.Clear();
        FactionGroupDict?.Clear();
        SettingsDict?.Clear();
        BlockTypeDict?.Clear();
        EntityDict?.Clear();
        BlockOwnerDictPlayer?.Clear();
        GroupSettingsDict?.Clear();

        ModSaveData = null;
        _debug = null;
        _ignoreTypes = null;
        _boxList = null;
        _gridHash = null;
        _startUpEnts = null;
        _missingComps = null;
        _compHash = null;
        _removals = null;
        _entityQueue = null;
        _restrictedDefinitions = null;
        _terminalBlockDefinitions = null;
        _typesThatCannotBeLocked = null;
        _cli = null;
        _newEnts = null;
        _gridAddHash = null;
        RemovalQueue = null;
        PlayerBlockDict = null;
        PlayerGroupDict = null;
        FactionBlockDict = null;
        FactionGroupDict = null;
        BlockTypeDict = null;
        SettingsDict = null;
        EntityDict = null;
        BlockOwnerDictPlayer = null;
        GroupSettingsDict = null;
        Logger = null;
      }
      finally
      {
        base.UnloadData();
      }
    }

    void Init()
    {
      _isSetup = true;

      foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
      {
        var cubeDef = def as MyCubeBlockDefinition;
        if (cubeDef == null)
          continue;

        var typeId = cubeDef.Id;
        if (_ignoreTypes.Contains(typeId.TypeId))
          continue;

        var ob = MyObjectBuilderSerializer.CreateNewObject(typeId);
        if (!(ob is MyObjectBuilder_TerminalBlock))
          continue;

        _terminalBlockDefinitions.Add(cubeDef.Id);

        if (!BlockTypeDict.ContainsKey(typeId))
          BlockTypeDict[typeId] = cubeDef;
      }

      ModSaveData = Config.ReadFileFromWorldStorage<BlockSaveData>("SC_BlockRestrictions.cfg", typeof(BlockSaveData), Logger) ?? new BlockSaveData();
      PlayerData = Config.ReadFileFromWorldStorage<PlayerSaveData>("PlayerData.cfg", typeof(PlayerSaveData), Logger) ?? new PlayerSaveData();
      
      if (PlayerData.PlayerSettings == null)
        PlayerData.PlayerSettings = new List<PlayerData>();

      for (int i = 0; i < PlayerData.PlayerSettings.Count; i++)
      {
        var data = PlayerData.PlayerSettings[i];
        PlayerCopyPasteDict[data.SteamId] = data.CopyPasteEnabled;
        PlayerCreativeDict[data.SteamId] = data.CreativeEnabled;
      }

      Logger.AddLine($"BeforeStart() - Checking Group Settings:");
      for (int i = ModSaveData.GroupSettings.Count - 1; i >= 0; i--)
      {
        var setting = ModSaveData.GroupSettings[i];
        Logger.AddLine($"-> GroupName = {setting?.GroupName ?? "NULL"}");
        if (setting?.Definitions == null)
        {
          Logger.AddLine($"->-> Setting was null");
          ModSaveData.GroupSettings.RemoveAtFast(i);
          continue;
        }

        Logger.AddLine($"->-> Setting has {setting.Definitions.Count} definitions:");
        for (int j = setting.Definitions.Count - 1; j >= 0; j--)
        {
          var definition = setting.Definitions[j]?.DefinitionId;
          Logger.AddLine($"->->-> Definition = {definition?.ToString() ?? "NULL"}");
          if (definition?.TypeId.IsNull != false)
          {
            Logger.AddLine($"->->-> Definition was null");
            setting.Definitions.RemoveAtFast(j);
            continue;
          }

          MyCubeBlockDefinition cubeDef;
          if (!BlockTypeDict.TryGetValue(definition.Value, out cubeDef))
          {
            Logger.AddLine($"->->-> Unable to retrieve MyCubeBlockDefinition");
            setting.Definitions.RemoveAtFast(j);
            continue;
          }

          if (_typesThatCannotBeLocked.Contains(cubeDef.Id))
          {
            Logger.AddLine($"->->-> Definition cannot be restricted");
            setting.Definitions.RemoveAtFast(j);
            continue;
          }

          DefinitionGroupMap[definition.Value] = setting.GroupName;

          if (!setting.AllowedForPlayer)
          {
            Logger.AddLine($"->->-> Definition added to restricted list");
            _restrictedDefinitions.Add(definition.Value);
          }
        }

        if (setting.Definitions.Count > 0)
        {
          var gSetting = new GroupSetting(setting.GroupName, setting.Definitions, setting.AllowedForPlayer, setting.AllowedForPlayerStaticOnly, setting.AllowedForNPC, setting.AllowedForNPCStaticOnly, setting.AllowedForUnowned, setting.AllowedForUnownedStaticOnly, setting.PlayerMaxCount, setting.GridMaxCount, setting.FactionMaxCount);

          GroupSettingsDict[setting.GroupName] = gSetting;
          Logger.AddLine($"->-> Setting added successfully");
        }
        else
          ModSaveData.GroupSettings.RemoveAtFast(i);
      }

      if (ModSaveData.GroupSettings.Count == 0)
      {
        var setting = new SerializableGroupSetting("UniqueNameHere", new List<SerialId> { new SerialId(new SerializableDefinitionId()) });
        ModSaveData.GroupSettings.Add(setting);
      }

      Logger.AddLine($"\nBeforeStart() - Checking Config for saved settings");
      foreach (var setting in ModSaveData.Settings)
      {
        MyDefinitionId objDef;
        Logger.AddLine($"-> Setting = {setting.Type}");
        if (!MyDefinitionId.TryParse(setting.Type, out objDef))
        {
          Logger.AddLine($"->-> Unable to parse MyDefinitionId");
          continue;
        }

        MyCubeBlockDefinition cubeDef;
        if (!BlockTypeDict.TryGetValue(objDef, out cubeDef))
        {
          Logger.AddLine($"->-> Unable to retrieve MyCubeBlockDefinition");
          continue;
        }

        if (_typesThatCannotBeLocked.Contains(cubeDef.Id))
        {
          Logger.AddLine($"->-> Definition cannot be restricted");
          setting.AllowedForPlayer = true;
        }
        else if (!setting.AllowedForPlayer)
        {
          Logger.AddLine($"->-> Definition added to restricted list");
          _restrictedDefinitions.Add(objDef);
        }

        var ownerAllowed = cubeDef.ContainsComputer();

        SettingsDict[objDef] = new BlockSetting(objDef, setting.AllowedForPlayer, setting.AllowedForPlayerStaticOnly, setting.AllowedForNPC, setting.AllowedForNPCStaticOnly, setting.AllowedForUnowned, setting.AllowedForUnownedStaticOnly, ownerAllowed, setting.PlayerMaxCount, setting.GridMaxCount, setting.FactionMaxCount);

        Logger.AddLine($"->-> Setting added successfully");
      }

      Logger.AddLine($"\nBeforeStart() - Checking Entity Components");
      foreach (var def in MyDefinitionManager.Static.GetEntityComponentDefinitions())
      {
        if (def.Id.SubtypeName.StartsWith("SC_BlockRestrictions"))
        {
          Logger.AddLine($"-> Found EC: Subtype = {def.Id.SubtypeName}");
          _debug.Clear();
          foreach (var ch in def.DescriptionText)
          {
            if (ch == '[')
              _debug.Append('<');
            else if (ch == ']')
              _debug.Append('>');
            else
              _debug.Append(ch);
          }

          var buffer = _debug.ToString().Trim();
          try
          {
            var defaultSettings = MyAPIGateway.Utilities.SerializeFromXML<DefaultSettings>(buffer);
            if (defaultSettings == null)
            {
              Logger.AddLine($"->-> Failed to deserialize XML. Buffer was:\n\n{buffer}\n\n");
              continue;
            }

            Logger.AddLine($"->-> EC contains {defaultSettings.Settings.Count} settings");
            foreach (var setting in defaultSettings.Settings)
            {
              MyDefinitionId defId = new MyDefinitionId();
              Logger.AddLine($"->->-> Setting = {setting.Type} (ForceSetting = {defaultSettings.ForceSetting})");
              if (string.IsNullOrWhiteSpace(setting.Type))
              {
                Logger.AddLine($"->->->-> Type was null or empty");
                continue;
              }

              if (!MyDefinitionId.TryParse(setting.Type, out defId))
              {
                Logger.AddLine($"->->->-> Unable to parse MyDefinitionId");
                continue;
              }

              if (!defaultSettings.ForceSetting && SettingsDict.ContainsKey(defId))
              {
                Logger.AddLine($"->->->-> A setting already exists (skipping this default setting)");
                continue;
              }

              var cubeDef = MyDefinitionManager.Static.GetCubeBlockDefinition(defId);
              if (cubeDef == null)
              {
                Logger.AddLine($"->->->-> Unable to retrieve MyCubeBlockDefinition");
                continue;
              }

              ModSaveData.Settings.Add(setting);

              if (_typesThatCannotBeLocked.Contains(cubeDef.Id))
              {
                Logger.AddLine($"->->->-> Type cannot be restricted");
                setting.AllowedForPlayer = true;
              }
              else if (!setting.AllowedForPlayer)
              {
                Logger.AddLine($"->->->-> Type added to the restricted list");
                _restrictedDefinitions.Add(defId);
              }

              var ownerAllowed = cubeDef.ContainsComputer();
              SettingsDict[defId] = new BlockSetting(defId, setting.AllowedForPlayer, setting.AllowedForPlayerStaticOnly, setting.AllowedForNPC, setting.AllowedForNPCStaticOnly, setting.AllowedForUnowned, setting.AllowedForUnownedStaticOnly, ownerAllowed, setting.PlayerMaxCount, setting.GridMaxCount, setting.FactionMaxCount);

              Logger.AddLine($"->->->-> Setting added successfully");
            }
          }
          catch(Exception ex)
          {
            Logger.AddLine($"->-> Error trying to deserialize Default Setting XML: {ex.Message}\n{ex.StackTrace}\n\nBuffer:\n{buffer}");
          }

          Logger.AddLine($"\n");
        }
      }
      _debug.Clear();
      Logger.AddLine($"\nFinished with settings\n\n");
      Logger.LogAll();

      foreach (var kvp in BlockTypeDict)
      {
        if (SettingsDict.ContainsKey(kvp.Key))
          continue;

        var setting = new SerializableBlockSetting(kvp.Key.ToString());
        ModSaveData.Settings.Add(setting);

        bool ownerAllowed = kvp.Value.ContainsComputer();

        SettingsDict[kvp.Key] = new BlockSetting(kvp.Key, setting.AllowedForPlayer, setting.AllowedForPlayerStaticOnly, setting.AllowedForNPC, setting.AllowedForNPCStaticOnly, setting.AllowedForUnowned, setting.AllowedForUnownedStaticOnly, ownerAllowed, setting.PlayerMaxCount, setting.GridMaxCount, setting.FactionMaxCount);
      }

      _startUpEnts.Clear();
      MyAPIGateway.Entities.GetEntities(_startUpEnts);

      foreach (var ent in _startUpEnts)
        MyEntities_OnEntityAdd((MyEntity)ent);

      _startUpEnts.Clear();

      //MyEntities.OnEntityCreate += MyEntities_OnEntityCreate;
      MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;
      MyEntities.OnEntityRemove += MyEntities_OnEntityRemove;
      MyAPIGateway.Session.Factions.FactionStateChanged += OnFactionStateChanged;
      MyAPIGateway.Session.Factions.FactionCreated += OnFactionCreated;

      SaveConfig();
    }

    public void SaveConfig()
    {
      UpdateAllPlayerData();

      Config.WriteFileToWorldStorage("SC_BlockRestrictions.cfg", typeof(BlockSaveData), ModSaveData, Logger);
      Config.WriteFileToWorldStorage("PlayerData.cfg", typeof(PlayerSaveData), PlayerData, Logger);
    }

    public void AddOrUpdatePlayerData(ulong steamId)
    {
      var list = PlayerData?.PlayerSettings;
      if (list == null || steamId == 0)
        return;

      bool found = false;
      for (int i = list.Count - 1; i >= 0; i--)
      {
        var playerData = list[i];
        if (playerData == null || playerData.SteamId == 0)
        {
          list.RemoveAtFast(i);
          continue;
        }

        if (playerData.SteamId != steamId)
          continue;

        playerData.IsAdmin = steamId.IsAdmin();
        playerData.CopyPasteEnabled = PlayerCopyPasteDict.GetValueOrDefault(steamId, false);
        playerData.CreativeEnabled = PlayerCreativeDict.GetValueOrDefault(steamId, false);

        found = true;
        break;
      }

      if (!found)
      {
        bool creative, copyPaste;
        PlayerCopyPasteDict.TryGetValue(steamId, out copyPaste);
        PlayerCreativeDict.TryGetValue(steamId, out creative);
        var pData = new PlayerData(steamId, creative, copyPaste);
        list.Add(pData);
      }

      SaveConfig();
    }

    public void UpdateAllPlayerData()
    {
      var list = PlayerData?.PlayerSettings;
      if (list == null)
        return;

      for (int i = list.Count - 1; i >= 0; i--)
      {
        var playerData = list[i];
        if (playerData == null || playerData.SteamId == 0)
        {
          list.RemoveAtFast(i);
          continue;
        }

        var steamId = playerData.SteamId;
        playerData.IsAdmin = steamId.IsAdmin();
        playerData.CopyPasteEnabled = PlayerCopyPasteDict.GetValueOrDefault(steamId, false);
        playerData.CreativeEnabled = PlayerCreativeDict.GetValueOrDefault(steamId, false);
      }
    }

    public override void LoadData()
    {
      try
      {
        _restrictedDefinitions.Clear();

        foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
        {
          var cubeDef = def as MyCubeBlockDefinition;
          if (cubeDef == null)
            continue;

          if (!cubeDef.Public)
            _restrictedDefinitions.Add(cubeDef.Id);
        }
      }
      finally
      {
        base.LoadData();
      }
    }

    public bool Registered;
    public override void BeforeStart()
    {
      try
      {
        IsServer = MyAPIGateway.Multiplayer.IsServer;
        IsDedicatedServer = MyAPIGateway.Utilities.IsDedicated;
        MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;

        Logger = new Logger("SC_BlockRestrictions.log", MyAPIGateway.Utilities.IsDedicated);
        Network = new Networking(3344961488, 61488, this, _debug);
        Network.Register();

        foreach (var mod in MyAPIGateway.Session.Mods)
        {
          if (mod.PublishedFileId == 2307665159)
          {
            _researchFrameworkInstalled = true;
            break;
          }
        }

        Utilities.CheckCreativeTools(out _creativeEnabled, out _copyPasteEnabled);
        if (!IsServer)
        {
          var packet = new Packet(_creativeEnabled, _copyPasteEnabled, false);
          Network.SendToServer(packet);
          return;
        }
        else if (!IsDedicatedServer)
        {
          var myId = MyAPIGateway.Multiplayer.MyId;
          PlayerCreativeDict[myId] = _creativeEnabled;
          PlayerCopyPasteDict[myId] = _copyPasteEnabled;
          
          AddOrUpdatePlayerData(myId);
        }

        if (!_isSetup)
          Init();

        Registered = true;
      }
      catch (Exception e)
      {
        Logger?.Log($"Error in BeforeStart:\n{e.Message}\n{e.StackTrace}", MessageType.ERROR);
        throw;
      }
    }

    MyCommandLine _cli = new MyCommandLine();
    private void OnMessageEntered(string messageText, ref bool sendToOthers)
    {
      if (messageText.IndexOf("/br", StringComparison.OrdinalIgnoreCase) < 0 || !_cli.TryParse(messageText) || _cli.ArgumentCount < 2)
        return;

      sendToOthers = false;

      if ((int)MyAPIGateway.Session.PromoteLevel < 4)
      {
        ShowMessage("You must have admin privileges to use admin commands");
        return;
      }

      if (_cli.Argument(1).Equals("creativeallowed", StringComparison.OrdinalIgnoreCase))
      {
        bool b;
        if (_cli.ArgumentCount < 3 || !bool.TryParse(_cli.Argument(2), out b))
          b = !ModSaveData.CreativeModeAllowed;

        if (b == ModSaveData.CreativeModeAllowed)
          return;

        ModSaveData.CreativeModeAllowed = b;
        _toolbarChecked = false;
        ShowMessage($"Creative Mode Allowed changed to {b}");

        if (b)
        {
          VerifyCreativeMode();
          CheckToolbarLocal(true);
        }
        else
          _creativeAllowed = b;

        var packet = new Packet(b);
        if (!IsServer)
          Network.SendToServer(packet);
        else
        {
          if (!b)
          {
            foreach (var blockDict in PlayerBlockDict.Values)
              blockDict.Clear();

            foreach (var ent in EntityDict.Values)
            {
              if (ent == null || ent.MarkedForClose)
                continue;

              ent.FactionUpdateCheck = true;
              ent.NeedsOwnerUpdate = true;
              ent.NeedsBlockOwnerUpdate = true;
              ent.NeedsCreativeRecalc = true;
            }
          }

          if (MyAPIGateway.Multiplayer.MultiplayerActive)
          {
            packet.BlockSettings = ModSaveData.Settings;
            Network.RelayToClients(packet);
          }

          SaveConfig();
        }
      }
      else if (MyAPIGateway.Session?.Player != null)
        ShowMessage($"Invalid command entered: {_cli.Argument(1)}");
    }

    public void ReceiveSettings(Packet pkt)
    {
      if (IsServer)
        return;

      if (ModSaveData == null)
        ModSaveData = new BlockSaveData();

      if (pkt.CreativeModeAllowed != ModSaveData.CreativeModeAllowed)
        ShowMessage($"Creative Mode Allowed has been changed to {pkt.CreativeModeAllowed}", timeToLive:5000);

      ModSaveData.CreativeModeAllowed = pkt.CreativeModeAllowed;

      GroupSettingsDict.Clear();
      foreach (var setting in pkt.GroupSettings)
      {
        if (setting?.Definitions == null)
          continue;

        for (int j = setting.Definitions.Count - 1; j >= 0; j--)
        {
          var definition = setting.Definitions[j].DefinitionId;
          if (definition.TypeId.IsNull)
          {
            setting.Definitions.RemoveAtFast(j);
            continue;
          }

          MyCubeBlockDefinition cubeDef;
          if (!BlockTypeDict.TryGetValue(definition, out cubeDef))
          {
            setting.Definitions.RemoveAtFast(j);
            continue;
          }

          if (_typesThatCannotBeLocked.Contains(cubeDef.Id))
          {
            setting.Definitions.RemoveAtFast(j);
            continue;
          }

          DefinitionGroupMap[definition] = setting.GroupName;

          if (!setting.AllowedForPlayer)
            _restrictedDefinitions.Add(definition);
        }

        if (setting.Definitions.Count > 0)
        {
          var gSetting = new GroupSetting(setting.GroupName, setting.Definitions, setting.AllowedForPlayer, setting.AllowedForPlayerStaticOnly, setting.AllowedForNPC, setting.AllowedForNPCStaticOnly, setting.AllowedForUnowned, setting.AllowedForUnownedStaticOnly, setting.PlayerMaxCount, setting.GridMaxCount, setting.FactionMaxCount);

          GroupSettingsDict[setting.GroupName] = gSetting;
        }
      }

      SettingsDict.Clear();
      foreach (var item in pkt.BlockSettings)
      {
        MyDefinitionId id;
        if (!MyDefinitionId.TryParse(item.Type, out id))
        {
          Logger.Log($"ReceiveSettings: Unable to parse type for block setting - {item.Type}", MessageType.WARNING);
          continue;
        }

        var cubeDef = MyDefinitionManager.Static.GetCubeBlockDefinition(id);
        if (cubeDef == null)
          continue;

        if (!item.AllowedForPlayer)
          _restrictedDefinitions.Add(id);

        SettingsDict[id] = new BlockSetting(id, item.AllowedForPlayer, item.AllowedForPlayerStaticOnly, item.AllowedForNPC, item.AllowedForNPCStaticOnly, item.AllowedForUnowned, item.AllowedForUnownedStaticOnly, cubeDef.ContainsComputer(), item.PlayerMaxCount, item.GridMaxCount, item.FactionMaxCount);
      }

      _firstCheck = true;
      _toolbarChecked = false;
    }

    bool _isUpdate10, _isUpdate20, _toolbarChecked, _firstCheck = true;
    int _tickCount = 0;

    public override void UpdateAfterSimulation()
    {
      try
      {
        ++_tickCount;
        _isUpdate10 = _tickCount % 10 == 0;
        _isUpdate20 = _tickCount % 20 == 0;

        if (!IsDedicatedServer)
        {
          if (_isUpdate20)
            VerifyCreativeMode();

          if (!_toolbarChecked)
            CheckToolbarLocal(_firstCheck);
  
          if(!IsServer)
            return;
        }

        for (int i = _newEnts.Count - 1; i >= 0; i--)
        {
          var ent = _newEnts[i];
          if (ent == null || EntityDict.TryAdd(ent.EntityId, ent))
            _newEnts.RemoveAtFast(i);
        }

        if (_addTask.IsComplete)
        {
          if (_addTask.Exceptions != null)
          {
            _debug.Clear().Append($"Exception(s) encountered during add task:\n");
            foreach (var e in _updateTask.Exceptions)
              _debug.Append($"{e.Message}\n{e.StackTrace}\n\n");

            Logger?.Log(_debug, MessageType.ERROR);
            throw new Exception("Task Exceptions found during AddEntities");
          }

          if (_entityQueue.Count > 0)
            _addTask = MyAPIGateway.Parallel.StartBackground(AddEntities);
        }

        if (_updateTask.IsComplete)
        {
          if (_updateTask.Exceptions != null)
          {
            _debug.Clear().Append($"Exception(s) encountered during update task:\n");
            foreach (var e in _updateTask.Exceptions)
              _debug.Append($"{e.Message}\n{e.StackTrace}\n\n");

            Logger?.Log(_debug, MessageType.ERROR);
            throw new Exception("Task Exceptions found during ProcessQueue");
          }

          if (_entityQueue.Count == 0 && _isUpdate10)
            _updateTask = MyAPIGateway.Parallel.StartBackground(ProcessUpdates);
        }

        while (RemovalQueue.Count > 0)
        {
          var kvp = RemovalQueue.Dequeue();
          RemoveAndRefund(kvp.Key, kvp.Value.Key, kvp.Value.Value);
        }

        if (_updateConfig && ++_simCount > 99)
        {
          _updateConfig = false;
          _simCount = 0;
          SaveConfig();
        }
      }
      catch (Exception e)
      {
        Logger?.Log($"Error in UpdateAfterSim: {e.Message}\n{e.StackTrace}", MessageType.ERROR);
        ShowMessage($"An unhandled exception has occurred. Please send the 'SC_BlockRestrictions.log' file to jTurp for analysis");
      }

      base.UpdateAfterSimulation();
    }

    ModPacket _modPacket;
    void CheckToolbarLocal(bool force = false)
    {
      bool allowAdmin = _copyPasteEnabled && _creativeEnabled && (!MyAPIGateway.Session.CreativeMode || ModSaveData.CreativeModeAllowed);
      if (!force && allowAdmin)
      {
        _toolbarChecked = true;
        return;
      }

      var player = MyAPIGateway.Session?.Player;
      if (player == null)
      {
        _toolbarChecked = true;
        return;
      }

      if (player.Character == null || player.Character.IsDead || player.Character.Parent is MyShipController)
        return;

      _firstCheck = false;
      if (_researchFrameworkInstalled)
      {
        try
        {
          if (_modPacket == null)
            _modPacket = new ModPacket(3344961488, _restrictedDefinitions.ToList());

          Network.SendToMod(2307665159, _modPacket);
        }
        catch (Exception ex)
        {
          Logger.Log($"Error during CheckToolbarLocal (framework installed): {ex.Message}\n{ex.StackTrace}\n\n", MessageType.ERROR);
        }

        _toolbarChecked = true;
        return;
      }

      if (_restrictedDefinitions.Count == 0)
      {
        _toolbarChecked = true;
        return;
      }

      try
      {
        if (!allowAdmin)
        {
          foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
          {
            var cubeDef = def as MyCubeBlockDefinition;
            if (cubeDef != null)
              cubeDef.Public = true;
          }

          for (int i = 0; i < 9; i++)
          {
            MyVisualScriptLogicProvider.SetToolbarPageLocal(i);

            for (int j = 0; j < 9; j++)
            {
              try
              {
                MyVisualScriptLogicProvider.SwitchToolbarToSlotLocal(j);
                var def = MyCubeBuilder.Static?.CubeBuilderState?.CurrentBlockDefinition;
                BlockSetting setting;
                if (def != null && SettingsDict.TryGetValue(def.Id, out setting) && !setting.AllowedForPlayer)
                  MyVisualScriptLogicProvider.ClearToolbarSlotLocal(j);
              }
              catch (Exception ex)
              {
                Logger.Log($"CheckToolbarLocal: Attempted to check page {i} slot {j} of the toolbar but encountered an exception - clearing the toolbar slot. Exception info:\n{ex.Message}\n\n{ex.StackTrace}", MessageType.ERROR);
                MyVisualScriptLogicProvider.ClearToolbarSlotLocal(j);
              }
            }
          }

          MyVisualScriptLogicProvider.SetToolbarPageLocal(0);
          var entController = player.Character as Sandbox.Game.Entities.IMyControllableEntity;
          entController.SwitchToWeapon(null);
        }
      }
      catch (Exception ex)
      {
        Logger.Log($"Error during CheckToolbarLocal: {ex.Message}\n{ex.StackTrace}\n\nClearing Toolbar..", MessageType.ERROR);
        MyVisualScriptLogicProvider.ClearAllToolbarSlots();
      }
      finally
      {
        _toolbarChecked = true;

        foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
        {
          if (!allowAdmin && _restrictedDefinitions.Contains(def.Id))
          {
            def.Public = false;
            continue;
          }

          var cubeDef = def as MyCubeBlockDefinition;
          if (cubeDef == null)
            continue;

          var typeId = cubeDef.Id;
          if (_ignoreTypes.Contains(typeId.TypeId))
            continue;

          if (!_terminalBlockDefinitions.Contains(cubeDef.Id))
            continue;

          BlockSetting setting;
          bool found = SettingsDict.TryGetValue(cubeDef.Id, out setting);
          cubeDef.Public = allowAdmin || !found || setting.AllowedForPlayer;
        }
      }
    }

    void VerifyCreativeMode()
    {
      bool creativeMode, copyPasteEnabled;
      Utilities.CheckCreativeTools(out creativeMode, out copyPasteEnabled);
      if (creativeMode != _creativeEnabled || _copyPasteEnabled != copyPasteEnabled || _creativeAllowed != ModSaveData.CreativeModeAllowed)
      {
        _creativeEnabled = creativeMode;
        _copyPasteEnabled = copyPasteEnabled;
        _creativeAllowed = ModSaveData.CreativeModeAllowed;

        CheckToolbarLocal(true);

        //if (_copyPasteEnabled)
        //{
        //  bool allowAdmin = copyPasteEnabled && _creativeEnabled && (!MyAPIGateway.Session.CreativeMode || ModSaveData.CreativeModeAllowed);
         
        //  foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
        //  {
        //    if (!allowAdmin && _restrictedDefinitions.Contains(def.Id))
        //    {
        //      def.Public = false;
        //      continue;
        //    }

        //    var cubeDef = def as MyCubeBlockDefinition;
        //    if (cubeDef == null)
        //      continue;

        //    var typeId = cubeDef.Id;
        //    if (_ignoreTypes.Contains(typeId.TypeId))
        //      continue;

        //    if (!_terminalBlockDefinitions.Contains(cubeDef.Id))
        //      continue;

        //    bool allowed = allowAdmin || !_restrictedDefinitions.Contains(cubeDef.Id);
        //    cubeDef.Public = allowed;
        //  }

        //  _toolbarChecked = false;
        //}

        if (!IsServer)
        {
          var pkt = new Packet(creativeMode, copyPasteEnabled, true);
          Network.SendToServer(pkt);
        }
        else
        {
          var myId = MyAPIGateway.Multiplayer.MyId;
          PlayerCreativeDict[myId] = _creativeEnabled;
          PlayerCopyPasteDict[myId] = _copyPasteEnabled;

          var player = MyAPIGateway.Session?.Player;
          if (player != null)
          {
            Dictionary<MyDefinitionId, int> playerBlocks;
            if (PlayerBlockDict.TryGetValue(player.IdentityId, out playerBlocks))
              playerBlocks.Clear();

            SetUpdateNeeded(player.IdentityId, true);
          }

          AddOrUpdatePlayerData(myId);
        }
      }
    }

    void AddEntities()
    {
      while (_entityQueue.Count > 0)
      {
        var grid = _entityQueue.Dequeue() as MyCubeGrid;
        if (grid?.Physics == null || grid.MarkedAsTrash || grid.MarkedForClose || grid.IsPreview)
          continue;

        bool isSubgrid = false;
        bool gridUpdated = false;
        long oldGridId = 0;

        foreach (var kvp in EntityDict)
        {
          var entity = kvp.Value;
          if (entity?.GridCollection?.BaseGrid == null || entity.MarkedForClose)
            continue;

          oldGridId = kvp.Key;
          Utilities.GetConnectedGrids(entity.GridCollection.BaseGrid, _gridHash);

          foreach (var cubeGrid in _gridHash)
          {
            if (cubeGrid?.Physics == null || cubeGrid.MarkedForClose || grid.EntityId != cubeGrid.EntityId)
              continue;

            isSubgrid = true;
            entity.AddGrid(grid, out gridUpdated);
            break;
          }

          _gridHash.Clear();
        }

        if (isSubgrid)
        {
          if (gridUpdated)
          {
            Entity entToRemove;
            if (EntityDict.TryRemove(oldGridId, out entToRemove))
            {
              if (!EntityDict.TryAdd(grid.EntityId, entToRemove))
                _newEnts.Add(entToRemove);
            }
            else
              Logger?.Log($"Failed to remove grid {oldGridId.ToString()} after it was updated", MessageType.WARNING);
          }

          continue;
        }

        var ent = new Entity(grid, _boxList, _gridHash, _gridAddHash, this, _debug, Logger);

        if (!EntityDict.TryAdd(ent.EntityId, ent))
        {
          Entity oldEnt;
          if (EntityDict.ContainsKey(ent.EntityId) && EntityDict.TryRemove(ent.EntityId, out oldEnt))
          {
            if (!EntityDict.TryAdd(ent.EntityId, ent))
              _newEnts.Add(ent);
          }
          else
            _newEnts.Add(ent);
        }
      }
    }

    public void AddBlockSetting(MyDefinitionId definition, BlockSetting setting)
    {
      SettingsDict[definition] = setting;
      _updateConfig = true;
      _simCount = 0;
    }

    void ProcessUpdates()
    {
      foreach (var ent in EntityDict.Values)
      {
        if (ent == null || ent.MarkedForClose)
        {
          _removals.Add(ent.EntityId);
          continue;
        }

        ent.Update();

        foreach (var grid in ent.GridsToReallocate)
        {
          var gridEnt = grid as MyEntity;
          if (!_entityQueue.Contains(gridEnt))
            _entityQueue.Enqueue(gridEnt);
        }

        ent.GridsToReallocate.Clear();

        if (ent.CloseAfterRedistrubution)
          _removals.Add(ent.EntityId);
      }

      foreach (var entId in _removals)
      {
        Entity ent;
        if (EntityDict.TryRemove(entId, out ent))
          ent?.Close();
      }
    }

    private void MyEntities_OnEntityCreate(MyEntity obj)
    {
      if (!Registered || !IsServer)
        return;

      var cube = obj as MyCubeBlock;
      if (cube == null || cube.MarkedForClose || cube.IsPreview)
      {
        return;
      }

      IMyTerminalBlock terminal = cube as IMyTerminalBlock;
      if (terminal == null)
      {
        return;
      }

      var cubeDef = cube.BlockDefinition;
      if (cubeDef == null)
      {
        return;
      }

      if (!_restrictedDefinitions.Contains(cubeDef.Id))
      {
        return;
      }

      bool found = false;
      foreach (var setting in GroupSettingsDict.Values)
      {
        if (setting?.Definitions?.Contains(cubeDef.Id) != true)
          continue;

        found = true;
        if (setting.AllowedForPlayer || setting.AllowedForNPC || setting.AllowedForUnowned)
          return;
      }

      BlockSetting blockSetting;
      if (!found && SettingsDict.TryGetValue(cubeDef.Id, out blockSetting))
      {
        if (blockSetting.AllowedForNPC || blockSetting.AllowedForPlayer || blockSetting.AllowedForUnowned)
          return;
      }

      bool creativeWorld = MyAPIGateway.Session?.SessionSettings?.GameMode == VRage.Library.Utils.MyGameModeEnum.Creative;
      bool creativeMode = creativeWorld;
      var builder = cube.BuiltBy;

      if (MyAPIGateway.Session?.Player != null && MyAPIGateway.Session.Player.IdentityId == builder)
      {
        creativeMode = creativeWorld || _copyPasteEnabled || _creativeEnabled;
        ShowMessage($"{cubeDef.DisplayNameText} is not allowed in this world.");
      }
      else
      {
        var steamId = MyAPIGateway.Players.TryGetSteamId(builder);
        if (steamId != 0)
        {
          if (!creativeWorld)
          {
            bool copyPaste;
            if (PlayerCopyPasteDict.TryGetValue(steamId, out copyPaste) && copyPaste)
              creativeMode = true;
            else
              PlayerCreativeDict.TryGetValue(steamId, out creativeMode);
          }

          var packet = new Packet($"{cubeDef.DisplayNameText} is not allowed in this world.");
          Network.SendToPlayer(steamId, packet);
        }
      }

      if (creativeMode)
        BlocksBuiltWithCreativeDict.Add(terminal.EntityId);

      var kvp = new KeyValuePair<IMyTerminalBlock, KeyValuePair<bool, long>>(terminal, new KeyValuePair<bool, long>(creativeMode, cube.BuiltBy));
      RemovalQueue.Enqueue(kvp);
    }

    private void MyEntities_OnEntityAdd(MyEntity obj)
    {
      if (!IsServer)
        return;

      var grid = obj as MyCubeGrid;
      if (grid == null || grid.MarkedForClose || grid.Physics == null || grid.IsPreview)
        return;

      _entityQueue.Enqueue(grid);
    }

    private void MyEntities_OnEntityRemove(MyEntity obj)
    {
      if (!IsServer)
        return;

      var grid = obj as MyCubeGrid;
      if (grid == null)
        return;

      foreach (var block in grid.GetFatBlocks())
      {
        var terminal = block as IMyTerminalBlock;
        if (terminal != null)
        {
          long ownerId;
          if (!BlockOwnerDictPlayer.TryGetValue(terminal.EntityId, out ownerId))
            continue;

          BlockOwnerDictPlayer.Remove(terminal.EntityId);

          Dictionary<MyDefinitionId, int> blockDict;
          int count;
          if (PlayerBlockDict.TryGetValue(ownerId, out blockDict) && blockDict.TryGetValue(terminal.BlockDefinition, out count))
            blockDict[terminal.BlockDefinition] = Math.Max(0, count - 1);

          long factionId; BlockFactionDict.TryGetValue(terminal.EntityId, out factionId);
          if (BlockFactionDict.Remove(terminal.EntityId) && FactionBlockDict.TryGetValue(factionId, out blockDict) && blockDict.TryGetValue(terminal.BlockDefinition, out count))
            blockDict[terminal.BlockDefinition] = Math.Max(0, count - 1);
        }
      }
    }

    public void SetUpdateNeeded(long ownerIdToCheck, bool creativeCheckNeeded = false)
    {
      foreach (var kvp in EntityDict)
      {
        var ent = kvp.Value;
        if (ent == null || ent.MarkedForClose || ent.OwnerId != ownerIdToCheck)
          continue;

        ent.FactionUpdateCheck = true;
        ent.NeedsOwnerUpdate = true;
        ent.NeedsBlockOwnerUpdate = true;
        ent.NeedsCreativeRecalc = creativeCheckNeeded;
      }
    }

    private void OnFactionStateChanged(MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
    {
      if (fromFactionId == toFactionId)
        return;

      if (action != MyFactionStateChange.FactionMemberLeave && action != MyFactionStateChange.FactionMemberKick && action != MyFactionStateChange.FactionMemberAcceptJoin && action != MyFactionStateChange.FactionMemberCancelJoin)
        return;

      SetUpdateNeeded(playerId);
    }

    private void OnFactionCreated(long factionId)
    {
      var faction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
      if (faction == null)
        return;

      var founderId = faction.FounderId;
      ulong steamId;
      if (!founderId.IsPlayer(out steamId))
        return;

      SetUpdateNeeded(founderId);
    }

    public void RemoveAndRefund(IMyTerminalBlock block, bool playerOwned, long ownerId)
    {
      if (block == null)
        return;

      bool creativeEnabled, copyPasteEnabled;
      Utilities.CheckCreativeTools(out creativeEnabled, out copyPasteEnabled);
      if (playerOwned)
      {
        var steamId = MyAPIGateway.Players.TryGetSteamId(ownerId);
        if (steamId != 0 && !PlayerCreativeDict.TryGetValue(steamId, out creativeEnabled))
        {
          PlayerCreativeDict[steamId] = creativeEnabled;
          PlayerCopyPasteDict[steamId] = copyPasteEnabled;

          AddOrUpdatePlayerData(steamId);
        }
      }

      if (playerOwned && !BlocksBuiltWithCreativeDict.Remove(block.EntityId))
        Utilities.ReturnComponentsToPlayer(_missingComps, _compHash, block, ownerId, _debug);

      var mech = block as IMyMechanicalConnectionBlock;
      var top = mech?.Top;

      if (top != null)
      {
        var topGrid = top?.CubeGrid;
        if (topGrid == null)
          top?.Close();
        else
          topGrid.RemoveBlock(top.SlimBlock, true);
      }

      var grid = block?.CubeGrid;
      if (grid == null)
        block?.Close();
      else
        grid.RemoveBlock(block.SlimBlock, true);
    }

    public void ShowMessage(string text, string font = MyFontEnum.Red, int timeToLive = 2000)
    {
      if (hudMsg == null)
        hudMsg = MyAPIGateway.Utilities.CreateNotification(string.Empty);

      hudMsg.Hide();
      hudMsg.Font = font;
      hudMsg.AliveTime = timeToLive;
      hudMsg.Text = text;
      hudMsg.Show();
    }
  }
}
