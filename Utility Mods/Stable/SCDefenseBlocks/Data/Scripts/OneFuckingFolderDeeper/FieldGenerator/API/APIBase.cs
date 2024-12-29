using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.Entity;

namespace FieldGenerator.API
{
    public class FieldGeneratorAPI
    {
        private bool _initialized;

        private Func<long, IMyFunctionalBlock> _getFirstFieldGeneratorOnGrid;

        private Func<IMyFunctionalBlock, bool> _isSiegeActive;
        private Action<IMyFunctionalBlock, bool> _setSiegeActive;

        private Func<IMyFunctionalBlock, bool> _isSiegeCooldownActive;
        private Action<IMyFunctionalBlock, bool> _setSiegeCooldownActive;

        private Func<IMyFunctionalBlock, int> _getSiegeCooldown;
        private Action<IMyFunctionalBlock, int> _setSiegeCooldown;

        private Func<IMyFunctionalBlock, float> _getFieldPower;
        private Action<IMyFunctionalBlock, float> _setFieldPower;

        private Func<IMyFunctionalBlock, float> _getMaximumFieldPower;
        private Func<IMyFunctionalBlock, float>_getMinimumFieldPower;

        private Func<IMyFunctionalBlock, float> _getPowerDraw;

        private Func<IMyFunctionalBlock, float> _getStability;
        private Action<IMyFunctionalBlock, float> _setStability;

        /// <summary>
        /// Returns first valid field generator for the specified grid EntityID.
        /// </summary>
        /// <param name="entityID">EntityID of the cubegrid to check against.</param>
        /// <returns><c>IMyFunctionalBlock</c> of the first field generator if one exists; otherwise, <c>null</c>.</returns>
        public IMyFunctionalBlock GetFirstFieldGeneratorOnGrid(long entityID) => _getFirstFieldGeneratorOnGrid?.Invoke(entityID) ?? null;

        /// <summary>
        /// Returns whether or not the specified block is in siege mode.
        /// </summary>
        /// <param name="block">Block  to check.</param>
        /// <returns><c>true</c> if siege mode is active; otherwise, <c>false</c>.</returns>
        public bool IsSiegeActive(IMyFunctionalBlock block) => _isSiegeActive?.Invoke(block) ?? false;

        /// <summary>
        /// Sets the siege mode state on the given block.
        /// </summary>
        /// <param name="block">Block whose siege state will be modified.</param>
        /// <param name="Active">Whether siege mode should be active (<c>true</c>) or inactive (<c>false</c>).</param>
        public void SetSiegeActive(IMyFunctionalBlock block, bool Active) => _setSiegeActive?.Invoke(block, Active);

        /// <summary>
        /// Returns whether or not the specified blocks siege mode is on cooldown.
        /// </summary>
        /// <param name="block">Block to check.</param>
        /// <returns><c>true</c> if the cooldown is active; otherwise, <c>false</c>.</returns>
        public bool IsSiegeCooldownActive(IMyFunctionalBlock block) => _isSiegeCooldownActive?.Invoke(block) ?? false;

        /// <summary>
        /// Sets the siege mode cooldown state on the given block.
        /// </summary>
        /// <param name="block">Block whose cooldown state will be modified.</param>
        /// <param name="Active">Whether the cooldown should be active (<c>true</c>) or inactive (<c>false</c>).</param>
        public void SetSiegeCooldownActive(IMyFunctionalBlock block, bool Active) => _setSiegeCooldownActive?.Invoke(block, Active);

        /// <summary>
        /// Returns the specified blocks current cooldown time.
        /// </summary>
        /// <param name="block">Block to check.</param>
        /// <returns>The siege cooldown time, or <c>0</c> if no cooldown is active.</returns>
        public int GetSiegeCooldown(IMyFunctionalBlock block) => _getSiegeCooldown?.Invoke(block) ?? 0;

        /// <summary>
        /// Sets the cooldown time on the given block.
        /// </summary>
        /// <param name="block">Block whose cooldown will be modified.</param>
        /// <param name="Time">Time to set the cooldown to, in seconds.</param>
        public void SetSiegeCooldown(IMyFunctionalBlock block, int Time) => _setSiegeCooldown?.Invoke(block, Time);

