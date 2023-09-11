using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using Digi;

namespace MIG.Shared.SE
{
    /*[ProtoContract]
    public class TestSettings
    {
        [ProtoMember(1)]
        public float CurrentThrust;
    }

    public class TestBlockSettings
    {
        public float FlameLength;
        public float MaxThrust;
    }

    
    public class TestGameLogic : GameLogicWithSyncAndSettings<TestSettings, TestBlockSettings, TestGameLogic>
    {
        private static Guid GUID = new Guid();
        private static Sync<TestSettings, TestGameLogic> sync;

        public override TestSettings GetDefaultSettings() { return new TestSettings { CurrentThrust = 0f }; }
        public override Guid GetGuid() { return GUID; }
        public override Sync<TestSettings, TestGameLogic> GetSync() { return sync; }
        public override TestBlockSettings InitBlockSettings() { 
            return new TestBlockSettings() { FlameLength = 5f }; 
        }

        public static void Init ()
        {
            sync = new Sync<TestSettings, TestGameLogic>(53334, (x)=>x.Settings, Handler);
        }

        protected override void OnSettingsChanged()
        {

        }

        public override void ApplyDataFromClient(TestSettings arrivedSettings)
        {
            Settings.CurrentThrust = MathHelper.Clamp(arrivedSettings.CurrentThrust, 0, BlockSettings.MaxThrust);
        }
    }*/

    public abstract class GameLogicWithSyncAndSettings<DynamicSettings, StaticSettings, FinalClass> : MyGameLogicComponent where FinalClass : GameLogicWithSyncAndSettings<DynamicSettings, StaticSettings, FinalClass>
    {
        /// <summary>
        /// Get guid, that belongs to this type of gamelogic. Must be STATIC and UNIQ per each nested class
        /// </summary>
        /// <returns></returns>
        protected abstract Guid GetGuid();

        /// <summary>
        /// Get sync, that belongs to this type of gamelogic. Must be STATIC and UNIQ per each nested class
        /// </summary>
        /// <returns></returns>
        protected abstract Sync<DynamicSettings, FinalClass> GetSync();

        /// <summary>
        /// Called, when data arrives on server from clients. 
        /// You must apply changes to gameLogic.Settings
        /// </summary>
        /// <param name="arrivedSettings">Data that arrived from client</param>
        protected abstract void ApplyDataFromClient (DynamicSettings arrivedSettings,ulong userSteamId, byte type);


        /// <summary>
        /// If new block placed, what settings it will have?
        /// </summary>
        /// <returns></returns>
        protected abstract DynamicSettings GetDefaultSettings();

        /// <summary>
        /// When block placed, we should define here static setting.
        /// </summary>
        /// <returns></returns>
        protected abstract StaticSettings InitBlockSettings();

        /// <summary>
        /// Data that is automaticly transfered between client and server. It is also stored in settings.
        /// </summary>
        public DynamicSettings Settings;

        /// <summary>
        /// Data that is not changed at all. It is somthing like SBC values
        /// </summary>
        public StaticSettings BlockSettings;
        
        /// <summary>
        /// Called when settings were changed
        /// </summary>
        protected virtual void OnSettingsChanged() { }
        
        /// <summary>
        /// Called when settings are loaded
        /// </summary>
        protected virtual void OnInitedSettings() { }
        
        /// <summary>
        /// Called once per blockClass. Here you init controls for your block gui
        /// </summary>
        protected virtual void InitControls() { }
        
        /// <summary>
        /// Called once in first UpdateBeforeFrame
        /// </summary>
        protected virtual void OnceInitBeforeFrame () { }

        private static readonly HashSet<Type> InitedControls = new HashSet<Type>();

        IMyEntity myEntity;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            LoadSettings();
            BlockSettings = InitBlockSettings();
            
            if (!MyAPIGateway.Session.IsServer)
            {
                GetSync().RequestData(Entity.EntityId);
            }

            OnInitedSettings();

            //Init controls once;
            bool needInit = false;
            lock (InitedControls)
            {
                needInit = InitedControls.Add(GetType());
            }

            if (needInit)
            {
                InitControls();
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            SaveSettings(true);
            return base.GetObjectBuilder(copy);
        }


        public static void Handler (FinalClass block, DynamicSettings settings, byte type,ulong userSteamId, bool isFromServer)
        {
            var tt = (GameLogicWithSyncAndSettings<DynamicSettings, StaticSettings, FinalClass>)block;

            if (isFromServer && !MyAPIGateway.Session.IsServer)
            {
                tt.Settings = settings;
                tt.OnSettingsChanged();
            }
            else
            {
                tt.ApplyDataFromClient(settings, userSteamId, type);
                tt.NotifyAndSave();
                tt.OnSettingsChanged();
            }
        }

        #region Init Settings

        /// <summary>
        /// Must be called on client side, in Gui elements, or on Server side where data from client is arrived;
        /// </summary>
        protected void NotifyAndSave(byte type=255, bool forceSave = false)
        {
            try
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    //Log.ChatError($"NotifyFromServer:[{type}][{Settings}]");
                    GetSync().SendMessageToOthers(Entity.EntityId, Settings, type: type);
                    SaveSettings(forceSave);

                    if (!MyAPIGateway.Session.isTorchServer())
                    {
                        ApplyDataFromClient(Settings, MyAPIGateway.Session.LocalHumanPlayer.SteamUserId, type);
                        OnSettingsChanged();
                    }
                }
                else
                {
                    var sync = GetSync();
                    if (sync != null)
                    {
                        //Log.ChatError($"NotifyFromClient:[{type}][{Settings}]");
                        sync.SendMessageToServer(Entity.EntityId, Settings, type: type);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ChatError($"NotifyAndSave {type} Exception {ex} {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Must be called on client side, in Gui elements, or on Server side where data from client is arrived;
        /// </summary>
        protected void Notify(DynamicSettings data, byte type = 255)
        {
            try
            {
                var sync = GetSync();
                if (sync != null)
                {
                    //Log.ChatError($"NotifyFromClient:[{type}][{data}]");
                    sync.SendMessageToServer(Entity.EntityId, data, type: type);
                }
            }
            catch (Exception ex)
            {
                Log.ChatError("NotifyAndSave Exception " + ex.ToString() + ex.StackTrace);
            }
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }
        }

        private void LoadSettings()
        {
            if (!Entity.TryGetStorageData(GetGuid(), out Settings))
            {
                Settings = GetDefaultSettings();
                SaveSettings();
            }
        }

        private bool m_settingsDirty;
        protected void SaveSettings(bool forceSave = false)
        {
            m_settingsDirty = true;
            forceSave = true; //TODO: currently GetObjectBuilder is not called
            if (MyAPIGateway.Session.IsServer)
            {
                if (forceSave)
                {
                    if (m_settingsDirty)
                    {
                        Entity.SetStorageData(GetGuid(), Settings);
                        m_settingsDirty = false;
                    }
                }
                else
                {
                    m_settingsDirty = true;
                }
            }
        }
        
        

        #endregion

        
        
        private bool m_firstUpdate = true;
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            
            if (m_firstUpdate) {
                m_firstUpdate = false;
                OnceInitBeforeFrame();
            }
        }
    }
}
