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
    public class Match
    {
        public long MatchNo = 0;
        public string MatchType = "auto";
        Team teamR, teamB;
        TextWriter MatchLogger;
        List<IMyPlayer> tempPlayers;
        List<IMyPlayer> tempBlue;
        List<IMyPlayer> tempRed;
        public Dictionary<Ship, long> Grids;
        public List<long> processedGrids;
        public Dictionary<long, List<IMyPlayer>> Beligerents;
        MatchLog ThisMatchLog;

        public Match(bool auto = true)
        {
            if (!auto) MatchType = "scrimmage";
            DateTime now = DateTime.Now;
            string year = now.Year.ToString();
            string month = now.Month.ToString();
            string day = now.Day.ToString();
            string hour = now.Hour.ToString();
            string minute = now.Minute.ToString();
            string second = now.Second.ToString();
            string matchString = year + month + day + hour + minute + second;
            long.TryParse(matchString, out MatchNo);
            MatchLogger = MyAPIGateway.Utilities.WriteFileInLocalStorage("MatchLog Match " + MatchNo + ".csv", typeof(Session));
        }

        public void Teleport()
        {
            teamB.Teleport();
            teamR.Teleport();
        }

        public void Convert(Field field)
        {
            if(MatchType == "auto")
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage("SpawnLocs.txt", typeof(Session)))
                {
                    var x = MyAPIGateway.Utilities.ReadFileInLocalStorage("SpawnLocs.txt", typeof(Session));
                    double a, b, c, d, e, f;
                    double.TryParse(x.ReadLine(), out a);
                    double.TryParse(x.ReadLine(), out b);
                    double.TryParse(x.ReadLine(), out c);
                    double.TryParse(x.ReadLine(), out d);
                    double.TryParse(x.ReadLine(), out e);
                    double.TryParse(x.ReadLine(), out f);

                    x.Close();
                    x.Dispose();

                    teamR = new Team(Session.Instance.factionControl.redFactionID, new Vector3D(a, b, c));
                    teamB = new Team(Session.Instance.factionControl.blueFactionID, new Vector3D(d, e, f));
                }
                else
                {
                    var x = MyAPIGateway.Utilities.WriteFileInLocalStorage("SpawnLocs.txt", typeof(Session));
                    x.WriteLine(0.0);
                    x.WriteLine(0.0);
                    x.WriteLine(9500.0);
                    x.WriteLine(0.0);
                    x.WriteLine(0.0);
                    x.WriteLine(-9500.0);
                    x.Flush();
                    x.Close();
                    x.Close();

                    teamR = new Team(Session.Instance.factionControl.redFactionID, new Vector3D(0, 0, 9500));
                    teamB = new Team(Session.Instance.factionControl.blueFactionID, new Vector3D(0, 0, -9500));
                }

                foreach(var grid in field.GridValuePairs.OrderByDescending(x => x.Key.Value))
                {
                    if (grid.Key.Faction == Session.Instance.factionControl.neutralFactionID && grid.Value < 17000)
                    {
                        if (teamB.Total > teamR.Total)
                        {
                            teamR.AddGrid(grid.Key);
                            teamR.AddPlayer(grid.Key.Owner);
                        }
                        else
                        {
                            teamB.AddGrid(grid.Key);
                            teamB.AddPlayer(grid.Key.Owner);
                        }
                    }
                }
            }
        }

        public void Recount()
        {
            try
            {
                // Get a list of the players in each belligerent faction
                List<IMyPlayer> tempPlayers = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(tempPlayers);

                List<IMyPlayer> tempBlue = new List<IMyPlayer>();
                List<IMyPlayer> tempRed = new List<IMyPlayer>();

                foreach (var player in tempPlayers)
                {
                    long playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId)?.FactionId ?? 0;

                    if (playerFaction == Session.Instance.factionControl.blueFactionID)
                    {
                        tempBlue.Add(player);
                    }
                    else if (playerFaction == Session.Instance.factionControl.redFactionID)
                    {
                        tempRed.Add(player);
                    }
                }

                if (teamB == null)
                    teamB = new Team(Session.Instance.factionControl.blueFactionID, new Vector3D(0, 0, -9500));

                if (teamR == null)
                    teamR = new Team(Session.Instance.factionControl.redFactionID, new Vector3D(0, 0, 9500));

                teamB.Recount(tempBlue);
                teamR.Recount(tempRed);
            }
            catch (System.Exception ex)
            {
                DisplayErrorMessage("An error occurred during Recount():\n" + ex.Message);
            }
        }
        private void DisplayErrorMessage(string message)
        {
            MyAPIGateway.Utilities.ShowMessage("Error", message);
        }
        public void PreMatch()
        {
            ThisMatchLog = new MatchLog(teamB, teamR, MatchNo);
            ThisMatchLog.PreMatch();
        }

        public void PostMatch()
        {
            ThisMatchLog.PostMatch();
        }

        public void Log()
        {
            ThisMatchLog.Log(MatchLogger);
            MatchLogger.Flush();
            string matchList = "";
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage("MatchList.txt", typeof(Session)))
            {
                TextReader readListFile = MyAPIGateway.Utilities.ReadFileInLocalStorage("MatchList.txt", typeof(Session));
                matchList = readListFile.ReadToEnd();
                readListFile.Close();
                readListFile.Dispose();
                MyAPIGateway.Utilities.DeleteFileInLocalStorage("MatchList.txt", typeof(Session));
            }
            TextWriter MatchList = MyAPIGateway.Utilities.WriteFileInLocalStorage("MatchList.txt", typeof(Session));
            MatchList.Write(matchList);
            MatchList.WriteLine("MatchLog Match " + MatchNo + ".csv");
            MatchList.Flush();
            MatchList.Close();
            MatchList.Dispose();
        }

        public void Close()
        {
            if (Session.Instance.stateControl.state == State.Run) Log();
            teamB?.Close();
            teamR?.Close();
            MatchLogger?.Flush();
            MatchLogger?.Close();
            MatchLogger?.Dispose();
        }
    }
}
