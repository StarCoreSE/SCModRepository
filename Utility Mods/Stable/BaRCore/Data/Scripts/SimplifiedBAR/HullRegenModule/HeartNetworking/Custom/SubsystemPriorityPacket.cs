using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.RepairModule.Networking.Custom
{
    [ProtoContract]
    public class SubsystemPriorityPacket : PacketBase
    {
        [ProtoMember(10)] private long subsystemPriority;
        [ProtoMember(11)] private long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recieved Terminal Controls Update Request. Contents:\n    SubsystemPriority: " + subsystemPriority);

            RepairModule.GetLogic<RepairModule>(entityId).SubsystemPriority = subsystemPriority;
        }

        public static void UpdateSubsystemPriority(long entityID)
        {
            try
            {
                SubsystemPriorityPacket packet = new SubsystemPriorityPacket
                {
                    subsystemPriority = RepairModule.GetLogic<RepairModule>(entityID).SubsystemPriority,
                    entityId = entityID,
                };

                Log.Info("Sending Terminal Controls Update. Contents:\n    SubsystemPriority: " + packet.subsystemPriority);

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