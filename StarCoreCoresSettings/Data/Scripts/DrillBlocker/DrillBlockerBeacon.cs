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
using Sandbox.ModAPI.Weapons;

namespace DrillBlocker_NoDrill
{
    [MyEntityComponentDescriptor(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_Beacon), false, new string[] { "DrillBlocker" })]
    public class DrillBlocker_Beacon : MyGameLogicComponent
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
            //DrillBlocker_Drill.beaconList.Add(beacon);

            if (beacon != null)
            {
                logicEnabled = true;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }

            client = MyAPIGateway.Session.LocalHumanPlayer;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

        }

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
                        var drill = MyAPIGateway.Session?.Player?.Character?.EquippedTool as IMyHandDrill;

                        if (character.EquippedTool is IMyHandDrill && drill != null)
                        {
                            var controlEnt = character as Sandbox.Game.Entities.IMyControllableEntity;
                            controlEnt?.SwitchToWeapon(null);
                        }
                    }

                    else
                    {
                        playerInZone = false;
                    }
                }
                catch (Exception e)
                {
                    MyAPIGateway.Utilities.ShowMessage("DrillBlocker", "An error happened in the mod" + e);
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
            /*if (DrillBlocker_Drill.beaconList.Contains(beacon))
            {
                DrillBlocker_Drill.beaconList.Remove(beacon);
            }*/
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

