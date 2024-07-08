using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class ShieldFillRequestPacket : PacketBase
    {
        public override void Received(ulong SenderSteamId)
        {
            foreach (var g in MyEntities.GetEntities())
                if (g != null && !g.MarkedForClose && g is MyCubeGrid)
                {
                    var grid = g as MyCubeGrid;
                    var block = SUGMA_SessionComponent.I.ShieldApi.GetShieldBlock(grid);
                    if (block != null) SUGMA_SessionComponent.I.ShieldApi.SetCharge(block, 99999999999);
                }

            MyAPIGateway.Utilities.ShowMessage("Shields", "Charged");
            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(this);
        }
    }
}
