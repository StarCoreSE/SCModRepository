using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace DynamicResistence
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "LargeBlockConveyorSorter")]
    public class DynamicResistLogic : MyGameLogicComponent
    {
        private IMyConveyorSorter dynResistBlock;

        internal static DynamicResistLogic Instance;
        private bool ModifiedTerminalControls = false;

        public float HullPolarization { get; set; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do calculations serverside
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            DynaResistTerminalControls.DoOnce(ModContext);
            dynResistBlock = (IMyConveyorSorter)Entity;
            if (dynResistBlock == null || dynResistBlock.CubeGrid.Physics == null) return;
            dynResistBlock.PropertiesChanged += DynResistValueChanged;

            Instance = this;

            SetupTerminalControls();
        }

        private void DynResistValueChanged(IMyTerminalBlock obj)
        {
            if (obj.EntityId != dynResistBlock.EntityId) return;

            var allBlocks = new List<IMySlimBlock>();
            dynResistBlock.CubeGrid.GetBlocks(allBlocks);

            float finalResistanceModifier = 0f;

            if (dynResistBlock.Enabled)
            {
                foreach (var block in allBlocks)
                {
                    var dynamicResistLogic = block.FatBlock?.GameLogic?.GetAs<DynamicResistLogic>();

                    if (dynamicResistLogic != null)
                    {
                        float hullPolarization = dynamicResistLogic.HullPolarization;

                        float minHullPolarization = 0;
                        float maxHullPolarization = 30;
                        float minResistanceModifier = 1.0f;
                        float maxResistanceModifier = 0.7f;

                        float t = (hullPolarization - minHullPolarization) / (float)(maxHullPolarization - minHullPolarization);
                        float resistanceModifier = minResistanceModifier + t * (maxResistanceModifier - minResistanceModifier);

                        // Round the resistance modifier to two decimal places
                        resistanceModifier = (float)Math.Round(resistanceModifier, 2);

                        block.BlockGeneralDamageModifier = resistanceModifier;

                        finalResistanceModifier = resistanceModifier;
                    }
                }
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("Block is disabled", 10000, MyFontEnum.Red);

            }
            

            MyAPIGateway.Utilities.ShowNotification("Current Resistence Value:" + finalResistanceModifier, 5000, MyFontEnum.Green);

            /*if (dynResistBlock.Enabled)
            {
                MyAPIGateway.Utilities.ShowNotification("Block is enabled", 10000, MyFontEnum.Green);
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("Block is disabled", 10000, MyFontEnum.Red);
            }*/
        }

        public static void SetupTerminalControls()
        {
            if (Instance.ModifiedTerminalControls)
                return;

            Instance.ModifiedTerminalControls = true;

            /*SetupActions();*/
            SetupControls();
        }

        private static void SetupControls()
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyConveyorSorter>(out controls);

            foreach (var c in controls)
            {
                switch (c.Id)
                {
                    case "DrainAll":
                    case "blacklistWhitelist":
                    case "CurrentList":
                    case "removeFromSelectionButton":
                    case "candidatesList":
                    case "addToSelectionButton":
                        c.Visible = CombineFunc.Create(c.Visible, Visible);
                        break;
                }
            }
        }

        private static bool Visible(IMyTerminalBlock block)
        {
            return block != null && !(block.GameLogic is DynamicResistLogic);
        }

        public override void Close()
        {
            if (dynResistBlock != null)
            {
                dynResistBlock.PropertiesChanged -= DynResistValueChanged;
            }
        }
    }
}
