namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;

    public class EnhancerState
    {
        internal EnhancerStateValues State = new EnhancerStateValues();
        internal readonly IMyFunctionalBlock Enhancer;
        internal EnhancerState(IMyFunctionalBlock enhancer)
        {
            Enhancer = enhancer;
        }

        public void StorageInit()
        {
            if (Enhancer.Storage == null)
            {
                Enhancer.Storage = new MyModStorageComponent {[Session.Instance.EnhancerStateGuid] = ""};
            }
        }

        public void SaveState(bool createStorage = false)
        {
            if (createStorage && Enhancer.Storage == null) Enhancer.Storage = new MyModStorageComponent();
            else if (Enhancer.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(State);
            Enhancer.Storage[Session.Instance.EnhancerStateGuid] = Convert.ToBase64String(binary);
        }

        public bool LoadState()
        {
            if (Enhancer.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Enhancer.Storage.TryGetValue(Session.Instance.EnhancerStateGuid, out rawData))
            {
                EnhancerStateValues loadedState = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedState = MyAPIGateway.Utilities.SerializeFromBinary<EnhancerStateValues>(base64);

                if (loadedState != null)
                {
                    State = loadedState;
                    loadedSomething = true;
                }
                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - EnhancerId [{Enhancer.EntityId}]:\n{State.ToString()}");
            }
            return loadedSomething;
        }

        #region Network
        public void NetworkUpdate()
        {

            if (Session.Instance.IsServer)
            {
                State.MId++;
                Session.Instance.PacketizeToClientsInRange(Enhancer, new DataEnhancerState(Enhancer.EntityId, State)); // update clients with server's settings
            }
        }
        #endregion
    }
}
