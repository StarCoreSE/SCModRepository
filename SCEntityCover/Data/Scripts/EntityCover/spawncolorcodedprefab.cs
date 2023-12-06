using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using ProtoBuf;

namespace Klime.spawncolorcodedprefab
{
    [ProtoInclude(1000, typeof(PrefabSpawnPacket))]
    [ProtoContract]
    public class Packet
    {
        public Packet() { }
    }

    [ProtoContract]
    public class PrefabSpawnPacket : Packet
    {
        [ProtoMember(1)]
        public string prefabName;

        [ProtoMember(2)]
        public int prefabAmount;

        [ProtoMember(3)]
        public double ringRadius; // New field

        [ProtoMember(4)]
        public double rotation; // New field

        public PrefabSpawnPacket() { }

        public PrefabSpawnPacket(string prefabName, int prefabAmount, double ringRadius, double rotation)
        {
            this.prefabName = prefabName;
            this.prefabAmount = prefabAmount;
            this.ringRadius = ringRadius;
            this.rotation = rotation;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class spawncolorprefab : MySessionComponentBase
    {
        private Dictionary<string, string> prefabMap = new Dictionary<string, string>
        {
            { "EntityCover4BLU", "#EntityCover4BLU" },
            { "EntityCover4RED", "#EntityCover4RED" },
        };

        private int defaultSpawnCount = 1; // Default number of prefabs to spawn
        private ushort netID = 29399;

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);
        }

        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            Packet packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet == null) return;

            PrefabSpawnPacket prefabPacket = packet as PrefabSpawnPacket;
            if (prefabPacket == null) return;

            SpawnRandomPrefabs(prefabPacket.prefabAmount, prefabPacket.ringRadius, prefabPacket.rotation);
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/spawncolorcover", StringComparison.OrdinalIgnoreCase)) return;
            string[] parts = messageText.Split(' ');

            if (parts.Length == 1)
            {
                ShowPrefabList();
                return;
            }

            if (parts.Length >= 2)
            {
                string prefabName = parts[1];

                // Initialize spawnCount to the default value to ensure it has a valid value.
                int spawnCount = defaultSpawnCount;
                double ringRadius = 5000; // Default ring radius
                double rotation = 0; // Default rotation

                // Parse spawn count
                if (parts.Length >= 3)
                {
                    // Try to parse the spawn count provided by the user.
                    if (int.TryParse(parts[2], out spawnCount))
                    {
                        // If the user has entered a number less than 1, override it with 1 to ensure at least one prefab spawns.
                        if (spawnCount < 1)
                        {
                            spawnCount = 1;
                        }
                    }
                    else
                    {
                        // If the parsing fails, notify the user and fall back to the default value.
                        MyAPIGateway.Utilities.ShowMessage("SpawnCover", "Invalid spawn count. Using default value of " + defaultSpawnCount);
                        spawnCount = defaultSpawnCount;
                    }
                }

                // Parse ring radius
                if (parts.Length >= 4)
                {
                    double parsedRadius;
                    if (double.TryParse(parts[3], out parsedRadius))
                    {
                        ringRadius = parsedRadius;
                    }
                }

                // Parse rotation
                if (parts.Length >= 5)
                {
                    double parsedRotation;
                    if (double.TryParse(parts[4], out parsedRotation))
                    {
                        rotation = parsedRotation;
                    }
                }

                // Create the packet with the user-specified or default values.
                PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket(prefabName, spawnCount, ringRadius, rotation);
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
                MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);
                // The message has been corrected to accurately reflect the user's request.
                MyAPIGateway.Utilities.ShowMessage("SpawnCover", $"Requesting {spawnCount} prefabs of {prefabName} at radius {ringRadius} with rotation {rotation}");
            }

            sendToOthers = false;
        }


        private void ShowPrefabList()
        {
            string prefabListMessage = "Available prefabs:";
            foreach (string prefabName in prefabMap.Keys)
            {
                prefabListMessage += "\n" + prefabName;
            }

            prefabListMessage += "\n\nTo spawn a prefab, type '/spawncolorcover [prefabName] [amount] [ringRadius] [rotation]'. Default 250 prefabs.";
            MyAPIGateway.Utilities.ShowMessage("SpawnCover", prefabListMessage);
        }

        private void SpawnRandomPrefabs(int spawnCount, double ringRadius, double rotation)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                double angle = (double)i / spawnCount * MathHelper.TwoPi + rotation;
                Vector3D spawnPosition = new Vector3D(ringRadius * Math.Cos(angle), 0, ringRadius * Math.Sin(angle));
                Vector3D origin = new Vector3D(0, 0, 0);
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));

                string targetPrefab = spawnPosition.X > 0 ? "EntityCover4RED" : "EntityCover4BLU";

                if (prefabMap.ContainsKey(targetPrefab))
                {
                    MyVisualScriptLogicProvider.SpawnPrefab(prefabMap[targetPrefab], spawnPosition, direction, up);
                }
                else
                {
                    MyVisualScriptLogicProvider.SendChatMessage($"Prefab {targetPrefab} not found", "SpawnCover");
                }
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
        }
    }
}
