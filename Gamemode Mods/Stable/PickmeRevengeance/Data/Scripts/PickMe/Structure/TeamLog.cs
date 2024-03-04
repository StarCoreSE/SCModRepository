using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickMe.Structure
{
    class TeamLog
    {
        string Name = "";
        float PreMatchValue = 0;
        float PostMatchValue = 0;

        public TeamLog(Team team)
        {
            Name = team.Name;
        }

        public void PreMatch(Team team)
        {
            foreach(var ship in team.Ships)
            {
                ship.PreMatchLog(Session.Header);
            }
            PreMatchValue = team.Value;
        }

        public void PostMatch(Team team)
        {
            foreach (var ship in team.Ships)
            {
                ship.PostMatchLog();
            }
            PostMatchValue = team.Value;
        }

        public string Log(Team team)
        {
            string logString = "";
            logString += "Team" + ",";
            logString += "Ship" + ",";
            logString += "PreMatchValue" + ",";
            logString += "PostMatchValue" + ",";
            logString += "Mass" + ",";
            logString += "Shields" + ",";
            foreach (var category in Session.PointValues) logString += category.Key + ",";
            logString += "\n";
            Session.Instance.debugLog.Write("Team " + Name);
            Session.Instance.debugLog.WriteLine(" has " + team.Ships.Count + " ships");
            foreach (var ship in team.Ships)
            {
                Session.Instance.debugLog.WriteLine(ship.Name);
                logString += ship.Log(Name) + "\n";
            }
            return logString;
        }

        public float Ratio()
        {
            if (PreMatchValue == 0) return 1;
            return PostMatchValue / PreMatchValue;
        }
    }
}
