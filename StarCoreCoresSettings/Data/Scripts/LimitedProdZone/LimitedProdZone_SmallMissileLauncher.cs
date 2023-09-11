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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false)]
    public class LimitedProdZone_SmallMissileLauncher : MyGameLogicComponent
    {
        private IMySmallMissileLauncher weapon;
        private IMyPlayer client;
        private bool isServer;
        private bool inZone;
        public static List<IMyBeacon> beaconList = new List<IMyBeacon>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            weapon = (Entity as IMySmallMissileLauncher);
            if (weapon != null)
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
                weapon.IsWorkingChanged += WorkingStateChange;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            try
            {
                if (isServer)
                {
                    if (!weapon.Enabled) return;

                    foreach (var beacon in beaconList)
                    {                        
                        if (beacon == null) continue;
                        if (!beacon.Enabled) continue;
                        if (Vector3D.Distance(weapon.GetPosition(), beacon.GetPosition()) < 20000)
                        {
                            string strSubBlockType = weapon.BlockDefinition.SubtypeId.ToString();
                            bool isBasicSmallMissileLauncher = false;
                            isBasicSmallMissileLauncher = strSubBlockType.Contains("Basic");

                            if (isBasicSmallMissileLauncher == false)
                            {
								inZone = true;
								weapon.Enabled = false;
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
            if (!weapon.Enabled)
            {
                foreach (var beacon in beaconList)
                {
                    if (beacon == null) continue;
                    if (!beacon.Enabled) continue;
                    if (Vector3D.Distance(weapon.GetPosition(), beacon.GetPosition()) < 20000)
                    {
                        string strSubBlockType = weapon.BlockDefinition.SubtypeId.ToString();
                        Boolean isBasicSmallMissileLauncher = false;
                        isBasicSmallMissileLauncher = strSubBlockType.Contains("Basic");

                        if (isBasicSmallMissileLauncher == false)
                        {
							weapon.Enabled = false;
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

            var Block = Entity as IMySmallMissileLauncher;

            if (Block == null) return;

            try
            {
                if (isServer)
                {
                    weapon.IsWorkingChanged -= WorkingStateChange;
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
