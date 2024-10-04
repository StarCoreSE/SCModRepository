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

        private int UpdateCounter = 0;
        private int UpdateInterval = 100;

        public override void LoadData()
        {
            base.LoadData();

            Networking.Init("FieldGeneratorNetwork");

            CoreSysAPI = new WcApi();
            CoreSysAPI.Load();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            long entityID = 0;
            PacketBase packet = null;

            packet = (PacketBase)TryGetFirstPacket<FloatSyncPacket>(HeartNetwork.I.queuedFloatPackets, ref entityID)
                  ?? (PacketBase)TryGetFirstPacket<IntSyncPacket>(HeartNetwork.I.queuedIntPackets, ref entityID)
                  ?? (PacketBase)TryGetFirstPacket<BoolSyncPacket>(HeartNetwork.I.queuedBoolPackets, ref entityID);

            if (packet != null)
            {
                UpdateCounter++;

                int updateCount = (int)(entityID % UpdateInterval);

                if (UpdateCounter % UpdateInterval == updateCount)
                {
                    if (MyAPIGateway.Session.IsServer)
                        HeartNetwork.I.SendToEveryone(packet);
                    else
                        HeartNetwork.I.SendToServer(packet);
                }

                if (UpdateCounter >= int.MaxValue - UpdateInterval)
                {
                    UpdateCounter = 0;
                }
            }
        }

        private T TryGetFirstPacket<T>(Dictionary<long, T> packetQueue, ref long entityID) where T : PacketBase
        {
            if (packetQueue.Any())
            {
                var dictFirst = packetQueue.First();
                entityID = dictFirst.Key;
                packetQueue.Remove(entityID);
                return dictFirst.Value;
            }

            return null;
        }

        protected override void UnloadData()
        {
            Networking.Close();

            if (CoreSysAPI.IsReady)
            {
                CoreSysAPI.Unload();
                CoreSysAPI = null;
            }
        }
    }
}
