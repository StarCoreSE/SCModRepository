using System.Collections.Generic;
using System.Linq;
using ParallelTasks;
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
            return 5000;   // holy shit this causes a lot of lag due to conveyors!!!!!!!!!!!!!!!!!   unless i fix it :^)
        }

        public override bool ServerOnly()
        {
            return true;
        }
        private int _currentShipyardIndex = 0; // assuming you want to start from the first shipyard

        // Declare some class-level fields to store shared state
        private Task _conveyorTask;
        private ShipyardItem _currentItem;
        private HashSet<IMyTerminalBlock> _disconnectedInventories = new HashSet<IMyTerminalBlock>();
        private HashSet<IMyTerminalBlock> _newConnections = new HashSet<IMyTerminalBlock>();
        private IMyParallelTask _myParallelTask = MyAPIGateway.Parallel; // Assuming this is how you obtain an instance

        public override void Handle()
        {
            if (ProcessShipyardDetection.ShipyardsList.Count == 0) return;

            // Add this check before using _currentShipyardIndex
            if (_currentShipyardIndex >= ProcessShipyardDetection.ShipyardsList.Count)
            {
                _currentShipyardIndex = 0; // Reset the index
            }

            // Now, it should be safe to get the element
            _currentItem = ProcessShipyardDetection.ShipyardsList.ElementAt(_currentShipyardIndex);
            _currentShipyardIndex = (_currentShipyardIndex + 1) % ProcessShipyardDetection.ShipyardsList.Count;

            _disconnectedInventories.Clear();
            _newConnections.Clear();

            _conveyorTask = _myParallelTask.StartBackground(ProcessConveyorBackground, ConveyorCallback);
        }


        private void ProcessConveyorBackground()
        {
            var grid = (IMyCubeGrid)_currentItem.YardEntity;

            if (grid.Physics == null || grid.Closed || _currentItem.YardType == ShipyardType.Invalid)
            {
                _currentItem.ConnectedCargo.Clear();
                return;
            }

            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            var blocks = new List<IMyTerminalBlock>();
            gts.GetBlocks(blocks);

            var cornerInventory = (IMyInventory)((MyEntity)_currentItem.Tools[0]).GetInventory();

            //remove blocks which are closed or no longer in the terminal system
            foreach (var block in _currentItem.ConnectedCargo)
            {
                if (block.Closed || !blocks.Contains(block))
                    _disconnectedInventories.Add(block);
            }

            Utilities.InvokeBlocking(() =>
            {
                // Use cornerInventory instead of _cornerInventory
                foreach (IMyTerminalBlock cargo in _currentItem.ConnectedCargo)
                {
                    if (cornerInventory == null)
                        return;

                    if (!cornerInventory.IsConnectedTo(((MyEntity)cargo).GetInventory()))
                        _disconnectedInventories.Add(cargo);
                }

                foreach (var block in blocks)
                {
                    // Avoid duplicate checks
                    if (_disconnectedInventories.Contains(block) || _currentItem.ConnectedCargo.Contains(block))
                        continue;

                    // To avoid shipyard corners pulling from each other
                    if (block.BlockDefinition.SubtypeName.Contains("ShipyardCorner"))
                        continue;

                    // Ignore reactors
                    if (block is IMyReactor)
                        continue;

                    // Ignore oxygen generators and tanks
                    if (block is IMyGasGenerator || block is IMyGasTank)
                        continue;

                    if (_currentItem.ConnectedCargo.Contains(block) || _disconnectedInventories.Contains(block))
                        continue;

                    if (((MyEntity)block).HasInventory)
                    {
                        MyInventory inventory = ((MyEntity)block).GetInventory();
                        if (cornerInventory == null)
                            return;
                        if (cornerInventory.IsConnectedTo(inventory))
                            _newConnections.Add(block);
                    }
                }
            });
        }

        private void ConveyorCallback()
        {
            // Handle the results of the background task and update connected cargo
            foreach (IMyTerminalBlock removeBlock in _disconnectedInventories)
                _currentItem.ConnectedCargo.Remove(removeBlock);

            foreach (IMyTerminalBlock newBlock in _newConnections)
                _currentItem.ConnectedCargo.Add(newBlock);
        }

        private void LogAnyTaskErrors(ref Task task, string taskName)
        {
            // Log the exception and handle the error. You can implement this depending on how you manage errors.
        }


    }
}