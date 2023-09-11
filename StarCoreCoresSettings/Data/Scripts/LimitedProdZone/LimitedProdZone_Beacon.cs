using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using VRage.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Components;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using System.IO;
using System.Runtime.Remoting.Messaging;
using Sandbox.Game.Entities;
using Sandbox.Game;
using VRage.Utils;

namespace LimitedProdZone
{
    [MyEntityComponentDescriptor(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_Beacon), false, new string[] { "LimitedProdZone" })]
    public class LimitedProdZone_Beacon : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;
        private IMyBeacon beacon;
        private IMyPlayer client;
        private bool playerInZone;
        private IMyCharacter character;
        private VRage.Game.ModAPI.Interfaces.IMyControllableEntity controller;

        private TextWriter logger = null;
        private String timeofload = "" + DateTime.Now.Year + "." + DateTime.Now.Month + "." + DateTime.Now.Day + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second;
        private bool logicEnabled = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            beacon = (Entity as IMyBeacon);
            LimitedProdZone_Assembler.beaconList.Add(beacon);
            LimitedProdZone_Refinery.beaconList.Add(beacon);
            LimitedProdZone_SafeZoneBlock.beaconList.Add(beacon);
            LimitedProdZone_SmallGatlingGun.beaconList.Add(beacon);
            LimitedProdZone_LargeGatlingTurret.beaconList.Add(beacon);
            LimitedProdZone_LargeMissileTurret.beaconList.Add(beacon);
            LimitedProdZone_SmallMissileLauncher.beaconList.Add(beacon);
            LimitedProdZone_SmallMissileLauncherReload.beaconList.Add(beacon);
            LimitedProdZone_InteriorTurret.beaconList.Add(beacon);
            LimitedProdZone_ConveyorSorter.beaconList.Add(beacon);
			LimitedProdZone_UpgradeModule.beaconList.Add(beacon);
			LimitedProdZone_StaticDrill.beaconList.Add(beacon);
            if (beacon != null)
            {
                logicEnabled = true;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }

            client = MyAPIGateway.Session.LocalHumanPlayer;
        }

        /*public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            MyAPIGateway.Parallel.Start(delegate {

                try
                {
                    if (!logicEnabled || beacon == null || !beacon.IsWorking)
                        return;

                    if (playerInZone)
                    {
                        if (controller == null) return;
                        if (controller.EnabledThrusts)
                        {
                             controller.SwitchThrusts();
                        }
                    }
                }
                catch (Exception e)
                {
                    MyAPIGateway.Utilities.ShowMessage("LimitedProdZone", "An error happened in the mod" + e);
                }
            });
        }*/

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            MyAPIGateway.Parallel.Start(delegate {

                try
                {
                    if (!logicEnabled || beacon == null || !beacon.IsWorking || client == null)
                    {
                        return;
                    }                       

                    if (Vector3D.Distance(client.GetPosition(), beacon.GetPosition()) < beacon.Radius)
                    {
                        playerInZone = true;
                        character = client.Character;
                        controller = character as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
                    }
                    else
                    {
                        playerInZone = false;
                    }
                }
                catch (Exception e)
                {
                    MyAPIGateway.Utilities.ShowMessage("LimitedProdZone", "An error happened in the mod" + e);
                }
            });
        }

        private void Log(string text)
        {
            if (logger == null)
            {
                try
                {
                    logger = MyAPIGateway.Utilities.WriteFileInLocalStorage(this.GetType().Name + "-" + timeofload + ".Log", this.GetType());
                }
                catch (Exception)
                {
                    MyAPIGateway.Utilities.ShowMessage("AICombatLib", "Could not open the Log file:" + this.GetType().Name + "-" + timeofload + ".Log");
                    return;
                }
            }

            String datum = DateTime.Now.Year + "." + DateTime.Now.Month + "." + DateTime.Now.Day + " " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
            logger.WriteLine(datum + ": " + text);
            logger.Flush();

        }

        public override void Close()
        {
            if (Entity == null)
            {
                return;
            }
                

            if (LimitedProdZone_Assembler.beaconList.Contains(beacon))
            {
                LimitedProdZone_Assembler.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_Refinery.beaconList.Contains(beacon))
            {
                LimitedProdZone_Refinery.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_SafeZoneBlock.beaconList.Contains(beacon))
            {
                LimitedProdZone_SafeZoneBlock.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_SmallGatlingGun.beaconList.Contains(beacon))
            {
                LimitedProdZone_SmallGatlingGun.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_LargeGatlingTurret.beaconList.Contains(beacon))
            {
                LimitedProdZone_LargeGatlingTurret.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_SmallMissileLauncher.beaconList.Contains(beacon))
            {
                LimitedProdZone_SmallMissileLauncher.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_SmallMissileLauncherReload.beaconList.Contains(beacon))
            {
                LimitedProdZone_SmallMissileLauncherReload.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_InteriorTurret.beaconList.Contains(beacon))
            {
                LimitedProdZone_InteriorTurret.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_ConveyorSorter.beaconList.Contains(beacon))
            {
                LimitedProdZone_ConveyorSorter.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_UpgradeModule.beaconList.Contains(beacon))
            {
                LimitedProdZone_UpgradeModule.beaconList.Remove(beacon);
            }
            if (LimitedProdZone_StaticDrill.beaconList.Contains(beacon))
            {
                LimitedProdZone_StaticDrill.beaconList.Remove(beacon);
            }
        }

        public override void OnRemovedFromScene()
        {

            base.OnRemovedFromScene();

            var Block = Entity as IMyBeacon;

            if (Block == null)
            {
                return;
            }

            //Unregister any handlers here

        }
    }
}

