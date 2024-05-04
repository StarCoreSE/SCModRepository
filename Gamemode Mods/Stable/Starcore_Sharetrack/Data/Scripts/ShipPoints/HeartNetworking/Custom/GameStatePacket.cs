using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using ShipPoints.HeartNetworking;

namespace ShipPoints.HeartNetworking.Custom
{
    [ProtoContract]
    internal class GameStatePacket : PacketBase
    {
        [ProtoMember(1)] public int MatchState { get; private set; }

        public GameStatePacket(PointCheck pointCheck)
        {
            MatchState = (int) PointCheck.MatchState;
        }

        public GameStatePacket(bool matchActive)
        {
            MatchState = matchActive ? 1 : 0;
        }

        private GameStatePacket()
        {
        }

        public override void Received(ulong SenderSteamId)
        {
            PointCheck.MatchState = (PointCheck.MatchStateEnum) MatchState;

            if (PointCheck.MatchState == PointCheck.MatchStateEnum.Active)
                PointCheck.BeginMatch();
            else
                PointCheck.EndMatch();
        }
    }
}
