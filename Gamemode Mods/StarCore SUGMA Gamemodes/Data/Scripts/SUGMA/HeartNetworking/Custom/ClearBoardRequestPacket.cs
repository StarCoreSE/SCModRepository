using ProtoBuf;
using Sandbox.ModAPI;
using SC.SUGMA.Utilities;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class ClearBoardRequestPacket : PacketBase
    {
        [ProtoMember(1)] public bool ResetFactions;

        public ClearBoardRequestPacket(bool resetFactions)
        {
            ResetFactions = resetFactions;
        }

        private ClearBoardRequestPacket()
        {
        }

        public override void Received(ulong SenderSteamId)
        {
            SUtils.ClearBoard(ResetFactions);
            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(this);
        }
    }
}
