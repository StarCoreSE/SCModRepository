using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    public class GameStatePacket : PacketBase
    {
        [ProtoMember(12)] public string[] Arguments;
        [ProtoMember(11)] public string Gamemode;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recieved gamestate update packet. Contents:\n    Gamemode: " + Gamemode + "\n    Arguments: " +
                     string.Join(", ", Arguments ?? Array.Empty<string>()));

            if (SUGMA_SessionComponent.I.CurrentGamemode != null)
                SUGMA_SessionComponent.I.CurrentGamemode.Arguments = Arguments ?? Array.Empty<string>();

            if (Gamemode == "null")
            {
                SUGMA_SessionComponent.I.StopGamemode();
                return;
            }

            if ((SUGMA_SessionComponent.I.CurrentGamemode?.ComponentId ?? "null") == Gamemode)
                return;

            if (!SUGMA_SessionComponent.I.StartGamemode(Gamemode, Arguments))
                Log.Info("Somehow received invalid gamemode request of type " + Gamemode);
        }

        public static void UpdateGamestate(string[] args = null)
        {
            var packet = new GameStatePacket
            {
                Gamemode = SUGMA_SessionComponent.I.CurrentGamemode?.ComponentId ?? "null",
                Arguments = args ?? SUGMA_SessionComponent.I.CurrentGamemode?.Arguments
            };
            Log.Info("Sending gamestate update packet. Contents:\n    Gamemode: " + packet.Gamemode +
                     "\n    Arguments: " + string.Join(", ", args ?? Array.Empty<string>()));

            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(packet);
            else
                HeartNetwork.I.SendToServer(packet);
        }
    }
}