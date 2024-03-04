using System.Collections.Generic;
using StarCore.AutoRepairModule.Sync;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;

namespace StarCore.AutoRepairModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class AutoRepairModuleMod : MySessionComponentBase
    {
        public static AutoRepairModuleMod Instance;

        public bool ControlsCreated = false;
        public Networking Networking = new Networking(58936);
        public List<MyEntity> Entities = new List<MyEntity>();
        public PacketBlockSettings CachedPacketSettings;

        public readonly MyStringId MATERIAL_SQUARE = MyStringId.GetOrCompute("Square");
        public readonly MyStringId MATERIAL_DOT = MyStringId.GetOrCompute("WhiteDot");

        public override void LoadData()
        {
            Instance = this;

            Networking.Register();

            CachedPacketSettings = new PacketBlockSettings();
        }

        protected override void UnloadData()
        {
            Instance = null;

            Networking?.Unregister();
            Networking = null;
        }
    }
}
