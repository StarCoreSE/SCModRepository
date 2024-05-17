using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace YourName.ModName.Data.Scripts.ScCoordWriter
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ScCoordWriter : MySessionComponentBase
    {
        public static ScCoordWriter Instance;
        private ushort NetworkId;
        private List<TrackedItem> TrackedItems = new List<TrackedItem>();
        private TextWriter Writer;
        private bool Recording;

        private const int Version = 2;
        private readonly string[] _columns =
        {
            "kind", "name", "owner", "faction", "factionColor", "entityId", "health", "position", "rotation", "gridSize"
        };

        private const string Extension = ".scc";
        private const string CommandPrefix = "/coordwriter";
        public string Usage = $"Usage: {CommandPrefix} [stop|start]";

        private int TickCounter = 0;

        private class TrackedItem
        {
            public object item;
            public int initialBlockCount;
            public bool isVolumeExported;

            public TrackedItem(object item, int initialBlockCount = 1)
            {
                this.item = item;
                this.initialBlockCount = initialBlockCount;
            }
        }

        public override void LoadData()
        {
            Instance = this;
            NetworkId = 12493;
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.MessageEnteredSender += HandleMessage;
            }
            else
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkId, ReceivedPacket);
            }

            MyAPIGateway.Entities.GetEntities(null, entity =>
            {
                if (ShouldBeTracked(entity))
                {
                    var grid = entity as IMyCubeGrid;
                    if (grid != null)
                    {
                        var blocks = new List<IMySlimBlock>();
                        grid.GetBlocks(blocks);
                        TrackedItems.Add(new TrackedItem(grid, blocks.Count));
                    }
                }
                return false;
            });
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            if (ShouldBeTracked(entity))
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    var blocks = new List<IMySlimBlock>();
                    grid.GetBlocks(blocks);
                    TrackedItems.Add(new TrackedItem(grid, blocks.Count));
                }
            }
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            for (var i = 0; i < TrackedItems.Count; ++i)
            {
                var cur = TrackedItems[i];
                var grid = cur.item as IMyCubeGrid;
                if (grid != null && grid.EntityId == entity.EntityId)
                {
                    TrackedItems.RemoveAt(i);
                    break;
                }
            }
        }

        private bool ShouldBeTracked(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            return grid != null && !grid.IsStatic && grid.Physics != null;
        }

        protected override void UnloadData()
        {
            Writer?.Close();
            TrackedItems?.Clear();
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.MessageEnteredSender -= HandleMessage;
            }
            else
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, ReceivedPacket);
            }
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
        }

        public void Start()
        {
            var fileName = $"{DateTime.Now:dd-MM-yyyy HHmm}{Extension}";

            try
            {
                Writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(ScCoordWriter));
                Writer.NewLine = "\n";
                MyVisualScriptLogicProvider.SendChatMessage("Global grid tracker file created");
                Writer.WriteLine($"version {Version}");
                Writer.WriteLine(string.Join(",", _columns));
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine("Failed to create grid tracker file.");
                MyVisualScriptLogicProvider.SendChatMessage("Failed to create grid tracker file.");
                MyLog.Default.WriteLine(ex);
            }

            if (TrackedItems != null)
            {
                foreach (var element in TrackedItems)
                {
                    element.isVolumeExported = false;
                }
            }

            Recording = true;
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkId, new byte[] { 1 });
            MyAPIGateway.Utilities.ShowNotification("Recording started.");
        }

        public void Stop()
        {
            Recording = false;
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkId, new byte[] { 0 });
            MyAPIGateway.Utilities.ShowNotification("Recording ended.");
        }

        public override void UpdateAfterSimulation()
        {
            if (!Recording) return;
            if (TrackedItems == null)
            {
                MyVisualScriptLogicProvider.SendChatMessage("TrackedItems is null");
                return;
            }

            if (TickCounter++ < 60) { return; }
            TickCounter = 0;

            Writer.WriteLine($"start_block,{DateTime.Now}");
            TrackedItems.ForEach(element =>
            {
                if (element.item == null)
                {
                    MyLog.Default.WriteLine("null item in TrackedItems");
                    return;
                }

                var grid = element.item as IMyCubeGrid;
                var owner = GetGridOwner(grid);
                var factionName = GetFactionName(owner);
                var factionColor = GetFactionColor(owner);

                // Use the grid's world matrix for position and rotation
                MatrixD worldMatrix = grid.WorldMatrix;
                Vector3D position = grid.Physics.CenterOfMassWorld;
                Quaternion rotation = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);

                var blockList = new List<IMySlimBlock>();
                grid.GetBlocks(blockList);
                var currentBlockCount = blockList.Count;
                if (currentBlockCount > element.initialBlockCount)
                {
                    element.initialBlockCount = currentBlockCount;
                }
                var healthPercent = (float)currentBlockCount / element.initialBlockCount;

                // Determine if the grid is small or large
                var gridSize = grid.GridSizeEnum == MyCubeSize.Small ? "Small" : "Large";

                Writer.WriteLine($"grid,{grid.CustomName},{owner?.DisplayName ?? "Unowned"},{factionName},{factionColor},{grid.EntityId},{SmallDouble(healthPercent)},{SmallVector3D(position)},{SmallQuaternion(rotation)},{gridSize}");

                if (!element.isVolumeExported)
                {
                    var volume = ConvertToBase64BinaryVolume(grid);
                    Writer.WriteLine($"volume,{grid.EntityId},{volume}");
                    element.isVolumeExported = true;
                }
            });
            Writer.Flush();
        }

        public string SmallQuaternion(Quaternion q)
        {
            return
                $"{SmallDouble(q.X)} {SmallDouble(q.Y)} {SmallDouble(q.Z)} {SmallDouble(q.W)}";
        }
        public string SmallVector3D(Vector3D v)
        {

            return $"{SmallDouble(v.X)} {SmallDouble(v.Y)} {SmallDouble(v.Z)}";
        }
        public string SmallDouble(double value)
        {
            const int decimalPlaces = 2;
            return value.ToString($"F{decimalPlaces}");
        }

        public void HandleMessage(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith(CommandPrefix)) return;
            sendToOthers = false;

            var args = messageText.Split(' ');

            if (args.Length != 2)
            {
                return;

            }

            switch (args[1])
            {
                case "start":
                    Start();
                    break;
                case "stop":
                    Stop();
                    break;
                default:
                    {
                        var error = $"[{nameof(ScCoordWriter)}] Unknown command '{args[1]}'";
                        MyLog.Default.WriteLine(error);
                        MyAPIGateway.Utilities.ShowMessage($"[{nameof(ScCoordWriter)}]", error);
                        MyAPIGateway.Utilities.ShowMessage($"[{nameof(ScCoordWriter)}]", Usage);
                    }
                    break;
            }
        }

        public void ReceivedPacket(ushort channelId, byte[] data, ulong steamSenderId, bool isSenderServer)
        {
            if (data != null && data.Length == 1)
            {
                Recording = data[0] == 1;
                if (Recording)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
        }

        private string GetFactionName(IMyIdentity player)
        {
            if (player == null) return "Unowned";
            IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
            return playerFaction != null ? playerFaction.Name : "Unowned";
        }

        private string GetFactionColor(IMyIdentity owner)
        {
            if (owner == null) return SmallVector3D(Vector3D.Zero);

            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner.IdentityId);
            if (faction != null)
            {
                // Example, replace with actual way to get faction color if available

                return SmallVector3D(faction.CustomColor);
            }
            return SmallVector3D(Vector3D.Zero); // Default color if no faction or no color defined
        }


        public IMyIdentity GetGridOwner(IMyCubeGrid grid)
        {
            IMyIdentity owner = null;
            if (grid.BigOwners.Count > 0)
            {
                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, id => id.IdentityId == grid.BigOwners[0]);
                if (identities.Count > 0)
                {
                    owner = identities[0];
                }
            }
            return owner;
        }

        public string ConvertToBase64BinaryVolume(IMyCubeGrid grid)
        {
            // Get grid dimensions
            var extents = grid.Max - grid.Min + Vector3I.One;
            int width = extents.X;
            int height = extents.Y;
            int depth = extents.Z;

            // Calculate number of bytes needed to store the volume
            int numBytes = (width * height * depth + 7) / 8;

            // Create byte array to store binary volume
            byte[] binaryVolume = new byte[numBytes];

            // Iterate over grid cells
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Calculate index in byte array
                        int byteIndex = z * width * height + y * width + x;
                        int bytePosition = byteIndex % 8;

                        // Check if block is present at the cell
                        var block = grid.GetCubeBlock(new Vector3I(x, y, z) + grid.Min);
                        bool blockPresent = block != null;

                        // Set corresponding bit in byte
                        if (blockPresent)
                        {
                            binaryVolume[byteIndex / 8] |= (byte)(1 << (7 - bytePosition)); // Invert the byte position
                        }
                    }
                }
            }

            // Create header with grid extents
            byte[] header = BitConverter.GetBytes(width)
                .Concat(BitConverter.GetBytes(height))
                .Concat(BitConverter.GetBytes(depth))
                .ToArray();

            // Combine header and binary volume
            byte[] result = header.Concat(binaryVolume).ToArray();

            // Compress the result using RLE
            byte[] compressedData = Compress(result);

            // Convert to base64 string
            string base64String = Convert.ToBase64String(compressedData);

            return base64String;
        }
        
        private byte[] Compress(byte[] data)
        {
            List<byte> compressedData = new List<byte>();

            int count = 1;
            byte currentByte = data[0];

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] == currentByte)
                {
                    count++;
                    if (count == 256)
                    {
                        // Add the current byte and max count (255), reset the count
                        compressedData.Add(currentByte);
                        compressedData.Add(255);
                        count = 1;
                    }
                }
                else
                {
                    compressedData.Add(currentByte);
                    compressedData.Add((byte)count);
                    count = 1;
                    currentByte = data[i];
                }
            }

            // Add the last byte and its count
            compressedData.Add(currentByte);
            compressedData.Add((byte)count);

            return compressedData.ToArray();
        }
    }
}
