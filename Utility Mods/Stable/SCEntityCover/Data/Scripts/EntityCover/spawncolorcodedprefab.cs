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
        [ProtoMember(2)]
        public int PrefabAmount { get; set; }

        [ProtoMember(3)]
        public double RingRadius { get; set; }

        [ProtoMember(4)]
        public Vector3D Rotation { get; set; }

        public PrefabSpawnPacket() { }

        public PrefabSpawnPacket(int prefabAmount, double ringRadius, Vector3D rotation)
        {
            PrefabAmount = prefabAmount;
            RingRadius = ringRadius;
            Rotation = rotation;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SpawnColorPrefab : MySessionComponentBase
    {
        private readonly Dictionary<string, string> prefabMap = new Dictionary<string, string>
        {
            ["EntityCover4BLU"] = "#EntityCover4BLU",
            ["EntityCover4RED"] = "#EntityCover4RED"
        };

        private const int DefaultSpawnCount = 50;
        private const double DefaultRingRadius = 5000;
        private static readonly Vector3D DefaultRotation = Vector3D.Zero;
        private const ushort NetId = 29399;

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetId, NetworkHandler);
        }

        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            var packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            var prefabPacket = packet as PrefabSpawnPacket;
            if (prefabPacket != null)
            {
                SpawnRandomPrefabs(prefabPacket.PrefabAmount, prefabPacket.RingRadius, prefabPacket.Rotation);
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/spawncolorcover", StringComparison.OrdinalIgnoreCase)) return;
            var parts = messageText.Split(' ');

            if (parts.Length == 1)
            {
                ShowPrefabList();
                return;
            }

            if (parts.Length >= 2)
            {
                HandleSpawnCommand(parts);
            }

            sendToOthers = false;
        }

        private void HandleSpawnCommand(string[] parts)
        {
            var spawnCount = ParseIntParameter(parts, 1, DefaultSpawnCount, "spawn count");
            var ringRadius = ParseDoubleParameter(parts, 2, DefaultRingRadius, "ring radius");
            var rotX = ParseDoubleParameter(parts, 3, DefaultRotation.X, "rotation X");
            var rotY = ParseDoubleParameter(parts, 4, DefaultRotation.Y, "rotation Y");
            var rotZ = ParseDoubleParameter(parts, 5, DefaultRotation.Z, "rotation Z");

            var rotation = new Vector3D(rotX, rotY, rotZ);

            var prefabSpawnPacket = new PrefabSpawnPacket(spawnCount, ringRadius, rotation);
            var data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
            MyAPIGateway.Multiplayer.SendMessageTo(NetId, data, MyAPIGateway.Multiplayer.ServerId);
            MyAPIGateway.Utilities.ShowMessage("SpawnCover", $"Requesting a ring of {spawnCount} color covers at radius {ringRadius} with rotation X: {rotX}, Y: {rotY}, Z: {rotZ}");
        }

        private int ParseIntParameter(string[] parts, int index, int defaultValue, string paramName)
        {
            int value;
            if (parts.Length > index && int.TryParse(parts[index], out value) && value > 0)
            {
                return value;
            }

            MyAPIGateway.Utilities.ShowMessage("SpawnCover", $"Invalid {paramName}. Using default value of {defaultValue}.");
            return defaultValue;
        }

        private double ParseDoubleParameter(string[] parts, int index, double defaultValue, string paramName)
        {
            double value;
            if (parts.Length > index && double.TryParse(parts[index], out value))
            {
                return value;
            }

            MyAPIGateway.Utilities.ShowMessage("SpawnCover", $"Invalid {paramName}. Using default value of {defaultValue}.");
            return defaultValue;
        }

        private void ShowPrefabList()
        {
            var prefabListMessage = "Color Cover Ring Generator:";
            prefabListMessage += $"\n\nTo spawn a ring of color covers, type '/spawncolorcover [amount] [ringRadius] [rotationX] [rotationY] [rotationZ]'.";
            prefabListMessage += "\nRed covers will spawn on +X, Blue covers on -X.";
            prefabListMessage += $"\n[amount]: Number of covers (default {DefaultSpawnCount})";
            prefabListMessage += $"\n[ringRadius]: Radius of the ring (default {DefaultRingRadius})";
            prefabListMessage += $"\n[rotationX]: Rotation around X axis in radians (default {DefaultRotation.X})";
            prefabListMessage += $"\n[rotationY]: Rotation around Y axis in radians (default {DefaultRotation.Y})";
            prefabListMessage += $"\n[rotationZ]: Rotation around Z axis in radians (default {DefaultRotation.Z})";
            MyAPIGateway.Utilities.ShowMessage("SpawnCover", prefabListMessage);
        }

        private void SpawnRandomPrefabs(int spawnCount, double ringRadius, Vector3D rotation)
        {
            var spawnedCount = 0;

            for (var i = 0; i < spawnCount; i++)
            {
                var angle = (double)i / spawnCount * MathHelper.TwoPi;
                var basePosition = new Vector3D(ringRadius * Math.Cos(angle), 0, ringRadius * Math.Sin(angle));

                basePosition = Vector3D.Transform(basePosition, Matrix.CreateRotationY((float)rotation.Y));
                basePosition = Vector3D.Transform(basePosition, Matrix.CreateRotationX((float)rotation.X));
                basePosition = Vector3D.Transform(basePosition, Matrix.CreateRotationZ((float)rotation.Z));

                var spawnPosition = basePosition;
                var direction = Vector3D.Normalize(-spawnPosition);
                var up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));

                var targetPrefab = spawnPosition.X > 0 ? "EntityCover4RED" : "EntityCover4BLU";

                if (prefabMap.ContainsKey(targetPrefab))
                {
                    MyVisualScriptLogicProvider.SpawnPrefab(prefabMap[targetPrefab], spawnPosition, direction, up);
                    spawnedCount++;
                }
                else
                {
                    MyVisualScriptLogicProvider.SendChatMessage($"Prefab {targetPrefab} not found", "SpawnCover");
                }
            }

            MyAPIGateway.Utilities.ShowMessage("SpawnCover", $"Successfully spawned {spawnedCount} color covers in a ring at radius {ringRadius} with rotation X: {rotation.X}, Y: {rotation.Y}, Z: {rotation.Z}.");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetId, NetworkHandler);
        }
    }
}