namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;

    public class ControllerState
    {
        internal readonly IMyFunctionalBlock Shield;

        internal ControllerState(IMyFunctionalBlock shield)
        {
            Shield = shield;
        }

        internal ControllerStateValues State { get; set; } = new ControllerStateValues();

        internal void StorageInit()
        {
            Shield.Storage = new MyModStorageComponent {[Session.Instance.ControllerSettingsGuid] = ""};
        }

        internal void SaveState(bool createStorage = false)
        {
            if (createStorage && Shield.Storage == null) Shield.Storage = new MyModStorageComponent();
            else if (Shield.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(State);
            Shield.Storage[Session.Instance.ControllerStateGuid] = Convert.ToBase64String(binary);
        }

        internal bool LoadState()
        {
            if (Shield.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Shield.Storage.TryGetValue(Session.Instance.ControllerStateGuid, out rawData))
            {
                var base64 = Convert.FromBase64String(rawData);
                var loadedState = MyAPIGateway.Utilities.SerializeFromBinary<ControllerStateValues>(base64);

                if (loadedState != null)
                {
                    State = loadedState;
                    loadedSomething = true;
                }
                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - ShieldId [{Shield.EntityId}]");
            }
            return loadedSomething;
        }

        internal void NetworkUpdate()
        {
            State.MId++;
            Session.Instance.PacketizeToClientsInRange(Shield, new DataControllerState(Shield.EntityId, State)); // update clients with server's state
        }
    }

    internal class ControllerSettings
    {
        internal readonly IMyFunctionalBlock Shield;

        internal ControllerSettings(IMyFunctionalBlock shield)
        {
            Shield = shield;
        }

        internal ControllerSettingsValues Settings { get; set; } = new ControllerSettingsValues();

        internal void SaveSettings(bool createStorage = false)
        {
            if (createStorage && Shield.Storage == null) Shield.Storage = new MyModStorageComponent();
            else if (Shield.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(Settings);
            Shield.Storage[Session.Instance.ControllerSettingsGuid] = Convert.ToBase64String(binary);
        }

        internal bool LoadSettings()
        {
            if (Shield.Storage == null) return false;

            string rawData;
            var loadedSomething = false;

            if (Shield.Storage.TryGetValue(Session.Instance.ControllerSettingsGuid, out rawData))
            {
                ControllerSettingsValues loadedSettings;

                try
                {
                    var base64 = Convert.FromBase64String(rawData);
                    loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<ControllerSettingsValues>(base64);
                }
                catch (Exception e)
                {
                    loadedSettings = null;
                    Log.Line($"Load - ShieldId [{Shield.EntityId}]: - Error loading settings!\n{e}");
                }

                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                    loadedSomething = true;
                }
                if (Session.Enforced.Debug == 3) Log.Line($"Loaded - ShieldId [{Shield.EntityId}]");
            }
            return loadedSomething;
        }

        internal void NetworkUpdate()
        {
            Settings.MId++;
            if (Session.Instance.IsServer)
            {
                Session.Instance.PacketizeToClientsInRange(Shield, new DataControllerSettings(Shield.EntityId, Settings)); 
            }
            else 
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataControllerSettings(Shield.EntityId, Settings));
                MyAPIGateway.Multiplayer.SendMessageToServer(Session.PACKET_ID, bytes);
            }
        }
    }
}
