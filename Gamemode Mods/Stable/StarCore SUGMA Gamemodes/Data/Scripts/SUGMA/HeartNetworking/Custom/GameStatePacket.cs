using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    public class GameStatePacket : PacketBase
    {
        [ProtoMember(11)] public string Gamemode;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recieved gamestate update packet. Contents:\n    Gamemode: " + Gamemode);

            if (Gamemode == "null")
            {
                SUGMA_SessionComponent.I.StopGamemode();
                return;
            }

            if ((SUGMA_SessionComponent.I.CurrentGamemode?.ComponentId ?? "null") == Gamemode)
                return;

            if (!SUGMA_SessionComponent.I.StartGamemode(Gamemode, Array.Empty<string>())) // TODO add arguments
                Log.Info("Somehow received invalid gamemode request of type " + Gamemode);
        }

        public static void UpdateGamestate(string message = "")
        {
            GameStatePacket packet = new GameStatePacket
            {
                Gamemode = SUGMA_SessionComponent.I.CurrentGamemode?.ComponentId ?? "null"
            };
            Log.Info("Sending gamestate update packet. Contents:\n    Gamemode: " + packet.Gamemode);

            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(packet);
            else
                HeartNetwork.I.SendToServer(packet);
        }
    }
}