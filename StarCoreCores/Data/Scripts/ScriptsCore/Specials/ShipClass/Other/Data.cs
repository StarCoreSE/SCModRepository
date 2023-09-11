using System;
using System.Collections.Generic;
using MIG.Shared.CSharp;
using ProtoBuf;


namespace MIG.SpecCores
{
    public class Limits : Dictionary<int, float>
    {
        public Limits(Limits limits) : base(limits) { }
        public Limits() : base() { }

        public Limits Copy()
        {
            return new Limits(this);
        }
    }

    [ProtoContract]
    public class UpgradableSpecBlockSettings
    {
        [ProtoMember(1)]
        public List<int> Upgrades = new List<int>();

        [ProtoMember(2)]
        public Dictionary<int, float> CustomStatic = new Dictionary<int, float>();
        
        [ProtoMember(3)]
        public Dictionary<int, float> CustomDynamic = new Dictionary<int, float>();

        public override string ToString()
        {
            return $"Settings {Upgrades.Print()}";
        }
    }
    
    /*public interface SpecBlockUpgrade
    {
        string Name { get; }
        void Upgrade(int times, Limits valuesStatic, Limits valuesDynamic);
    }

    public class DefaultSpecBlockUpgrade : SpecBlockUpgrade
    {
        public string Name { get; }
        private Action<int, Limits, Limits> ToBeApplied;
        public DefaultSpecBlockUpgrade(string name, Action<int, Limits, Limits> toBeApplied)
        {
            Name = name;
            ToBeApplied = toBeApplied;
        }

        public void Upgrade(int times, Limits valuesStatic, Limits valuesDynamic)
        {
            ToBeApplied.Invoke(times, valuesStatic, valuesDynamic);
        }
    }*/
}