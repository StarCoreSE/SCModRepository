using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Digi;
using VRage.Game.ModAPI;
using VRageMath;

namespace MIG.Shared.SE {
    public static class Common {

       public static void SendChatMessage(string message, string author = "", long playerId = 0L, string font = "Blue") {
            MyVisualScriptLogicProvider.SendChatMessage (message, author, playerId, font);
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
    }
}
