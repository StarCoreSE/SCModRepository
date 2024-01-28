using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Game.ModAPI;
using VRageMath;

namespace YourName.ModName.Data.Scripts.ScCoordWriter
{
    internal class CoordWriter
    {
        private IMyCubeGrid grid;
        private TextWriter writer;
        private Vector3 oldPos;
        private Quaternion oldRot;

        public CoordWriter(IMyCubeGrid grid, string fileExtension = ".scc")
        {

            /*Per-ship:
             * File name is $"{DateTime}|{ShipName}.json"
             * Format:
             *   CSV table:
             *     StartTime,FactionName,ShipName,OwnerName
             *     GridSizeValue,Size.X,Size.Y,Size.Z
             *     Tick,IsAlive,CurrentHealth%,Position.X,Position.Y,Position.Z,Rotation.X,Rotation.Y,Rotation.Z,Rotation.W
             *     (continued above single row)
             */

            this.grid = grid;
            string fileName = $"{DateTime.Now:dd-MM-yyyy HHmm} , {grid.EntityId}{fileExtension}";

            // Use the Space Engineers modding API to open the file for writing
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(CoordWriter)))
            {
                // The file already exists; delete it before creating a new one
                MyAPIGateway.Utilities.DeleteFileInWorldStorage(fileName, typeof(CoordWriter));
            }

            writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(CoordWriter));
        }

        public void WriteStartingData(string factionName)
        {
            string owner = "";

            if (grid.BigOwners.Count > 0)
            {
                List<IMyIdentity> identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities);
                foreach (IMyIdentity ident in identities)
                {
                    if (ident.IdentityId == grid.BigOwners[0])
                    {
                        owner = ident.DisplayName;
                        break;
                    }
                }
            }

            writer.WriteLine($"{DateTime.Now},{factionName},{grid.CustomName},{owner}");
            Vector3I size = Vector3I.Abs(grid.Min) + Vector3I.Abs(grid.Max);
            writer.WriteLine($"{grid.GridSize},{size.X},{size.Y},{size.Z}");
        }

        public void WriteNextTick(int currentTick, bool isAlive, float healthPercent)
        {
            Vector3 position = grid.GetPosition();
            Quaternion rotation = Quaternion.CreateFromForwardUp(grid.WorldMatrix.Forward, grid.WorldMatrix.Up);

            if (position == oldPos && rotation == oldRot)
                return;

            oldPos = position;
            oldRot = rotation;

            writer.WriteLine($"{currentTick},{isAlive},{Math.Round(healthPercent, 2)},{position.X},{position.Y},{position.Z},{rotation.X},{rotation.Y},{rotation.Z},{rotation.W}");
        }

        public void Close()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
            }
        }
    }
}
