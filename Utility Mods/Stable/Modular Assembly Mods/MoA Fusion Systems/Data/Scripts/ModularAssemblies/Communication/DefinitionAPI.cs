using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    Communication
{
    public class ModularDefinitionAPI
    {
        /// <summary>
        ///     Returns the IMyCubeGrid of a given IMyCubeBlock's EntityId.
        /// </summary>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public IMyCubeGrid GridFromBlockId(long blockId)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(blockId);
            if (entity is IMyCubeBlock)
                return ((IMyCubeBlock)entity).CubeGrid;
            return null;
        }


        #region API calls

        private Func<MyEntity[]> _getAllParts;
        private Func<int[]> _getAllAssemblies;
        private Func<int, MyEntity[]> _getMemberParts;
        private Func<MyEntity, bool, MyEntity[]> _getConnectedBlocks;
        private Func<int, MyEntity> _getBasePart;
        private Func<bool> _isDebug;
        private Func<MyEntity, int> _getContainingAssembly;
        private Func<int, IMyCubeGrid> _getAssemblyGrid;
        private Action<Action<int>> _addOnAssemblyClose;
        private Action<Action<int>> _removeOnAssemblyClose;

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
        ///     Gets the base part of a PhysicalAssembly. Returns null if assembly does not exist.
        /// </summary>
        public MyEntity GetBasePart(int assemblyId)
        {
            return _getBasePart?.Invoke(assemblyId);
        }

        /// <summary>
        ///     Returns true if debug mode is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsDebug()
        {
            return _isDebug?.Invoke() ?? false;
        }

        public int GetContainingAssembly(MyEntity blockPart)
        {
            return _getContainingAssembly?.Invoke(blockPart) ?? -1;
        }

        public IMyCubeGrid GetAssemblyGrid(int assemblyId)
        {
            return _getAssemblyGrid?.Invoke(assemblyId) ?? null;
        }

        public void AddOnAssemblyClose(Action<int> action)
        {
            _addOnAssemblyClose?.Invoke(action);
        }

        public void RemoveOnAssemblyClose(Action<int> action)
        {
            _removeOnAssemblyClose?.Invoke(action);
        }


        public Action OnReady;
        public bool IsReady;
        private bool _isRegistered;
        private bool _apiInit;
        private readonly long ApiChannel = 8774;
        private IReadOnlyDictionary<string, Delegate> methodMap;

        public void ApiAssign()
        {
            _apiInit = methodMap != null;
            SetApiMethod("GetAllParts", ref _getAllParts);
            SetApiMethod("GetAllAssemblies", ref _getAllAssemblies);
            SetApiMethod("GetMemberParts", ref _getMemberParts);
            SetApiMethod("GetConnectedBlocks", ref _getConnectedBlocks);
            SetApiMethod("GetBasePart", ref _getBasePart);
            SetApiMethod("IsDebug", ref _isDebug);
            SetApiMethod("GetContainingAssembly", ref _getContainingAssembly);
            SetApiMethod("GetAssemblyGrid", ref _getAssemblyGrid);
            SetApiMethod("AddOnAssemblyClose", ref _addOnAssemblyClose);
            SetApiMethod("RemoveOnAssemblyClose", ref _removeOnAssemblyClose);

            if (_apiInit)
                MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI loaded!");
            else
                MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI cleared.");

            methodMap = null;
            OnReady?.Invoke();
        }

        private void SetApiMethod<T>(string name, ref T method) where T : class
        {
            if (!methodMap.ContainsKey(name))
                throw new Exception("Method Map does not contain method " + name);
            Delegate del = methodMap[name];
            if (del.GetType() != typeof(T))
                throw new Exception($"Method {name} type mismatch! [MapMethod: {del.GetType().Name} | ApiMethod: {typeof(T).Name}]");
            method = methodMap[name] as T;
        }

        public void LoadData()
        {
            if (_isRegistered)
                throw new Exception($"{GetType().Name}.Load() should not be called multiple times!");

            _isRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(ApiChannel, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(ApiChannel, "ApiEndpointRequest");
            MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI inited.");
        }

        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiChannel, HandleMessage);

            ApiAssign();

            _isRegistered = false;
            _apiInit = false;
            IsReady = false;
            MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI unloaded.");
        }

        private void HandleMessage(object obj)
        {
            try
            {
                if (_apiInit ||
                    obj is string) // the sent "ApiEndpointRequest" will also be received here, explicitly ignoring that
                {
                    MyLog.Default.WriteLineAndConsole(
                        $"ModularDefinitions: ModularDefinitionsAPI ignored message {obj as string}!");
                    return;
                }

                var dict = obj as Dictionary<string, Delegate>;

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
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("Exception in ModularAssemblies Client Mod DefinitionAPI! " + ex);
                MyAPIGateway.Utilities.ShowMessage("Fusion Systems", "Exception in ModularAssemblies Client Mod DefinitionAPI! " + ex);
            }
        }

        #endregion
    }
}