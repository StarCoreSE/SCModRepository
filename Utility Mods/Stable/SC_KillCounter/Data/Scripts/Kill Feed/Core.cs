using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using KillFeed;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using IMyFunctionalBlock = Sandbox.ModAPI.Ingame.IMyFunctionalBlock;

namespace KillFeed
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        private int skipUpdate = 0;
        private static bool initialized;

        private Dictionary<IMyIdentity, DateTime> ignoreVictims = new Dictionary<IMyIdentity, DateTime>();
        private Dictionary<IMyCockpit, GridAttack> attacked = new Dictionary<IMyCockpit, GridAttack>();


        public class GridAttack
        {
            public IMyCubeGrid grid;
            public IMyCockpit cockpit;
            public IMyIdentity attacker;
            public IMyIdentity victimPilot;
            public IMyIdentity victimLastPilot;
            public int blockCount;
            public string gridName;
            public Vector3D position;
            public bool didFirstCheck;
            public DateTime firstCheck;
            public DateTime finalCheck;
        }

        // Initializers
        private void Initialize()
        {

            //MyAPIGateway.Utilities.ShowNotification("Initialize: Called.", 2000);
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                // track damage events on the server
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamageHandler);
               // MyAPIGateway.Utilities.ShowNotification("Initialize: Server-side Initialization Complete.", 2000);
            }
            else
            {
                // track network messages on the client
                MyAPIGateway.Multiplayer.RegisterMessageHandler(Config.NetworkMessageId, NetworkMessageReceiver);
            }
            initialized = true;
        }

        private void NetworkMessageReceiver(byte[] receivedData)
        {
            try
            {
                // initialization check
                if (!initialized) { return; }
                if (MyAPIGateway.Multiplayer.IsServer) { return; }

                // update last weapon fire
                var messageData = MyAPIGateway.Utilities.SerializeFromBinary<MessageData>(receivedData);
                if (messageData == null || MyAPIGateway.Multiplayer.IsServer) { return; }
                IMyIdentity attacker = Utilities.IdentityIdToIdentity(messageData.Attacker);
                IMyIdentity victim = Utilities.IdentityIdToIdentity(messageData.Victim);
                if (attacker == null || victim == null) { return; }
                if (ignoreVictims.ContainsKey(victim)) { return; }
                MyAPIGateway.Utilities.ShowMessage("Kill Feed", Utilities.TagPlayerName(attacker) + " destroyed " + Utilities.TagPlayerName(victim) + "'s cockpit!");
                ignoreVictims.Add(victim, DateTime.Now + new TimeSpan(0, 0, 10));
            }
            catch (Exception ex)
            {
                Logging.Instance.WriteLine(string.Format("NetworkMessageReceiver(): {0}", ex));
            }
        }

        private void TrackCockpit(IMyCockpit victim, IMyEntity attacker)
        {

            // Check if CubeGrid or CustomName is null, and use "Unknown" as a fallback
            string gridName = victim.CubeGrid?.CustomName ?? "Unknown";


          //  MyAPIGateway.Utilities.ShowNotification($"TrackCockpit: Tracked Victim Grid {gridName} and Attacker {attacker.EntityId}.", 2000);
            // null checking
            if (attacker == null || victim == null || victim.CubeGrid == null) { return; }

            // only track functional cockpits
            if (!victim.IsFunctional || victim.Closed) { return; }

            // grab identity of attacker
            IMyIdentity attackerId = Utilities.EntityToIdentity(attacker);
            if (attackerId == null) { return; }

            // check team status
            var relation = victim.GetUserRelationToOwner(attackerId.IdentityId);
            if (relation != MyRelationsBetweenPlayerAndBlock.Enemies)
            {
           //     MyAPIGateway.Utilities.ShowNotification("TrackCockpit: Friendly fire detected, timer cancelled.", 2000);
                return;
            }

            // create/get attack event
            if (!attacked.ContainsKey(victim))
            {
                attacked.Add(victim, new GridAttack());
          //      MyAPIGateway.Utilities.ShowNotification($"TrackCockpit: Timer started for Victim {victim.CubeGrid.CustomName}.", 2000);
            }
            else
            {
           //     MyAPIGateway.Utilities.ShowNotification($"TrackCockpit: Timer reset for Victim {victim.CubeGrid.CustomName}.", 2000);
            }
            var attack = attacked[victim];

            // update attack event
            attack.attacker = attackerId;
            attack.didFirstCheck = false;
            attack.firstCheck = DateTime.Now + Config.firstCheckTimespan;
            attack.finalCheck = DateTime.Now + Config.finalCheckTimespan;
            attack.cockpit = victim;
            if (attack.grid == null) { attack.grid = victim.CubeGrid; }
            if (victim.Pilot != null) { attack.victimPilot = Utilities.CharacterToIdentity(victim.Pilot); }
            if (victim.LastPilot != null) { attack.victimLastPilot = Utilities.CharacterToIdentity(victim.LastPilot); }
            if (attack.gridName == null && attack.grid != null) { attack.gridName = attack.grid.CustomName; }
            if (attack.blockCount == 0 && attack.grid != null)
            {
                var blocks = new List<IMySlimBlock>();
                attack.grid.GetBlocks(blocks);
                attack.blockCount = blocks.Count;
            }
            // try really hard to get the position
            if (attack.position == null && attack.grid != null) { attack.position = victim.GetPosition(); }
            if (attack.position != null && victim != null && attack.position.X == 0 && attack.position.Y == 0 && attack.position.Z == 0) { attack.position = victim.GetPosition(); }
            if (attack.position != null && attack.grid != null && attack.position.X == 0 && attack.position.Y == 0 && attack.position.Z == 0) { attack.position = attack.grid.GetPosition(); }
        }

        private void BeforeDamageHandler(object target, ref MyDamageInformation info)
        {
            try
            {
                // Only track cockpits in multiplayer
                //  if (!MyAPIGateway.Multiplayer.IsServer) { return; }
                //only track if the damage is a cockpit
                if (!Utilities.IsCockpit(target)) { return; }

                // debug logging
                //Utilities.Loggy("Before", ref info);

                // track cockpit
                var cockpit = Utilities.GetCockpit(target);
                var attacker = MyAPIGateway.Entities.GetEntityById(info.AttackerId);
                TrackCockpit(cockpit, attacker);
            }
            catch (Exception ex)
            {
                Logging.Instance.WriteLine(string.Format("BeforeDamageHandler(): {0}", ex));
            }
        }

        private IMyIdentity GetVictim(GridAttack attack)
        {
            IMyIdentity victim = attack.victimPilot;
            if (victim == null) { victim = attack.victimLastPilot; }
            if (victim == null && !attack.grid.Closed) { victim = Utilities.GridToIdentity(attack.grid); }
            if (victim == null) { victim = Utilities.CubeBlockBuiltByToIdentity(((MyCockpit)attack.cockpit).BuiltBy); }
            return victim;
        }

        private bool CheckKill(GridAttack attack)
        {

        //    MyAPIGateway.Utilities.ShowNotification($"CheckKill: Starting kill check for Grid {attack.gridName ?? "Unknown"}.", 2000);


            // grab attacker
            IMyIdentity attacker = attack.attacker;

            // figure out victim
            IMyIdentity victim = GetVictim(attack);

            // validate attacker and victim
            if (attacker == null || victim == null || attacker == victim) { return false; }

            // make sure there is no other cockpit
            if (attack.grid != null && !attack.grid.Closed)
            {
                var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(attack.grid);
                var cockpits = new List<IMyCockpit>();
                gts.GetBlocksOfType<IMyCockpit>(cockpits);

                foreach (var cockpit in cockpits)
                {
                    // have we found a functional cockpit?
                    if (cockpit.IsFunctional && !cockpit.Closed) { return false; }
                }
            }

            // make sure we shouldn't ignore this victim
            if (ignoreVictims.ContainsKey(victim)) { return false; }

            if (Config.useXmlFile) { XmlOutput.Instance.Write(attack, victim); }

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                MyAPIGateway.Utilities.ShowMessage("Kill Feed", Utilities.TagPlayerName(attacker) + " destroyed " + Utilities.TagPlayerName(victim) + "'s cockpit!");
                Utilities.SendMessageToAllPlayers(new MessageData(attacker.IdentityId, victim.IdentityId));

                // Add this debug message to indicate that the timer ended and the kill got reported.
            //    MyAPIGateway.Utilities.ShowNotification($"CheckKill: Timer ended, kill reported for Grid {attack.grid?.EntityId ?? 0}.", 2000);
            }

            ignoreVictims.Add(victim, DateTime.Now + Config.ignoreVictimTimespan);
            return true;
        }

        // Overrides
        public override void UpdateBeforeSimulation()
        {
            try
            {
                // validation
                if (MyAPIGateway.Session == null) { return; }

                // only check once every 100 ticks
                if (skipUpdate > 0) { skipUpdate--; return; }
                skipUpdate = 100;

                // run initialization
                if (!initialized) { Initialize(); }

                // clean up victims
                foreach (var victim in ignoreVictims.Keys.ToArray())
                {
                    if (DateTime.Now >= ignoreVictims[victim])
                    {
                        // don't continue to ignore this victim
                        ignoreVictims.Remove(victim);

                        // stop tracking any grids with this victim
                        foreach (var key in attacked.Keys.ToArray())
                        {
                            if (victim == GetVictim(attacked[key]))
                            {
                                attacked.Remove(key);
                            }
                        }
                    }
                }

                // only run the rest on the server
                if (!MyAPIGateway.Multiplayer.IsServer) { return; }

                // check for kills
                var finishedCockpits = new List<IMyCockpit>();
                var finishedGrids = new List<GridAttack>();
                foreach (var cockpit in attacked.Keys)
                {
                    var attack = attacked[cockpit];

                    // New: Check if the grid is null or closed, and if so, mark for removal
                    if (attack.grid == null || attack.grid.Closed)
                    {
                   //     MyAPIGateway.Utilities.ShowNotification($"UpdateBeforeSimulation: Grid {attack.gridName ?? "Unknown"} no longer exists. Timer cancelled.", 2000);
                        finishedCockpits.Add(cockpit);
                        continue;
                    }

                    // skip attacks we've already counted
                    if (finishedGrids.Contains(attack))
                    {
                    //    MyAPIGateway.Utilities.ShowNotification($"UpdateBeforeSimulation: Timer check skipped for Grid {attack.gridName ?? "Unknown"}, already counted.", 2000);
                        finishedCockpits.Add(cockpit);
                        continue;
                    }

                    // check on attacks
                    if (!attack.didFirstCheck && DateTime.Now >= attack.firstCheck)
                    {
                  //      MyAPIGateway.Utilities.ShowNotification($"UpdateBeforeSimulation: Timer check due for Grid {attack.gridName ?? "Unknown"}.", 2000);
                        // do initial kill check
                        if (CheckKill(attack))
                        {
                            finishedCockpits.Add(cockpit);
                            finishedGrids.Add(attack);
                        }
                    }
                    else if (DateTime.Now >= attack.finalCheck)
                    {
                        // do final kill check
                        CheckKill(attack);
                        finishedCockpits.Add(cockpit);
                        finishedGrids.Add(attack);
                    }
                }

                // clean up attacked dictionary
                foreach (var cockpit in finishedCockpits)
                {
                    attacked.Remove(cockpit);
                }
            }
            catch (Exception ex)
            {
                Logging.Instance.WriteLine(string.Format("UpdateBeforeSimulation(): {0}", ex));
            }
        }

        protected override void UnloadData()
        {
            try
            {
                // clean up
                if (Logging.Instance != null) { Logging.Instance.Close(); }
                if (XmlOutput.Instance != null) { XmlOutput.Instance.Close(); }
            }
            catch (Exception ex)
            {
                Logging.Instance.WriteLine(string.Format("UnloadData(): {0}", ex));
            }

            base.UnloadData();
        }
    }
}