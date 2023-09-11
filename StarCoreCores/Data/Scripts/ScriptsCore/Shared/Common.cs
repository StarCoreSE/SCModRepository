using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using VRage.Game.ModAPI;
using VRageMath;

namespace MIG.Shared.SE {
    public static class Common {

        [ProtoContract]
        class PlayerMessage
        {
            [ProtoMember(1)]
            public string message;
            
            [ProtoMember(2)]
            public string author;
            

            [ProtoMember(4)] public string font = "Blue";
        }

        private static Connection<PlayerMessage> SendMessageConnection;
        public static void Init()
        {
            SendMessageConnection = new Connection<PlayerMessage>(16623, RequestSendMessageHandler);
        }

         
        private static void RequestSendMessageHandler(PlayerMessage arg1, ulong arg2, bool arg3)
        {
            SendChatMessage(arg1.message, arg1.author, arg2.IdentityId(), arg1.font);
        }

        public static void SendChatMessage(string message, string author = "", long playerId = 0L, string font = "Blue") {
            if (!MyAPIGateway.Session.IsServer)
            {
                SendMessageConnection.SendMessageToServer(new PlayerMessage()
                {
                    message = message,
                    author = author,
                    font = font
                });
            }
            else
            {
                MyVisualScriptLogicProvider.SendChatMessage (message, author, playerId, font);
            }
            
        }

        public static void SendChatMessageToMe(string message, string author = "", string font = "Blue") {
            if (MyAPIGateway.Session.Player != null) {
                MyVisualScriptLogicProvider.SendChatMessage (message, author, MyAPIGateway.Session.Player.IdentityId, font);
            }
        }

        public static void ShowNotification(string message, int disappearTimeMs, string font = "White", long playerId = 0L) {
            MyVisualScriptLogicProvider.ShowNotification (message, disappearTimeMs, font, playerId);
        }

        public static void ShowNotificationForAllInRange(string message, int disappearTimeMs, Vector3D pos, float r, string font = "White") {
            var pl = GetOnlinePlayersInRange (pos, r);
            foreach (var x in pl) {
                MyVisualScriptLogicProvider.ShowNotification (message, disappearTimeMs, font, x.IdentityId);
            }
        }
        
        
        public static void ShowNotificationForMeInGrid(IMyCubeGrid grid, string message, int disappearTimeMs, string font = "White") {
            if (MyAPIGateway.Session.isTorchServer()) return;
            try {
                var cock = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyCockpit;
                if (cock == null) return;
                if (cock.CubeGrid != grid) {
                    return;
                }
                MyVisualScriptLogicProvider.ShowNotificationLocal(message, disappearTimeMs, font);
            } catch (Exception e) { }
        }
        

        public static List<IMyPlayer> GetOnlinePlayersInRange (Vector3D pos, float r) {
            List<IMyPlayer> players = new List<IMyPlayer>();
            r = r*r;
            MyAPIGateway.Multiplayer.Players.GetPlayers(players, (x)=>{ 
                var ch = x.Character;
                if (ch != null) {
                    return (ch.WorldMatrix.Translation - pos).LengthSquared() < r;
                }
                return false;
            });
            return players;
        }

        


        
        public static String getPlayerName (long id) {
            var p = getPlayer (id);
            return p ==null ? "UnknownP" : p.DisplayName;
        }

        public static IMyPlayer getPlayer (long id) {
            var ind = new List<IMyPlayer>();
           
            MyAPIGateway.Players.GetPlayers (ind,  (x) => { return x.IdentityId == id; });
            return ind.FirstOrDefault(null) as IMyPlayer;
        }
        //public static bool isBot (long id) {
        //    var ind = new List<IMyIdentity>();
        //    MyAPIGateway.Players.GetAllIdentites (ind,  (x) => { return x.IdentityId == id; });
        //    
        //    if (ind.Count == 1) {
        //        ind[0].
        //    }
        //}

        //public static void ShowNotificationToAll(string message, int disappearTimeMs, string font = "White") {
        //    MyVisualScriptLogicProvider.ShowNotificationToAll (message, disappearTimeMs, font);
        //}
        //
        //public static void ShowSystemMessage(string from, string text, long player) {
        //    //MyAPIGateway.Utilities.ShowMessage("System", "Killed by : [" +killer.DisplayName + "] Sent to him: [" + (-took)+"] credits");
        //
        //}
    }
}
