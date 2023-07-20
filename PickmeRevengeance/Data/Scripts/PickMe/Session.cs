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
using DefenseShields;

namespace PickMe
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class Session: MySessionComponentBase
    {
        public static Session Instance; // the only way to access session comp from other classes and the only accepted static field.
        public FactionControl factionControl = new FactionControl();
        public static Dictionary<string, int> PointValues = new Dictionary<string, int>();
        public static List<string> Header = new List<string>();
        public Field currentField;
        public Match currentMatch;
        public Network networking = new Network(4116);
        public StateControl stateControl = new StateControl();
        public TextWriter debugLog;
        internal static ShieldApi Sh_Api;
        public int time = 0;
        public int matchTime = 60 * 60 * 20;

        public override void LoadData()
        {
            Instance = this;
            MyAPIGateway.Utilities.RegisterMessageHandler(2546247, AddPointValues); 
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.MessageEntered += MessageEntered;
        }

        public override void BeforeStart()
        {
            networking.Register();
            if (MyAPIGateway.Session.IsServer)
            {
                using (debugLog = MyAPIGateway.Utilities.WriteFileInLocalStorage("Debug.txt", typeof(Session)))
                {
                    factionControl.Setup();
                    Sh_Api = new ShieldApi();
                    if (Sh_Api != null)
                    {
                        Sh_Api.Load();
                    }
                }
            }
        }


        public override void UpdateBeforeSimulation()
        {
            if(stateControl.state == State.Run)
            {
                time++;
                if(time >= matchTime)
                {
                    //stateControl.EndMatch();
                }
            }
            base.UpdateBeforeSimulation();
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer && stateControl.state == State.Run)
            {
                currentMatch.Close();
                currentField.Close();
                debugLog.Close();
                debugLog.Dispose(); // Ensure the file is disposed
            }
            Instance = null; // important for avoiding this object to remain allocated in memory
            MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
            networking?.Unregister();
            networking = null;
        }

        public void AddPointValues(object obj)
        {
            string var = MyAPIGateway.Utilities.SerializeFromBinary<string>((byte[])obj);
            if (var != null)
            {
                string[] split = var.Split(';');
                foreach (string s in split)
                {
                    string[] parts = s.Split('@');
                    int value;
                    if (parts.Length == 2 && int.TryParse(parts[1], out value))
                    {
                        string name = parts[0].Trim();
                        if (name.Contains("{LS}"))
                        {
                            PointValues.Remove(name.Replace("{LS}", "Large"));
                            PointValues.Add(name.Replace("{LS}", "Large"), value);

                            PointValues.Remove(name.Replace("{LS}", "Small"));
                            PointValues.Add(name.Replace("{LS}", "Small"), value);
                        }
                        else
                        {
                            PointValues.Remove(name);
                            PointValues.Add(name, value);
                        }
                    }
                }
            }
            foreach (var key in PointValues) Header.Add(key.Key);
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            return base.GetObjectBuilder(); // leave as-is.
        }

        private void MessageEntered(string messageText, ref bool sendToOthers)
        {
            sendToOthers = true;

            if(messageText.ToLower() == "/setup")
            {
                networking.SendToServer(new StatePacket("/setup"));
            }
            if (messageText.ToLower() == "/auto")
            {
                networking.SendToServer(new StatePacket("/auto"));
            }
            if (messageText.ToLower() == "/match")
            {
                networking.SendToServer(new StatePacket("/match"));
            }
            if (messageText.ToLower() == "/start")
            {
                networking.SendToServer(new StatePacket("/start"));
            }
            if (messageText.ToLower() == "/end")
            {
                networking.SendToServer(new StatePacket("/end"));
            }
            if (messageText.ToLower() == "/cancel")
            {
                networking.SendToServer(new StatePacket("/cancel"));
            }
            if (messageText.ToLower() == "/dump")
            {
                networking.SendToServer(new StatePacket("/dump"));
            }
        }
    }
}
