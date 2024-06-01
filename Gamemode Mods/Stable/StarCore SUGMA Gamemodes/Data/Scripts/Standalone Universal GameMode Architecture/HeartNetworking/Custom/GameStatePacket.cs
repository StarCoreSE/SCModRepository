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
            Log.Info("Recieved gamestate update packet. Contents: " + Gamemode);

            if (Gamemode == "null")
            {
                SUGMA_SessionComponent.I.StopGamemode();
                return;
            }

            if ((SUGMA_SessionComponent.I.CurrentGamemode?.Id ?? "null") == Gamemode)
                return;

            if (!SUGMA_SessionComponent.I.StartGamemode(Gamemode))
                Log.Info("Somehow received invalid gamemode request of type " + Gamemode);
        }

        public static void UpdateGamestate()
        {
            GameStatePacket packet = new GameStatePacket
            {
                Gamemode = SUGMA_SessionComponent.I.CurrentGamemode?.Id ?? "null"
            };
            Log.Info("Sending gamestate update packet. Contents: " + packet.Gamemode);

            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(packet);
            else
                HeartNetwork.I.SendToServer(packet);
        }
    }
}
