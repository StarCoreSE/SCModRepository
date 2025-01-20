using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.RepairModule.Networking.Custom
{
    [ProtoContract]
    public class IgnoreArmorPacket : PacketBase
    {
        [ProtoMember(6)] private bool ignoreArmor;
        [ProtoMember(7)] private long entityId;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recieved Terminal Controls Update Request. Contents:\n    IgnoreArmor: " + ignoreArmor);

            var repairModule = RepairModule.GetLogic<RepairModule>(entityId);

            if (repairModule != null)
            {
                repairModule.ignoreArmor = ignoreArmor;

                if (MyAPIGateway.Session.IsServer)
                    HeartNetwork.I.SendToEveryone(this);
            }
            else
            {
                Log.Info("Received method failed: RepairModule is null. Entity ID: " + entityId);
            }
        }

        public static void UpdateIgnoreArmor(long entityID)
        {
            try
            {
                IgnoreArmorPacket packet = new IgnoreArmorPacket
                {
                    ignoreArmor = RepairModule.GetLogic<RepairModule>(entityID).IgnoreArmor,
                    entityId = entityID,
                };

                Log.Info("Sending Terminal Controls Update. Contents:\n    IgnoreArmor: " + packet.ignoreArmor);

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