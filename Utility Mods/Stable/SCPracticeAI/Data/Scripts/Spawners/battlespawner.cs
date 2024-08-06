using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using ProtoBuf;
using System.Collections.Generic;
using System;

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
            if (MyAPIGateway.Session.IsServer)
            {
                config = ConfigManager.LoadConfig();
            }
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);

            if (MyAPIGateway.Session.IsServer)
            {
                RedFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("RED");
                BluFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("BLU");

                if (config.AutomaticSpawnBattle)
                {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        MyVisualScriptLogicProvider.SendChatMessage("Automatic battle will start", "Server");
                    });

                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        SpawnRandomPrefabs(new List<string>(PrefabMaster.PrefabMap.Keys), config.AutomaticSpawnBattleAmount);
                    }, 60000.ToString()); // 60 seconds delay
                }
            }
        }

        private void UpdateConfig(bool automaticSpawnBattle, int automaticSpawnBattleAmount)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                config.AutomaticSpawnBattle = automaticSpawnBattle;
                config.AutomaticSpawnBattleAmount = automaticSpawnBattleAmount;
                ConfigManager.SaveConfig(config);
            }
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
                // Start with RED team for the first half and switch to BLU team for the second half
                SpawnRandomPrefabs(new List<string>(PrefabMaster.PrefabMap.Keys), prefabPacket.PrefabAmount);
            }
            else
            {
                MyVisualScriptLogicProvider.SendChatMessage($"Prefab {prefabPacket.PrefabName} not found", "spawnbattle");
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/spawnbattle", StringComparison.OrdinalIgnoreCase)) return;
            string[] parts = messageText.Split(' ');

            // Config command handling
            if (parts.Length >= 2 && parts[1].Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    HandleConfigCommand(parts, ref sendToOthers);
                }
                return;  // Exit after handling config command
            }

            // Battle spawning command
            if (parts.Length == 1)
            {
                ShowPrefabList();
            }
            else if (parts.Length >= 2)
            {
                HandleSpawnCommand(parts, ref sendToOthers);
            }

            sendToOthers = false;
        }

        private void HandleConfigCommand(string[] parts, ref bool sendToOthers)
        {
            if (parts.Length == 2)
            {
                // Display current config
                MyAPIGateway.Utilities.ShowMessage("Config", $"AutomaticSpawnBattle: {config.AutomaticSpawnBattle}");
                MyAPIGateway.Utilities.ShowMessage("Config", $"AutomaticSpawnBattleAmount: {config.AutomaticSpawnBattleAmount}");
                MyAPIGateway.Utilities.ShowMessage("Config", "Usage: /spawnbattle config [true/false] [amount]");
            }
            else if (parts.Length == 4)
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
            else
            {
                MyAPIGateway.Utilities.ShowMessage("Config", "Invalid input! Usage: /spawnbattle config [true/false] [amount]");
            }

            sendToOthers = false;
        }

        private void HandleSpawnCommand(string[] parts, ref bool sendToOthers)
        {
            int spawnCount;
            if (int.TryParse(parts[1], out spawnCount))
            {
                if (spawnCount > 0)
                {
                    List<string> prefabNames = new List<string>(PrefabMaster.PrefabMap.Keys);
                    SpawnRandomPrefabs(prefabNames, spawnCount);
                    MyAPIGateway.Utilities.ShowMessage("spawnbattle", $"Spawned: {spawnCount} prefabs alternately.");
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

            double maxSpawnRadius = 10000; // Maximum spawn radius in meters

            List<Vector3D> spawnPositions = new List<Vector3D>();
            Dictionary<string, int> spawnedCounts = new Dictionary<string, int>(); // To store the counts of each spawned prefab

            string currentFactionName = "RED"; // Start with RED team

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3D origin = new Vector3D(0, 0, 0);

                // Calculate a random spawn position
                Vector3D spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));

                // Calculate orientation vectors
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));

                // Determine the current faction based on the iteration
                if (i < spawnCount / 2)
                {
                    currentFactionName = "RED";
                }
                else
                {
                    currentFactionName = "BLU";
                }

                // Check if the spawn position is valid
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
                        // Randomly select a prefab name from the provided list
                        string randomPrefabName = prefabNames[MyUtils.GetRandomInt(0, prefabNames.Count)];

                        // Spawn the prefab
                        IMyPrefabManager prefabManager = MyAPIGateway.PrefabManager;
                        List<IMyCubeGrid> resultList = new List<IMyCubeGrid>();

                        // Determine the faction to use for the spawned prefab
                        IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(currentFactionName);

                        // Spawn the prefab with the current faction as the owner
                        prefabManager.SpawnPrefab(resultList, randomPrefabName, spawnPosition, direction, up, ownerId: faction?.FounderId ?? 0, spawningOptions: SpawningOptions.None);

                        // Change ownership of the spawned grids
                        foreach (IMyCubeGrid spawnedGrid in resultList)
                        {
                            spawnedGrid.ChangeGridOwnership(faction?.FounderId ?? 0, MyOwnershipShareModeEnum.All);
                        }

                        // Add the spawn position to the list
                        spawnPositions.Add(spawnPosition);

                        // Add to the spawned counts dictionary
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
                    MyAPIGateway.Utilities.ShowMessage("spawnbattle", $"Failed to spawn prefab {prefabNames[i]} after {maxRetryAttempts} attempts.");
                }
            }

            // Report the spawned counts
            foreach (var kvp in spawnedCounts)
            {
                MyAPIGateway.Utilities.ShowMessage("spawnbattle", $"Spawned: {kvp.Key} x {kvp.Value}");
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
            if (MyAPIGateway.Session.IsServer)
            {
                ConfigManager.SaveConfig(config);
            }
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
        }
    }
}