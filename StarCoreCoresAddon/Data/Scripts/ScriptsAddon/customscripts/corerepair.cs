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
        private IMyHudNotification notifStatus = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            shipCore = Entity as IMyBeacon;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            SetStatus($"StarCoreCoreRepair Initialized", 5000, MyFontEnum.Green);
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
                SetStatus($"Core is no longer functional. Resetting countdown", 2000, MyFontEnum.Red);
                shipCore.SlimBlock.BlockGeneralDamageModifier = 0.01f;
            }
        }

        private void ShipCoreEnabledChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != shipCore.EntityId) return;
            if (shipCore.IsFunctional)
            {
                SetStatus($"Block is functional. Resetting countdown", 2000, MyFontEnum.Green);
                shipCore.SlimBlock.BlockGeneralDamageModifier = 1.0f;
            }
        }

        private void SetStatus(string text, int aliveTime = 300, string font = MyFontEnum.Green)
        {
            if (notifStatus == null)
                notifStatus = MyAPIGateway.Utilities.CreateNotification("", aliveTime, font);

            notifStatus.Hide();
            notifStatus.Font = font;
            notifStatus.Text = text;
            notifStatus.AliveTime = aliveTime;
            notifStatus.Show();
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
