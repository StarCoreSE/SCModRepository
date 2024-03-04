using VRage.Game;
using VRage.Game.Components;

namespace MIG.Shared.SE
{
    public abstract class SessionComponentWithSettings<T> : MySessionComponentBase
    {
        protected T Settings;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Settings = Other.LoadWorldFile<T>(GetFileName(), GetDefault);
        }
        public override void SaveData()
        {
            Other.SaveWorldFile(GetFileName(), Settings);
            base.SaveData();
        }

        protected abstract T GetDefault();
        protected abstract string GetFileName();
    }
    
    public abstract class SessionComponentWithSyncSettings<T> : MySessionComponentBase
    {
        protected StaticSync<T> Sync;
        public T Settings;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Settings = Other.LoadWorldFile<T>(GetFileName(), GetDefault);
            Sync = new StaticSync<T>(GetPort(), SettingsGetter, HandleData);
        }

        public virtual T SettingsGetter()
        {
            return Settings;
        }

        public override void SaveData()
        {
            base.SaveData();
            Other.SaveWorldFile<T>(GetFileName(), Settings);
        }

        protected abstract void HandleData(T data, byte action, ulong player, bool isFromServer);
        protected abstract T GetDefault();
        protected abstract string GetFileName();
        protected abstract ushort GetPort();
    }
    
    public abstract class SessionComponentExternalSettings<T> : MySessionComponentBase
    {
        protected StaticSync<T> Sync;
        public T Settings;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Settings = Other.LoadFirstModFile<T>(GetFileName(), GetDefault);
            //Other.SaveWorldFile<T>(GetFileName(), Settings);
        }
        protected abstract T GetDefault();
        protected abstract string GetFileName();
    }
    
    public abstract class SessionComponentWithSyncAndExternalSettings<T> : MySessionComponentBase
    {
        protected StaticSync<T> Sync;
        public T Settings;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Settings = Other.LoadFirstModFile<T>(GetFileName(), GetDefault);
            Sync = new StaticSync<T>(GetPort(), SettingsGetter, HandleData);
        }

        public virtual T SettingsGetter()
        {
            return Settings;
        }
        protected abstract void HandleData(T data, byte action, ulong player, bool isFromServer);
        protected abstract T GetDefault();
        protected abstract string GetFileName();
        protected abstract ushort GetPort();
    }
}
