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
    public class ShipLog
    {
        public Dictionary<string,PartLog> Partlist = new Dictionary<string, PartLog>();
        string Name = "";
        float PreMatchValue = 0;
        float PostMatchValue = 0;
        float Mass = 0;
        float Shields = 0;

        public ShipLog(Ship ship)
        {
            Name = ship.Name;
            PreMatchValue = ship.Value;
            Mass = ship.Mass;
        }

        public void Prematch(Ship ship)
        {
            foreach(var grid in ship.construct)
            {
                if (Session.Sh_Api.GridHasShield(grid))
                {
                    MyEntity ent = grid as MyEntity;
                    Shields = Session.Sh_Api.GetShieldInfo(ent).Item4;
                    break;
                }
            }
            foreach(var part in ship.Parts)
            {
                if (Partlist.ContainsKey(part.SubtypeID))
                {
                    Session.Instance.debugLog.WriteLine("Added Key: " + part.SubtypeID);
                    MyAPIGateway.Utilities.ShowNotification(part.SubtypeID);
                    Partlist[part.SubtypeID].Check(part);
                }
                else
                {
                    Session.Instance.debugLog.WriteLine(part.SubtypeID + " Incremented");
                    Partlist.Add(part.SubtypeID, new PartLog(part));
                    Partlist[part.SubtypeID].Check(part);
                }
                PreMatchValue = ship.Value;
            }
        }

        public void Postmatch(Ship ship)
        {
            foreach(var part in ship.Parts)
            {
                if(part.IsFunctional())
                    PostMatchValue += part.Value;
            }
        }

        public string Log(string teamName)
        {
            string logString = "";
            logString += teamName + ",";
            logString += Name + ",";
            logString += PreMatchValue + ",";
            logString += PostMatchValue + ",";
            logString += Mass + ",";
            logString += Shields + ",";
            Session.Instance.debugLog.WriteLine("   Number of Parts: " + Partlist.First().Value.SubtypeId);
            foreach(var part in Session.Header)
            {
                if (Partlist.ContainsKey(part))
                    logString += Partlist[part].Quantity + ",";
                else logString += "0,";
            }
            return logString;
        }

        public void Close()
        {
            Partlist.Clear();
        }
    }
}
