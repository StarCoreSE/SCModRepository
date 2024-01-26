using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace coordoutput
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class coordoutput : MyGameLogicComponent
    {
        private Sandbox.ModAPI.IMyCockpit Cockpit;
        private int triggerTick = 0;
        private string fileExtension = ".txt";

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only do stuff serverside
            Cockpit = Entity as Sandbox.ModAPI.IMyCockpit;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (Cockpit == null || Cockpit.CubeGrid.Physics == null) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                return; // Only execute on the server
            }

            if (Cockpit == null || Cockpit.CubeGrid.Physics == null) return;
            if (Cockpit.IsFunctional) // Only execute when cockpit is functional
            {
                if (Cockpit.IsUnderControl) // Only execute when cockpit is manned
                {
                    if (triggerTick % 60 == 0) // Show notification every second
                    {
                        // Get current position of cockpit
                        Vector3D currentPosition = Cockpit.GetPosition();
                        // Get grid name
                        string gridName = Cockpit.CubeGrid.CustomName;
                        // Create debug message with current position and grid name
                        string message = string.Format("{0} position: X={1}, Y={2}, Z={3}", gridName, currentPosition.X, currentPosition.Y, currentPosition.Z);
                        // Show the debug message
                        //MyVisualScriptLogicProvider.ShowNotificationLocal(message, 1000, "Debug");

                        // Create unique file name based on grid name
                        string fileName = gridName + fileExtension;

                        // Read existing contents of the file, or create an empty string if the file doesn't exist
                        string existingContents = "";
                        if (MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(coordoutput)))
                        {
                            var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, typeof(coordoutput));
                            existingContents = reader.ReadToEnd();
                            reader.Close();
                        }

                        // Append new data to the existing contents
                        string newData = string.Format("{0} position: X={1}, Y={2}, Z={3}\n", gridName, currentPosition.X, currentPosition.Y, currentPosition.Z);
                        string updatedContents = existingContents + newData;

                        // Write the updated contents back to the file
                        var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(coordoutput));
                        writer.Write(updatedContents);
                        writer.Flush();
                        writer.Close();
                    }

                    triggerTick += 1;
                }
                else
                {
                    triggerTick = 0; // Reset triggerTick if cockpit is unmanned
                }
            }
        }
    }
}
