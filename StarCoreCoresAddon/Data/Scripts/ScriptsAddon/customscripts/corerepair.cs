using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace StarCoreCoreRepair
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockBeacon_HeavyCore")]
    public class StarCoreCoreRepair : MyGameLogicComponent
    {
        private IMyBeacon shipCore;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            shipCore = Entity as IMyBeacon;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            MyAPIGateway.Utilities.ShowNotification("StarCoreCoreRepair Initialized", 2000);
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;
            shipCore.EnabledChanged += ShipCoreEnabledChanged;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (shipCore == null || shipCore.CubeGrid.Physics == null) return;

            if (!shipCore.IsFunctional)
            {
                MyAPIGateway.Utilities.ShowNotification("Core is no longer functional", 2000, MyFontEnum.Red);
            }
        }

        private void ShipCoreEnabledChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != shipCore.EntityId) return;
            if (shipCore.IsFunctional)
            {
                MyAPIGateway.Utilities.ShowNotification("Block is functional. Resetting countdown.", 2000);
            }
        }

        public override void Close()
        {
            if (shipCore != null)
            {
                shipCore.EnabledChanged -= ShipCoreEnabledChanged;
            }
        }
    }
}
