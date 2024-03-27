namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;

    public class O2GeneratorState
    {
        internal O2GeneratorStateValues State = new O2GeneratorStateValues();
        internal readonly IMyFunctionalBlock O2Generator;

        internal O2GeneratorState(IMyFunctionalBlock o2Generator)
        {
            O2Generator = o2Generator;
        }

        public void StorageInit()
        {
            if (O2Generator.Storage == null)
            {
                O2Generator.Storage = new MyModStorageComponent {[Session.Instance.O2GeneratorStateGuid] = ""};
            }
        }

        public void SaveState(bool createStorage = false)
        {
            if (createStorage && O2Generator.Storage == null) O2Generator.Storage = new MyModStorageComponent();
            else if (O2Generator.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(State);
            O2Generator.Storage[Session.Instance.O2GeneratorStateGuid] = Convert.ToBase64String(binary);
        }

        public bool LoadState()
        {
            if (O2Generator.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (O2Generator.Storage.TryGetValue(Session.Instance.O2GeneratorStateGuid, out rawData))
            {
                O2GeneratorStateValues loadedState = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedState = MyAPIGateway.Utilities.SerializeFromBinary<O2GeneratorStateValues>(base64);

                if (loadedState != null)
                {
                    State = loadedState;
                    loadedSomething = true;
                }

                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - O2GeneratorId [{O2Generator.EntityId}]:\n{State.ToString()}");
            }

            return loadedSomething;
        }

        #region Network

        public void NetworkUpdate()
        {

            if (Session.Instance.IsServer)
            {
                State.MId++;
                Session.Instance.PacketizeToClientsInRange(O2Generator, new DataO2GeneratorState(O2Generator.EntityId, State)); // update clients with server's settings
            }
        }

        #endregion
    }

    public class O2GeneratorSettings
    {
        internal O2GeneratorSettingsValues Settings = new O2GeneratorSettingsValues();
        internal readonly IMyFunctionalBlock O2Generator;

        internal O2GeneratorSettings(IMyFunctionalBlock o2Generator)
        {
            O2Generator = o2Generator;
        }

        public void SaveSettings(bool createStorage = false)
        {
            if (createStorage && O2Generator.Storage == null) O2Generator.Storage = new MyModStorageComponent();
            else if (O2Generator.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(Settings);
            O2Generator.Storage[Session.Instance.O2GeneratorSettingsGuid] = Convert.ToBase64String(binary);
        }

        public bool LoadSettings()
        {
            if (O2Generator.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (O2Generator.Storage.TryGetValue(Session.Instance.O2GeneratorSettingsGuid, out rawData))
            {
                O2GeneratorSettingsValues loadedSettings = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<O2GeneratorSettingsValues>(base64);

                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                    loadedSomething = true;
                }

                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - O2GeneratorId [{O2Generator.EntityId}]:\n{Settings.ToString()}");
            }

            return loadedSomething;
        }

        public void NetworkUpdate()
        {
            Settings.MId++;
            if (Session.Instance.IsServer)
            {
                Session.Instance.PacketizeToClientsInRange(O2Generator, new DataO2GeneratorSettings(O2Generator.EntityId, Settings)); // update clients with server's settings
            }
            else
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataO2GeneratorSettings(O2Generator.EntityId, Settings));
                MyAPIGateway.Multiplayer.SendMessageToServer(Session.PACKET_ID, bytes);
            }
        }
    }
}
