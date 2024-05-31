namespace SC.SUGMA.HeartNetworking.Custom
{
    public class GameStatePacket : PacketBase
    {
        public string Gamemode;
        public bool IsStarted;

        public override void Received(ulong SenderSteamId)
        {
            
        }
    }
}
