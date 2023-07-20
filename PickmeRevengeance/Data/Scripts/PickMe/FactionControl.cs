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

namespace PickMe
{
    public class FactionControl
    {
        public long adminFactionID = 0;
        public long outFactionID = 0;
        public long neutralFactionID = 0;
        public long blueFactionID = 0;
        public long redFactionID = 0;

        public void Setup()
        {
            IMyFactionCollection factions = MyAPIGateway.Session.Factions;
            if (!MyAPIGateway.Session.IsServer) return;
            if (factions == null) return;
            if (!factions.FactionNameExists("ADMIN"))
            {
                factions.CreateNPCFaction("ADM", "ADMIN", "", "");
                adminFactionID = factions.TryGetFactionByTag("ADM").FactionId;
                factions.AddNewNPCToFaction(adminFactionID, "Admin Mascot");
                factions.ChangeAutoAccept(adminFactionID, MyAPIGateway.Session.Factions.TryGetFactionById(adminFactionID).FounderId, true, true);
            }
            if (!factions.FactionNameExists("Out"))
            {
                factions.CreateNPCFaction("OUT", "Out", "", "");
                outFactionID = factions.TryGetFactionByTag("OUT").FactionId;
                factions.AddNewNPCToFaction(outFactionID, "Out Mascot");
                factions.ChangeAutoAccept(outFactionID, MyAPIGateway.Session.Factions.TryGetFactionById(outFactionID).FounderId, true, true);
            }
            if (!factions.FactionNameExists("Neutral"))
            {
                factions.CreateNPCFaction("NEU", "Neutral", "", "");
                neutralFactionID = factions.TryGetFactionByTag("NEU").FactionId;
                factions.AddNewNPCToFaction(neutralFactionID, "Neutral Mascot");
                factions.ChangeAutoAccept(neutralFactionID, MyAPIGateway.Session.Factions.TryGetFactionById(neutralFactionID).FounderId, true, true);
            }
            if (!factions.FactionNameExists("BLUE"))
            {
                factions.CreateNPCFaction("BLU", "BLUE", "", "");
                blueFactionID = factions.TryGetFactionByTag("BLU").FactionId;
                factions.AddNewNPCToFaction(blueFactionID, "Blue Mascot");
                factions.ChangeAutoAccept(blueFactionID, MyAPIGateway.Session.Factions.TryGetFactionById(blueFactionID).FounderId, true, true);
            }
            if (!factions.FactionNameExists("RED_"))
            {
                factions.CreateNPCFaction("RED", "RED_", "", "");
                redFactionID = factions.TryGetFactionByTag("RED").FactionId;
                factions.AddNewNPCToFaction(redFactionID, "Red Mascot");
                factions.ChangeAutoAccept(redFactionID, MyAPIGateway.Session.Factions.TryGetFactionById(redFactionID).FounderId, true, true);
            }
            foreach(var faction in factions.Factions)
            {
                if (faction.Value.Tag != "ADM" &&
                    faction.Value.Tag != "OUT" &&
                    faction.Value.Tag != "NEU" &&
                    faction.Value.Tag != "BLU" &&
                    faction.Value.Tag != "RED")
                    factions.RemoveFaction(faction.Key);
            }
            adminFactionID = factions.TryGetFactionByTag("ADM").FactionId;
            outFactionID = factions.TryGetFactionByTag("OUT").FactionId;
            neutralFactionID = factions.TryGetFactionByTag("NEU").FactionId;
            blueFactionID = factions.TryGetFactionByTag("BLU").FactionId;
            redFactionID = factions.TryGetFactionByTag("RED").FactionId;
            SetForMatchEnd();
        }

        public void SetForMatchBegin()
        {
            List<long> beligerentIds = new List<long>();
            foreach(var faction in MyAPIGateway.Session.Factions.Factions)
            {
                if(faction.Value.Tag != "ADM")
                {
                    if(faction.Value.Tag != "OUT")
                    {
                        if (faction.Value.Tag != "NEU")
                        {
                            long thisFaction = faction.Value.FactionId;
                            MyAPIGateway.Session.Factions.SendPeaceRequest(thisFaction, outFactionID);
                            MyAPIGateway.Session.Factions.SendPeaceRequest(thisFaction, adminFactionID);
                            MyAPIGateway.Session.Factions.SendPeaceRequest(thisFaction, neutralFactionID);
                            beligerentIds.Add(thisFaction);
                        }
                    }
                }
            }
            foreach(var beligerent in beligerentIds)
            {
                foreach(var other in beligerentIds)
                {
                    if(beligerent != other)
                    {
                        if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(beligerent, other) == MyRelationsBetweenFactions.Enemies) continue;
                        MyAPIGateway.Session.Factions.DeclareWar(beligerent, other);
                    }
                }
            }
            beligerentIds?.Clear();
            if(MyAPIGateway.Session.Factions.GetRelationBetweenFactions(adminFactionID, outFactionID) == MyRelationsBetweenFactions.Enemies)
                MyAPIGateway.Session.Factions.SendPeaceRequest(outFactionID, adminFactionID);
            if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(adminFactionID, neutralFactionID) == MyRelationsBetweenFactions.Enemies)
                MyAPIGateway.Session.Factions.SendPeaceRequest(neutralFactionID, adminFactionID);
            if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(outFactionID, neutralFactionID) == MyRelationsBetweenFactions.Enemies)
                MyAPIGateway.Session.Factions.SendPeaceRequest(neutralFactionID, outFactionID);
        }

        public void SetForMatchEnd()
        {
            foreach(var faction in MyAPIGateway.Session.Factions.Factions)
            {
                foreach(var cofaction in MyAPIGateway.Session.Factions.Factions)
                {
                    if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(faction.Value.FactionId, cofaction.Value.FactionId) == MyRelationsBetweenFactions.Enemies)
                        MyAPIGateway.Session.Factions.SendPeaceRequest(faction.Value.FactionId, cofaction.Value.FactionId);
                }
            }
        }
    }
}
