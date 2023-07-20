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
using PickMe.Structure;
using PickMe.Networking;

namespace PickMe.Structure
{
    class MatchLog
    {
        Team TeamB, TeamR;
        public long MatchNo;

        public MatchLog(Team teamB, Team teamR, long matchNo)
        {
            TeamB = teamB;
            TeamR = teamR;
            MatchNo = matchNo;
        }

        public void PreMatch()
        {
            //TeamB.PreMatch();
            //TeamR.PreMatch();
        }

        public void PostMatch()
        {
            //TeamB.PostMatch();
            //TeamR.PostMatch();
        }

        public void Log(TextWriter writer)
        {
            return;
            writer.WriteLine("Match:," + MatchNo + "\n");
            //writer.WriteLine("Green : " + TeamG.ThisTeamLog.Ratio());
            //writer.WriteLine("Violet: " + TeamV.ThisTeamLog.Ratio());
            if (TeamR.ThisTeamLog.Ratio() > TeamB.ThisTeamLog.Ratio())
            {
                writer.WriteLine("Winner");
                writer.WriteLine(TeamR.Log());
            }
            else
            {
                writer.WriteLine("Winner");
                writer.Write(TeamB.Log());
            }
            if (TeamR.ThisTeamLog.Ratio() <= TeamB.ThisTeamLog.Ratio())
            {
                writer.WriteLine("Loser");
                writer.WriteLine(TeamR.Log());
            }
            else
            {
                writer.WriteLine("Loser");
                writer.Write(TeamB.Log());
            }
        }
    }
}
