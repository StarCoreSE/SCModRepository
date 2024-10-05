using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using CoreSystems.Api;
using Starcore.FieldGenerator.Networking;
using Sandbox.ModAPI;
using Starcore.FieldGenerator.Networking.Custom;

namespace Starcore.FieldGenerator
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FieldGeneratorSession : MySessionComponentBase
    {
        public static WcApi CoreSysAPI;
        public static HeartNetwork Networking = new HeartNetwork();
        public static PacketQueueManager PacketQueue = new PacketQueueManager();

        public override void LoadData()
        {
            base.LoadData();

            Networking.Init("FieldGeneratorNetwork");
            PacketQueue.Init();

            CoreSysAPI = new WcApi();
            CoreSysAPI.Load();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!PacketQueueManager.I.HasPackets())
                return;

            foreach (long entityID in PacketQueueManager.I.GetEntitiesWithPackets())
            {
                PacketBase packet = PacketQueueManager.I.PeekNextPacket(entityID);

                if (packet != null)
                {                 
                    if (HeartNetwork.CheckRateLimit(entityID))
                    {
                        Log.Info($"PacketManager: Queued Packet Found for Entity ID: {entityID}");

                        if (MyAPIGateway.Session.IsServer)
                            HeartNetwork.I.SendToEveryone(packet);
                        else
                            HeartNetwork.I.SendToServer(packet);

                        Log.Info($"PacketManager: Sent Queued Packet for Entity ID: {entityID}, Removing From Queue");

                        PacketQueueManager.I.DequeuePacket(entityID);
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            Networking.Close();
            PacketQueue.Close();

            if (CoreSysAPI.IsReady)
            {
                CoreSysAPI.Unload();
                CoreSysAPI = null;
            }
        }
    }
}
