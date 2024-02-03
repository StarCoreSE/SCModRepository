using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace YourName.ModName.Data.Scripts.ScCoordWriter
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class coordoutput : MyGameLogicComponent
    {
        private Sandbox.ModAPI.IMyCockpit Cockpit;
        private const string fileExtension = ".scc";
        private int tickCounter = 0;
        CoordWriter writer;
        private static HashSet<long> gridsWithWriters = new HashSet<long>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do stuff serverside
            Cockpit = Entity as Sandbox.ModAPI.IMyCockpit;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (Cockpit == null || Cockpit.CubeGrid.Physics == null) return;

            // Determine if the grid is static or not
            bool isStatic = Cockpit.CubeGrid.IsStatic;

            // Check if this grid already has a writer
            if (gridsWithWriters.Contains(Cockpit.CubeGrid.EntityId))
                return;

            // If not, add it to the set and proceed with creating a writer
            gridsWithWriters.Add(Cockpit.CubeGrid.EntityId);

            string factionName = GetFactionName(Cockpit.OwnerId);
            writer = new CoordWriter(Cockpit.CubeGrid, fileExtension, factionName, isStatic); // Pass isStatic to the constructor
            writer.WriteStartingData(factionName);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

       // public void ClearWorldStorageFiles()
       // {
       //     try
       //     {
       //         var files = MyAPIGateway.Utilities.GetFilesInWorldStorage(typeof(CoordWriter));
       //
       //         foreach (var file in files)
       //         {
       //             MyAPIGateway.Utilities.DeleteFileInWorldStorage(file, typeof(CoordWriter));
       //         }
       //
       //         MyVisualScriptLogicProvider.SendChatMessage("Cleared all files in mod's world storage folder.", "Mod Debug");
       //     }
       //     catch (Exception ex)
       //     {
       //         MyLog.Default.WriteLine($"Error clearing world storage files: {ex.Message}");
       //         MyVisualScriptLogicProvider.SendChatMessage("Error clearing world storage files. See log for details.", "Mod Debug");
       //     }
       // }


        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return; // Only execute on the server

            if (Cockpit == null || Cockpit.CubeGrid.Physics == null) return;
            if (Cockpit.IsFunctional) // Only execute when cockpit is functional
            {
                tickCounter++;
                if (tickCounter % 30 == 0) // Output once per quarter second
                {
                    writer.WriteNextTick(MyAPIGateway.Session.GameplayFrameCounter, true, 1.0f);
                }
            }
            else
            {
                tickCounter = 0; // Reset tick counter if cockpit is unmanned
            }
        }

        private string GetFactionName(long playerId)
        {
            IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            return playerFaction != null ? playerFaction.Name : "Unowned";
        }
    }
}
