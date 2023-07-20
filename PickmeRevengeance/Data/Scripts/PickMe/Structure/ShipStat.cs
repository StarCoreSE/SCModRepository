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

namespace PickMe.Structure
{
    class ShipStat
    {
        public Dictionary<string,PartQuantity> Partlist = new Dictionary<string, PartQuantity>();
        string Name = "";
        float Value = 0;
        float Mass = 0;

        public ShipStat(Ship ship)
        {
            Name = ship.Name;
            Value = ship.Value;
            Mass = ship.Mass;
            foreach (var part in ship.Parts)
            {
                if(part.IsFunctional() && !part.Block.MarkedForClose)
                {
                    if (!Partlist.ContainsKey(part.SubtypeID))
                        Partlist.Add(part.SubtypeID, new PartQuantity(part.Name));
                    else
                    {
                        Partlist[part.SubtypeID].Increment();
                    }
                }
            }
        }

        public void Close()
        {
            Partlist.Clear();
        }

        public string Log()
        {
            string logString = "";
            logString += "\t\t\tBegin_Grid\n";
            logString += "\t\t\t\t" + "Ship_Name:" + Name + "\n";
            logString += "\t\t\t\t" + "Value:" + Value + "\n";
            logString += "\t\t\t\t" + "Mass:" + Mass + "\n";
            foreach (var part in Partlist)
                if (part.Value.Quantity > 0) logString += "\t\t\t\t-" + part.Value.Quantity + "," + part.Value + "\n";
            logString += "\t\t\tEnd_Grid\n";
            return logString;
        }
    }
}
