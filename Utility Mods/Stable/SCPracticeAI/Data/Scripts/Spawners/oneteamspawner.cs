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
    public class spawnoneteam : MySessionComponentBase
    {
        private int defaultSpawnCount = 1; // Default number of prefabs to spawn
        private ushort netID = 29397;
        private double minSpawnRadiusFromCenter = 1000; // Minimum spawn distance from the center in meters
        private double minSpawnRadiusFromGrids = 1000;  // Minimum spawn distance from other grids in meters
        private IMyFaction RedFaction = null;
        private IMyFaction BluFaction = null;
        private const int maxRetryAttempts = 3; // Configurable retry attempts

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered; // Listen for chat messages
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);
            RedFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("RED");
            BluFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("BLU");
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
                string factionName = prefabPacket.FactionName; // Set to faction name from packet
                SpawnRandomPrefabs(new List<string>(PrefabMaster.PrefabMap.Keys), prefabPacket.PrefabAmount, factionName);
            }
            else
            {
                MyVisualScriptLogicProvider.SendChatMessage($"Prefab {prefabPacket.PrefabName} not found", "spawnteam");
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            string[] parts = messageText.Split(' ');
            if (messageText.StartsWith("/spawnblueteam", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length == 1)
                {
                    ShowPrefabList();
                }
                if (parts.Length >= 2)
                {
                    int spawnCount;
                    if (int.TryParse(parts[1], out spawnCount))
                    {
                        if (spawnCount > 0)
                        {
                            string factionName = "BLU";  // Set to BLU team
                            List<string> prefabNames = new List<string>(PrefabMaster.PrefabMap.Keys);
                            for (int i = 0; i < spawnCount; i++)
                            {
                                string randomPrefabName = prefabNames[MyUtils.GetRandomInt(0, prefabNames.Count)];
                                PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket(randomPrefabName, 1, factionName);
                                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
                                MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);
                            }
                            MyAPIGateway.Utilities.ShowMessage("spawnblueteam", $"Spawned: {spawnCount} prefabs on BLU team.");
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage("spawnblueteam", "Invalid spawn count. Please specify a positive number.");
                        }
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("spawnblueteam", "Invalid spawn count. Please specify a valid number.");
                    }
                }
            }
            else if (messageText.StartsWith("/spawnredteam", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length == 1)
                {
                    ShowPrefabList();
                }
                if (parts.Length >= 2)
                {
                    int spawnCount;
                    if (int.TryParse(parts[1], out spawnCount))
                    {
                        if (spawnCount > 0)
                        {
                            string factionName = "RED";  // Set to RED team
                            List<string> prefabNames = new List<string>(PrefabMaster.PrefabMap.Keys);
                            for (int i = 0; i < spawnCount; i++)
                            {
                                string randomPrefabName = prefabNames[MyUtils.GetRandomInt(0, prefabNames.Count)];
                                PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket(randomPrefabName, 1, factionName);
                                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
                                MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);
                            }
                            MyAPIGateway.Utilities.ShowMessage("spawnredteam", $"Spawned: {spawnCount} prefabs on RED team.");
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage("spawnredteam", "Invalid spawn count. Please specify a positive number.");
                        }
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("spawnredteam", "Invalid spawn count. Please specify a valid number.");
                    }
                }
            }
            else { return; }
            sendToOthers = false;
        }

        private void ShowPrefabList()
        {
            string prefabListMessage = "Available prefabs:";
            foreach (string prefabName in PrefabMaster.PrefabMap.Keys)
            {
                prefabListMessage += "\n" + prefabName;
            }
            if (PrefabMaster.PrefabMap.Count > 0)
            {
                prefabListMessage += "\n\nTo start a battle, type '/spawnblueteam [amount]' (e.g., /spawnblueteam  10. Default 10.";
            }
            else
            {
                prefabListMessage += "\nNo prefabs available.";
            }
            MyAPIGateway.Utilities.ShowMessage("spawnblueteam", prefabListMessage);
        }

        private void SpawnRandomPrefabs(List<string> prefabNames, int spawnCount, string startingFactionName)
        {
            double maxSpawnRadius = 3000;
            List<Vector3D> spawnPositions = new List<Vector3D>();
            Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();
            string currentFactionName = startingFactionName;  // Set to packet team
            Vector3D origin = new Vector3D(0, 0, 0);

            if (startingFactionName == "BLU")
            {
                origin = new Vector3D(-6000, 0, 0);
            }
            if (startingFactionName == "RED")
            {
                origin = new Vector3D(6000, 0, 0);
            }

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3D spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));
                string factionName = currentFactionName;
                bool isValidPosition = CheckAsteroidDistance(spawnPosition, minSpawnRadiusFromGrids) && CheckGridDistance(spawnPosition, minSpawnRadiusFromGrids);

                int attempts = 0;
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
                        string randomPrefabName = prefabNames[MyUtils.GetRandomInt(0, prefabNames.Count)];
                        IMyPrefabManager prefabManager = MyAPIGateway.PrefabManager;
                        List<IMyCubeGrid> resultList = new List<IMyCubeGrid>();
                        IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionName);
                        prefabManager.SpawnPrefab(resultList, randomPrefabName, spawnPosition, direction, up, ownerId: faction?.FounderId ?? 0, spawningOptions: SpawningOptions.None);
                        foreach (IMyCubeGrid spawnedGrid in resultList)
                        {
                            spawnedGrid.ChangeGridOwnership(faction?.FounderId ?? 0, MyOwnershipShareModeEnum.All);
                        }
                        spawnPositions.Add(spawnPosition);
                        if (spawnedCounts.ContainsKey(randomPrefabName))
                        {
                            spawnedCounts[randomPrefabName]++;
                        }
                        else
                        {
                            spawnedCounts[randomPrefabName] = 1;
                        }
                    }
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("spawnblueteam", $"Failed to spawn prefab {prefabNames[i]} after {maxRetryAttempts} attempts.");
                }
            }

            foreach (var kvp in spawnedCounts)
            {
                MyAPIGateway.Utilities.ShowMessage("spawnblueteam", $"Spawned: {kvp.Key} x {kvp.Value}");
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
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered; // Unsubscribe from chat message events
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
        }
    }
}
