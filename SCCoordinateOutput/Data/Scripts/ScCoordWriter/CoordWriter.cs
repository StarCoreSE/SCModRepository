using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace YourName.ModName.Data.Scripts.ScCoordWriter
{
    internal class CoordWriter
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


        IMyCubeGrid grid;
        TextWriter writer;
        Vector3 oldPos;
        Quaternion oldRot;

        public CoordWriter(IMyCubeGrid grid, string fileExtension = ".scc")
        {
            this.grid = grid;
            writer = MyAPIGateway.Utilities.WriteFileInWorldStorage($"{DateTime.Now:dd-mm-yyyy HHmm} , {grid.EntityId}{fileExtension}", typeof(CoordWriter));
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
            writer.Flush();
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
            writer.Flush();
        }

        public void Close()
        {
            writer.Close();
        }
    }
}