        /// <summary>
        /// Returns the specified block current field power.
        /// </summary>
        /// <param name="block">Block to check.</param>
        /// <returns>The current field power.</returns>
        public float GetFieldPower(IMyFunctionalBlock block) => _getFieldPower?.Invoke(block) ?? 0;

        /// <summary>
        /// Sets the field power on the given block.
        /// </summary>
        /// <param name="block">Block whose field power will be modified.</param>
        /// <param name="Power">
        /// The field power to set as a float, expressed as a percentage and capped by minimum/maximum field power.
        /// </param>
        public void SetFieldPower(IMyFunctionalBlock block, float Power) => _setFieldPower?.Invoke(block, Power);

        /// <summary>
        /// Returns the specified blocks maximum field power.
        /// </summary>
        /// <param name="block">Block to check.</param>
        /// <returns>The maximum field power.</returns>
        public float GetMaximumFieldPower(IMyFunctionalBlock block) => _getMaximumFieldPower?.Invoke(block) ?? 0;

        /// <summary>
        /// Returns the specified block minimum field power.
        /// </summary>
        /// <param name="block">Block to check.</param>
        /// <returns>The minimum field power.</returns>
        public float GetMinimumFieldPower(IMyFunctionalBlock block) => _getMinimumFieldPower?.Invoke(block) ?? 0;

        /// <summary>
        /// Returns the specified blocks current power draw.
        /// </summary>
        /// <param name="block">Block to check.</param>
        /// <returns>The current power draw.</returns>
        public float GetPowerDraw(IMyFunctionalBlock block) => _getPowerDraw?.Invoke(block) ?? 0;

        /// <summary>
        /// Returns the specified blocks current stability.
        /// </summary>
        /// <param name="block">Block to check.</param>
        /// <returns>The current stability.</returns>
        public float GetStability(IMyFunctionalBlock block) => _getStability?.Invoke(block) ?? 0;

        /// <summary>
        /// Sets the stability on the given block.
        /// </summary>
        /// <param name="block">Block whose stability will be modified.</param>
        /// <param name="Stability">
        /// The stability to set as a float, expressed as a percentage with a maximum of 100.
        /// </param>
        public void SetStability(IMyFunctionalBlock block, float Stability) => _setStability?.Invoke(block, Stability);


        private const long HandlerID = 917632;
        private bool _APIRegistered;
        private Action _ReadyCallback;

        public bool IsReady {  get; private set; }


        public void LoadAPI(Action ReadyCallback = null)
        {
            if (_APIRegistered)
                throw new Exception($"{GetType().Name}.LoadAPI() should not be called multiple times!");

            _ReadyCallback = ReadyCallback;
            _APIRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(HandlerID, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(HandlerID, "APIRequest");
        }

        public void UnloadAPI()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(HandlerID, HandleMessage);

            ApiAssign(null);

            _APIRegistered = false;
            _initialized = false;
            IsReady = false;
        }

        private void HandleMessage(object obj)
        {
            if (_initialized || obj is string) 
                return;

            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
                return;

            ApiAssign(dict);

            IsReady = true;
            _ReadyCallback?.Invoke();
        }

        public void ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            _initialized = delegates != null;

            AssignMethod(delegates, "GetFirstFieldGeneratorOnGrid", ref _getFirstFieldGeneratorOnGrid);

            AssignMethod(delegates, "IsSiegeActive", ref _isSiegeActive);
            AssignMethod(delegates, "SetSiegeActive", ref _setSiegeActive);

            AssignMethod(delegates, "IsSiegeCooldownActive", ref _isSiegeCooldownActive);
            AssignMethod(delegates, "SetSiegeCooldownActive", ref _setSiegeCooldownActive);

            AssignMethod(delegates, "GetSiegeCooldown", ref _getSiegeCooldown);
            AssignMethod(delegates, "SetSiegeCooldown", ref _setSiegeCooldown);

            AssignMethod(delegates, "GetFieldPower", ref _getFieldPower);
            AssignMethod(delegates, "SetFieldPower", ref _setFieldPower);

            AssignMethod(delegates, "GetMaximumFieldPower", ref _getMaximumFieldPower);
            AssignMethod(delegates, "GetMinimumFieldPower", ref _getMinimumFieldPower);

            AssignMethod(delegates, "GetPowerDraw", ref _getPowerDraw);

            AssignMethod(delegates, "GetStability", ref _getStability);
            AssignMethod(delegates, "SetStability", ref _setStability);
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
    }
}
