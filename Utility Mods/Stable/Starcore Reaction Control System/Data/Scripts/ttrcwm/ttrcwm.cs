using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace ttrcwm
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Session_handler : MySessionComponentBase
    {
        private delegate void grid_handler();

        private ConcurrentDictionary<IMyCubeGrid, Grid_logic> _grids = new ConcurrentDictionary<IMyCubeGrid, Grid_logic>();
        private grid_handler _grids_handle_60Hz = null, _grids_handle_4Hz = null, _grids_handle_2s_period = null;
        private int _count15 = 0, _count8 = 0;
        private bool _entity_events_set = false;

        public override void LoadData()
        {
            base.LoadData();

            Try_register_handlers();

            // Ensure all existing grids are registered
            var existing_entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(existing_entities, entity => entity is IMyCubeGrid);

            foreach (var entity in existing_entities)
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null && !_grids.ContainsKey(grid))
                {
                    On_entity_added(grid);
                }
            }
        }


        private void On_entity_added(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    // Check if this grid is new and doesn't have logic yet
                    if (!_grids.ContainsKey(grid))
                    {
                        var new_grid_logic = new Grid_logic(grid);
                        _grids_handle_60Hz += new_grid_logic.Handle_60Hz;
                        _grids_handle_4Hz += new_grid_logic.Handle_4Hz;
                        _grids_handle_2s_period += new_grid_logic.Handle_2s_period;
                        _grids.TryAdd(grid, new_grid_logic);

                        MyLog.Default.WriteLineAndConsole($"New grid detected and added: {grid.DisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error in On_entity_added: {ex}");
                throw new Exception("An error occurred while adding a new entity.", ex);
            }
        }

        private void On_entity_removed(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null && _grids.ContainsKey(grid))
                {
                    Grid_logic grid_logic_to_remove;
                    if (_grids.TryRemove(grid, out grid_logic_to_remove))
                    {
                        _grids_handle_60Hz -= grid_logic_to_remove.Handle_60Hz;
                        _grids_handle_4Hz -= grid_logic_to_remove.Handle_4Hz;
                        _grids_handle_2s_period -= grid_logic_to_remove.Handle_2s_period;
                        grid_logic_to_remove.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"An error occurred while handling entity removal: {ex.Message}");
            }
        }

        private void Try_register_handlers()
        {
            try
            {
                if (!Sync_helper.Network_handlers_registered)
                    Sync_helper.Try_register_handlers();

                if (!_entity_events_set && MyAPIGateway.Entities != null)
                {
                    MyAPIGateway.Entities.OnEntityAdd += On_entity_added;
                    MyAPIGateway.Entities.OnEntityRemove += On_entity_removed;

                    // Register existing entities
                    var existing_entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(existing_entities);
                    foreach (var cur_entity in existing_entities)
                        On_entity_added(cur_entity);

                    _entity_events_set = true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Error in Try_register_handlers: {e.Message}");
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                base.UpdateAfterSimulation();

                MyAPIGateway.Parallel.Start(() =>
                {
                    Sync_helper.Handle_60Hz();
                    _grids_handle_60Hz?.Invoke();
                });

                if (--_count15 <= 0)
                {
                    _count15 = 15;
                    MyAPIGateway.Parallel.Start(() =>
                    {
                        _grids_handle_4Hz?.Invoke();
                        if (--_count8 <= 0)
                        {
                            _count8 = 8;
                            MyAPIGateway.Parallel.Start(() =>
                            {
                                Try_register_handlers();
                                _grids_handle_2s_period?.Invoke();
                            });
                        }
                    });
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification($"Error in UpdateAfterSimulation: {e.Message}", 10000, "Red");
                MyLog.Default.WriteLineAndConsole($"Error in UpdateAfterSimulation: {e.Message}");
            }
        }

        protected override void UnloadData()
        {
            try
            {
                base.UnloadData();

                if (Sync_helper.Network_handlers_registered)
                    Sync_helper.Deregister_handlers();

                foreach (var leftover_grid in _grids.Keys.ToList())
                    On_entity_removed(leftover_grid);

                MyAPIGateway.Entities.OnEntityAdd -= On_entity_added;
                MyAPIGateway.Entities.OnEntityRemove -= On_entity_removed;
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occurred while unloading data: {ex.Message}";
                MyLog.Default.WriteLineAndConsole(errorMessage);
            }
        }
    }
}
