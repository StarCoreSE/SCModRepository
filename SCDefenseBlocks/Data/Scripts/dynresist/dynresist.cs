using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace DynResist
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "LargeBlockConveyorSorter")]
    public class DynResist : MyGameLogicComponent
    {
        private IMyConveyorSorter dynResistBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do calculations serverside
            dynResistBlock = Entity as IMyConveyorSorter;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (dynResistBlock == null || dynResistBlock.CubeGrid.Physics == null) return;
            dynResistBlock.EnabledChanged += DynResistBlockEnabledChanged;
        }

        private void DynResistBlockEnabledChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != dynResistBlock.EntityId) return;

            if (dynResistBlock.Enabled)
            {
                dynResistBlock.SlimBlock.BlockGeneralDamageModifier = 1.0f; // Reset damage modifier when enabled
                MyAPIGateway.Utilities.ShowNotification("Block is enabled", 10000, MyFontEnum.Green);
            }
            else
            {
                dynResistBlock.SlimBlock.BlockGeneralDamageModifier = 0.5f; // Set damage modifier when disabled
                MyAPIGateway.Utilities.ShowNotification("Block is disabled", 10000, MyFontEnum.Red);
            }
        }

        public override void Close()
        {
            if (dynResistBlock != null)
            {
                dynResistBlock.EnabledChanged -= DynResistBlockEnabledChanged;
            }
        }
    }
}
