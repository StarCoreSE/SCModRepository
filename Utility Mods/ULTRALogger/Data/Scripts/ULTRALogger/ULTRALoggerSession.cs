using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;

namespace StarCore.ULTRALogger
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ULTRALoggerSession : MySessionComponentBase
    {
        public const long ExternalApiChannel = 1129001129;

        private const long CoreSystemsApiChannel = 67549756549;
        private const long CoreSystemsLoggerModId = 1129001129;
        private const string Command = "/ultralogger";
        private const string ShortCommand = "/ulog";
        private const string SettingsFile = "ULTRALogger.cfg";

        private readonly object _sync = new object();
        private readonly Queue<string> _queue = new Queue<string>();
        private readonly Dictionary<long, MyCubeGrid> _trackedGrids = new Dictionary<long, MyCubeGrid>();
        private readonly Dictionary<ulong, PlayerSnapshot> _players = new Dictionary<ulong, PlayerSnapshot>();
        private readonly List<IMyPlayer> _playerBuffer = new List<IMyPlayer>();
        private readonly HashSet<ulong> _currentPlayerIds = new HashSet<ulong>();
        private readonly List<ulong> _lostPlayers = new List<ulong>();

        private ULTRALoggerSettings _settings = new ULTRALoggerSettings();
        private Dictionary<string, Delegate> _apiMethods;
        private Action<long, int, Action<ListReader<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>>>> _registerCoreSystemsDamageEvent;
        private TextWriter _writer;
        private int _lastFlushFrame;
        private int _lastPlayerScanFrame;
        private int _droppedLines;
        private bool _server;
        private bool _started;
        private bool _closed;
        private bool _failed;
        private bool _entityHandlersRegistered;
        private bool _messageHandlersRegistered;
        private bool _coreSystemsMessageRegistered;
        private bool _coreSystemsDamageRegistered;

        public override void LoadData()
        {
            try
            {
                _server = MyAPIGateway.Session != null && MyAPIGateway.Session.IsServer;
                if (!_server)
                    return;

                OpenWriter();
                LoadSettings();
                RegisterSessionEvents();
                TrackExistingGrids();
                ScanPlayers(true);

                _started = true;
                Enqueue("SYSTEM", "ULTRALogger started. Enabled=" + _settings.Enabled, true);
                FlushQueue();
            }
            catch (Exception ex)
            {
                FailClosed("LoadData", ex);
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!_server || _closed || _failed || !_started)
                return;

            try
            {
                int frame = MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;

                if (frame - _lastPlayerScanFrame >= SecondsToFrames(_settings.PlayerScanIntervalSeconds))
                {
                    _lastPlayerScanFrame = frame;
                    ScanPlayers(false);
                }

                if (frame - _lastFlushFrame >= SecondsToFrames(_settings.FlushIntervalSeconds))
                {
                    _lastFlushFrame = frame;
                    FlushQueue();
                }
            }
            catch (Exception ex)
            {
                FailClosed("UpdateAfterSimulation", ex);
            }
        }

        protected override void UnloadData()
        {
            if (_server)
            {
                Enqueue("SYSTEM", "ULTRALogger unloading.", true);
                FlushQueue();
            }

            _closed = true;
            DetachAllEvents();
            _players.Clear();
            _queue.Clear();
            _apiMethods = null;

            if (_writer != null)
            {
                try
                {
                    _writer.Flush();
                    _writer.Close();
                }
                catch
                {
                }

                _writer = null;
            }
        }

        private void RegisterSessionEvents()
        {
            if (_entityHandlersRegistered)
                return;

            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
            _entityHandlersRegistered = true;

            MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEnteredSender;
            MyAPIGateway.Utilities.RegisterMessageHandler(ExternalApiChannel, OnExternalApiMessage);
            _messageHandlersRegistered = true;

            MyAPIGateway.Utilities.RegisterMessageHandler(CoreSystemsApiChannel, OnCoreSystemsApiMessage);
            MyAPIGateway.Utilities.SendModMessage(CoreSystemsApiChannel, "ApiEndpointRequest");
            _coreSystemsMessageRegistered = true;

            if (MyAPIGateway.Session != null && MyAPIGateway.Session.DamageSystem != null)
            {
                MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(0, OnAfterDamage);
            }

            _apiMethods = new Dictionary<string, Delegate>
            {
                { "Log", new Action<string, string, string, long>(ExternalLog) },
                { "LogSimple", new Action<string>(ExternalLogSimple) },
                { "IsEnabled", new Func<bool>(IsExternalApiEnabled) }
            };
        }

        private void DetachAllEvents()
        {
            if (!_server)
                return;

            if (_entityHandlersRegistered)
            {
                try
                {
                    MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
                    MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
                }
                catch
                {
                }

                _entityHandlersRegistered = false;
            }

            if (_messageHandlersRegistered)
            {
                try
                {
                    MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEnteredSender;
                    MyAPIGateway.Utilities.UnregisterMessageHandler(ExternalApiChannel, OnExternalApiMessage);
                }
                catch
                {
                }

                _messageHandlersRegistered = false;
            }

            if (_coreSystemsDamageRegistered && _registerCoreSystemsDamageEvent != null)
            {
                try
                {
                    _registerCoreSystemsDamageEvent(CoreSystemsLoggerModId, 0, OnCoreSystemsProjectileDamage);
                }
                catch
                {
                }

                _coreSystemsDamageRegistered = false;
            }

            if (_coreSystemsMessageRegistered)
            {
                try
                {
                    MyAPIGateway.Utilities.UnregisterMessageHandler(CoreSystemsApiChannel, OnCoreSystemsApiMessage);
                }
                catch
                {
                }

                _coreSystemsMessageRegistered = false;
            }

            _registerCoreSystemsDamageEvent = null;

            var grids = new List<MyCubeGrid>(_trackedGrids.Values);
            foreach (var grid in grids)
                UntrackGrid(grid, false);

            _trackedGrids.Clear();
        }

        private void OpenWriter()
        {
            string fileName = "ULTRALogger_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
            _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(ULTRALoggerSession));
        }

        private void LoadSettings()
        {
            _settings = new ULTRALoggerSettings();

            try
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(SettingsFile, typeof(ULTRALoggerSession)))
                {
                    using (TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(SettingsFile, typeof(ULTRALoggerSession)))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                            _settings.Apply(line);
                    }
                }

                _settings.Clamp();
                SaveSettings();
            }
            catch (Exception ex)
            {
                _settings = new ULTRALoggerSettings();
                Enqueue("CONFIG", "Failed to load settings. Defaults restored. " + ex.Message, true);
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(SettingsFile, typeof(ULTRALoggerSession)))
                    writer.Write(_settings.ToFileText());
            }
            catch (Exception ex)
            {
                Enqueue("CONFIG", "Failed to save settings. " + ex.Message, true);
            }
        }

        private void TrackExistingGrids()
        {
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            foreach (IMyEntity entity in entities)
            {
                var grid = entity as MyCubeGrid;
                if (grid != null)
                    TrackGrid(grid, "existing");
            }
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            RunSafe("OnEntityAdd", delegate
            {
                var grid = entity as MyCubeGrid;
                if (grid == null)
                    return;

                TrackGrid(grid, "added");
            });
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            RunSafe("OnEntityRemove", delegate
            {
                var grid = entity as MyCubeGrid;
                if (grid == null)
                    return;

                if (_settings.LogGridLifecycle)
                    Enqueue("GRID_REMOVE", DescribeGrid(grid));

                UntrackGrid(grid, true);
            });
        }

        private void TrackGrid(MyCubeGrid grid, string reason)
        {
            if (grid == null || grid.MarkedForClose || _trackedGrids.ContainsKey(grid.EntityId))
                return;

            _trackedGrids[grid.EntityId] = grid;

            grid.OnBlockAdded += OnBlockAdded;
            grid.OnBlockRemoved += OnBlockRemoved;
            grid.OnBlockIntegrityChanged += OnBlockIntegrityChanged;
            grid.OnBlockOwnershipChanged += OnBlockOwnershipChanged;
            grid.OnGridSplit += OnGridSplit;
            grid.OnStaticChanged += OnStaticChanged;
            grid.OnClosing += OnGridClosing;

            if (_settings.LogGridLifecycle)
                Enqueue("GRID_TRACK", reason + " " + DescribeGrid(grid));
        }

        private void UntrackGrid(MyCubeGrid grid, bool removeFromMap)
        {
            if (grid == null)
                return;

            try
            {
                grid.OnBlockAdded -= OnBlockAdded;
                grid.OnBlockRemoved -= OnBlockRemoved;
                grid.OnBlockIntegrityChanged -= OnBlockIntegrityChanged;
                grid.OnBlockOwnershipChanged -= OnBlockOwnershipChanged;
                grid.OnGridSplit -= OnGridSplit;
                grid.OnStaticChanged -= OnStaticChanged;
                grid.OnClosing -= OnGridClosing;
            }
            catch
            {
            }

            if (removeFromMap)
                _trackedGrids.Remove(grid.EntityId);
        }

        private void OnBlockAdded(IMySlimBlock block)
        {
            RunSafe("OnBlockAdded", delegate
            {
                if (_settings.LogBlocks)
                    Enqueue("BLOCK_ADD", DescribeBlock(block));
            });
        }

        private void OnBlockRemoved(IMySlimBlock block)
        {
            RunSafe("OnBlockRemoved", delegate
            {
                if (_settings.LogBlocks)
                    Enqueue("BLOCK_REMOVE", DescribeBlock(block));
            });
        }

        private void OnBlockIntegrityChanged(IMySlimBlock block)
        {
            RunSafe("OnBlockIntegrityChanged", delegate
            {
                if (_settings.LogBlocks)
                    Enqueue("BLOCK_INTEGRITY", DescribeBlock(block) + " integrity=" + SafeDouble(block == null ? 0 : block.Integrity));
            });
        }

        private void OnBlockOwnershipChanged(MyCubeGrid grid)
        {
            RunSafe("OnBlockOwnershipChanged", delegate
            {
                if (_settings.LogGridLifecycle)
                    Enqueue("GRID_OWNERSHIP", DescribeGrid(grid));
            });
        }

        private void OnGridSplit(MyCubeGrid originalGrid, MyCubeGrid newGrid)
        {
            RunSafe("OnGridSplit", delegate
            {
                if (_settings.LogGridLifecycle)
                    Enqueue("GRID_SPLIT", "original=" + DescribeGrid(originalGrid) + " new=" + DescribeGrid(newGrid));

                if (newGrid != null)
                    TrackGrid(newGrid, "split");
            });
        }

        private void OnStaticChanged(MyCubeGrid grid, bool isStatic)
        {
            RunSafe("OnStaticChanged", delegate
            {
                if (_settings.LogGridLifecycle)
                    Enqueue("GRID_STATIC", DescribeGrid(grid) + " isStatic=" + isStatic);
            });
        }

        private void OnGridClosing(MyEntity entity)
        {
            RunSafe("OnGridClosing", delegate
            {
                var grid = entity as MyCubeGrid;
                if (grid == null)
                    return;

                if (_settings.LogGridLifecycle)
                    Enqueue("GRID_CLOSING", DescribeGrid(grid));

                UntrackGrid(grid, true);
            });
        }

        private void OnAfterDamage(object target, MyDamageInformation info)
        {
            RunSafe("OnAfterDamage", delegate
            {
                if (!_settings.LogDamage)
                    return;

                string targetText = DescribeDamageTarget(target);
                Enqueue("DAMAGE", targetText + " attacker=" + info.AttackerId + " amount=" + SafeDouble(info.Amount) + " type=" + info.Type);
            });
        }

        private void ScanPlayers(bool startup)
        {
            if (!_settings.LogPlayers)
                return;

            _playerBuffer.Clear();
            _currentPlayerIds.Clear();
            _lostPlayers.Clear();

            MyAPIGateway.Players.GetPlayers(_playerBuffer, player => player != null && !player.IsBot);

            foreach (IMyPlayer player in _playerBuffer)
            {
                if (player == null)
                    continue;

                ulong steamId = player.SteamUserId;
                _currentPlayerIds.Add(steamId);

                PlayerSnapshot snapshot = PlayerSnapshot.FromPlayer(player);
                PlayerSnapshot previous;

                if (!_players.TryGetValue(steamId, out previous))
                {
                    _players[steamId] = snapshot;
                    Enqueue(startup ? "PLAYER_ONLINE" : "PLAYER_JOIN", snapshot.ToLogText());
                    continue;
                }

                if (previous.ControlledEntityId != snapshot.ControlledEntityId)
                    Enqueue("PLAYER_CONTROL", snapshot.ToLogText() + " previousEntity=" + previous.ControlledEntityId);

                if (!string.Equals(previous.DisplayName, snapshot.DisplayName, StringComparison.Ordinal))
                    Enqueue("PLAYER_NAME", previous.DisplayName + " -> " + snapshot.DisplayName + " steam=" + steamId);

                _players[steamId] = snapshot;
            }

            foreach (var pair in _players)
            {
                if (!_currentPlayerIds.Contains(pair.Key))
                    _lostPlayers.Add(pair.Key);
            }

            foreach (ulong steamId in _lostPlayers)
            {
                PlayerSnapshot snapshot;
                if (_players.TryGetValue(steamId, out snapshot))
                    Enqueue("PLAYER_LEAVE", snapshot.ToLogText());

                _players.Remove(steamId);
            }
        }

        private void OnMessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            RunSafe("OnMessageEnteredSender", delegate
            {
                if (string.IsNullOrWhiteSpace(messageText))
                    return;

                if (!StartsWithCommand(messageText, Command) && !StartsWithCommand(messageText, ShortCommand))
                    return;

                sendToOthers = false;

                if (!IsCommandSenderAdmin(sender))
                {
                    Reply("Admin privileges are required.");
                    Enqueue("COMMAND_DENIED", "sender=" + sender + " command=" + messageText, true);
                    return;
                }

                HandleCommand(sender, messageText);
            });
        }

        private void HandleCommand(ulong sender, string messageText)
        {
            string[] parts = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                Reply("Use /ultralogger status, on, off, flush, saveinterval, scaninterval, maxqueue, damage, blocks, grids, players, or external.");
                return;
            }

            string verb = parts[1].ToLowerInvariant();

            if (verb == "status")
            {
                Reply(_settings.ToStatusText(_queue.Count, _droppedLines, _trackedGrids.Count, _players.Count, _failed));
            }
            else if (verb == "on")
            {
                _settings.Enabled = true;
                SaveSettings();
                Reply("Logging enabled.");
                Enqueue("COMMAND", "sender=" + sender + " enabled logging", true);
            }
            else if (verb == "off")
            {
                Enqueue("COMMAND", "sender=" + sender + " disabled logging", true);
                _settings.Enabled = false;
                SaveSettings();
                Reply("Logging disabled.");
            }
            else if (verb == "flush")
            {
                FlushQueue();
                Reply("Log queue flushed.");
            }
            else if (verb == "saveinterval")
            {
                SetIntCommand(parts, 2, 1, 3600, "save interval", delegate(int value) { _settings.FlushIntervalSeconds = value; });
            }
            else if (verb == "scaninterval")
            {
                SetIntCommand(parts, 2, 1, 3600, "player scan interval", delegate(int value) { _settings.PlayerScanIntervalSeconds = value; });
            }
            else if (verb == "maxqueue")
            {
                SetIntCommand(parts, 2, 100, 100000, "max queued lines", delegate(int value) { _settings.MaxQueuedLines = value; });
            }
            else if (verb == "damage")
            {
                SetBoolCommand(parts, 2, "damage logging", delegate(bool value) { _settings.LogDamage = value; });
            }
            else if (verb == "blocks")
            {
                SetBoolCommand(parts, 2, "block logging", delegate(bool value) { _settings.LogBlocks = value; });
            }
            else if (verb == "grids")
            {
                SetBoolCommand(parts, 2, "grid logging", delegate(bool value) { _settings.LogGridLifecycle = value; });
            }
            else if (verb == "players")
            {
                SetBoolCommand(parts, 2, "player logging", delegate(bool value) { _settings.LogPlayers = value; });
            }
            else if (verb == "external")
            {
                SetBoolCommand(parts, 2, "external API logging", delegate(bool value) { _settings.LogExternalApi = value; });
            }
            else
            {
                Reply("Unknown ULTRALogger command: " + verb);
            }
        }

        private void SetIntCommand(string[] parts, int index, int min, int max, string label, Action<int> setter)
        {
            if (parts.Length <= index)
            {
                Reply("Missing value for " + label + ".");
                return;
            }

            int value;
            if (!int.TryParse(parts[index], out value))
            {
                Reply("Invalid number for " + label + ".");
                return;
            }

            value = Math.Max(min, Math.Min(max, value));
            setter(value);
            _settings.Clamp();
            SaveSettings();
            Reply(label + " set to " + value + ".");
            Enqueue("COMMAND", label + " set to " + value, true);
        }

        private void SetBoolCommand(string[] parts, int index, string label, Action<bool> setter)
        {
            if (parts.Length <= index)
            {
                Reply("Missing on/off value for " + label + ".");
                return;
            }

            bool value;
            if (!TryParseBool(parts[index], out value))
            {
                Reply("Use on/off for " + label + ".");
                return;
            }

            setter(value);
            SaveSettings();
            Reply(label + " " + (value ? "enabled." : "disabled."));
            Enqueue("COMMAND", label + " set to " + value, true);
        }

        private void OnExternalApiMessage(object message)
        {
            RunSafe("OnExternalApiMessage", delegate
            {
                if (!_settings.LogExternalApi)
                    return;

                var text = message as string;
                if (text != null)
                {
                    if (string.Equals(text, "ApiEndpointRequest", StringComparison.OrdinalIgnoreCase))
                    {
                        MyAPIGateway.Utilities.SendModMessage(ExternalApiChannel, _apiMethods);
                        return;
                    }

                    Enqueue("MODAPI", text);
                    return;
                }

                var apiMessage = message as ULTRALoggerExternalMessage;
                if (apiMessage != null)
                {
                    ExternalLog(apiMessage.Source, apiMessage.Category, apiMessage.Message, apiMessage.EntityId);
                    return;
                }

                if (message is Dictionary<string, Delegate>)
                    return;

                Enqueue("MODAPI", message == null ? "null" : message.ToString());
            });
        }

        private void OnCoreSystemsApiMessage(object message)
        {
            RunSafe("OnCoreSystemsApiMessage", delegate
            {
                if (_coreSystemsDamageRegistered || message is string)
                    return;

                var delegates = message as IReadOnlyDictionary<string, Delegate>;
                if (delegates == null)
                    return;

                Delegate raw;
                if (!delegates.TryGetValue("RegisterDamageEvent", out raw))
                    return;

                _registerCoreSystemsDamageEvent = raw as Action<long, int, Action<ListReader<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>>>>;
                if (_registerCoreSystemsDamageEvent == null)
                    return;

                _registerCoreSystemsDamageEvent(CoreSystemsLoggerModId, 1, OnCoreSystemsProjectileDamage);
                _coreSystemsDamageRegistered = true;
                Enqueue("MODAPI", "CoreSystems projectile damage hook registered.", true);
            });
        }

        private void OnCoreSystemsProjectileDamage(ListReader<MyTuple<ulong, long, int, MyEntity, MyEntity, ListReader<MyTuple<Vector3D, object, float>>>> projectiles)
        {
            RunSafe("OnCoreSystemsProjectileDamage", delegate
            {
                if (!_settings.LogExternalApi)
                    return;

                foreach (var projectile in projectiles)
                {
                    int hitCount = 0;
                    float damage = 0f;

                    foreach (var hit in projectile.Item6)
                    {
                        hitCount++;
                        damage += hit.Item3;
                    }

                    Enqueue(
                        "CORESYSTEMS_DAMAGE",
                        "projectile=" + projectile.Item1 +
                        " player=" + projectile.Item2 +
                        " weaponId=" + projectile.Item3 +
                        " weapon={" + DescribeEntity(projectile.Item4) + "}" +
                        " parent={" + DescribeEntity(projectile.Item5) + "}" +
                        " hits=" + hitCount +
                        " totalDamage=" + SafeDouble(damage));
                }
            });
        }

        private void ExternalLog(string source, string category, string message, long entityId)
        {
            if (!_settings.LogExternalApi)
                return;

            Enqueue("MODAPI", "source=" + Clean(source) + " category=" + Clean(category) + " entity=" + entityId + " message=" + Clean(message));
        }

        private void ExternalLogSimple(string message)
        {
            if (!_settings.LogExternalApi)
                return;

            Enqueue("MODAPI", Clean(message));
        }

        private bool IsExternalApiEnabled()
        {
            return !_closed && !_failed && _settings.Enabled && _settings.LogExternalApi;
        }

        private void Enqueue(string category, string message, bool force = false)
        {
            if (_closed || (!force && (!_settings.Enabled || _failed)))
                return;

            string line = FormatLine(category, message);

            lock (_sync)
            {
                if (_queue.Count >= _settings.MaxQueuedLines && !force)
                {
                    _droppedLines++;
                    return;
                }

                _queue.Enqueue(line);
            }
        }

        private void FlushQueue()
        {
            if (_writer == null)
                return;

            var lines = new List<string>();

            lock (_sync)
            {
                if (_droppedLines > 0)
                {
                    _queue.Enqueue(FormatLine("QUEUE", "Dropped " + _droppedLines + " log lines because MaxQueuedLines was reached."));
                    _droppedLines = 0;
                }

                while (_queue.Count > 0)
                    lines.Add(_queue.Dequeue());
            }

            foreach (string line in lines)
                _writer.WriteLine(line);

            _writer.Flush();
        }

        private void FailClosed(string context, Exception ex)
        {
            if (_failed)
                return;

            _failed = true;
            _settings.Enabled = false;

            try
            {
                Enqueue("FATAL", context + " failed. Logger disabled and events detached. " + ex, true);
                FlushQueue();
            }
            catch
            {
            }

            DetachAllEvents();
        }

        private void RunSafe(string context, Action action)
        {
            if (_closed || _failed)
                return;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                FailClosed(context, ex);
            }
        }

        private static int SecondsToFrames(int seconds)
        {
            return Math.Max(1, seconds) * 60;
        }

        private static bool StartsWithCommand(string message, string command)
        {
            if (!message.StartsWith(command, StringComparison.OrdinalIgnoreCase))
                return false;

            return message.Length == command.Length || message[command.Length] == ' ';
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        private static bool IsCommandSenderAdmin(ulong sender)
        {
            if (MyAPIGateway.Session == null)
                return false;

            MyPromoteLevel level = MyAPIGateway.Session.GetUserPromoteLevel(sender);
            return level == MyPromoteLevel.Owner || level == MyPromoteLevel.Admin || level == MyPromoteLevel.SpaceMaster;
        }

        private static void Reply(string message)
        {
            MyAPIGateway.Utilities.ShowMessage("ULTRALogger", message);
        }

        private static string FormatLine(string category, string message)
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + category + "] [T" + Environment.CurrentManagedThreadId + "] " + Clean(message);
        }

        private static string DescribeGrid(MyCubeGrid grid)
        {
            if (grid == null)
                return "grid=null";

            return "gridId=" + grid.EntityId +
                   " name=\"" + Clean(grid.CustomName) + "\"" +
                   " blocks=" + grid.BlocksCount +
                   " size=" + grid.GridSizeEnum +
                   " static=" + grid.IsStatic +
                   " position=" + FormatPosition(grid.GetPosition());
        }

        private static string DescribeBlock(IMySlimBlock block)
        {
            if (block == null)
                return "block=null";

            IMyCubeGrid grid = block.CubeGrid;
            IMyCubeBlock fat = block.FatBlock;
            string definition = block.BlockDefinition.Id.ToString();

            var builder = block.BuiltBy;
            long owner = fat == null ? 0 : fat.OwnerId;
            long entityId = fat == null ? 0 : fat.EntityId;
            IMyTerminalBlock terminal = fat as IMyTerminalBlock;
            string customName = terminal == null ? "" : terminal.CustomName;

            return "gridId=" + (grid == null ? 0 : grid.EntityId) +
                   " blockEntity=" + entityId +
                   " owner=" + owner +
                   " builtBy=" + builder +
                   " def=" + Clean(definition) +
                   " name=\"" + Clean(customName) + "\"";
        }

        private static string DescribeDamageTarget(object target)
        {
            var slim = target as IMySlimBlock;
            if (slim != null)
                return "targetBlock={" + DescribeBlock(slim) + "}";

            var entity = target as IMyEntity;
            if (entity != null)
                return "targetEntity={" + DescribeEntity(entity) + "}";

            return "target=" + (target == null ? "null" : target.GetType().Name);
        }

        private static string DescribeEntity(IMyEntity entity)
        {
            if (entity == null)
                return "entity=null";

            return "entityId=" + entity.EntityId + " type=" + entity.GetType().Name + " name=\"" + Clean(entity.DisplayName) + "\" position=" + FormatPosition(entity.GetPosition());
        }

        private static string FormatPosition(Vector3D position)
        {
            return "(" + SafeDouble(position.X) + "," + SafeDouble(position.Y) + "," + SafeDouble(position.Z) + ")";
        }

        private static string SafeDouble(double value)
        {
            return value.ToString("0.###");
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        }

        private class PlayerSnapshot
        {
            public ulong SteamId;
            public long IdentityId;
            public string DisplayName;
            public long ControlledEntityId;
            public string ControlledEntityName;

            public static PlayerSnapshot FromPlayer(IMyPlayer player)
            {
                var snapshot = new PlayerSnapshot();
                snapshot.SteamId = player.SteamUserId;
                snapshot.IdentityId = player.IdentityId;
                snapshot.DisplayName = player.DisplayName;

                IMyControllableEntity controlled = player.Controller == null ? null : player.Controller.ControlledEntity;
                IMyEntity entity = controlled == null ? null : controlled.Entity;
                snapshot.ControlledEntityId = entity == null ? 0 : entity.EntityId;
                snapshot.ControlledEntityName = entity == null ? "" : entity.DisplayName;

                return snapshot;
            }

            public string ToLogText()
            {
                return "steam=" + SteamId +
                       " identity=" + IdentityId +
                       " name=\"" + Clean(DisplayName) + "\"" +
                       " controlledEntity=" + ControlledEntityId +
                       " controlledName=\"" + Clean(ControlledEntityName) + "\"";
            }
        }
    }

    public class ULTRALoggerExternalMessage
    {
        public string Source;
        public string Category;
        public string Message;
        public long EntityId;
    }

    public class ULTRALoggerSettings
    {
        public bool Enabled = true;
        public bool LogDamage = true;
        public bool LogBlocks = true;
        public bool LogGridLifecycle = true;
        public bool LogPlayers = true;
        public bool LogExternalApi = true;
        public int FlushIntervalSeconds = 5;
        public int PlayerScanIntervalSeconds = 5;
        public int MaxQueuedLines = 5000;

        public void Apply(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            string trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
                return;

            int index = trimmed.IndexOf('=');
            if (index <= 0)
                return;

            string key = trimmed.Substring(0, index).Trim();
            string value = trimmed.Substring(index + 1).Trim();

            bool boolValue;
            int intValue;

            if (key.Equals("Enabled", StringComparison.OrdinalIgnoreCase) && TryParseBool(value, out boolValue))
                Enabled = boolValue;
            else if (key.Equals("LogDamage", StringComparison.OrdinalIgnoreCase) && TryParseBool(value, out boolValue))
                LogDamage = boolValue;
            else if (key.Equals("LogBlocks", StringComparison.OrdinalIgnoreCase) && TryParseBool(value, out boolValue))
                LogBlocks = boolValue;
            else if (key.Equals("LogGridLifecycle", StringComparison.OrdinalIgnoreCase) && TryParseBool(value, out boolValue))
                LogGridLifecycle = boolValue;
            else if (key.Equals("LogPlayers", StringComparison.OrdinalIgnoreCase) && TryParseBool(value, out boolValue))
                LogPlayers = boolValue;
            else if (key.Equals("LogExternalApi", StringComparison.OrdinalIgnoreCase) && TryParseBool(value, out boolValue))
                LogExternalApi = boolValue;
            else if (key.Equals("FlushIntervalSeconds", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out intValue))
                FlushIntervalSeconds = intValue;
            else if (key.Equals("PlayerScanIntervalSeconds", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out intValue))
                PlayerScanIntervalSeconds = intValue;
            else if (key.Equals("MaxQueuedLines", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out intValue))
                MaxQueuedLines = intValue;
        }

        public void Clamp()
        {
            FlushIntervalSeconds = Math.Max(1, Math.Min(3600, FlushIntervalSeconds));
            PlayerScanIntervalSeconds = Math.Max(1, Math.Min(3600, PlayerScanIntervalSeconds));
            MaxQueuedLines = Math.Max(100, Math.Min(100000, MaxQueuedLines));
        }

        public string ToFileText()
        {
            Clamp();

            var builder = new StringBuilder();
            builder.AppendLine("# ULTRALogger server settings");
            builder.AppendLine("Enabled=" + Enabled);
            builder.AppendLine("LogDamage=" + LogDamage);
            builder.AppendLine("LogBlocks=" + LogBlocks);
            builder.AppendLine("LogGridLifecycle=" + LogGridLifecycle);
            builder.AppendLine("LogPlayers=" + LogPlayers);
            builder.AppendLine("LogExternalApi=" + LogExternalApi);
            builder.AppendLine("FlushIntervalSeconds=" + FlushIntervalSeconds);
            builder.AppendLine("PlayerScanIntervalSeconds=" + PlayerScanIntervalSeconds);
            builder.AppendLine("MaxQueuedLines=" + MaxQueuedLines);
            return builder.ToString();
        }

        public string ToStatusText(int queued, int dropped, int grids, int players, bool failed)
        {
            return "enabled=" + Enabled +
                   " failed=" + failed +
                   " queued=" + queued +
                   " dropped=" + dropped +
                   " grids=" + grids +
                   " players=" + players +
                   " saveInterval=" + FlushIntervalSeconds +
                   " scanInterval=" + PlayerScanIntervalSeconds;
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "off", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }
    }
}
