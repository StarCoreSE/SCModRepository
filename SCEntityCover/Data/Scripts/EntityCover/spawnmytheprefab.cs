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

namespace Klime.spawnmytheprefab
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class spawnmytheprefab : MySessionComponentBase
    {
        private Random random;
        private Dictionary<string, string> prefabMap; // Map prefab name to blueprint file
        private int defaultSpawnCount = 250; // Default number of prefabs to spawn

        public override void LoadData()
        {
            if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
            {
                random = new Random();
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;

                // Initialize the prefab map
                prefabMap = new Dictionary<string, string>
                {
                    { "EntityCover1", "#EntityCover1" },
                    { "EntityCover3", "#EntityCover3" },
                    { "EntityCoverEveFreighter", "#EntityCoverEveFreighter" },
                    // Add more entries for other prefabs
                };
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/spawncover", StringComparison.OrdinalIgnoreCase))
            {
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

                    if (prefabMap.ContainsKey(prefabName))
                    {
                        // Check if the script is running on a dedicated server
                        if (MyAPIGateway.Utilities.IsDedicated)
                        {
                            MyAPIGateway.Utilities.ShowMessage("SpawnCover", "Cannot spawn prefabs on a dedicated server.");
                        }
                        else
                        {
                            SpawnRandomPrefabs(prefabMap[prefabName], spawnCount);
                        }
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("SpawnCover", $"Prefab '{prefabName}' not found.");
                    }
                }
            }
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
            List<string> prefabList = new List<string> { targetPrefab };

            int prefabCount = prefabList.Count;

            Vector3D origin = new Vector3D(0, 0, 1);
            double spawnRadius = 10000; // Maximum spawn radius in meters
            double minSpawnDistance = 1000; // Minimum spawn distance from the origin in meters

            int existingEntityCoverGridCount = 0;

            // Get all entities in the game world
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            // Count the number of grids containing the specified prefab
            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid != null && grid.DisplayName.Contains(targetPrefab))
                {
                    existingEntityCoverGridCount++;
                }
            }

            for (int i = 0; i < spawnCount; i++)
            {
                // Check if the maximum prefab grids limit has been reached
                if (existingEntityCoverGridCount >= spawnCount)
                {
                    break; // Stop spawning if the limit has been reached
                }

                int randomIndex = random.Next(prefabCount);
                string randomPrefab = prefabList[randomIndex];

                Vector3D spawnPosition = Vector3D.Zero; // Initialize spawnPosition with a default value
                bool isValidSpawn = false;

                int maxAttempts = 10; // Maximum number of attempts to find a valid spawn position
                int currentAttempt = 0;

                do
                {
                    currentAttempt++;

                    if (currentAttempt > maxAttempts)
                    {
                        break; // Break the loop if unable to find a valid spawn position
                    }

                    // Generate a random position within the spawnRadius
                    Vector3D randomPosition = new Vector3D(
                        random.NextDouble() * spawnRadius * 2 - spawnRadius,
                        random.NextDouble() * spawnRadius * 2 - spawnRadius,
                        random.NextDouble() * spawnRadius * 2 - spawnRadius
                    );

                    spawnPosition = origin + randomPosition;

                    // Check distance to existing grids, origin, and asteroids
                    isValidSpawn = CheckGridDistance(spawnPosition, minSpawnDistance) &&
                                   Vector3D.Distance(origin, spawnPosition) >= minSpawnDistance &&
                                   CheckAsteroidDistance(spawnPosition, minSpawnDistance);
                }
                while (!isValidSpawn);

                if (isValidSpawn)
                {
                    Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                    Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up)); // Calculate an appropriate up vector

                    MyVisualScriptLogicProvider.SpawnPrefab(randomPrefab, spawnPosition, direction, up);

                    // Increment the prefab grid count if a new prefab is spawned
                    if (randomPrefab.Contains(targetPrefab))
                    {
                        existingEntityCoverGridCount++;
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
        }
    }
}