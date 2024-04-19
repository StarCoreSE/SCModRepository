using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    Communication
{
    public class ModularDefinitionAPI
    {
        public const int ApiVersion = 0;
        public readonly int FrameworkVersion;
        private Action OnLoad;
        private IMyModContext ModContext;

        #region Delegates

        // Global assembly methods
        private Func<MyEntity[]> _getAllParts;
        private Func<int[]> _getAllAssemblies;

        // Per-assembly methods
        private Func<int, MyEntity[]> _getMemberParts;
        private Func<int, MyEntity> _getBasePart;
        private Func<int, IMyCubeGrid> _getAssemblyGrid;
        private Action<Action<int>> _addOnAssemblyClose;
        private Action<Action<int>> _removeOnAssemblyClose;

        // Per-part methods
        private Func<MyEntity, bool, MyEntity[]> _getConnectedBlocks;
        private Func<MyEntity, int> _getContainingAssembly;

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
        public MyEntity[] GetAllParts()
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
        public MyEntity[] GetMemberParts(int assemblyId)
        {
            return _getMemberParts?.Invoke(assemblyId);
        }

        /// <summary>
        ///     Gets the base part of a PhysicalAssembly. Returns null if assembly does not exist.
        /// </summary>
        public MyEntity GetBasePart(int assemblyId)
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
        /// De-registers an Action<AssemblyId> triggered on assembly removal.
        /// </summary>
        /// <param name="action"></param>
        public void RemoveOnAssemblyClose(Action<int> action)
        {
            _removeOnAssemblyClose?.Invoke(action);
        }

        #endregion

        #region Per-Part Methods

        /// <summary>
        ///     Gets all connected parts to a block. Returns an empty list on fail.
        ///     <para>
        ///         <paramref name="useCached" />: Set this to 'false' if used in OnPartAdd.
        ///     </para>
        /// </summary>
        public MyEntity[] GetConnectedBlocks(MyEntity partBlockId, bool useCached = true)
        {
            return _getConnectedBlocks?.Invoke(partBlockId, useCached);
        }

        /// <summary>
        /// Returns the ID of the assembly containing a given part, or -1 if no assembly was found.
        /// </summary>
        /// <param name="blockPart"></param>
        /// <returns></returns>
        public int GetContainingAssembly(MyEntity blockPart)
        {
            return _getContainingAssembly?.Invoke(blockPart) ?? -1;
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

        public void Log(string text)
        {
            _logWriteLine?.Invoke($"[{ModContext.ModName}] {text}");
        }

        #endregion


        public Action OnReady;
        public bool IsReady;
        private bool _isRegistered;
        private bool _apiInit;
        private readonly long ApiChannel = 8774;
        private IReadOnlyDictionary<string, Delegate> methodMap;

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

            // Per-part methods
            SetApiMethod("GetConnectedBlocks", ref _getConnectedBlocks);
            SetApiMethod("GetContainingAssembly", ref _getContainingAssembly);

            // Global methods
            SetApiMethod("IsDebug", ref _isDebug);
            SetApiMethod("LogWriteLine", ref _logWriteLine);
            SetApiMethod("AddChatCommand", ref _addChatCommand);
            SetApiMethod("RemoveChatCommand", ref _removeChatCommand);
            

            if (_apiInit)
                MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI loaded!");
            else
                MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI cleared.");

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
            OnLoad = onLoad;
            _isRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(ApiChannel, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(ApiChannel, "ApiEndpointRequest");
            MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: ModularDefinitionsAPI inited.");
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
                        $"ModularDefinitions: ModularDefinitionsAPI ignored message {obj as string}!");
                    return;
                }

                var tuple = (MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>)obj;
                var receivedVersion = tuple.Item1;
                var dict = tuple.Item2;

                if (dict == null)
                {
                    MyLog.Default.WriteLineAndConsole(
                        "ModularDefinitions: ModularDefinitionsAPI ERR: Recieved null dictionary!");
                    return;
                }

                methodMap = dict;
                ApiAssign();
                methodMap = null;

                IsReady = true;
                Log($"Modular API v{ApiVersion} loaded!");
                if (receivedVersion.Y != ApiVersion)
                    Log("Expected API version differs from received API; errors may occur.");
                OnLoad?.Invoke();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("Exception in ModularAssemblies Client Mod DefinitionAPI! " + ex);
                MyAPIGateway.Utilities.ShowMessage("Fusion Systems", "Exception in ModularAssemblies Client Mod DefinitionAPI! " + ex);
            }
        }
    }
}