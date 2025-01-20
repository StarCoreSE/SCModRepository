using System.Collections.Generic;
//using Sandbox.Common.ObjectBuilders;
//using Sandbox.Common.ObjectBuilders.Definitions;
//using Sandbox.Game.Multiplayer;
//using Sandbox.Definitions;
using Sandbox.Game;
//using Sandbox.Game.Entities;
//using Sandbox.Game.Weapons;
//using Sandbox.ModAPI;
using System;
using VRage.Game;
//using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
//using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRage.Game.Components;
using Sandbox.ModAPI;
//using VRageMath;

namespace Thesccriptthatfixeskeensfactionbullshit_Relations
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Relations : MySessionComponentBase
    {
        public static bool isInit = false;
        public static List<IMyFaction> neutralList = new List<IMyFaction>();
        public static List<IMyFaction> hostileList = new List<IMyFaction>();
        public static List<IMyFaction> alliedList = new List<IMyFaction>();
        public int runCount = 0;
        public static List<string> neutList = new List<string>();
        public static List<string> hostList = new List<string>();
        public static List<string> allyList = new List<string>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
        }

        public override void UpdateBeforeSimulation()
        {
            if (!MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                return;
            if (!isInit)
                init();
            if (++runCount % 3600 > 0)
                return;
            runCount = 0;
            main();
        }

        public void init()
        {
            loadNeut();
            saveNeut();
            loadAlly();
            saveAlly();
            loadHost();
            saveHost();
            var factionList = MyAPIGateway.Session.Factions.Factions;
            foreach (var faction in factionList)
                if (neutList.Contains(faction.Value.Tag))
                    neutralList.Add(faction.Value);
                else if (hostList.Contains(faction.Value.Tag))
                    hostileList.Add(faction.Value);
                else if (allyList.Contains(faction.Value.Tag))
                    alliedList.Add(faction.Value);
            Echo("Relations", "Initialised");
            isInit = true;
        }

        public static void loadNeut()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage("Neutrals.xml", typeof(Relations)))
                {
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("Neutrals.xml", typeof(Relations));
                    var xmlText = reader.ReadToEnd();
                    reader.Close();
                    neutList = MyAPIGateway.Utilities.SerializeFromXML<List<string>>(xmlText);
                }
                else
                {
                    neutList.Add("OUT");
                }
            }
            catch (Exception ex)
            {
                Echo("Relations loadNeut error", ex.ToString());
            }
        }

        public static void saveNeut()
        {
            try
            {
                var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Neutrals.xml", typeof(Relations));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(neutList));
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                Echo("Relations saveNeut error", ex.ToString());
            }
        }

        public static void loadHost()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage("Hostiles.xml", typeof(Relations)))
                {
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("Hostiles.xml", typeof(Relations));
                    var xmlText = reader.ReadToEnd();
                    reader.Close();
                    hostList = MyAPIGateway.Utilities.SerializeFromXML<List<string>>(xmlText);
                }
                else
                {
                    hostList.Add("SPRT");
                    hostList.Add("RED");
                    hostList.Add("BLU");
                }
            }
            catch (Exception ex)
            {
                Echo("Relations loadHost error", ex.ToString());
            }
        }

        public static void saveHost()
        {
            try
            {
                var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Hostiles.xml", typeof(Relations));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(hostList));
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                Echo("Relations saveHost error", ex.ToString());
            }
        }

        public static void loadAlly()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage("Allies.xml", typeof(Relations)))
                {
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("Allies.xml", typeof(Relations));
                    var xmlText = reader.ReadToEnd();
                    reader.Close();
                    allyList = MyAPIGateway.Utilities.SerializeFromXML<List<string>>(xmlText);
                }
                else
                {
                    allyList.Add("ADM");
                }
            }
            catch (Exception ex)
            {
                Echo("Relations loadAlly error", ex.ToString());
            }
        }

        public static void saveAlly()
        {
            try
            {
                var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Allies.xml", typeof(Relations));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(allyList));
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                Echo("Relations saveAlly error", ex.ToString());
            }
        }




        public void main()
        {
            try
            {
                var pList = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(pList);
                foreach (var player in pList)
                {
                    foreach (var faction in neutralList)
                        MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(player.IdentityId, faction.FactionId, 0);
                    foreach (var faction in alliedList)
                        MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(player.IdentityId, faction.FactionId, 1500);
                    foreach (var faction in hostileList)
                        MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(player.IdentityId, faction.FactionId, -1500);
                }

                var factionList = MyAPIGateway.Session.Factions.Factions;

                foreach (var hostile in hostileList)
                {
                    foreach (var faction in factionList)
                    {
                        if (hostile.Tag != faction.Value.Tag)
                        {
                            MyVisualScriptLogicProvider.SetRelationBetweenFactions(hostile.Tag, faction.Value.Tag, -1500);
                        }
                    }
                }

                foreach (var allied in alliedList)
                {
                    foreach (var faction in factionList)
                    {
                        if (allied.Tag != faction.Value.Tag)
                        {
                            MyVisualScriptLogicProvider.SetRelationBetweenFactions(allied.Tag, faction.Value.Tag, 1500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Echo("Relations Main error", ex.ToString());
            }
        }

        public static void Echo(string msg1, string msg2 = "")
        {
            //      MyAPIGateway.Utilities.ShowMessage(msg1, msg2);
            MyLog.Default.WriteLineAndConsole(msg1 + ": " + msg2);
        }

    }
}