using ProtoBuf;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    public class MissingPlayerOverridePacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                if (MyAPIGateway.Session.IsUserAdmin(SenderSteamId))
                {
                    DisconnectHandler.I.ResolveProblem();
                    HeartNetwork.I.SendToEveryone(new MissingPlayerOverridePacket());
                }
            }
            else
            {
                //should be coming from server
                DisconnectHandler.I.ResolveProblem();
            }
        }
    }
}
