using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Utils;

namespace StarCore.RemoveRingSGOptions
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorAdvancedStator), false, "TRR_LG_5x5x1_TR_Stator", "TRR_LG_7x7x1_TR_Stator", "TRR_LG_9x9x1_TR_Stator", "TRR_LG_11x11x1_TR_Stator")]
    public class OptionRemoval_SGRingHead : MyGameLogicComponent
    {
        private IMyCubeBlock block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (IMyCubeBlock)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block?.CubeGrid?.Physics == null)
                return;

            RingRotor_HideButton.DoOnce();
        }

         public override void MarkForClose()
        {
            block = null;
        }
 
    }
}