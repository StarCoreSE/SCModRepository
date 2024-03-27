namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;

    public class ModulatorState
    {
        internal ModulatorStateValues State = new ModulatorStateValues();
        internal readonly IMyFunctionalBlock Modulator;
        internal ModulatorState(IMyFunctionalBlock modulator)
        {
            Modulator = modulator;
        }

        public void StorageInit()
        {
            if (Modulator.Storage == null)
            {
                Modulator.Storage = new MyModStorageComponent {[Session.Instance.ModulatorSettingsGuid] = ""};
            }
        }

        public void SaveState(bool createStorage = false)
        {
            if (createStorage && Modulator.Storage == null) Modulator.Storage = new MyModStorageComponent();
            else if (Modulator.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(State);
            Modulator.Storage[Session.Instance.ModulatorStateGuid] = Convert.ToBase64String(binary);
        }

        public bool LoadState()
        {
            if (Modulator.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Modulator.Storage.TryGetValue(Session.Instance.ModulatorStateGuid, out rawData))
            {
                ModulatorStateValues loadedState = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedState = MyAPIGateway.Utilities.SerializeFromBinary<ModulatorStateValues>(base64);

                if (loadedState != null)
                {
                    State = loadedState;
                    loadedSomething = true;
                }
                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - ModulatorId [{Modulator.EntityId}]:\n{State.ToString()}");
            }
            return loadedSomething;
        }

        #region Network
        public void NetworkUpdate()
        {

            if (Session.Instance.IsServer)
            {
                State.MId++;
                Session.Instance.PacketizeToClientsInRange(Modulator, new DataModulatorState(Modulator.EntityId, State)); // update clients with server's state
            }
        }
        #endregion
    }

    public class ModulatorSettings
    {
        internal ModulatorSettingsValues Settings = new ModulatorSettingsValues();
        internal readonly IMyFunctionalBlock Modulator;
        internal ModulatorSettings(IMyFunctionalBlock modulator)
        {
            Modulator = modulator;
        }

        public void SaveSettings(bool createStorage = false)
        {
            if (createStorage && Modulator.Storage == null) Modulator.Storage = new MyModStorageComponent();
            else if (Modulator.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(Settings);
            Modulator.Storage[Session.Instance.ModulatorSettingsGuid] = Convert.ToBase64String(binary);
        }

        public bool LoadSettings()
        {
            if (Modulator.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Modulator.Storage.TryGetValue(Session.Instance.ModulatorSettingsGuid, out rawData))
            {
                ModulatorSettingsValues loadedSettings = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<ModulatorSettingsValues>(base64);

                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                    loadedSomething = true;
                }
                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - ModulatorId [{Modulator.EntityId}]:\n{Settings.ToString()}");
            }
            return loadedSomething;
        }

        #region Network
        public void NetworkUpdate()
        {
            Settings.MId++;
            if (Session.Instance.IsServer)
            {
                Session.Instance.PacketizeToClientsInRange(Modulator, new DataModulatorSettings(Modulator.EntityId, Settings)); // update clients with server's settings
            }
            else // client, send settings to server
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataModulatorSettings(Modulator.EntityId, Settings));
                MyAPIGateway.Multiplayer.SendMessageToServer(Session.PACKET_ID, bytes);
            }
        }
        #endregion
    }
}
