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

namespace Invalid.NidSpawner
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

        public PrefabSpawnPacket()
        {

        }

        public PrefabSpawnPacket(string prefabName, int prefabAmount)
        {
            PrefabName = prefabName;
            PrefabAmount = prefabAmount;
        }
    }

    public class WaveSpawn
    {
        public string PrefabName;
        public int PrefabAmount;
        public int WaitTimeSeconds;

        public WaveSpawn(string prefabName, int prefabAmount, int waitTimeSeconds)
        {
            PrefabName = prefabName;
            PrefabAmount = prefabAmount;
            WaitTimeSeconds = waitTimeSeconds;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class spawntargetComponent : MySessionComponentBase
    {
        private long NextSpawnTime = 0;
        private int WavesToSpawn = 0;
        private long SpawnInterval = TimeSpan.TicksPerSecond * 10;

        private Dictionary<string, string> prefabMap = new Dictionary<string, string>
        {
            { "LeachdroneAI", "LeachdroneAI" },
            { "CorrosiveAI", "CorrosiveAI" },
            { "BioclutchAI", "BioclutchAI" },
            // Add more prefab mappings here.
        };

        private List<WaveSpawn> predefinedWaves = new List<WaveSpawn>
        {
            new WaveSpawn("LeachdroneAI", 5, 10), // (PrefabName, Quantity, Wait Time in seconds until next Wave in list)
            new WaveSpawn("CorrosiveAI", 4, 30),
            new WaveSpawn("BioclutchAI", 3, 120)
            // Add more waves as needed
        };

        private int defaultSpawnCount = 1; // Default number of prefabs to spawn
        private ushort netID = 24116;
        private double minSpawnRadiusFromCenter = 1000; // Minimum spawn distance from the center in meters
        private double minSpawnRadiusFromGrids = 1000;  // Minimum spawn distance from other grids in meters
        private IMyFaction PirateFaction = null;
        private List<WaveSpawn> waveSpawns = new List<WaveSpawn>();

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered; // Listen for chat messages
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, NetworkHandler);
            PirateFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT");
        }

        public override void UpdateBeforeSimulation()
        {
            if (WavesToSpawn > 0 && DateTime.Now.Ticks >= NextSpawnTime)
            {
                WaveSpawn waveSpawn = waveSpawns[waveSpawns.Count - WavesToSpawn];
                SpawnRandomPrefabs(waveSpawn.PrefabName, waveSpawn.PrefabAmount);

                // Show debug notification with the time when the wave is spawned
                string debugMessage = $"Spawning wave {waveSpawns.Count - WavesToSpawn + 1} at {DateTime.Now.ToLongTimeString()}";
                MyAPIGateway.Utilities.ShowNotification(debugMessage, 5000, "Green");

                NextSpawnTime = DateTime.Now.Ticks + waveSpawn.WaitTimeSeconds * TimeSpan.TicksPerSecond;
                WavesToSpawn--;

                if (WavesToSpawn == 0)
                {
                    // All waves spawned, reset for the next trigger
                    NextSpawnTime = 0;
                    MyAPIGateway.Utilities.ShowNotification("All waves spawned. Resetting for the next trigger.", 5000, "White");
                }
                else
                {
                    MyAPIGateway.Utilities.ShowNotification($"Wave {waveSpawns.Count - WavesToSpawn} spawned. {WavesToSpawn} waves left.", 5000, "White");
                }
            }
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
                SpawnRandomPrefabs(prefabMap[prefabPacket.PrefabName], prefabPacket.PrefabAmount);
            }
            else
            {
                MyVisualScriptLogicProvider.SendChatMessage($"Prefab {prefabPacket.PrefabName} not found", "spawntarget");
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            // Debug notification to check if the message is received
            MyAPIGateway.Utilities.ShowNotification("OnMessageEntered called with message: " + messageText, 5000, "Yellow");

            if (messageText.StartsWith("/wavestart", StringComparison.OrdinalIgnoreCase))
            {
                // Use the predefined list of waves
                waveSpawns.Clear();
                waveSpawns.AddRange(predefinedWaves);
                WavesToSpawn = waveSpawns.Count;

                // Start spawning the first wave immediately
                NextSpawnTime = DateTime.Now.Ticks;

                // Show debug notification when the wave spawning is triggered
                MyAPIGateway.Utilities.ShowNotification("Wave spawning started", 2000, "Green");
            }
            else if (messageText.StartsWith("/spawnnids", StringComparison.OrdinalIgnoreCase))
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

                    PrefabSpawnPacket prefabSpawnPacket = new PrefabSpawnPacket(prefabName, spawnCount);
                    byte[] data = MyAPIGateway.Utilities.SerializeToBinary(prefabSpawnPacket);

                    MyAPIGateway.Multiplayer.SendMessageTo(netID, data, MyAPIGateway.Multiplayer.ServerId);

                    MyAPIGateway.Utilities.ShowMessage("spawntarget", $"Requesting: {prefabName} x {spawnCount}");
                }

                sendToOthers = false;
            }
        }

        private void ShowPrefabList()
        {
            string prefabListMessage = "Available prefabs:";
            foreach (string prefabName in prefabMap.Keys)
            {
                prefabListMessage += "\n" + prefabName;
            }

            prefabListMessage += "\n\nTo spawn a prefab, type '/spawntarget [prefabName] [amount]' (e.g., /spawntarget LamiaAI 1). Default 1.";
            MyAPIGateway.Utilities.ShowMessage("spawntarget", prefabListMessage);
        }

        private void SpawnRandomPrefabs(string targetPrefab, int spawnCount)
        {
            double maxSpawnRadius = 3000; // Maximum spawn radius in meters
            List<Vector3D> spawnPositions = new List<Vector3D>();

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3D origin = new Vector3D(0, 0, 0);
                Vector3D tangent = Vector3D.Forward;
                Vector3D bitangent = Vector3D.Right;

                Vector3D spawnPosition = origin + (Vector3D.Normalize(MyUtils.GetRandomVector3D()) * MyUtils.GetRandomDouble(minSpawnRadiusFromCenter, maxSpawnRadius));
                Vector3D direction = Vector3D.Normalize(origin - spawnPosition);
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(direction, Vector3D.Up));

                bool isValidPosition = CheckAsteroidDistance(spawnPosition, minSpawnRadiusFromGrids) && CheckGridDistance(spawnPosition, minSpawnRadiusFromGrids);

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
