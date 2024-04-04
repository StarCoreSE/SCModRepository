using System;
using System.Collections.Generic;

//using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using VRage.Game;
//using VRage;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;

namespace ttrcwm
{
    static class Sync_helper
    {
        internal const ushort ROTATION_MESSAGE_ID = 17371;

        private static Dictionary<long, Grid_logic> entities = new Dictionary<long, Grid_logic>();

        public static bool Network_handlers_registered { get; private set; }
        //public static bool        is_spectator_mode_on { get; private set; }

        public static IMyPlayer             local_player     { get; private set; }
        public static IMyControllableEntity local_controller { get; private set; }

        public static void Try_register_handlers()
        {
            if (!Network_handlers_registered && MyAPIGateway.Multiplayer != null)
            {
                /*
                if (MyAPIGateway.Multiplayer.IsServer)
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(ROTATION_MESSAGE_ID, grid_logic.rotation_message_handler);
                */
                Network_handlers_registered = true;
            }
        }

        public static void Deregister_handlers()
        {
            if (!Network_handlers_registered)
                return;
            /*
            if (MyAPIGateway.Multiplayer.IsServer)
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ROTATION_MESSAGE_ID, grid_logic.rotation_message_handler);
            */
        }

        public static void Register_logic_object(Grid_logic obj, long entity_id)
        {
            try
            {
                entities.Add(entity_id, obj);
            }
            catch (ArgumentNullException ex)
            {
                MyAPIGateway.Utilities.ShowNotification($"Error: {ex.Message}", 5000, MyFontEnum.Red);
                MyLog.Default.WriteLine($"Error adding entity ID {entity_id} to entities dictionary: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                MyAPIGateway.Utilities.ShowNotification($"Error: {ex.Message}", 5000, MyFontEnum.Red);
                MyLog.Default.WriteLine($"Error adding entity ID {entity_id} to entities dictionary: {ex.Message}");
            }
        }

        public static void Deregister_logic_object(long entity_id)
        {
            try
            {
                entities.Remove(entity_id);
            }
            catch (ArgumentNullException ex)
            {
                MyAPIGateway.Utilities.ShowNotification($"Error: {ex.Message}", 5000, MyFontEnum.Red);
                MyLog.Default.WriteLine($"Error removing entity ID {entity_id} from entities dictionary: {ex.Message}");
            }
            catch (KeyNotFoundException ex)
            {
                MyAPIGateway.Utilities.ShowNotification($"Error: {ex.Message}", 5000, MyFontEnum.Red);
                MyLog.Default.WriteLine($"Error removing entity ID {entity_id} from entities dictionary: {ex.Message}");
            }
        }


        public static void Encode_entity_id(IMyCubeGrid entity, byte[] message)
        {
            long entity_id = entity.EntityId;
            for (int cur_byte = 0; cur_byte < 8; ++cur_byte)
            {
                message[cur_byte] = (byte)(entity_id & 0xFF);
                entity_id >>= 8;
            }
        }

        public static Grid_logic Decode_entity_id(byte[] message)
        {
            long entity_id = 0;
            for (int cur_byte = 7; cur_byte >= 0; --cur_byte)
                entity_id = (entity_id << 8) | message[cur_byte];
            return entities.ContainsKey(entity_id) ? entities[entity_id] : null;
        }

        public static void Handle_60Hz()
        {


            local_player     = MyAPIGateway.Session.LocalHumanPlayer;
            local_controller = local_player?.Controller.ControlledEntity;
        }
    }
}
