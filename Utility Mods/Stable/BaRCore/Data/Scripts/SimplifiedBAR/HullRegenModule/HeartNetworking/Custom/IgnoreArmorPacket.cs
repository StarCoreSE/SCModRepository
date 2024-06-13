using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace StarCore.RepairModule.Networking.Custom
{
    [ProtoContract]
    public class IgnoreArmorPacket : PacketBase
    {
        [ProtoMember(6)] private bool ignoreArmor;

        public override void Received(ulong SenderSteamId)
        {
            Log.Info("Recieved Terminal Controls Update Request. Contents:\n    IgnoreArmor: " + ignoreArmor);

            RepairModule.Instance.IgnoreArmor = ignoreArmor;
        }

        public static void UpdateIgnoreArmor(string message = "")
        {
            try
            {
                IgnoreArmorPacket packet = new IgnoreArmorPacket
                {
                    ignoreArmor = RepairModule.Instance.IgnoreArmor,
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