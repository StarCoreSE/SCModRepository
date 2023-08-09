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

        public override void BeforeStart()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                random = new Random();
                SpawnRandomPrefab();
            }
        }

        private void SpawnRandomPrefab()
        {
            List<string> prefabList = new List<string>()
    {
        "#EntityCover1", // Add your prefab names here
        "#EntityCover3", // Add your prefab names here

        // Add more prefab names here
    };

            int prefabCount = prefabList.Count;
            int spawnCount = 1000; // Number of prefabs to spawn

            Vector3D origin = new Vector3D(0, 0, 1);
            double spawnRadius = 5000; // Maximum spawn radius in meters
            double minSpawnDistance = 2000; // Minimum spawn distance from the origin in meters

            int existingEntityCoverGridCount = 0;

            // Get all entities in the game world
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            // Count the number of grids containing the word "EntityCover"
            foreach (IMyEntity entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid != null && grid.DisplayName.Contains("EntityCover"))
                {
                    existingEntityCoverGridCount++;
                }
            }

            for (int i = 0; i < spawnCount; i++)
            {
                // Check if the maximum EntityCover grids limit has been reached
                if (existingEntityCoverGridCount >= 1000)
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
                    Vector3D up = Vector3D.Normalize(spawnPosition);

                    MyVisualScriptLogicProvider.SpawnPrefab(randomPrefab, spawnPosition, direction, up);

                    // Increment the EntityCover grid count if a new EntityCover is spawned
                    if (randomPrefab.Contains("EntityCover"))
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
        }
    }
}
