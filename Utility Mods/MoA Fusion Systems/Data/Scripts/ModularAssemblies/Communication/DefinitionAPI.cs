using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Epstein_Fusion_DS.Communication
{
    /// <summary>
    ///     Class used to communicate with the Modular Assemblies Framework mod. <br /><br />
    ///     Want to include in this your own mod? Check out the following documentation link: <br />
    ///     <see href="https://github.com/StarCoreSE/Modular-Assemblies/wiki/The-Modular-API"></see>
    /// </summary>
    public class ModularDefinitionApi
    {
        /// <summary>
        ///     The expected API version. Don't touch this unless you're developing for the Modular Assemblies Framework.
        /// </summary>
        public const int ApiVersion = 3;

        /// <summary>
        ///     Triggered whenever the API is ready - added to by the constructor or manually.
        /// </summary>
        public Action OnReady;

        /// <summary>
        ///     The currently loaded Modular Assemblies Framework version.
        ///     <remarks>
        ///         Not the API version; see <see cref="ApiVersion" />
        ///     </remarks>
        /// </summary>
        public int FrameworkVersion { get; private set; } = -1;

        /// <summary>
        ///     Displays whether endpoints are loaded and the API is ready for use.
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        ///     Call this to initialize the Modular API.<br />
        ///     <remarks>
        ///         API methods will be unusable until the endpoints are populated. Check <see cref="IsReady" /> or utilize
        ///         <see cref="OnReady" /> for safety.
        ///     </remarks>
        /// </summary>
        /// <param name="modContext"></param>
        /// <param name="onLoad">Method to be triggered when the API is ready.</param>
        /// <exception cref="Exception"></exception>
        public void Init(IMyModContext modContext, Action onLoad = null)
        {
            if (_isRegistered)
                throw new Exception($"{GetType().Name}.Load() should not be called multiple times!");

            _modContext = modContext;
            OnReady = onLoad;
            _isRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(ApiChannel, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(ApiChannel, "ApiEndpointRequest");
            MyLog.Default.WriteLineAndConsole(
                $"{_modContext.ModName}: ModularDefinitionsAPI listening for API methods...");
        }

        /// <summary>
        ///     Call this to unload the Modular API; i.e. in case of instantiating a new API or for freeing up resources.
        ///     <remarks>
        ///         This method will also be called automatically when the Modular Assemblies Framework is
        ///         closed.
        ///     </remarks>
        /// </summary>
        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiChannel, HandleMessage);

            if (_apiInit)
                ApiAssign(); // Clear API methods if the API is currently inited.

            _isRegistered = false;
            _apiInit = false;
            IsReady = false;
            OnReady = null;
            MyLog.Default.WriteLineAndConsole($"{_modContext.ModName}: ModularDefinitionsAPI unloaded.");
        }

        // These sections are what the user can actually see when referencing the API, and can be used freely. //
        // Note the null checks. //

        #region Global Assembly Methods

        /// <summary>
        ///     Gets all AssemblyParts in the world. Returns an array of all AssemblyParts.
        /// </summary>
        public IMyCubeBlock[] GetAllParts()
        {
            return _getAllParts?.Invoke();
        }

        /// <summary>
        ///     Gets all PhysicalAssembly ids in the world. Returns an empty list on fail.
        ///     <para>
        ///         Arg1 is assembly id
        ///     </para>
        /// </summary>
        public int[] GetAllAssemblies()
        {
            return _getAllAssemblies?.Invoke();
        }

        /// <summary>
        ///     Gets all PhysicalAssembly ids on a specific grid. Returns an empty list on fail.
        ///     <para>
        ///         Arg1 is assembly id
        ///     </para>
        /// </summary>
        public int[] GetGridAssemblies(IMyCubeGrid grid)
        {
            var allAssemblies = GetAllAssemblies();
            var validAssemblies = new List<int>();

            foreach (var assemblyId in allAssemblies)
                if (GetAssemblyGrid(assemblyId) == grid)
                    validAssemblies.Add(assemblyId);

            return validAssemblies.ToArray();
        }

        #endregion

        #region Per-Assembly Methods

        /// <summary>
        ///     Gets all member parts of a assembly. Returns an empty list on fail.
        ///     <para>
        ///         Arg1 is EntityId
        ///     </para>
        /// </summary>
        public IMyCubeBlock[] GetMemberParts(int assemblyId)
        {
            return _getMemberParts?.Invoke(assemblyId);
        }

        /// <summary>
        ///     Gets the base part of a PhysicalAssembly. Returns null if assembly does not exist.
        /// </summary>
        public IMyCubeBlock GetBasePart(int assemblyId)
        {
            return _getBasePart?.Invoke(assemblyId);
        }

        /// <summary>
        ///     Returns the IMyCubeGrid containing a given assembly ID.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <returns></returns>
        public IMyCubeGrid GetAssemblyGrid(int assemblyId)
        {
            return _getAssemblyGrid?.Invoke(assemblyId);
        }

        /// <summary>
        ///     Registers an Action<AssemblyId> triggered on assembly removal.
        /// </summary>
        /// <param name="action"></param>
        public void RegisterOnAssemblyClose(string definitionName, Action<int> action)
        {
            _registerOnAssemblyClose?.Invoke(definitionName, action);
        }

        /// <summary>
        ///     De-registers an Action(AssemblyId) triggered on assembly removal.
        /// </summary>
        /// <param name="action"></param>
        public void UnregisterOnAssemblyClose(string definitionName, Action<int> action)
        {
            _unregisterOnAssemblyClose?.Invoke(definitionName, action);
        }

        /// <summary>
        ///     Removes all blocks from the assembly and queues them for a connection check.
        /// </summary>
        /// <param name="assemblyId"></param>
        public void RecreateAssembly(int assemblyId)
        {
            _recreateAssembly?.Invoke(assemblyId);
        }

        /// <summary>
        ///     Returns a given property of an assembly, or the default value of T if it could not be found.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public T GetAssemblyProperty<T>(int assemblyId, string propertyName)
        {
            object value = _getAssemblyProperty(assemblyId, propertyName);

            return value == null ? default(T) : (T) value;
        }

        /// <summary>
        ///     Sets a global property of an assembly. Properties are saved to the assembly, and are accessible by all mods.<br />
        ///     <remarks>
        ///         Properties can be removed by setting them to null. value must be a byte array, string, bool, or number.
        ///     </remarks>
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetAssemblyProperty<T>(int assemblyId, string propertyName, T value)
        {
            if (!(value is byte[] || value is string || value is bool ||
                  value is int || value is short || value is float || value is double || value is long))
                return;

            _setAssemblyProperty?.Invoke(assemblyId, propertyName, value);
        }

        /// <summary>
        ///     Lists all set properties of an assembly.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <returns></returns>
        public string[] ListAssemblyProperties(int assemblyId)
        {
            return _listAssemblyProperties?.Invoke(assemblyId);
        }

        #endregion

        #region Per-Part Methods

        /// <summary>
        ///     Gets all connected parts to a block. Returns an empty list on fail.
        ///     <para>
        ///         <paramref name="useCached" />: Set this to 'false' if used in OnPartAdd.
        ///     </para>
        /// </summary>
        public IMyCubeBlock[] GetConnectedBlocks(IMyCubeBlock partBlockId, string definition, bool useCached = true)
        {
            return _getConnectedBlocks?.Invoke(partBlockId, definition, useCached);
        }

        /// <summary>
        ///     Returns the ID of the assembly containing a given part, or -1 if no assembly was found.
        /// </summary>
        /// <param name="blockPart"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        public int GetContainingAssembly(IMyCubeBlock blockPart, string definition)
        {
            return _getContainingAssembly?.Invoke(blockPart, definition) ?? -1;
        }

        /// <summary>
        ///     Removes a part from its assembly and queues it for a connection check.
        /// </summary>
        /// <param name="blockPart"></param>
        /// <param name="definition"></param>
        public void RecreateConnections(IMyCubeBlock blockPart, string definition)
        {
            _recreateConnections?.Invoke(blockPart, definition);
        }

        #endregion

        #region Definition Methods

        /// <summary>
        ///     Registers a set of definitions with Modular Assemblies Framework.
        /// </summary>
        /// <param name="modularDefinitionContainer"></param>
        /// <returns></returns>
        public string[] RegisterDefinitions(DefinitionDefs.ModularDefinitionContainer modularDefinitionContainer)
        {
            var validDefinitions =
                _registerDefinitions?.Invoke(MyAPIGateway.Utilities.SerializeToBinary(modularDefinitionContainer));

            foreach (var definition in modularDefinitionContainer.PhysicalDefs)
            {
                RegisterOnPartAdd(definition.Name, definition.OnPartAdd);
                RegisterOnPartRemove(definition.Name, definition.OnPartRemove);
                RegisterOnPartDestroy(definition.Name, definition.OnPartDestroy);
                RegisterOnAssemblyClose(definition.Name, definition.OnAssemblyClose);

                if (validDefinitions.Contains(definition.Name))
                    definition.OnInit?.Invoke();
            }

            return validDefinitions;
        }

        /// <summary>
        ///     Unregisters a definition and removes all parts referencing it.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <returns></returns>
        public bool UnregisterDefinition(string definitionName)
        {
            return _unregisterDefinition?.Invoke(definitionName) ?? false;
        }

        /// <summary>
        ///     Returns a list of all registered definition names.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllDefinitions()
        {
            return _getAllDefinitions?.Invoke();
        }

        /// <summary>
        ///     Registers an action to be triggered when a part is added.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <param name="action"></param>
        public void RegisterOnPartAdd(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            if (action == null)
                return;
            _registerOnPartAdd?.Invoke(definitionName, action);
        }

        /// <summary>
        ///     Registers an action to be triggered when a part is removed.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <param name="action"></param>
        public void RegisterOnPartRemove(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            if (action == null)
                return;
            _registerOnPartRemove?.Invoke(definitionName, action);
        }

        /// <summary>
        ///     Registers an action to be triggered when a part is destroyed. Triggered immediately after OnPartRemove if a block
        ///     was destroyed.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <param name="action"></param>
        public void RegisterOnPartDestroy(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            if (action == null)
                return;
            _registerOnPartDestroy?.Invoke(definitionName, action);
        }

        /// <summary>
        ///     Unregisters an action to be triggered when a part is added.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <param name="action"></param>
        public void UnregisterOnPartAdd(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            if (action == null)
                return;
            _unregisterOnPartAdd?.Invoke(definitionName, action);
        }

        /// <summary>
        ///     Unregisters an action to be triggered when a part is removed.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <param name="action"></param>
        public void UnregisterOnPartRemove(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            if (action == null)
                return;
            _unregisterOnPartRemove?.Invoke(definitionName, action);
        }

        /// <summary>
        ///     Unregisters an action to be triggered when a part is destroyed.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <param name="action"></param>
        public void UnregisterOnPartDestroy(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            if (action == null)
                return;
            _unregisterOnPartDestroy?.Invoke(definitionName, action);
        }

        #endregion

        #region Global Methods

        /// <summary>
        ///     Returns true if debug mode is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsDebug()
        {
            return _isDebug?.Invoke() ?? false;
        }

        /// <summary>
        ///     Writes a line to the Modular Assemblies log. %AppData%\Space Engineers\Storage\ModularAssemblies.log
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text)
        {
            _logWriteLine?.Invoke($"[{_modContext.ModName}] {text}");
        }

        /// <summary>
        ///     Registers a chat command. Help page is autogenerated and tied into "!md help"
        /// </summary>
        /// <param name="command"></param>
        /// <param name="helpText"></param>
        /// <param name="onTrigger"></param>
        public void AddChatCommand(string command, string helpText, Action<string[]> onTrigger)
        {
            _addChatCommand?.Invoke(command, helpText, onTrigger, _modContext.ModName);
        }

        /// <summary>
        ///     De-registers a chat command.
        /// </summary>
        /// <param name="command"></param>
        public void RemoveChatCommand(string command)
        {
            _removeChatCommand?.Invoke(command);
        }

        #endregion


        // This section lists all the delegates that will be assigned and utilized below. //

        #region Delegates

        // Global assembly methods
        private Func<IMyCubeBlock[]> _getAllParts;
        private Func<int[]> _getAllAssemblies;

        // Per-assembly methods
        private Func<int, IMyCubeBlock[]> _getMemberParts;
        private Func<int, IMyCubeBlock> _getBasePart;
        private Func<int, IMyCubeGrid> _getAssemblyGrid;
        private Action<string, Action<int>> _registerOnAssemblyClose;
        private Action<string, Action<int>> _unregisterOnAssemblyClose;
        private Action<int> _recreateAssembly;
        private Func<int, string, object> _getAssemblyProperty;
        private Action<int, string, object> _setAssemblyProperty;
        private Func<int, string[]> _listAssemblyProperties;

        // Per-part methods
        private Func<IMyCubeBlock, string, bool, IMyCubeBlock[]> _getConnectedBlocks;
        private Func<IMyCubeBlock, string, int> _getContainingAssembly;
        private Action<IMyCubeBlock, string> _recreateConnections;

        // Definition methods
        private Func<byte[], string[]> _registerDefinitions;
        private Func<string, bool> _unregisterDefinition;
        private Func<string[]> _getAllDefinitions;
        private Action<string, Action<int, IMyCubeBlock, bool>> _registerOnPartAdd;
        private Action<string, Action<int, IMyCubeBlock, bool>> _registerOnPartRemove;
        private Action<string, Action<int, IMyCubeBlock, bool>> _registerOnPartDestroy;
        private Action<string, Action<int, IMyCubeBlock, bool>> _unregisterOnPartAdd;
        private Action<string, Action<int, IMyCubeBlock, bool>> _unregisterOnPartRemove;
        private Action<string, Action<int, IMyCubeBlock, bool>> _unregisterOnPartDestroy;

        // Global methods
        private Func<bool> _isDebug;
        private Action<string> _logWriteLine;
        private Action<string, string, Action<string[]>, string> _addChatCommand;
        private Action<string> _removeChatCommand;

        #endregion


        // This section is the 'guts' of the API; it assigns out all the API endpoints internally and registers with the main framework mod. //

        #region API Initialization

        private bool _isRegistered;
        private bool _apiInit;
        private const long ApiChannel = 8774;
        private IReadOnlyDictionary<string, Delegate> _methodMap;
        private IMyModContext _modContext;

        /// <summary>
        ///     Assigns all API methods. Internal function, avoid editing.
        /// </summary>
        /// <returns></returns>
        public bool ApiAssign()
        {
            _apiInit = _methodMap != null;

            // Global assembly methods
            SetApiMethod("GetAllParts", ref _getAllParts);
            SetApiMethod("GetAllAssemblies", ref _getAllAssemblies);

            // Per-assembly methods
            SetApiMethod("GetMemberParts", ref _getMemberParts);
            SetApiMethod("GetBasePart", ref _getBasePart);
            SetApiMethod("GetAssemblyGrid", ref _getAssemblyGrid);
            SetApiMethod("RegisterOnAssemblyClose", ref _registerOnAssemblyClose);
            SetApiMethod("UnregisterOnAssemblyClose", ref _unregisterOnAssemblyClose);
            SetApiMethod("RecreateAssembly", ref _recreateAssembly);
            SetApiMethod("GetAssemblyProperty", ref _getAssemblyProperty);
            SetApiMethod("SetAssemblyProperty", ref _setAssemblyProperty);
            SetApiMethod("ListAssemblyProperties", ref _listAssemblyProperties);

            // Per-part methods
            SetApiMethod("GetConnectedBlocks", ref _getConnectedBlocks);
            SetApiMethod("GetContainingAssembly", ref _getContainingAssembly);
            SetApiMethod("RecreateConnections", ref _recreateConnections);

            // Definition methods
            SetApiMethod("RegisterDefinitions", ref _registerDefinitions);
            SetApiMethod("UnregisterDefinition", ref _unregisterDefinition);
            SetApiMethod("GetAllDefinitions", ref _getAllDefinitions);
            SetApiMethod("RegisterOnPartAdd", ref _registerOnPartAdd);
            SetApiMethod("RegisterOnPartRemove", ref _registerOnPartRemove);
            SetApiMethod("RegisterOnPartDestroy", ref _registerOnPartDestroy);
            SetApiMethod("UnregisterOnPartAdd", ref _unregisterOnPartAdd);
            SetApiMethod("UnregisterOnPartRemove", ref _unregisterOnPartRemove);
            SetApiMethod("UnregisterOnPartDestroy", ref _unregisterOnPartDestroy);

            // Global methods
            SetApiMethod("IsDebug", ref _isDebug);
            SetApiMethod("LogWriteLine", ref _logWriteLine);
            SetApiMethod("AddChatCommand", ref _addChatCommand);
            SetApiMethod("RemoveChatCommand", ref _removeChatCommand);

            // Unload data if told to by the framework, otherwise notify that the API is ready.
            if (_methodMap == null)
            {
                UnloadData();
                return false;
            }

            _methodMap = null;
            OnReady?.Invoke();
            return true;
        }

        /// <summary>
        ///     Assigns a single API endpoint.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Shared endpoint name; matches with the framework mod.</param>
        /// <param name="method">Method to assign.</param>
        /// <exception cref="Exception"></exception>
        private void SetApiMethod<T>(string name, ref T method) where T : class
        {
            if (_methodMap == null)
            {
                method = null;
                return;
            }

            if (!_methodMap.ContainsKey(name))
                throw new Exception("Method Map does not contain method " + name);
            var del = _methodMap[name];
            if (del.GetType() != typeof(T))
                throw new Exception(
                    $"Method {name} type mismatch! [MapMethod: {del.GetType().Name} | ApiMethod: {typeof(T).Name}]");
            method = _methodMap[name] as T;
        }

        /// <summary>
        ///     Triggered whenever the API receives a message from the framework mod.
        /// </summary>
        /// <param name="obj"></param>
        private void HandleMessage(object obj)
        {
            try
            {
                if (_apiInit || obj is string ||
                    obj == null) // the "ApiEndpointRequest" message will also be received here, we're ignoring that
                    return;

                var tuple = (MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>)obj;
                var receivedVersion = tuple.Item1;
                var dict = tuple.Item2;

                if (dict == null)
                {
                    MyLog.Default.WriteLineAndConsole(
                        $"{_modContext.ModName}: ModularDefinitionsAPI ERR: Received null dictionary!");
                    return;
                }

                if (receivedVersion.Y != ApiVersion)
                    Log(
                        $"Expected API version ({ApiVersion}) differs from received API version {receivedVersion}; errors may occur.");

                _methodMap = dict;

                if (!ApiAssign()) // If we're unassigning the API, don't notify when ready
                    return;

                FrameworkVersion = receivedVersion.X;
                IsReady = true;
                Log($"Modular API v{ApiVersion} loaded!");
            }
            catch (Exception ex)
            {
                // We really really want to notify the player if something goes wrong here.
                MyLog.Default.WriteLineAndConsole($"{_modContext.ModName}: Exception in ModularDefinitionsAPI! " + ex);
                MyAPIGateway.Utilities.ShowMessage(_modContext.ModName, "Exception in ModularDefinitionsAPI!\n" + ex);
            }
        }

        #endregion
    }

    public class DefinitionDefs
    {
        /// <summary>
        ///     Stores and serialized an array of definitions.
        /// </summary>
        [ProtoContract]
        public class ModularDefinitionContainer
        {
            [ProtoMember(1)] internal ModularPhysicalDefinition[] PhysicalDefs;
        }

        /// <summary>
        ///     Class representing a Modular Assemblies definition.
        /// </summary>
        [ProtoContract]
        public class ModularPhysicalDefinition
        {
            /// <summary>
            ///     The name of this definition. Must be unique!
            /// </summary>
            [ProtoMember(1)]
            public string Name { get; set; }

            /// <summary>
            ///     Triggered whenever the definition is first loaded.
            /// </summary>
            public Action OnInit { get; set; }

            /// <summary>
            ///     Called when a valid part is placed.
            ///     <para>
            ///         Arg1 is PhysicalAssemblyId, Arg2 is BlockEntity, Arg3 is IsBaseBlock
            ///     </para>
            /// </summary>
            public Action<int, IMyCubeBlock, bool> OnPartAdd { get; set; }

            /// <summary>
            ///     Called when a valid part is removed.
            ///     <para>
            ///         Arg1 is PhysicalAssemblyId, Arg2 is BlockEntity, Arg3 is IsBaseBlock
            ///     </para>
            /// </summary>
            public Action<int, IMyCubeBlock, bool> OnPartRemove { get; set; }

            /// <summary>
            ///     Called when a component part is destroyed. Note - OnPartRemove is called simultaneously.
            ///     <para>
            ///         Arg1 is PhysicalAssemblyId, Arg2 is BlockEntity, Arg3 is IsBaseBlock
            ///     </para>
            /// </summary>
            public Action<int, IMyCubeBlock, bool> OnPartDestroy { get; set; }

            /// <summary>
            ///     Called when an assembly is closed. Note - OnPartRemove will not be called.
            ///     <para>
            ///         Arg1 is PhysicalAssemblyId
            ///     </para>
            /// </summary>
            public Action<int> OnAssemblyClose { get; set; }

            /// <summary>
            ///     All allowed SubtypeIds. The mod will likely misbehave if two mods allow the same blocks, so please be cautious.
            /// </summary>
            [ProtoMember(2)]
            public string[] AllowedBlockSubtypes { get; set; }

            /// <summary>
            ///     Allowed connection directions. Measured in blocks. If an allowed SubtypeId is not included here, connections are
            ///     allowed on all sides. If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            /// </summary>
            [ProtoMember(3)]
            public Dictionary<string, Dictionary<Vector3I, string[]>> AllowedConnections { get; set; }

            /// <summary>
            ///     The primary block of a PhysicalAssembly. Make sure this is an AssemblyCore block OR null.
            /// </summary>
            [ProtoMember(4)]
            public string BaseBlockSubtype { get; set; }
        }
    }
}