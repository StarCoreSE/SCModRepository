using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Scripts.ModularAssemblies.Communication
{
    public class ModularDefinitionAPI
    {
        /// <summary>
        /// Returns the IMyCubeGrid of a given IMyCubeBlock's EntityId.
        /// </summary>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public IMyCubeGrid GridFromBlockId(long blockId)
        {
            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockId);
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

        /// <summary>
        /// Gets all AssemblyParts in the world. Returns an array of all AssemblyParts.
        /// </summary>
        public MyEntity[] GetAllParts()
        {
            return _getAllParts?.Invoke();
        }

        /// <summary>
        /// Gets all PhysicalAssembly ids in the world. Returns an empty list on fail.
        /// <para>
        /// Arg1 is assembly id
        /// </para>
        /// </summary>
        public int[] GetAllAssemblies()
        {
            return _getAllAssemblies?.Invoke();
        }

        /// <summary>
        /// Gets all member parts of a assembly. Returns an empty list on fail.
        /// <para>
        /// Arg1 is EntityId
        /// </para>
        /// </summary>
        public MyEntity[] GetMemberParts(int assemblyId)
        {
            return _getMemberParts?.Invoke(assemblyId);
        }

        /// <summary>
        /// Gets all connected parts to a block. Returns an empty list on fail.
        /// <para>
        /// <paramref name="useCached"/>: Set this to 'false' if used in OnPartAdd.
        /// </para>
        /// </summary>
        public MyEntity[] GetConnectedBlocks(MyEntity partBlockId, bool useCached = true)
        {
            return _getConnectedBlocks?.Invoke(partBlockId, useCached);
        }

        /// <summary>
        /// Gets the base part of a PhysicalAssembly. Returns null if assembly does not exist.
        /// </summary>
        public MyEntity GetBasePart(int assemblyId)
        {
            return _getBasePart?.Invoke(assemblyId);
        }

        /// <summary>
        /// Returns true if debug mode is enabled.
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




        public bool IsReady = false;
        private bool _isRegistered = false;
        private bool _apiInit = false;
        private long ApiChannel = 8774;

        public void ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            _apiInit = delegates != null;
            AssignMethod(delegates, "GetAllParts", ref _getAllParts);
            AssignMethod(delegates, "GetAllAssemblies", ref _getAllAssemblies);
            AssignMethod(delegates, "GetMemberParts", ref _getMemberParts);
            AssignMethod(delegates, "GetConnectedBlocks", ref _getConnectedBlocks);
            AssignMethod(delegates, "GetBasePart", ref _getBasePart);
            AssignMethod(delegates, "IsDebug", ref _isDebug);
            AssignMethod(delegates, "GetContainingAssembly", ref _getContainingAssembly);

            if (_apiInit)
                MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI loaded!");
            else
                MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI cleared.");
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

            ApiAssign(null);

            _isRegistered = false;
            _apiInit = false;
            IsReady = false;
            MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI unloaded.");
        }

        private void HandleMessage(object obj)
        {
            if (_apiInit || obj is string) // the sent "ApiEndpointRequest" will also be received here, explicitly ignoring that
            {
                MyLog.Default.WriteLineAndConsole($"ModularDefinitions: ModularDefinitionsAPI ignored message {obj as string}!");
                return;
            }

            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
            {
                MyLog.Default.WriteLineAndConsole("ModularDefinitions: ModularDefinitionsAPI ERR: Recieved null dictionary!");
                return;
            }

            ApiAssign(dict);
            IsReady = true;
        }

        private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field)
            where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;

            if (field == null)
                throw new Exception(
                    $"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }

        #endregion
    }
}
