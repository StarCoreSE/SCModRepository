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

namespace Invalid.spawnbattle
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
        public string PrefabName;

        [ProtoMember(2)]
        public int PrefabAmount;

        [ProtoMember(3)]  // New member for faction name
        public string FactionName;

        // Add a parameterless constructor required by ProtoBuf
        public PrefabSpawnPacket()
        {

        }

        public PrefabSpawnPacket(string prefabName, int prefabAmount, string factionName)
        {
            PrefabName = prefabName;
            PrefabAmount = prefabAmount;
            FactionName = factionName; // Set the faction name
        }
    }



    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class spawnbattleComponent : MySessionComponentBase
    {
        private Dictionary<string, string> prefabMap = new Dictionary<string, string>
        {
            { "CharyAI", "CharyAI" },
            { "HermesAI", "HermesAI" },
            { "LamAI", "LamAI" },
            { "ZerkAI", "ZerkAI" },

            // Add more prefab mappings here.
        };

        private int defaultSpawnCount = 1; // Default number of prefabs to spawn

        private ushort netID = 29396;

        private double minSpawnRadiusFromCenter = 1000; // Minimum spawn distance from the center in meters
        private double minSpawnRadiusFromGrids = 1000;  // Minimum spawn distance from other grids in meters
        private IMyFaction RedFaction = null;
        private IMyFaction BluFaction = null;

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

            if (prefabMap.ContainsKey(prefabPacket.PrefabName))
            {
                // Randomly choose the faction
                string factionName = MyUtils.GetRandomInt(0, 2) == 0 ? "RED" : "BLU";

                SpawnRandomPrefabs(new List<string>(prefabMap.Keys), prefabPacket.PrefabAmount, factionName);
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

            if (parts.Length == 1)
            {
                // Show list of available prefabs and usage instructions
                ShowPrefabList();
            }
            if (parts.Length >= 2)
            {
                int spawnCount;
                if (int.TryParse(parts[1], out spawnCount))
                {
                    if (spawnCount > 0)
                    {
                        // Randomly choose the starting faction
                        string factionName = MyUtils.GetRandomInt(0, 2) == 0 ? "RED" : "BLU";

                        // Select a random prefab from the prefabMap
                        List<string> prefabNames = new List<string>(prefabMap.Keys);

                        // Spawn prefabs alternately using the selected faction
                        for (int i = 0; i < spawnCount; i++)
                        {
                            string randomPrefabName = prefabNames[MyUtils.GetRandomInt(0, prefabNames.Count)];

                            // Create PrefabSpawnPacket instance with the factionName parameter
                            PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket(randomPrefabName, 1, factionName);

                            // Serialize and send the packet
                            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);
                            MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);

                            // Alternate the faction for the next spawn
                            factionName = factionName == "RED" ? "BLU" : "RED";
                        }

                        // Show a confirmation message
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

            if (prefabMap.Count > 0)
            {
                prefabListMessage += "\n\nTo start a battle, type '/spawnbattle [amount]' (e.g., /spawnbattle  10. Default 10.";
            }
            else
            {
                prefabListMessage += "\nNo prefabs available.";
            }

            MyAPIGateway.Utilities.ShowMessage("spawnbattle", prefabListMessage);
        }


        private void SpawnRandomPrefabs(List<string> prefabNames, int spawnCount, string startingFactionName)
        {
            double maxSpawnRadius = 10000; // Maximum spawn radius in meters

            List<Vector3D> spawnPositions = new List<Vector3D>();
            Dictionary<string, int> spawnedCounts = new Dictionary<string, int>(); // To store the counts of each spawned prefab

            string currentFactionName = startingFactionName; // Set the starting faction name

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3D origin = new Vector3D(0, 0, 0);

                // Calculate a random spawn position
                Vector3D spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));

                // Calculate orientation vectors
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));

                // Determine the current faction based on the iteration
                string factionName = currentFactionName;

                // Check if the spawn position is valid
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
                        // Randomly select a prefab name from the provided list
                        string randomPrefabName = prefabNames[MyUtils.GetRandomInt(0, prefabNames.Count)];

                        // Spawn the prefab
                        IMyPrefabManager prefabManager = MyAPIGateway.PrefabManager;
                        List<IMyCubeGrid> resultList = new List<IMyCubeGrid>();

                        // Determine the faction to use for the spawned prefab
                        IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionName);

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

                        // Switch to the other faction for the next spawn
                        currentFactionName = (currentFactionName == "RED") ? "BLU" : "RED";
                    }
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
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered; // Unsubscribe from chat message events
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, NetworkHandler);
        }
    }
}
