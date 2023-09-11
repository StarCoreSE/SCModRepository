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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), false)]
    public class LimitedProdZone_StaticDrill : MyGameLogicComponent
    {
        private IMyShipDrill staticDrill;
        private IMyPlayer client;
        private bool isServer;
        private bool inZone;
        public static List<IMyBeacon> beaconList = new List<IMyBeacon>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            staticDrill = (Entity as IMyShipDrill);
            if (staticDrill != null)
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
                staticDrill.IsWorkingChanged += WorkingStateChange;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            try
            {
                if (isServer)
                {
                    if (!staticDrill.Enabled) return;

                    foreach (var beacon in beaconList)
                    {                        
                        if (beacon == null) continue;
                        if (!beacon.Enabled) continue;
                        if (Vector3D.Distance(staticDrill.GetPosition(), beacon.GetPosition()) < 20000)
                        {
                            string strSubBlockType = staticDrill.BlockDefinition.SubtypeId.ToString();
                            bool isBasicStaticDrill = false;
                            isBasicStaticDrill = strSubBlockType.Contains("BasicStaticDrill");

                            if (isBasicStaticDrill == false)
                            {
								inZone = true;
								staticDrill.Enabled = false;
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
            if (!staticDrill.Enabled)
            {
                foreach (var beacon in beaconList)
                {
                    if (beacon == null) continue;
                    if (!beacon.Enabled) continue;
                    if (Vector3D.Distance(staticDrill.GetPosition(), beacon.GetPosition()) < 20000)
                    {
                        string strSubBlockType = staticDrill.BlockDefinition.SubtypeId.ToString();
                        Boolean isBasicStaticDrill = false;
                        isBasicStaticDrill = strSubBlockType.Contains("BasicStaticDrill");

                        if (isBasicStaticDrill == false)
                        {
							staticDrill.Enabled = false;
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

            var Block = Entity as IMyShipDrill;

            if (Block == null) return;

            try
            {
                if (isServer)
                {
                    staticDrill.IsWorkingChanged -= WorkingStateChange;
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
