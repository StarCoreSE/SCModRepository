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

namespace Klime.spawnmytheprefab
{
    [ProtoInclude(1000, typeof(PrefabSpawnPacket))]
    [ProtoContract]
    public class Packet
    {
        public Packet()
        {

        }
    }

    [ProtoContract]
    public class PrefabSpawnPacket : Packet
    {
        [ProtoMember(1)]
        public string prefabName;

        [ProtoMember(2)]
        public int prefabAmount;

        public PrefabSpawnPacket()
        {

        }

        public PrefabSpawnPacket(string prefabName, int prefabAmount)
        {
            this.prefabName = prefabName;
            this.prefabAmount = prefabAmount;
        }
    }


    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class spawnmytheprefab : MySessionComponentBase
    {
        private Dictionary<string, string> prefabMap = new Dictionary<string, string>
        {
            { "EntityCover1", "#EntityCover1" },        //prefab subtype, prefab name
            { "EntityCover2", "#EntityCover2" },
            { "EntityCover3", "#EntityCover3" },
            { "EntityCover4", "#EntityCover4" },
            { "EntityCoverEveFreighter", "#EntityCoverEveFreighter" },
            { "EntityCover4BLU", "#EntityCover4BLU" },
            { "EntityCover4RED", "#EntityCover4RED" },
            { "EntityCoverFractal", "#EntityCoverFractal" },
        };

        private int defaultSpawnCount = 250; // Default number of prefabs to spawn

        private ushort netID = 29394;

        private double minSpawnRadiusFromCenter = 1000; // Minimum spawn distance from the center in meters
        private double minSpawnRadiusFromGrids = 1000;  // Minimum spawn distance from other grids in meters


        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered; // Listen for chat messages
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);
        }

        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            Packet packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet == null) return;

            PrefabSpawnPacket prefabPacket = packet as PrefabSpawnPacket;
            if (prefabPacket == null) return;


            if (prefabMap.ContainsKey(prefabPacket.prefabName))
            {
                SpawnRandomPrefabs(prefabMap[prefabPacket.prefabName], prefabPacket.prefabAmount);
            }
            else
            {
                MyVisualScriptLogicProvider.SendChatMessage($"Prefab {prefabPacket.prefabName} not found", "SpawnCover");
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/spawncover", StringComparison.OrdinalIgnoreCase)) return;
            string[] parts = messageText.Split(' ');

            if (parts.Length == 1)
            {
                // Show list of available prefabs and usage instructions
                ShowPrefabList();
            }
            else if (parts.Length >= 2)
            {
                string prefabName = parts[1];
                int spawnCount = defaultSpawnCount;

                if (parts.Length >= 3)
                {
                    int parsedCount;
                    if (int.TryParse(parts[2], out parsedCount))
                    {
                        spawnCount = parsedCount;
                    }
                }

                PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket(prefabName, spawnCount);
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);

                MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);

                MyAPIGateway.Utilities.ShowMessage("SpawnCover", $"Requesting: {prefabName} x {spawnCount}");
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

            prefabListMessage += "\n\nTo spawn a prefab, type '/spawncover [prefabName] [amount]' (e.g., /spawncover EntityCover1 100). Default 250.";
            MyAPIGateway.Utilities.ShowMessage("SpawnCover", prefabListMessage);
        }

        private void SpawnRandomPrefabs(string targetPrefab, int spawnCount)
        {
            double maxSpawnRadius = 10000; // Maximum spawn radius in meters

            List<Vector3D> spawnPositions = new List<Vector3D>();

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3D origin = new Vector3D(0, 0, 0);
                Vector3D tangent = Vector3D.Forward;
                Vector3D bitangent = Vector3D.Right;

                Vector3D spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up)); // Calculate an appropriate up vector

                bool isValidPosition = CheckAsteroidDistance(spawnPosition, minSpawnRadiusFromGrids) && CheckGridDistance(spawnPosition, minSpawnRadiusFromGrids);

                if (isValidPosition)
                {
                    // Avoid overcrowding by checking against other spawn positions
                    bool tooCloseToOtherPosition = false;
                    foreach (Vector3D existingPosition in spawnPositions)
                    {
                        if (Vector3D.Distance(existingPosition, spawnPosition) < minSpawnRadiusFromGrids)
                        {
                            tooCloseToOtherPosition = true;
                            break;
                        }
                    }

                    if (!tooCloseToOtherPosition)
                    {
                        MyVisualScriptLogicProvider.SpawnPrefab(targetPrefab, spawnPosition, direction, up);
                        spawnPositions.Add(spawnPosition);
                    }
                }
            }
        }



        private bool CheckGridDistance(Vector3D spawnPosition, double minDistance)
        {
            // Get all entities in the game world
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    double distance = Vector3D.Distance(spawnPosition, grid.GetPosition());

                    if (distance < minDistance)
                    {
                        return false; // Distance is too close, not a valid spawn position
                    }
                }
            }

            // Check distance from origin
            double distanceFromOrigin = Vector3D.Distance(spawnPosition, Vector3D.Zero);
            if (distanceFromOrigin < minDistance)
            {
                return false; // Distance from origin is too close, not a valid spawn position
            }

            return true; // Valid spawn position
        }

        private bool CheckAsteroidDistance(Vector3D spawnPosition, double minDistance)
        {
            // Get all asteroid entities in the game world
            List<IMyVoxelBase> voxels = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(voxels);

            foreach (IMyVoxelBase voxel in voxels)
            {
                if (voxel is IMyVoxelMap)
                {
                    BoundingBoxD voxelBox = voxel.PositionComp.WorldAABB;

                    if (voxelBox.Contains(spawnPosition) != ContainmentType.Disjoint)
                    {
                        return false; // Spawn position is inside an asteroid, not a valid spawn position
                    }
                }
            }

            return true; // Valid spawn position
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered; // Unsubscribe from chat message events
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
        }
    }
}