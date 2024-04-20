using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRage.Utils;
using VRageMath;


namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    Communication
{
    public class ModularDefinitionAPI
    {
        /// <summary>
        /// The expected API version. Don't touch this unless you're developing for the Modular Assemblies Framework.
        /// </summary>
        public const int ApiVersion = 1;
        /// <summary>
        /// The currently loaded Modular Assemblies Framework version.
        /// </summary>
        public int FrameworkVersion { get; private set; } = -1;
        

        #region Delegates

        // Global assembly methods
        private Func<IMyCubeBlock[]> _getAllParts;
        private Func<int[]> _getAllAssemblies;

        // Per-assembly methods
        private Func<int, IMyCubeBlock[]> _getMemberParts;
        private Func<int, IMyCubeBlock> _getBasePart;
        private Func<int, IMyCubeGrid> _getAssemblyGrid;
        private Action<Action<int>> _addOnAssemblyClose;
        private Action<Action<int>> _removeOnAssemblyClose;
        private Action<int> _recreateAssembly;

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
        /// Returns the IMyCubeGrid containing a given assembly ID.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <returns></returns>
        public IMyCubeGrid GetAssemblyGrid(int assemblyId)
        {
            return _getAssemblyGrid?.Invoke(assemblyId);
        }

        /// <summary>
        /// Registers an Action<AssemblyId> triggered on assembly removal.
        /// </summary>
        /// <param name="action"></param>
        public void AddOnAssemblyClose(Action<int> action)
        {
            _addOnAssemblyClose?.Invoke(action);
        }

        /// <summary>
        /// De-registers an Action(AssemblyId) triggered on assembly removal.
        /// </summary>
        /// <param name="action"></param>
        public void RemoveOnAssemblyClose(Action<int> action)
        {
            _removeOnAssemblyClose?.Invoke(action);
        }

        /// <summary>
        /// Removes all blocks from the assembly and queues them for a connection check.
        /// </summary>
        /// <param name="assemblyId"></param>
        public void RecreateAssembly(int assemblyId)
        {
            _recreateAssembly?.Invoke(assemblyId);
        }

        #endregion

        #region Per-Part Methods

        /// <summary>
        /// Gets all connected parts to a block. Returns an empty list on fail.
        /// <para>
        /// <paramref name="useCached" />: Set this to 'false' if used in OnPartAdd.
        /// </para>
        /// </summary>
        public IMyCubeBlock[] GetConnectedBlocks(IMyCubeBlock partBlockId, string definition, bool useCached = true)
        {
            return _getConnectedBlocks?.Invoke(partBlockId, definition, useCached);
        }

        /// <summary>
        /// Returns the ID of the assembly containing a given part, or -1 if no assembly was found.
        /// </summary>
        /// <param name="blockPart"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        public int GetContainingAssembly(IMyCubeBlock blockPart, string definition)
        {
            return _getContainingAssembly?.Invoke(blockPart, definition) ?? -1;
        }

        /// <summary>
        /// Removes a part from its assembly and queues it for a connection check.
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
        /// Registers a set of definitions with Modular Assemblies Framework.
        /// </summary>
        /// <param name="definitionContainer"></param>
        /// <returns></returns>
        public string[] RegisterDefinitions(DefinitionDefs.DefinitionContainer definitionContainer)
        {
            string[] validDefinitions =
                _registerDefinitions?.Invoke(MyAPIGateway.Utilities.SerializeToBinary(definitionContainer));

            foreach (var definition in definitionContainer.PhysicalDefs)
            {
                RegisterOnPartAdd(definition.Name, definition.OnPartAdd);
                RegisterOnPartRemove(definition.Name, definition.OnPartRemove);
                RegisterOnPartDestroy(definition.Name, definition.OnPartDestroy);
            }

            return validDefinitions;
        }

        /// <summary>
        /// Unregisters a definition and removes all parts referencing it.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <returns></returns>
        public bool UnregisterDefinition(string definitionName)
        {
            return _unregisterDefinition?.Invoke(definitionName) ?? false;
        }

        /// <summary>
        /// Returns a list of all registered definition names.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllDefinitions()
        {
            return _getAllDefinitions?.Invoke();
        }

        /// <summary>
        /// Registers an action to be triggered when a part is added.
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
        /// Registers an action to be triggered when a part is removed.
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
        /// Registers an action to be triggered when a part is destroyed. Triggered immediately after OnPartRemove if a block was destroyed.
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
        /// Unregisters an action to be triggered when a part is added.
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
        /// Unregisters an action to be triggered when a part is removed.
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
        /// Unregisters an action to be triggered when a part is destroyed.
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
        /// Returns true if debug mode is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsDebug()
        {
            return _isDebug?.Invoke() ?? false;
        }

        /// <summary>
        /// Writes a line to the Modular Assemblies log. %AppData%\Space Engineers\Storage\ModularAssemblies.log
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text)
        {
            _logWriteLine?.Invoke($"[{ModContext.ModName}] {text}");
        }

        /// <summary>
        /// Registers a chat command. Help page is autogenerated and tied into "!md help"
        /// </summary>
        /// <param name="command"></param>
        /// <param name="helpText"></param>
        /// <param name="onTrigger"></param>
        public void AddChatCommand(string command, string helpText, Action<string[]> onTrigger)
        {
            _addChatCommand?.Invoke(command, helpText, onTrigger, ModContext.ModName);
        }

        /// <summary>
        /// De-registers a chat command.
        /// </summary>
        /// <param name="command"></param>
        public void RemoveChatCommand(string command)
        {
            _removeChatCommand?.Invoke(command);
        }

        #endregion


        public bool IsReady;
        private bool _isRegistered;
        private bool _apiInit;
        private readonly long ApiChannel = 8774;
        private IReadOnlyDictionary<string, Delegate> methodMap;
        public Action OnReady;
        private IMyModContext ModContext;

        public void ApiAssign()
        {
            _apiInit = methodMap != null;

            // Global assembly methods
            SetApiMethod("GetAllParts", ref _getAllParts);
            SetApiMethod("GetAllAssemblies", ref _getAllAssemblies);

            // Per-assembly methods
            SetApiMethod("GetMemberParts", ref _getMemberParts);
            SetApiMethod("GetBasePart", ref _getBasePart);
            SetApiMethod("GetAssemblyGrid", ref _getAssemblyGrid);
            SetApiMethod("AddOnAssemblyClose", ref _addOnAssemblyClose);
            SetApiMethod("RemoveOnAssemblyClose", ref _removeOnAssemblyClose);
            SetApiMethod("RecreateAssembly", ref _recreateAssembly);

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

            methodMap = null;
            OnReady?.Invoke();
        }

        private void SetApiMethod<T>(string name, ref T method) where T : class
        {
            if (methodMap == null)
            {
                method = null;
                return;
            }

            if (!methodMap.ContainsKey(name))
                throw new Exception("Method Map does not contain method " + name);
            Delegate del = methodMap[name];
            if (del.GetType() != typeof(T))
                throw new Exception($"Method {name} type mismatch! [MapMethod: {del.GetType().Name} | ApiMethod: {typeof(T).Name}]");
            method = methodMap[name] as T;
        }

        public void LoadData(IMyModContext modContext, Action onLoad = null)
        {
            if (_isRegistered)
                throw new Exception($"{GetType().Name}.Load() should not be called multiple times!");

            ModContext = modContext;
            OnReady = onLoad;
            _isRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(ApiChannel, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(ApiChannel, "ApiEndpointRequest");
            MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: ModularDefinitionsAPI listening for API methods...");
        }

        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiChannel, HandleMessage);

            ApiAssign();

            _isRegistered = false;
            _apiInit = false;
            IsReady = false;
            MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: ModularDefinitionsAPI unloaded.");
        }

        private void HandleMessage(object obj)
        {
            try
            {
                if (_apiInit || obj is string || obj == null) // the sent "ApiEndpointRequest" will also be received here, explicitly ignoring that
                {
                    MyLog.Default.WriteLineAndConsole(
                        $"{ModContext.ModName}: ModularDefinitionsAPI ignored message \"{obj as string}\"");
                    return;
                }

                MyLog.Default.WriteLineAndConsole(obj.GetType().ToString());

                var tuple = (MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>) obj;
                var receivedVersion = tuple.Item1;
                var dict = tuple.Item2;

                if (dict == null)
                {
                    MyLog.Default.WriteLineAndConsole(
                        $"{ModContext.ModName}: ModularDefinitionsAPI ERR: Received null dictionary!");
                    return;
                }

                if (receivedVersion.Y != ApiVersion)
                    Log($"Expected API version ({ApiVersion}) differs from received API version {receivedVersion}; errors may occur.");

                methodMap = dict;
                ApiAssign();
                methodMap = null;

                FrameworkVersion = receivedVersion.X;
                IsReady = true;
                Log($"Modular API v{ApiVersion} loaded!");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: Exception in ModularDefinitionsAPI! " + ex);
                MyAPIGateway.Utilities.ShowMessage(ModContext.ModName, "Exception in ModularDefinitionsAPI!\n" + ex);
            }
        }
    }
}