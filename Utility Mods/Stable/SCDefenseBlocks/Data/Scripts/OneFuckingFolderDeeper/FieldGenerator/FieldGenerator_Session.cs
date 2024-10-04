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

        private int UpdateCounter = 0;
        private int UpdateInterval = 10;

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

            Log.Info($"PacketManager: Packets in Queue");

            long entityID = 0;
            PacketBase packet = null;

            packet = PacketQueueManager.I.Peek(out entityID);           

            if (packet != null)
            {
                Log.Info($"PacketManager: Queued Packet Found!");

                UpdateCounter++;

                int updateCount = (int)(entityID % UpdateInterval);

                if (UpdateCounter % UpdateInterval == updateCount)
                {
                    Log.Info($"PacketManager: Sending Queued Packet, Then Removing From Queue");

                    if (MyAPIGateway.Session.IsServer)
                        HeartNetwork.I.SendToEveryone(packet);
                    else
                        HeartNetwork.I.SendToServer(packet);

                    PacketQueueManager.I.Dequeue();
                }
                else 
                {
                    Log.Info($"PacketManager: Packet Not Sent, Left In Queue");
                }

                if (UpdateCounter >= int.MaxValue - UpdateInterval)
                {
                    UpdateCounter = 0;
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
