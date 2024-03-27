namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;

    public class EmitterState
    {
        internal EmitterStateValues State = new EmitterStateValues();
        internal readonly IMyFunctionalBlock Emitter;
        internal EmitterState(IMyFunctionalBlock emitter)
        {
            Emitter = emitter;
        }

        public void StorageInit()
        {
            if (Emitter.Storage == null)
            {
                Emitter.Storage = new MyModStorageComponent {[Session.Instance.EmitterStateGuid] = ""};
            }
        }

        public void SaveState(bool createStorage = false)
        {
            if (createStorage && Emitter.Storage == null) Emitter.Storage = new MyModStorageComponent();
            else if (Emitter.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(State);
            Emitter.Storage[Session.Instance.EmitterStateGuid] = Convert.ToBase64String(binary);
        }


        public bool LoadState()
        {
            if (Emitter.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Emitter.Storage.TryGetValue(Session.Instance.EmitterStateGuid, out rawData))
            {
                EmitterStateValues loadedState = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedState = MyAPIGateway.Utilities.SerializeFromBinary<EmitterStateValues>(base64);

                if (loadedState != null)
                {
                    State = loadedState;
                    loadedSomething = true;
                }
                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - EmitterId [{Emitter.EntityId}]:");
            }
            return loadedSomething;
        }

        #region Network
        public void NetworkUpdate()
        {
            if (Session.Instance.IsServer)
            {
                State.MId++;
                Session.Instance.PacketizeToClientsInRange(Emitter, new DataEmitterState(Emitter.EntityId, State)); // update clients with server's settings
            }
        }
        #endregion
    }
}
