using System;
using System.Collections.Generic;
using Invalid.SCPracticeAI;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using ProtoBuf;

namespace Invalid.SCPracticeAI
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class spawntargetComponent : MySessionComponentBase
    {
        private int defaultSpawnCount = 1;
        private ushort netID = 29395;
        private double minSpawnRadiusFromCenter = 1000;
        private double minSpawnRadiusFromGrids = 1000;
        private IMyFaction PirateFaction = null;
        private const int maxRetryAttempts = 3; // Configurable retry attempts

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);
            PirateFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT");
        }

        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            Packet packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet == null) return;
            PrefabSpawnPacket prefabPacket = packet as PrefabSpawnPacket;
            if (prefabPacket == null) return;
            if (PrefabMaster.PrefabMap.ContainsKey(prefabPacket.PrefabName))
            {
                SpawnRandomPrefabs(PrefabMaster.PrefabMap[prefabPacket.PrefabName], prefabPacket.PrefabAmount);
            }
            else
            {
                MyVisualScriptLogicProvider.SendChatMessage($"Prefab {prefabPacket.PrefabName} not found", "spawntarget");
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/spawntarget", StringComparison.OrdinalIgnoreCase)) return;
            string[] parts = messageText.Split(' ');
            if (parts.Length == 1)
            {
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
                PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket(prefabName, spawnCount, null);
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
                MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);
                MyAPIGateway.Utilities.ShowMessage("spawntarget", $"Requesting: {prefabName} x {spawnCount}");
            }
            sendToOthers = false;
        }

        private void ShowPrefabList()
        {
            string prefabListMessage = "Available prefabs:";
            foreach (string prefabName in PrefabMaster.PrefabMap.Keys)
            {
                prefabListMessage += "\n" + prefabName;
            }
            prefabListMessage += "\n\nTo spawn a prefab, type '/spawntarget [prefabName] [amount]' (e.g., /spawntarget LamiaAI 1). Default 1.";
            MyAPIGateway.Utilities.ShowMessage("spawntarget", prefabListMessage);
        }

        private void SpawnRandomPrefabs(string targetPrefab, int spawnCount)
        {
            double maxSpawnRadius = 10000;
            List<Vector3D> spawnPositions = new List<Vector3D>();
            int attempts = 0;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3D origin = new Vector3D(0, 0, 0);
                Vector3D spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));
                bool isValidPosition = CheckAsteroidDistance(spawnPosition, minSpawnRadiusFromGrids) && CheckGridDistance(spawnPosition, minSpawnRadiusFromGrids);

                while (!isValidPosition && attempts < maxRetryAttempts)
                {
                    spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));
                    isValidPosition = CheckAsteroidDistance(spawnPosition, minSpawnRadiusFromGrids) && CheckGridDistance(spawnPosition, minSpawnRadiusFromGrids);
                    attempts++;
                }

                if (isValidPosition)
                {
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
                        IMyPrefabManager prefabManager = MyAPIGateway.PrefabManager;
                        List<IMyCubeGrid> resultList = new List<IMyCubeGrid>();
                        prefabManager.SpawnPrefab(resultList, targetPrefab, spawnPosition, direction, up, ownerId: PirateFaction.FounderId, spawningOptions: SpawningOptions.None);
                        spawnPositions.Add(spawnPosition);
                    }
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("spawntarget", $"Failed to spawn prefab {targetPrefab} after {maxRetryAttempts} attempts.");
                }

                attempts = 0; // Reset attempts for the next spawn
            }
        }

        private bool CheckGridDistance(Vector3D spawnPosition, double minDistance)
        {
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
                        return false;
                    }
                }
            }
            double distanceFromOrigin = Vector3D.Distance(spawnPosition, Vector3D.Zero);
            if (distanceFromOrigin < minDistance)
            {
                return false;
            }
            return true;
        }

        private bool CheckAsteroidDistance(Vector3D spawnPosition, double minDistance)
        {
            List<IMyVoxelBase> voxels = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(voxels);
            foreach (IMyVoxelBase voxel in voxels)
            {
                if (voxel is IMyVoxelMap)
                {
                    BoundingBoxD voxelBox = voxel.PositionComp.WorldAABB;
                    if (voxelBox.Contains(spawnPosition) != ContainmentType.Disjoint)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
        }
    }
}
