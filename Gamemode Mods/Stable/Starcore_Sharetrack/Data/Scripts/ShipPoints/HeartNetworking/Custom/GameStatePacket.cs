using ProtoBuf;

namespace ShipPoints.HeartNetworking.Custom
{
    [ProtoContract]
    internal class GameStatePacket : PacketBase
    {
        public GameStatePacket(PointCheck pointCheck)
        {
            MatchState = (int)PointCheck.MatchState;
        }

        public GameStatePacket(bool matchActive)
        {
            MatchState = matchActive ? 1 : 0;
        }

        private GameStatePacket()
        {
        }

        [ProtoMember(1)] public int MatchState { get; }

        public override void Received(ulong SenderSteamId)
        {
            PointCheck.MatchState = (PointCheck.MatchStateEnum)MatchState;

            if (PointCheck.MatchState == PointCheck.MatchStateEnum.Active)
                PointCheck.BeginMatch();
            else
                PointCheck.EndMatch();
        }
    }
}