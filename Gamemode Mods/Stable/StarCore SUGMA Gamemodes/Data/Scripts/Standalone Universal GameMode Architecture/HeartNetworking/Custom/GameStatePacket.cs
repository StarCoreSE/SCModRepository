using Sandbox.ModAPI;

namespace SC.SUGMA.HeartNetworking.Custom
{
    public class GameStatePacket : PacketBase
    {
        public string Gamemode;

        public override void Received(ulong SenderSteamId)
        {
            if (Gamemode == "null")
            {
                SUGMA_SessionComponent.I.StopGamemode();
                return;
            }

            if (SUGMA_SessionComponent.I.CurrentGamemode?.Id == Gamemode)
                return;

            SUGMA_SessionComponent.I.StartGamemode(Gamemode);
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
