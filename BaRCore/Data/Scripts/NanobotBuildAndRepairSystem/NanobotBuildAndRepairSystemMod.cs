namespace SpaceEquipmentLtd.NanobotBuildAndRepairSystem
{
   using System;
   using System.Collections.Generic;

   using VRage;
   using VRage.Game;
   using VRage.Game.Components;
   using VRage.Game.ModAPI;
   using VRage.ModAPI;

   using Sandbox.ModAPI;
   using Sandbox.ModAPI.Weapons;

   using SpaceEquipmentLtd.Utils;
   using System.IO;


   static class Mod
   {
      public static Logging Log = new Logging("NanobotBuildAndRepairSystem", 0, "NanobotBuildAndRepairSystem.log", typeof(NanobotBuildAndRepairSystemMod)) {
         LogLevel = Logging.Level.Error, //Default
         EnableHudNotification = false
      };
      public static bool DisableLocalization = false;
   }

   /*Change log:
   * V 2.1.6:
   *     -Fix: Fixed issue with ResourceSink/Power after Heavy Industrie Update
   * V 2.1.5:
   *     -Fix: Edit priority list while multiple block are selected
   * V 2.1.4:
   *     -Fix: Reverted to 2.1.2
   * V 2.1.3:
   *     -Fix: Added internal TransportInventory to Block componentes list (ownership)
   * V 2.1.2:
   *     -New: Works now with API changes from public test
   * V 2.1.1:
   *     -New: Complete Russian translation thanks to Lexx Lord
   *     -New: Chat command to export a language file for easier translation
   *     -Fix: If Server version of the mod was older than client version some janitor functions were disabled
   * V 2.1.0:
   *     -New: Localization support
   *     -New: Setting to disable specific janitor functions (e.g. disable offence functions)
   * V 2.0.8:
   *     -Fix: Now working with economy update
   * V 2.0.7:
   *     -Fix: Creating of settings object to late
   *     -Fix: Update of CustomInfo in case of Enabled/IsWorking changed
   * V 2.0.6:
   *     -Fix: Now all changed settings inside settings file will also change settings of allready build blocks. (Has been at least not the case for power settings)
   * V 2.0.5:
   *     -New: Show current mod/settings folders in help screen.
   * V 2.0.4:
   *     -Fix: Assembler queuing now ignores assemblers in "Disassembly" mode
   * V 2.0.3:
   *     -New: Use new api function IsWithinWorldLimits / Build with buildByInfo
   *     -Fix: Priority IMyPowerProducer handled as PowerBlock
   *     -Fix: Priority Thrusters, Wheels, Motors handled as Thruster
   * V 2.0.2:
   *     -Fix: Ignore and Grind color not set correct in client/server communication
   * V 2.0.1:
   *     -Fix: Exception while mixed version of mod (incompatible ProtoMember numbering)
   * V 2.0.0:
   *     -Changed: Rearranged GUI Controls, new grouping
   *     -Changed: command format from /xx to -xx e.g. /nanobars /cwsf -> /nanobars -cwsf
   *     -New: Weld option "Weld to functional only"
   *           Area Offset (Changed AreaWidthLeft/Right,AreaWidthTop/Bottom, AreaWidthFront/Rear to AreaWidth, AreaHeight, AreaDepth and add Horizontal offset, Vertical offset, Forward offset)
   *           Detailed GUI Info in case of not working block (switched off, damaged/incomplete, not enought power)
   *           Model added orientation indicators (left, right, front, back, down)
   *     -New Settings: 
   *        MaxBackgroundTasks
   *        MaximumOffset
   *        MaximumRequiredElectricPowerStandby
   *        AllowedEffects (WeldingVisualEffect WeldingSoundEffect GrindingVisualEffect GrindingSoundEffect TransportVisualEffect)
   *     -Fix: Ignore/Grind Color not allways found equal to block color (as Keen converted color to uint and back to float loosing significant precision)
   */

   [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
   public class NanobotBuildAndRepairSystemMod : MySessionComponentBase
   {
      private const string Version = "V2.1.6 2021-08-29"
      ;

      private const string CmdKey = "/nanobars";
      private const string CmdHelp1 = "-?";
      private const string CmdHelp2 = "-help";
      private const string CmdCwsf = "-cwsf";
      private const string CmdCpsf = "-cpsf";
      private const string CmdLogLevel = "-loglevel";
      private const string CmdWritePerfCounter = "-writeperf";
      private const string CmdWriteTranslation = "-writetranslation";

      private const string CmdLogLevel_All = "all";
      private const string CmdLogLevel_Default = "default";

      private static ushort MSGID_MOD_DATAREQUEST = 40000;
      private static ushort MSGID_MOD_SETTINGS = 40001;
      private static ushort MSGID_MOD_COMMAND = 40010;
      private static ushort MSGID_BLOCK_DATAREQUEST = 40100;
      private static ushort MSGID_BLOCK_SETTINGS_FROM_SERVER = 40102;
      private static ushort MSGID_BLOCK_SETTINGS_FROM_CLIENT = 40103;
      private static ushort MSGID_BLOCK_STATE_FROM_SERVER = 40104;

      private bool _Init = false;
      public static bool SettingsValid = false;
      public static SyncModSettings Settings = new SyncModSettings();
      private static TimeSpan _LastSourcesAndTargetsUpdateTimer;
      private static TimeSpan SourcesAndTargetsUpdateTimerInterval = new TimeSpan(0, 0, 2);
      private static TimeSpan _LastSyncModDataRequestSend;

      public static Guid ModGuid = new Guid("8B57046C-DA20-4DE1-8E35-513FD21E3B9F");
      public const int MaxBackgroundTasks_Default = 4;
      public const int MaxBackgroundTasks_Max = 10;
      public const int MaxBackgroundTasks_Min = 1;

      public static List<Action> AsynActions = new List<Action>();
      private static int ActualBackgroundTaskCount = 0;

      /// <summary>
      /// Current known Build and Repair Systems in world
      /// </summary>
      private static Dictionary<long, NanobotBuildAndRepairSystemBlock> _BuildAndRepairSystems;
      public static Dictionary<long, NanobotBuildAndRepairSystemBlock> BuildAndRepairSystems
      {
         get
         {
            return _BuildAndRepairSystems != null ? _BuildAndRepairSystems : _BuildAndRepairSystems = new Dictionary<long, NanobotBuildAndRepairSystemBlock>();
         }
      }


      /// <summary>
      /// 
      /// </summary>
      public void Init()
      {
         Mod.Log.Write("BuildAndRepairSystemMod: Initializing IsServer={0}, IsDedicated={1}", MyAPIGateway.Session.IsServer, MyAPIGateway.Utilities.IsDedicated);
         _Init = true;

         Settings = SyncModSettings.Load();
         SettingsValid = MyAPIGateway.Session.IsServer;
         Mod.Log.LogLevel = Settings.LogLevel;
         SettingsChanged();

         MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamageHandlerNoDamageByBuildAndRepairSystem);
         if (MyAPIGateway.Session.IsServer)
         {
            //Detect friendly damage (only needed on server)
            MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(100, AfterDamageHandlerNoDamageByBuildAndRepairSystem);

            MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_MOD_COMMAND, SyncModCommandReceived);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_MOD_DATAREQUEST, SyncModDataRequestReceived);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_BLOCK_DATAREQUEST, SyncBlockDataRequestReceived);
            //Same as MSGID_BLOCK_SETTINGS but SendMessageToOthers sends also to self, which will result in stack overflow
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_BLOCK_SETTINGS_FROM_CLIENT, SyncBlockSettingsReceived);
         }
         else
         {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_MOD_SETTINGS, SyncModSettingsReceived);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_BLOCK_SETTINGS_FROM_SERVER, SyncBlockSettingsReceived);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MSGID_BLOCK_STATE_FROM_SERVER, SyncBlockStateReceived);
            SyncModDataRequestSend();
         }
         MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;

         Mod.Log.Write("BuildAndRepairSystemMod: Initialized.");
      }

      /// <summary>
      /// 
      /// </summary>
      private static void SettingsChanged()
      {
         if (SettingsValid)
         {
            foreach (var entry in BuildAndRepairSystems)
            {
               entry.Value.SettingsChanged();
            }
            InitControls();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public static void InitControls()
      {
         //Call also on dedicated else the properties for the scripting interface are not initialized
         if (SettingsValid && !NanobotBuildAndRepairSystemTerminal.CustomControlsInit && BuildAndRepairSystems.Count > 0) NanobotBuildAndRepairSystemTerminal.InitializeControls();
      }

      /// <summary>
      /// 
      /// </summary>
      private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
      {
         if (string.IsNullOrEmpty(messageText)) return;
         var cmd = messageText.ToLower();
         if (cmd.StartsWith(CmdKey))
         {
            if (Mod.Log.ShouldLog(Logging.Level.Verbose)) Mod.Log.Write(Logging.Level.Verbose, "BuildAndRepairSystemMod: Cmd: {0}", messageText);
            var args = cmd.Remove(0, CmdKey.Length).Trim().Split(' ');
            if (args.Length > 0)
            {
               if (Mod.Log.ShouldLog(Logging.Level.Verbose)) Mod.Log.Write(Logging.Level.Verbose, "BuildAndRepairSystemMod: Cmd args[0]: {0}", args[0]);
               switch (args[0].Trim())
               {
                  case CmdCpsf:
                     if (MyAPIGateway.Session.IsServer)
                     {
                        SyncModSettings.Save(Settings, false);
                        MyAPIGateway.Utilities.ShowMessage(CmdKey, "Settings file created inside mod folder");
                     }
                     else
                     {
                        MyAPIGateway.Utilities.ShowMessage(CmdKey, "command not allowed on client");
                     }
                     break;
                  case CmdCwsf:
                     if (MyAPIGateway.Session.IsServer)
                     {
                        SyncModSettings.Save(Settings, true);
                        MyAPIGateway.Utilities.ShowMessage(CmdKey, "Settings file created inside world folder");
                     }
                     else
                     {
                        MyAPIGateway.Utilities.ShowMessage(CmdKey, "command not allowed on client");
                     }
                     break;
                  case CmdLogLevel:
                     if (args.Length > 1)
                     {
                        switch (args[1].Trim())
                        {
                           case CmdLogLevel_All:
                              Mod.Log.LogLevel = Logging.Level.All;
                              MyAPIGateway.Utilities.ShowMessage(CmdKey, string.Format("Logging level switched to All [{0:X}]", Mod.Log.LogLevel));
                              Mod.Log.Write("BuildAndRepairSystemMod: Logging level switched to [{0:X}]", Mod.Log.LogLevel);
                              foreach(var modItem in MyAPIGateway.Session.Mods)
                              {
                                 Mod.Log.Write("BuildAndRepairSystemMod: Mod {0} (Name={1}, FileId={2}, Path={3})", modItem.FriendlyName, modItem.Name, modItem.PublishedFileId, modItem.GetPath());
                              }
                              break;
                           case CmdLogLevel_Default:
                           default:
                              Mod.Log.LogLevel = Settings.LogLevel;
                              MyAPIGateway.Utilities.ShowMessage(CmdKey, string.Format("Logging level switched to Default [{0:X}]", Mod.Log.LogLevel));
                              Mod.Log.Write("BuildAndRepairSystemMod: Logging level switched to [{0:X}]", Mod.Log.LogLevel);
                              break;
                        }
                     }
                     break;
                  case CmdWriteTranslation:
                     if (Mod.Log.ShouldLog(Logging.Level.Verbose)) Mod.Log.Write(Logging.Level.Verbose, "BuildAndRepairSystemMod: CmdWriteTranslation");
                     if (args.Length > 1)
                     {
                        MyLanguagesEnum lang;
                        if (Enum.TryParse(args[1], true, out lang))
                        {
                           Localization.LocalizationHelper.ExportDictionary(lang.ToString() + ".txt", Texts.GetDictionary(lang));
                           MyAPIGateway.Utilities.ShowMessage(CmdKey, string.Format(lang.ToString() + ".txt writtenwa."));
                        } else {
                           MyAPIGateway.Utilities.ShowMessage(CmdKey, string.Format("'{0}' is not a valid language name {1}", args[1], string.Join(",", Enum.GetNames(typeof(MyLanguagesEnum)))));
                        }
                     }
                     break;
                  case CmdHelp1:
                  case CmdHelp2:
                  default:
                     MyAPIGateway.Utilities.ShowMissionScreen("NanobotBuildAndRepairSystem", "Help", "", GetHelpText());
                     break;
               }
            }
            else
            {
               MyAPIGateway.Utilities.ShowMissionScreen("NanobotBuildAndRepairSystem", "Help", "", GetHelpText());
            }
            sendToOthers = false;
         }
      }

      private string GetHelpText()
      {
         var text = string.Format(Texts.Cmd_HelpClient.String, Version, CmdHelp1, CmdHelp2, 
            CmdLogLevel, CmdLogLevel_All, CmdLogLevel_Default, 
            CmdWriteTranslation, string.Join(",", Enum.GetNames(typeof(MyLanguagesEnum))), MyAPIGateway.Utilities.GamePaths.UserDataPath + Path.DirectorySeparatorChar + "Storage" + Path.DirectorySeparatorChar + MyAPIGateway.Utilities.GamePaths.ModScopeName);
         if (MyAPIGateway.Session.IsServer) text += string.Format(Texts.Cmd_HelpServer.String, CmdCwsf, CmdCpsf);
         return text;
      }

      /// <summary>
      /// 
      /// </summary>
      protected override void UnloadData()
      {
         while (true)
         {
            int actualBackgroundTaskCount;
            lock (AsynActions)
            {
               actualBackgroundTaskCount = ActualBackgroundTaskCount;
            }
            if (actualBackgroundTaskCount <= 0) break;
         }

         _Init = false;
         try
         {
            if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null)
            {
               if (MyAPIGateway.Session.IsServer)
               {
                  MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_MOD_DATAREQUEST, SyncModDataRequestReceived);
                  MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_BLOCK_DATAREQUEST, SyncBlockDataRequestReceived);
                  MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_BLOCK_SETTINGS_FROM_CLIENT, SyncBlockSettingsReceived);
               }
               else
               {
                  MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_MOD_SETTINGS, SyncModSettingsReceived);
                  MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_BLOCK_SETTINGS_FROM_SERVER, SyncBlockSettingsReceived);
                  MyAPIGateway.Multiplayer.UnregisterMessageHandler(MSGID_BLOCK_STATE_FROM_SERVER, SyncBlockStateReceived);
               }
            }
            Mod.Log.Write("BuildAndRepairSystemMod: UnloadData.");
            Mod.Log.Close();
         }
         catch (Exception e)
         {
            Mod.Log.Error("NanobotBuildAndRepairSystemMod.UnloadData: {0}", e.ToString());
         }
         base.UnloadData();
      }

      /// <summary>
      /// 
      /// </summary>
      public override void UpdateBeforeSimulation()
      {
         try
         {
            if (!_Init)
            {
               if (MyAPIGateway.Session == null) return;
               Init();
            }
            else
            {
               if (MyAPIGateway.Session.IsServer)
               {
                  RebuildSourcesAndTargetsTimer();
               }
               else if (!SettingsValid)
               {
                  if (MyAPIGateway.Session.ElapsedPlayTime.Subtract(_LastSyncModDataRequestSend) >= TimeSpan.FromSeconds(10))
                  {
                     SyncModDataRequestSend();
                     _LastSyncModDataRequestSend = MyAPIGateway.Session.ElapsedPlayTime;
                  }
               }
            }
         }
         catch (Exception e)
         {
            Mod.Log.Error(e);
         }
      }

      /// <summary>
      /// Damage Handler: Prevent Damage from BuildAndRepairSystem
      /// </summary>
      public void BeforeDamageHandlerNoDamageByBuildAndRepairSystem(object target, ref MyDamageInformation info)
      {
         try
         {
            if (info.Type == MyDamageType.Weld)
            {
               if (target is IMyCharacter)
               {
                  var logicalComponent = BuildAndRepairSystems.GetValueOrDefault(info.AttackerId);
                  if (logicalComponent != null)
                  {
                     var terminalBlock = logicalComponent.Entity as IMyTerminalBlock;
                     if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: Prevent Damage from BuildAndRepairSystem={0} Amount={1}", terminalBlock != null ? terminalBlock.CustomName : logicalComponent.Entity.DisplayName, info.Amount);
                     info.Amount = 0;
                  }
               }
            }
         }
         catch (Exception e)
         {
            Mod.Log.Error("BuildAndRepairSystemMod: Exception in BeforeDamageHandlerNoDamageByBuildAndRepairSystem: Source={0}, Message={1}", e.Source, e.Message);
         }
      }

      /// <summary>
      /// Damage Handler: Register friendly damage
      /// </summary>
      public void AfterDamageHandlerNoDamageByBuildAndRepairSystem(object target, MyDamageInformation info)
      {
         try
         {
            if (info.Type == MyDamageType.Grind && info.Amount > 0)
            {
               var targetBlock = target as IMySlimBlock;
               if (targetBlock != null)
               {
                  IMyEntity attackerEntity;
                  MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attackerEntity);

                  var attackerId = 0L;

                  var shipGrinder = attackerEntity as IMyShipGrinder;
                  if (shipGrinder != null)
                  {
                     attackerId = shipGrinder.OwnerId;
                  }
                  else
                  {
                     var characterGrinder = attackerEntity as IMyEngineerToolBase;
                     if (characterGrinder != null)
                     {
                        attackerId = characterGrinder.OwnerIdentityId;
                     }
                  }
                  if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: AfterDamaged1 {0} from {1} attackerId={2} Amount={3}", Logging.BlockName(target), Logging.BlockName(attackerEntity), attackerId, info.Amount);                  

                  if (attackerId != 0)
                  {
                     if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: Damaged {0} from attackerId={1} Amount={2}", Logging.BlockName(target), attackerId, info.Amount);
                     foreach (var entry in BuildAndRepairSystems)
                     {
                        var relation = entry.Value.Welder.GetUserRelationToOwner(attackerId);
                        if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: {0} Damaged Check Add FriendlyDamage {1} relation {2}", Logging.BlockName(entry.Value.Welder), Logging.BlockName(targetBlock), relation);
                        if (MyRelationsBetweenPlayerAndBlockExtensions.IsFriendly(relation))
                        {
                           //A 'friendly' damage from grinder -> do not repair (for a while)
                           entry.Value.FriendlyDamage[targetBlock] = MyAPIGateway.Session.ElapsedPlayTime + Settings.FriendlyDamageTimeout;
                           if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: {0} Damaged Add FriendlyDamage {0} Timeout {1}", Logging.BlockName(entry.Value.Welder), Logging.BlockName(targetBlock), entry.Value.FriendlyDamage[targetBlock]);
                        }
                     }
                  }
               }
            }
         }
         catch (Exception e)
         {
            Mod.Log.Error("BuildAndRepairSystemMod: Exception in BeforeDamageHandlerNoDamageByBuildAndRepairSystem: Source={0}, Message={1}", e.Source, e.Message);
         }
      }

      /// <summary>
      /// Rebuild the list of targets and inventory sources
      /// </summary>
      protected void RebuildSourcesAndTargetsTimer()
      {
         if (MyAPIGateway.Session.ElapsedPlayTime.Subtract(_LastSourcesAndTargetsUpdateTimer) > SourcesAndTargetsUpdateTimerInterval)
         {
            foreach (var buildAndRepairSystem in BuildAndRepairSystems.Values)
            {
               buildAndRepairSystem.UpdateSourcesAndTargetsTimer();
            }
            _LastSourcesAndTargetsUpdateTimer = MyAPIGateway.Session.ElapsedPlayTime;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newAction"></param>
      public static void AddAsyncAction(Action newAction)
      {
         lock (AsynActions)
         {
            AsynActions.Add(newAction);
            if (ActualBackgroundTaskCount < Settings.MaxBackgroundTasks)
            {
               ActualBackgroundTaskCount++;
               if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: AddAsyncAction Start Task {0} of max {1}", ActualBackgroundTaskCount, Settings.MaxBackgroundTasks);
               MyAPIGateway.Parallel.StartBackground(() =>
               {
                  try
                  {
                     while (true)
                     {
                        Action pendingAction = null;
                        lock (AsynActions)
                        {
                           if (AsynActions.Count > 0)
                           {
                              pendingAction = AsynActions[0];
                              AsynActions.RemoveAt(0);
                           }
                           if (pendingAction == null)
                           {
                              ActualBackgroundTaskCount--;
                              break;
                           }
                        }
                        if (pendingAction != null)
                        {
                           try
                           {
                              if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: AddAsyncAction Task Working start.");
                              pendingAction();
                              if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: AddAsyncAction Task Working finished.");
                           }
                           catch { };
                        }
                     }
                     if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: AddAsyncAction Task ended. Still running {0} of max {1}", ActualBackgroundTaskCount, Settings.MaxBackgroundTasks);
                  }
                  catch {
                     lock (AsynActions)
                     {
                        ActualBackgroundTaskCount--;
                     }
                  }
               });
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncModCommandSend(string command)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncModCommandReceived(byte[] dataRcv)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncModDataRequestSend()
      {
         if (MyAPIGateway.Session.IsServer) return;

         var msgSnd = new MsgModDataRequest();
         if (MyAPIGateway.Session.Player != null)
            msgSnd.SteamId = MyAPIGateway.Session.Player.SteamUserId;
         else
            msgSnd.SteamId = 0;

         if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModDataRequestSend SteamId={0}", msgSnd.SteamId);
         MyAPIGateway.Multiplayer.SendMessageToServer(MSGID_MOD_DATAREQUEST, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), true);
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncModDataRequestReceived(byte[] dataRcv)
      {
         var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgModDataRequest>(dataRcv);
         if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModDataRequestReceived SteamId={0}", msgRcv.SteamId);
         SyncModSettingsSend(msgRcv.SteamId);
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncModSettingsSend(ulong steamId)
      {
         if (!MyAPIGateway.Session.IsServer) return;
         var msgSnd = new MsgModSettings();
         msgSnd.Settings = Settings;
         if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModSettingsSend SteamId={0}", steamId);
         if (!MyAPIGateway.Multiplayer.SendMessageTo(MSGID_MOD_SETTINGS, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), steamId, true))
         {
            if (Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncModSettingsSend failed");
         }
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncModSettingsReceived(byte[] dataRcv)
      {
         try
         {
            var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgModSettings>(dataRcv);
            SyncModSettings.AdjustSettings(msgRcv.Settings);
            Settings = msgRcv.Settings;
            SettingsValid = true;
            if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModSettingsReceived");
            SettingsChanged();
         }
         catch (Exception ex)
         {
            Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncModSettingsReceived Exception:{0}", ex);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public static void SyncBlockDataRequestSend(NanobotBuildAndRepairSystemBlock block)
      {
         if (MyAPIGateway.Session.IsServer) return;

         var msgSnd = new MsgBlockDataRequest();
         if (MyAPIGateway.Session.Player != null)
            msgSnd.SteamId = MyAPIGateway.Session.Player.SteamUserId;
         else
            msgSnd.SteamId = 0;
         msgSnd.EntityId = block.Entity.EntityId;

         if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockDataRequestSend SteamId={0} EntityId={1}/{2}", msgSnd.SteamId, msgSnd.EntityId, Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
         MyAPIGateway.Multiplayer.SendMessageToServer(MSGID_BLOCK_DATAREQUEST, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), true);
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncBlockDataRequestReceived(byte[] dataRcv)
      {

         var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgBlockDataRequest>(dataRcv);

         NanobotBuildAndRepairSystemBlock system;
         if (BuildAndRepairSystems.TryGetValue(msgRcv.EntityId, out system))
         {
            if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockDataRequestReceived SteamId={0} EntityId={1}/{2}", msgRcv.SteamId, msgRcv.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None));
            SyncBlockSettingsSend(msgRcv.SteamId, system);
            SyncBlockStateSend(msgRcv.SteamId, system);
         }
         else
         {
            if (Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockDataRequestReceived for unknown system SteamId{0} EntityId={1}", msgRcv.SteamId, msgRcv.EntityId);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public static void SyncBlockSettingsSend(ulong steamId, NanobotBuildAndRepairSystemBlock block)
      {

         var msgSnd = new MsgBlockSettings();
         msgSnd.EntityId = block.Entity.EntityId;
         msgSnd.Settings = block.Settings.GetTransmit();

         var res = false;
         if (MyAPIGateway.Session.IsServer)
         {
            if (steamId == 0)
            {
               if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsSend To Others EntityId={0}/{1}", block.Entity.EntityId, Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
               res = MyAPIGateway.Multiplayer.SendMessageToOthers(MSGID_BLOCK_SETTINGS_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), true);
            }
            else
            {
               if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsSend To SteamId={2} EntityId={0}/{1}", block.Entity.EntityId, Logging.BlockName(block.Entity, Logging.BlockNameOptions.None), steamId);
               res = MyAPIGateway.Multiplayer.SendMessageTo(MSGID_BLOCK_SETTINGS_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), steamId, true);
            }
         }
         else
         {
            if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsSend To Server EntityId={0}/{1} to Server", block.Entity.EntityId, Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
            res = MyAPIGateway.Multiplayer.SendMessageToServer(MSGID_BLOCK_SETTINGS_FROM_CLIENT, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), true);
         }
         if (!res && Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockSettingsSend failed", Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncBlockSettingsReceived(byte[] dataRcv)
      {
         try
         {
            var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgBlockSettings>(dataRcv);

            NanobotBuildAndRepairSystemBlock system;
            if (BuildAndRepairSystems.TryGetValue(msgRcv.EntityId, out system))
            {
               if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsReceived EntityId={0}/{1}", msgRcv.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None));
               system.Settings.AssignReceived(msgRcv.Settings, system.BlockWeldPriority, system.BlockGrindPriority, system.ComponentCollectPriority);
               system.SettingsChanged();
               if (MyAPIGateway.Session.IsServer)
               {
                  SyncBlockSettingsSend(0, system);
               }
            } else
            {
               if (Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockSettingsReceived for unknown system EntityId={0}", msgRcv.EntityId);
            }
         }
         catch (Exception ex)
         {
            Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockSettingsReceived Exception:{0}", ex);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public static void SyncBlockStateSend(ulong steamId, NanobotBuildAndRepairSystemBlock system)
      {
         if (!MyAPIGateway.Session.IsServer) return;
         if (!MyAPIGateway.Multiplayer.MultiplayerActive) return;


         var msgSnd = new MsgBlockState();
         msgSnd.EntityId = system.Entity.EntityId;
         msgSnd.State = system.State.GetTransmit();

         var res = false;
         if (steamId == 0)
         {
            if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockStateSend to others EntityId={0}/{1}, State={2}", system.Entity.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None), msgSnd.State.ToString());
            res = MyAPIGateway.Multiplayer.SendMessageToOthers(MSGID_BLOCK_STATE_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), true);
         }
         else
         {
            if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockStateSend to SteamId={0} EntityId={1}/{2}, State={3}", steamId, system.Entity.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None), msgSnd.State.ToString());
            res = MyAPIGateway.Multiplayer.SendMessageTo(MSGID_BLOCK_STATE_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), steamId, true);
         }

         if (!res && Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockStateSend Failed");
      }

      /// <summary>
      /// 
      /// </summary>
      private void SyncBlockStateReceived(byte[] dataRcv)
      {
         try
         {
            var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgBlockState>(dataRcv);

            NanobotBuildAndRepairSystemBlock system;
            if (BuildAndRepairSystems.TryGetValue(msgRcv.EntityId, out system))
            {
               if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockStateReceived EntityId={0}/{1}, State={2}", system.Entity.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None), msgRcv.State.ToString());
               system.State.AssignReceived(msgRcv.State);
            }
            else
            {
               if (Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockStateReceived for unknown system EntityId={0}", msgRcv.EntityId);
            }
         }
         catch (Exception ex)
         {
            Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockStateReceived Exception:{0}", ex);
         }
      }

   }
}
