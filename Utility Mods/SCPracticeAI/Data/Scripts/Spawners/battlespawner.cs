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

namespace Invalid.SCPracticeAI
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class spawnbattleComponent : MySessionComponentBase
    {
        private ModConfig config;
        private int defaultSpawnCount = 1;
        private ushort netID = 29396;
        private double minSpawnRadiusFromCenter = 1000;
        private double minSpawnRadiusFromGrids = 1000;
        private IMyFaction RedFaction = null;
        private IMyFaction BluFaction = null;
        private const int maxRetryAttempts = 3;

        public override void LoadData()
        {
            config = ConfigManager.LoadConfig();
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);

            RedFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("RED");
            BluFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("BLU");

            if (config.AutomaticSpawnBattle && MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    MyVisualScriptLogicProvider.SendChatMessage("Automatic battle will start", "Server");
                });

                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket("BattleSpawn", config.AutomaticSpawnBattleAmount, null);
                    byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
                    NetworkHandler(netID, data, 0, false);
                }, 60000.ToString()); // 60 seconds delay
            }
        }

        private void NetworkHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            Packet packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(arg2);
            if (packet == null) return;

            PrefabSpawnPacket prefabPacket = packet as PrefabSpawnPacket;
            if (prefabPacket == null) return;

            if (prefabPacket.PrefabName == "BattleSpawn")
            {
                List<string> prefabNames = new List<string>(PrefabMaster.PrefabMap.Keys);
                SpawnRandomPrefabs(prefabNames, prefabPacket.PrefabAmount);
            }
            else
            {
                MyVisualScriptLogicProvider.SendChatMessage($"Invalid battle spawn request", "spawnbattle");
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/spawnbattle", StringComparison.OrdinalIgnoreCase)) return;
            string[] parts = messageText.Split(' ');

            if (parts.Length >= 2 && parts[1].Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                HandleConfigCommand(parts);
                sendToOthers = false;
                return;
            }

            if (parts.Length == 1)
            {
                ShowPrefabList();
            }
            else if (parts.Length >= 2)
            {
                int spawnCount;
                if (int.TryParse(parts[1], out spawnCount))
                {
                    if (spawnCount > 0)
                    {
                        PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket("BattleSpawn", spawnCount, null);
                        byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
                        MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);
                        MyAPIGateway.Utilities.ShowMessage("spawnbattle", $"Requesting battle spawn: {spawnCount} prefabs");
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("spawnbattle", "Invalid spawn count. Please specify a positive number.");
                    }
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("spawnbattle", "Invalid spawn count. Please specify a valid number.");
                }
            }

            sendToOthers = false;
        }

        private void HandleConfigCommand(string[] parts)
        {
            if (parts.Length == 2)
            {
                DisplayCurrentConfig();
            }
            else if (parts.Length == 4)
            {
                UpdateConfig(parts);
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("Config", "Invalid input! Usage: /spawnbattle config [true/false] [amount]");
            }
        }

        private void DisplayCurrentConfig()
        {
            MyAPIGateway.Utilities.ShowMessage("Config", $"AutomaticSpawnBattle: {config.AutomaticSpawnBattle}");
            MyAPIGateway.Utilities.ShowMessage("Config", $"AutomaticSpawnBattleAmount: {config.AutomaticSpawnBattleAmount}");
            MyAPIGateway.Utilities.ShowMessage("Config", "Usage: /spawnbattle config [true/false] [amount]");
        }

        private void UpdateConfig(string[] parts)
        {
            bool newAutomaticSpawnBattle;
            int newAutomaticSpawnBattleAmount;

            if (bool.TryParse(parts[2], out newAutomaticSpawnBattle) &&
               int.TryParse(parts[3], out newAutomaticSpawnBattleAmount))
            {
                config.AutomaticSpawnBattle = newAutomaticSpawnBattle;
                config.AutomaticSpawnBattleAmount = newAutomaticSpawnBattleAmount;
                ConfigManager.SaveConfig(config);

                MyAPIGateway.Utilities.ShowMessage("Config", "Configuration updated successfully!");
                MyAPIGateway.Utilities.ShowMessage("Config", $"New values: AutomaticSpawnBattle: {newAutomaticSpawnBattle}, AutomaticSpawnBattleAmount: {newAutomaticSpawnBattleAmount}");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("Config", "Invalid input! Usage: /spawnbattle config [true/false] [amount]");
            }
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
                prefabListMessage += "\n\nTo start a battle, type '/spawnbattle [amount]' (e.g., /spawnbattle 10). Default 10.";
            }
            else
            {
                prefabListMessage += "\nNo prefabs available.";
            }

            MyAPIGateway.Utilities.ShowMessage("spawnbattle", prefabListMessage);
        }

        private void SpawnRandomPrefabs(List<string> prefabNames, int spawnCount)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            double maxSpawnRadius = 10000;
            List<Vector3D> spawnPositions = new List<Vector3D>();
            Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();
            string currentFactionName = "RED";

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3D origin = new Vector3D(0, 0, 0);
                Vector3D spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));

                currentFactionName = (i < spawnCount / 2) ? "RED" : "BLU";

                bool isValidPosition = CheckAsteroidDistance(spawnPosition, minSpawnRadiusFromGrids) && CheckGridDistance(spawnPosition, minSpawnRadiusFromGrids);

                int attempts = 0;
                while (!isValidPosition && attempts < maxRetryAttempts)
                {
                    spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));
                    isValidPosition = CheckAsteroidDistance(spawnPosition, minSpawnRadiusFromGrids) && CheckGridDistance(spawnPosition, minSpawnRadiusFromGrids);
                    attempts++;
                }

                if (isValidPosition && !IsTooCloseToOtherPositions(spawnPosition, spawnPositions))
                {
                    string randomPrefabName = prefabNames[MyUtils.GetRandomInt(0, prefabNames.Count)];
                    IMyPrefabManager prefabManager = MyAPIGateway.PrefabManager;
                    List<IMyCubeGrid> resultList = new List<IMyCubeGrid>();

                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(currentFactionName);

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
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("spawnbattle", $"Failed to spawn prefab after {maxRetryAttempts} attempts.");
                }
            }

            foreach (var kvp in spawnedCounts)
            {
                MyAPIGateway.Utilities.ShowMessage("spawnbattle", $"Spawned: {kvp.Key} x {kvp.Value}");
            }
        }

        private bool IsTooCloseToOtherPositions(Vector3D spawnPosition, List<Vector3D> existingPositions)
        {
            foreach (Vector3D existingPosition in existingPositions)
            {
                if (Vector3D.Distance(existingPosition, spawnPosition) < minSpawnRadiusFromGrids)
                {
                    return true;
                }
            }
            return false;
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
            ConfigManager.SaveConfig(config);
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
        }
    }
}