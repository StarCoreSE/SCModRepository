using System;
using VRage.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Utils;
using System.Collections.Generic;

namespace LimitedProdZone
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Assembler), false)]
    public class LimitedProdZone_Assembler : MyGameLogicComponent
    {
        private IMyAssembler assembler;
        private IMyPlayer client;
        private bool isServer;
        private bool inZone;
        public static List<IMyBeacon> beaconList = new List<IMyBeacon>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            assembler = (Entity as IMyAssembler);
            if (assembler != null)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            isServer = MyAPIGateway.Multiplayer.IsServer;
            client = MyAPIGateway.Session.LocalHumanPlayer;

            if (isServer)
            {
                assembler.IsWorkingChanged += WorkingStateChange;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            try
            {
                if (isServer)
                {
                    if (!assembler.Enabled) return;

                    foreach (var beacon in beaconList)
                    {                        
                        if (beacon == null) continue;
                        if (!beacon.Enabled) continue;
                        if (Vector3D.Distance(assembler.GetPosition(), beacon.GetPosition()) < 35000)
                        {
                            string strSubBlockType = assembler.BlockDefinition.SubtypeId.ToString();
                            bool isBasicAssembler = false;
                            isBasicAssembler = strSubBlockType.Contains("Basic");

                            if (isBasicAssembler == false)
                            {
								inZone = true;
								assembler.Enabled = false;
								return;
                            }
                        }
                    }

                    inZone = false;
                }
            }
            catch (Exception exc)
            {
                MyLog.Default.WriteLineAndConsole($"Failed looping through beacon list: {exc}");
            }
        }

        private void WorkingStateChange(IMyCubeBlock block)
        {
            if (!assembler.Enabled)
            {
                foreach (var beacon in beaconList)
                {
                    if (beacon == null) continue;
                    if (!beacon.Enabled) continue;
                    if (Vector3D.Distance(assembler.GetPosition(), beacon.GetPosition()) < 35000)
                    {
                        string strSubBlockType = assembler.BlockDefinition.SubtypeId.ToString();
                        Boolean isBasicAssembler = false;
                        isBasicAssembler = strSubBlockType.Contains("Basic");

                        if (isBasicAssembler == false)
                        {
							assembler.Enabled = false;
                        }
                    }
                }               
            }
        }

        public override void Close()
        {
            if (Entity == null)
                return;
        }

        public override void OnRemovedFromScene()
        {

            base.OnRemovedFromScene();

            if (Entity == null || Entity.MarkedForClose)
            {
                return;
            }

            var Block = Entity as IMyAssembler;

            if (Block == null) return;

            try
            {
                if (isServer)
                {
                    assembler.IsWorkingChanged -= WorkingStateChange;
                }

            }
            catch (Exception exc)
            {

                MyLog.Default.WriteLineAndConsole($"Failed to deregister event: {exc}");
                return;
            }
            //Unregister any handlers here
        }
    }
}
