using Digi;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace MIG.Shared.CSharp {
    public static class Gps {
        public static void AddGps(string name, string description, Vector3D position) {
            IMyGps gps = MyAPIGateway.Session.GPS.Create(name, description, position, true, true);
            MyAPIGateway.Session.GPS.AddLocalGps(gps);
        }

        public static void AddGpsColored(string name, string description, Vector3D position, Vector3D color)
        {
            IMyGps gps = MyAPIGateway.Session.GPS.Create(name, description, position, true, true);
            gps.GPSColor = new Color(color);
            MyAPIGateway.Session.GPS.AddLocalGps(gps);
        }

        public static void RemoveWithDescription (string startsWith) {
            if (MyAPIGateway.Session == null) return;
            if (MyAPIGateway.Session.Player == null) return;

            try {
                List<IMyGps> list = MyAPIGateway.Session.GPS.GetGpsList(MyAPIGateway.Session.Player.PlayerID);
                foreach (var item in list) {
                    if (item.Description != null && item.Description.StartsWith(startsWith)) {
                        MyAPIGateway.Session.GPS.RemoveLocalGps(item);
                    }
                }
            } catch (Exception ex) {
                Log.Error(ex, "RemoveSavedGPS()");
            }
        }

        public static IMyGps RemoveWithDescription(string startsWith, long player, bool all = true)
        {
            IMyGps gps = null;
            List<IMyGps> list = MyAPIGateway.Session.GPS.GetGpsList(player);
            foreach (var item in list)
            {
                if (item.Description != null && item.Description.StartsWith(startsWith))
                {
                    gps = item;
                    MyAPIGateway.Session.GPS.RemoveGps(player, item);
                    if (!all) return gps;
                }
            }

            return gps;
        }

        public static IMyGps GetWithDescription(string startsWith, long player)
        {          
            List<IMyGps> list = MyAPIGateway.Session.GPS.GetGpsList(player);
            foreach (var item in list)
            {
                if (item.Description != null && item.Description.StartsWith(startsWith))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
