using System;
using MIG.Shared.CSharp;
using ProtoBuf;
using VRage.ModAPI;

namespace MIG.Shared.SE
{
    public class FreezableTimerLogic <T>
    {
        private Wrapper<T> SettingsWrapper;
        private Action<int> TickAction;
        private IMyEntity Entity;
        private Guid Guid;
        private long MinimalMsToTrigger;

        private long LastRun
        {
            get { return SettingsWrapper.LastRun; }
            set { SettingsWrapper.LastRun = value; }
        }

        //public T Settings {
        //    get { return SettingsWrapper.Settings; }
        //    set { SettingsWrapper.Settings = value; }
        //}

        public void Init(Guid guid, IMyEntity entity, Action<int> tick, Func<T> getDefaultSettings, long minimalMsToTrigger = 1000, bool stopAtStart = false)
        {
            Guid = guid;
            Entity = entity;
            TickAction = tick;
            MinimalMsToTrigger = minimalMsToTrigger;
            if (!Entity.TryGetStorageData(guid, out SettingsWrapper, protoBuf:true))
            {
                SettingsWrapper = new Wrapper<T>();
                //SettingsWrapper.Settings = getDefaultSettings();
                LastRun = stopAtStart ? -1 : SharpUtils.msTimeStamp();
            }
            
        }

        public void Tick()
        {
            if (LastRun < 0)
            {
                return;
            }
            var dx = (SharpUtils.msTimeStamp() - LastRun) / MinimalMsToTrigger;
            if (dx < 0) return;
            TickAction((int) dx);
            LastRun += MinimalMsToTrigger * dx;
            Save();
        }

        public void Stop() { if (LastRun != -1) { LastRun = -1; Save(); } }

        public void Start(int extraTime) { LastRun = SharpUtils.msTimeStamp() - extraTime; Save(); }

        public void AddTime(int time) { LastRun -= time; Save(); }
        
        private void Save() { Entity.SetStorageData(Guid, SettingsWrapper, protoBuf:true); }
        
        [ProtoContract]
        private class Wrapper<T>
        {
            [ProtoMember(1)]
            public long LastRun;
            
            [ProtoMember(2)]
            public T Settings;
        }
    }
}