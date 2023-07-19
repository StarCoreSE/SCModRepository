using System;
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
    public class session_handler: MySessionComponentBase
    {
        private delegate void grid_handler();

        private Dictionary<IMyCubeGrid, grid_logic> _grids = new Dictionary<IMyCubeGrid, grid_logic>();
        private grid_handler _grids_handle_60Hz = null, _grids_handle_4Hz = null, _grids_handle_2s_period = null;

        private int  _count15 = 0, _count8 = 0;
        private bool _entity_events_set = false;

        private void log_session_action(string method_name, string message)
        {
            MyLog.Default.WriteLine(string.Format("TTDTWM\tsession_handler.{0}(): {1}\n\t\tTotal grids: {2}", method_name, message, _grids.Count));
        }

        private void on_entity_added(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    var new_grid_logic = new grid_logic(grid);
                    _grids_handle_60Hz += new_grid_logic.handle_60Hz;
                    _grids_handle_4Hz += new_grid_logic.handle_4Hz;
                    _grids_handle_2s_period += new_grid_logic.handle_2s_period;
                    _grids.Add(grid, new_grid_logic);
                }
            }
            catch (Exception ex)
            {
                // Log the error to the game's log file
                MyLog.Default.WriteLineAndConsole($"Error in on_entity_added: {ex}");

                // Throw a new exception with a more user-friendly error message
                throw new Exception("An error occurred while adding a new entity.", ex);
            }
        }


        private void on_entity_removed(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null && _grids.ContainsKey(grid))
                {
                    grid_logic grid_logic_to_remove = _grids[grid];
                    _grids_handle_60Hz -= grid_logic_to_remove.handle_60Hz;
                    _grids_handle_4Hz -= grid_logic_to_remove.handle_4Hz;
                    _grids_handle_2s_period -= grid_logic_to_remove.handle_2s_period;
                    grid_logic_to_remove.Dispose();
                    _grids.Remove(grid);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"An error occurred while handling entity removal: {ex.Message}");
            }
        }



        private void try_register_handlers()
        {
            try
            {
                if (!sync_helper.network_handlers_registered)
                    sync_helper.try_register_handlers();
                if (!_entity_events_set && MyAPIGateway.Entities != null)
                {
                    var existing_entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(existing_entities);
                    foreach (var cur_entity in existing_entities)
                        on_entity_added(cur_entity);
                    MyAPIGateway.Entities.OnEntityAdd += on_entity_added;
                    MyAPIGateway.Entities.OnEntityRemove += on_entity_removed;
                    _entity_events_set = true;
                }
            }
            catch (Exception e)
            {
                // Log the error
                MyLog.Default.WriteLineAndConsole($"Error in try_register_handlers: {e.Message}");
            }
        }


        public override void UpdateAfterSimulation()
        {
            try
            {
                base.UpdateAfterSimulation();

                sync_helper.handle_60Hz();

                if (_grids_handle_60Hz != null)
                    _grids_handle_60Hz();

                if (--_count15 <= 0)
                {
                    _count15 = 15;
                    if (_grids_handle_4Hz != null)
                        _grids_handle_4Hz();

                    if (--_count8 <= 0)
                    {
                        _count8 = 8;

                        try
                        {
                            try_register_handlers();

                            if (_grids_handle_2s_period != null)
                                _grids_handle_2s_period();
                        }
                        catch (Exception e)
                        {
                            MyAPIGateway.Utilities.ShowNotification($"Error in UpdateAfterSimulation: {e.Message}", 10000, "Red");
                            MyLog.Default.WriteLineAndConsole($"Error in UpdateAfterSimulation: {e.Message}");
                        }
                    }
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

                if (sync_helper.network_handlers_registered)
                    sync_helper.deregister_handlers();

                foreach (var leftover_grid in _grids.Keys.ToList())
                    on_entity_removed(leftover_grid);

                MyAPIGateway.Entities.OnEntityAdd -= on_entity_added;
                MyAPIGateway.Entities.OnEntityRemove -= on_entity_removed;
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occurred while unloading data: {ex.Message}";
                MyLog.Default.WriteLineAndConsole(errorMessage);
            }
        }

    }
}
