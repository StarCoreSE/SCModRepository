using System;
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
using Sandbox.ModAPI.Interfaces;

namespace PickMe.Structure
{
    public class Ship
    {
        public long gridId = 0;
        public float Value = 0;
        public float Mass = 0;
        public float Total = 0;
        public List<Part> Parts;
        Dictionary<string, int> partlist;
        public long Owner = 0;
        public string Name = "";
        public long Faction = 0;
        public List<IMyCubeGrid> construct;
        public ShipLog ThisShipLog;

        public Ship(MyCubeGrid grid)
        {
            try
            {
                if (grid == null)
                {
                    DisplayErrorMessage("Ship constructor error: Grid is null.");
                    return;
                }

                gridId = grid.EntityId;
                Parts = new List<Part>();
                Mass = grid.GetCurrentMass();

                if (grid.BigOwners.Count > 0)
                    Owner = grid.BigOwners.First();

                Name = grid.DisplayName;

                if (Owner != 0)
                    Faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Owner)?.FactionId ?? 0;

                if (partlist != null)
                    partlist.Clear();

                partlist = new Dictionary<string, int>();

                if (construct != null)
                    construct.Clear();

                construct = new List<IMyCubeGrid>();
                MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, construct);

                foreach (var mysubgrid in construct)
                {
                    MyCubeGrid subgrid = (MyCubeGrid)mysubgrid;
                    Mass += subgrid.Mass;

                    foreach (var fBlock in subgrid.GetFatBlocks())
                    {
                        Part newPart = new Part(fBlock);
                        Parts.Add(newPart);

                        if (!partlist.ContainsKey(newPart.Name))
                            partlist.Add(newPart.Name, 1);
                        else
                            partlist[newPart.Name] += 1;

                        Value += newPart.Value;
                    }
                }

                Total = Value;
            }
            catch (System.Exception ex)
            {
                DisplayErrorMessage("An error occurred in the Ship constructor:\n" + ex.Message);
            }
        }

        public void Recount()
        {
            Value = 0;

            foreach (var part in Parts)
            {
                if (part.IsFunctional() && !part.Block.MarkedForClose)
                    Value += part.Value;
            }
        }

        public void PreMatchLog(List<string> partsHeader)
        {
            ThisShipLog = new ShipLog(this);
            ThisShipLog.Prematch(this);
        }

        public void PostMatchLog()
        {
            Recount();
            ThisShipLog.Postmatch(this);
        }

        public string Log(string teamName)
        {
            return ThisShipLog.Log(teamName);
        }

        public void Close()
        {
            Parts?.Clear();
            partlist?.Clear();
            construct?.Clear();
        }

        private void DisplayErrorMessage(string message)
        {
            MyAPIGateway.Utilities.ShowMessage("Error", message);
        }
    }
}
