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
    public enum State
    {
        Ended = 0, Setup = 1, Auto = 2, Match = 3, Run = 4
    }

    public class StateControl
    {
        public State state = State.Ended;

        public string Check(StatePacket packet)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                if(packet.Text != null)
                {
                    if(packet.Text.ToLower() == "/setup")
                    {
                        if (state == State.Ended) 
                        {
                            state = State.Setup;
                            Session.Instance.factionControl.Setup();
                            return "Setting up the field. To create a new match, use /match to create a custom match or /auto to auto-generate a match.";
                        } 
                        else
                        {
                            return "Match must be ended to begin setup. Use /Cancel.";
                        }
                    }
                    if (packet.Text.ToLower() == "/auto")
                    {
                        if (state == State.Setup)
                        {
                            state = State.Auto;
                            Session.Instance.currentField = new Field();
                            Session.Instance.currentField.FieldSetup();
                            //Session.Instance.currentField.Echo();
                            Session.Instance.currentMatch = new Match();
                            Session.Instance.currentMatch.Convert(Session.Instance.currentField);
                            Session.Instance.currentMatch.Teleport();
                            return "Creating an auto match. Use /start to begin.";
                        }
                        else
                        {
                            return "Field not set up. Use /Setup.";
                        }
                    }
                    if (packet.Text.ToLower() == "/match")
                    {
                        if (state == State.Setup)
                        {
                            state = State.Auto;
                            Session.Instance.currentField = new Field();
                            Session.Instance.currentField.FieldSetup();
                            //Session.Instance.currentField.Echo();
                            Session.Instance.currentMatch = new Match();
                            Session.Instance.currentMatch.Recount();
                            return "Creating a custom match. Use /start to begin.";
                        }
                        else
                        {
                            return "Field not set up. Use /Setup.";
                        }
                    }
                    if (packet.Text.ToLower() == "/start")
                    {
                        if (state == State.Auto)
                        {
                            state = State.Run;
                            Session.Instance.currentMatch.Recount();
                            Session.Instance.factionControl.SetForMatchBegin();
                            Session.Instance.currentMatch.PreMatch();
                            return "Starting the match. Use /end when the match is complete";
                        }
                        else if (state == State.Match)
                        {
                            state = State.Run;
                            Session.Instance.currentMatch.Recount();
                            Session.Instance.factionControl.SetForMatchBegin();
                            Session.Instance.currentMatch.PreMatch();
                            return "Starting the match. Use /end when the match is complete";
                        }
                        else
                        {
                            return "Match not set up. Use /Auto or /Match.";
                        }
                    }
                    if (packet.Text.ToLower() == "/end")
                    {
                        if (state == State.Run)
                        {
                            state = State.Ended;
                            Session.Instance.factionControl.SetForMatchEnd();
                            Session.Instance.currentMatch.PostMatch();
                            Session.Instance.currentMatch.Log(); 
                            Session.Instance.currentField.Close();
                            Session.Instance.currentMatch.Close();
                            Session.Instance.debugLog.Flush();
                            Session.Instance.debugLog.Close();
                            return "Match ended. Start a new field with /setup.";
                        }
                        else
                        {
                            return "Match not running. Use /Start.";
                        }
                    }
                    if (packet.Text.ToLower() == "/cancel")
                    {
                        Session.Instance.currentField.Close();
                        Session.Instance.currentMatch.Close();
                        state = State.Ended;
                        Session.Instance.factionControl.SetForMatchEnd();
                        Session.Instance.debugLog.Flush();
                        Session.Instance.debugLog.Close();
                        return "Match canceled.";
                    }
                    if(packet.Text.ToLower() == "/dump")
                    {
                        //MyAPIGateway.Utilities.SendMessage("dump detected");
                        LogReader.Read(packet.SenderId);
                        return "Sending Statistics";
                    }
                }
            }
            return "Command not recognized. Try starting with /setup.";
        }

        public void EndMatch()
        {
            if (state == State.Ended) return;
            MyAPIGateway.Utilities.SendMessage(state.ToString());
            if (state == State.Run)
            {
                state = State.Ended;
                Session.Instance.currentMatch.PostMatch();
                Session.Instance.currentMatch.Log();
                Session.Instance.currentField.Close();
                Session.Instance.currentMatch.Close();
                Session.Instance.debugLog.Flush();
                Session.Instance.debugLog.Close();
                MyAPIGateway.Utilities.SendMessage("match ended");
            }
        }

        public string CheckClient(StatePacket packet)
        {
            if (!packet.Text.StartsWith("Stats")) return "";
            //MyAPIGateway.Utilities.SendMessage("dump message recieved");
            if (packet.Text != "")
            {
                //MyAPIGateway.Utilities.SendMessage("dump not null");
                if (packet.Text.StartsWith("Stats"))
                {
                    //MyAPIGateway.Utilities.SendMessage("dump valid");
                    uint now = DateTime.Now.ToUnixTimestamp();
                    TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(now + ".csv", typeof(Session));
                    writer.Write(packet.Text);
                    writer.Flush();
                    writer.Close();
                    writer.Dispose();
                    return "dump succeeded";
                }
            }
            return "dump failed";
        }
    }
}
