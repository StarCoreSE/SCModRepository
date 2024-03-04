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
using PickMe.Structure;
using PickMe.Networking;
using DefenseShields;
using System.IO;

namespace PickMe
{
    public static class LogReader
    {
        static List<string> lines;
        static TextReader reader;
        static string BigString = "Stats\n";
        public static void Read(ulong senderId)
        {
            MyAPIGateway.Utilities.SendMessage("dump running");
            //get the list of csv files to read from match list
            lines?.Clear();
            lines = new List<string>();
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage("MatchList.txt", typeof(Session)))
                reader = MyAPIGateway.Utilities.ReadFileInLocalStorage("MatchList.txt", typeof(Session));
            else return;
            string temp = reader.ReadToEnd();
            string[] templines = temp.Split('\n');
            foreach (var line in templines)
                lines.Add(line);
            //open each file succesivly and add them to a single string
            reader.Close();
            reader.Dispose();
            foreach(var match in lines)
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(match.Trim().Replace("\n", ""), typeof(Session)))
                {
                    MyAPIGateway.Utilities.SendMessage("loading: " + match);
                    reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(match.Trim().Replace("\n", ""), typeof(Session));
                    BigString += reader?.ReadToEnd();
                    reader.Close();
                    reader.Dispose();
                }
            }
            Session.Instance.networking.SendToPlayer(new StatePacket(BigString), senderId);
        }
    }
}
