using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.ModularAssemblies;
using Scripts.ModularAssemblies.Communication;
using Scripts.ModularAssemblies.FusionParts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;

namespace Data.Scripts.ModularAssemblies.FusionParts.FusionReactor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "Caster_Reactor")]
    public class FusionReactorLogic : MyGameLogicComponent, IMyEventProxy
    {
        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;
        static bool HaveControlsInited = false;

        IMyReactor Block;
        public MySync<float, SyncDirection.BothWays> PowerUsageSync;


        #region Base Methods

        public override void Init(MyObjectBuilder_EntityBase definition)
        {
            base.Init(definition);
            Block = (IMyReactor) Entity;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            if (!HaveControlsInited)
                CreateControls();
        }

        #endregion

        void CreateControls()
        {
            {
                var reactorPowerUsageSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyReactor>("FusionSystems.ReactorPowerUsage");
                reactorPowerUsageSlider.Title = MyStringId.GetOrCompute("Fusion Power Usage");
                reactorPowerUsageSlider.Tooltip = MyStringId.GetOrCompute("Portion of available Fusion Power this reactor should use.");
                reactorPowerUsageSlider.SetLimits(0, 2);
                reactorPowerUsageSlider.Getter = (block) => block.GameLogic.GetAs<FusionReactorLogic>()?.PowerUsageSync.Value ?? 0;
                reactorPowerUsageSlider.Setter = (block, value) => block.GameLogic.GetAs<FusionReactorLogic>().PowerUsageSync.Value = value;

                reactorPowerUsageSlider.Writer = (block, builder) => builder.Append(Math.Round(block.GameLogic.GetAs<FusionReactorLogic>().PowerUsageSync.Value * 100)).Append('%');

                reactorPowerUsageSlider.Visible = (block) => block.BlockDefinition.SubtypeName == "Caster_Reactor";
                reactorPowerUsageSlider.SupportsMultipleBlocks = true;
                reactorPowerUsageSlider.Enabled = (block) => true;

                MyAPIGateway.TerminalControls.AddControl<IMyReactor>(reactorPowerUsageSlider);
            }

            HaveControlsInited = true;
        }
    }
}
