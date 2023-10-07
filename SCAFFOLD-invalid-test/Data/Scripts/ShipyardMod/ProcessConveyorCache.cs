using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using ShipyardMod.ItemClasses;
using ShipyardMod.Utility;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace ShipyardMod.ProcessHandlers
{
    public class ProcessConveyorCache : ProcessHandlerBase
    {
        public override int GetUpdateResolution()
        {
            return 5000;   // holy shit this causes a lot of lag due to conveyors!!!!!!!!!!!!!!!!!
        }

        public override bool ServerOnly()
        {
            return true;
        }

        private int _currentShipyardIndex = 0;

        public override void Handle()
        {
            if (ProcessShipyardDetection.ShipyardsList.Count == 0) return;

            ShipyardItem currentItem = ProcessShipyardDetection.ShipyardsList.ElementAt(_currentShipyardIndex);
            _currentShipyardIndex = (_currentShipyardIndex + 1) % ProcessShipyardDetection.ShipyardsList.Count;

            var grid = (IMyCubeGrid)currentItem.YardEntity;

            if (grid.Physics == null || grid.Closed || currentItem.YardType == ShipyardType.Invalid)
            {
                currentItem.ConnectedCargo.Clear();
                return;
            }
            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

            var blocks = new List<IMyTerminalBlock>();
            gts.GetBlocks(blocks);

            //assume that all the tools are connected, so only check against the first one in the list
            var cornerInventory = (IMyInventory)((MyEntity)currentItem.Tools[0]).GetInventory();

            var disconnectedInventories = new HashSet<IMyTerminalBlock>();

            //remove blocks which are closed or no longer in the terminal system
            foreach (var block in currentItem.ConnectedCargo)
            {
                if (block.Closed || !blocks.Contains(block))
                    disconnectedInventories.Add(block);
            }

            foreach (var dis in disconnectedInventories)
            {
                currentItem.ConnectedCargo.Remove(dis);
            }

            var newConnections = new HashSet<IMyTerminalBlock>();
            Utilities.InvokeBlocking(() =>
            {
                //check our cached inventories for connected-ness
                foreach (IMyTerminalBlock cargo in currentItem.ConnectedCargo)
                {
                    if (cornerInventory == null)
                        return;

                    if (!cornerInventory.IsConnectedTo(((MyEntity)cargo).GetInventory()))
                        disconnectedInventories.Add(cargo);
                }

                foreach (var block in blocks)
                {
                    //avoid duplicate checks
                    if (disconnectedInventories.Contains(block) || currentItem.ConnectedCargo.Contains(block))
                        continue;

                    //to avoid shipyard corners pulling from each other. Circles are no fun.
                    if (block.BlockDefinition.SubtypeName.Contains("ShipyardCorner"))
                        continue;

                    //ignore reactors
                    if (block is IMyReactor)
                        continue;

                    //ignore oxygen generators and tanks
                    if (block is IMyGasGenerator || block is IMyGasTank)
                        continue;

                    if (currentItem.ConnectedCargo.Contains(block) || disconnectedInventories.Contains(block))
                        continue;

                    if (((MyEntity)block).HasInventory)
                    {
                        MyInventory inventory = ((MyEntity)block).GetInventory();
                        if (cornerInventory == null)
                            return;
                        if (cornerInventory.IsConnectedTo(inventory))
                            newConnections.Add(block);
                    }
                }
            });

            foreach (IMyTerminalBlock removeBlock in disconnectedInventories)
                currentItem.ConnectedCargo.Remove(removeBlock);

            foreach (IMyTerminalBlock newBlock in newConnections)
                currentItem.ConnectedCargo.Add(newBlock);
        }

    }
}