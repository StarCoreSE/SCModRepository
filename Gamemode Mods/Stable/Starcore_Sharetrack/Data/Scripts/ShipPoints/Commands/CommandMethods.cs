using System;
using Sandbox.ModAPI;
using SCModRepository_Dev.Gamemode_Mods.Development.Starcore_Sharetrack_Dev.Data.Scripts.ShipPoints.HeartNetworking.
    Custom;
using ShipPoints.HeartNetworking;
using ShipPoints.HeartNetworking.Custom;

namespace ShipPoints.Commands
{
    internal static class CommandMethods
    {
        #region Utility Commands

        public static void Shields(string[] args)
        {
            if (MyAPIGateway.Session.IsServer)
                new ShieldFillRequestPacket().Received(0);
            else
                HeartNetwork.I.SendToServer(new ShieldFillRequestPacket());
        }

        public static void ReportProblem(string[] args)
        {
            var message = "@" + (MyAPIGateway.Session.Player?.DisplayName ?? "ERR") + ":";
            for (var i = 1; i < args.Length; i++) // Skip the first argument as it's always "problem"
                message += ' ' + args[i];

            PointCheck.I.ReportProblem(args.Length > 1 ? message : "");
        }

        public static void ReportFixed(string[] args)
        {
            PointCheck.I.ResolvedProblem();
        }

        #endregion
    }
}