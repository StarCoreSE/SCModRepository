using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.RepairModule.Networking.Custom
{
    [ProtoContract]
    public class PriorityOnlyPacket : PacketBase
    {
        [ProtoMember(7)] private bool priorityOnly;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recieved Terminal Controls Update Request. Contents:\n    PriorityOnly: " + priorityOnly);

            RepairModule.Instance.PriorityOnly = priorityOnly;
        }

        public static void UpdatePriorityOnly(string message = "")
        {
            try
            {
                PriorityOnlyPacket packet = new PriorityOnlyPacket
                {
                    priorityOnly = RepairModule.Instance.PriorityOnly,
                };

                Log.Info("Sending Terminal Controls Update. Contents:\n    SubsystemPriority: " + packet.priorityOnly);

                if (MyAPIGateway.Session.IsServer)
                    HeartNetwork.I.SendToEveryone(packet);
                else
                    HeartNetwork.I.SendToServer(packet);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
}
    }
}