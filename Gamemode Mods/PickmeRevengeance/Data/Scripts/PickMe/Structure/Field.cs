using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;
using ProtoBuf;

namespace PickMe.Structure
{
    public class Field
    {
        public Dictionary<Ship, float> GridValuePairs;
        public List<long> processedGrids;

        public Field()
        {
            FieldSetup();
        }

        public void FieldSetup()
        {
            //get the list of grids in the arena
            GridValuePairs?.Clear();
            GridValuePairs = new Dictionary<Ship, float>();
            BoundingSphereD arena = new BoundingSphereD(Vector3D.Zero, 200000);
            List<IMyEntity> topmostEntities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref arena);
            processedGrids = new List<long>();
            foreach (var entity in topmostEntities)
            {
                if (entity is IMyCubeGrid)
                {
                    MyCubeGrid grid = (MyCubeGrid)entity;
                    if (!processedGrids.Contains(grid.EntityId))
                    {
                        Ship newShip = new Ship(grid);
                        foreach(var subGrid in newShip.construct) processedGrids.Add(subGrid.EntityId);
                        GridValuePairs.Add(newShip, newShip.Value);
                    }
                }
            }
            processedGrids.Clear();
        }

        public void Echo()
        {
            if (GridValuePairs.Count == 0) return;
            foreach (var grid in GridValuePairs)
            {
                Session.Instance.networking.RelayToClients(new Networking.StatePacket("\t" + grid.Key.Name + "    " + grid.Value));
            }
        }

        public void Close()
        {
            GridValuePairs?.Clear();
            processedGrids?.Clear();
        }
    }
}
