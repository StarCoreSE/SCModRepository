using System;
using Sandbox.ModAPI;

namespace MIG.SpecCores
{
    public class DecayingByFramesLazy<T> : Lazy<T>
    {
        private long lastUpdate = -1;
        private long framesValid = 0;
        
        public DecayingByFramesLazy(int framesValid = 0) : base() { this.framesValid = framesValid;}
        protected override bool ShouldUpdate(){ return (MyAPIGateway.Session.GameplayFrameCounter - lastUpdate >= framesValid); }
    }
    
    public class Lazy<T>
    {
        private bool HasCorrectValue = false;
        private Func<T, T> getter;
        private T m_value = default(T);
        
        public T Value
        { 
            get {
                if (ShouldUpdate()) { m_value = getter(m_value); }
                return m_value;
            } 
        }

        
        public Lazy() { }

        public void SetGetter(Func<T, T> getter)
        {
            this.getter = getter;
        }
        
        protected virtual bool ShouldUpdate() { return !HasCorrectValue; }
    }
}