using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Epstein_Fusion_DS.FusionParts.FusionConveyor
{
    /// <summary>
    /// Forces stockpile on and prevents players from disabling it on caster conveyor tanks.
    /// </summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenTank), false, "Caster_ConveyorCap")]
    // ReSharper disable once UnusedType.Global
    internal class ConveyorCap : MyGameLogicComponent
    {
        private static readonly string[] ValidSubtypes = {
            "Caster_ConveyorCap"
        };


        private IMyGasTank _block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            _block = (IMyGasTank)Entity;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            ControlsManager.DoOnce();
            base.UpdateOnceBeforeFrame();

            if (_block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!_block.Stockpile)
                _block.Stockpile = true;
        }

        private static class ControlsManager
        {
            // we love digi
            private static bool _done;

            public static void DoOnce()
            {
                if (_done)
                    return;

                RemoveControls();
                RemoveActions();

                _done = true;
            }

            private static void RemoveControls()
            {
                List<IMyTerminalControl> controls;

                MyAPIGateway.TerminalControls.GetControls<IMyGasTank>(out controls);

                foreach (IMyTerminalControl c in controls)
                {
                    switch (c.Id)
                    {
                        case "Stockpile":
                        {
                            c.Visible = block =>
                                !ConveyorCap.ValidSubtypes.Contains(block.BlockDefinition.SubtypeId);
                            break;
                        }
                    }
                }
            }

            private static void RemoveActions()
            {
                List<IMyTerminalAction> actions;

                MyAPIGateway.TerminalControls.GetActions<IMyGasTank>(out actions);

                foreach (IMyTerminalAction a in actions)
                {
                    switch (a.Id)
                    {
                        case "Stockpile":
                        case "Stockpile_On":
                        case "Stockpile_Off":
                        {
                            a.Enabled = block =>
                                !ConveyorCap.ValidSubtypes.Contains(block.BlockDefinition.SubtypeId);
                            break;
                        }
                    }
                }
            }
        }
    }
}
