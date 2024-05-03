using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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
        private List<IMyCubeGrid> TrackedGrids;
        private TextWriter Writer;
        private bool Recording;

        private const string Extension = ".scc";
        private const string CommandPrefix = "/coordwriter";
        public string Usage = $"Usage: {CommandPrefix} [stop|start]";

        private int TickCounter = 0;

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

            var fileName = $"{DateTime.Now:dd-MM-yyyy HHmm}{Extension}";

            try
            {
                Writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(ScCoordWriter));
                MyVisualScriptLogicProvider.SendChatMessage($"Global grid tracker file created");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine("Failed to create grid tracker file.");
                MyVisualScriptLogicProvider.SendChatMessage("Failed to create grid tracker file.");
                MyLog.Default.WriteLine(ex);
            }

            TrackedGrids = new List<IMyCubeGrid>();
            MyAPIGateway.Entities.GetEntities(null, e =>
            {
                var grid = e as IMyCubeGrid;
                if (e != null)
                {
                    TrackedGrids.Add(grid);
                }
                return false;
            });
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid == null) return;
            TrackedGrids.Add(grid);
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid == null) return;
            TrackedGrids.Remove(grid);
        }

        protected override void UnloadData()
        {
            Writer.Close();
            TrackedGrids.Clear();
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
            Recording = true;
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkId, new byte[] { 1 });
        }

        public void Stop()
        {
            Recording = false;
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkId, new byte[] { 0 });
        }

        public override void UpdateAfterSimulation()
        {
            if (!Recording) return;
            if (TrackedGrids == null)
            {
                MyVisualScriptLogicProvider.SendChatMessage("TrackedGrids is null");
                return;
            }

            if (TickCounter++ < 60) { return; }
            TickCounter = 0;

            // TODO: Use seconds, milliseconds, frames, or ticks since start of recording instead?
            Writer.WriteLine($"[START:T={DateTime.Now}]");
            TrackedGrids.ForEach(grid =>
            {
                if (grid == null)
                {
                    MyLog.Default.WriteLine("null grid in TrackedGrids");
                    return;
                }


                // TODO: Just use the grid's matrix and forget cockpits?
                IMyCockpit Cockpit = null;
                var cubeGrid = grid as MyCubeGrid;
                if (cubeGrid != null && cubeGrid.HasMainCockpit())
                {
                    Cockpit = cubeGrid.MainCockpit as IMyCockpit;
                }
                
                if (Cockpit == null)
                {
                    foreach (var cockpit in grid.GetFatBlocks<IMyCockpit>())
                    {
                        if (cockpit.IsOccupied)
                        {
                            Cockpit = cockpit;
                            break;
                        }
                    }
                }
                MatrixD worldMatrix = Cockpit?.WorldMatrix ?? grid.WorldMatrix;
                Vector3D forwardDirection = worldMatrix.Forward;
                var position = grid.GetPosition();
                var rotation = Quaternion.CreateFromForwardUp(forwardDirection, grid.WorldMatrix.Up);

                var healthPercent = 1.0f;
                var owner = GetGridOwner(grid);
                var faction = "none";
                if (owner != null)
                {
                    faction = GetFactionName(owner.IdentityId);
                }
                Writer.WriteLine($"[GRID]{grid.CustomName},{owner?.DisplayName ?? "Unowned"},{faction},{SmallDouble(healthPercent)},{SmallVector3D(position)},{SmallQuaternion(rotation)}");
            });
            const string frameSeparator = "[STOP]";
            Writer.WriteLine(frameSeparator);
            Writer.Flush();
        }

        public string SmallQuaternion(Quaternion q)
        {
            return
                $"{SmallDouble(q.X)},{SmallDouble(q.Y)},{SmallDouble(q.Z)},{SmallDouble(q.W)}";
        }
        public string SmallVector3D(Vector3D v)
        {
            
            return $"{SmallDouble(v.X)},{SmallDouble(v.Y)},{SmallDouble(v.Z)}";
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
                case "start": Start();
                    break;
                case "stop": Stop();
                    break;
                default:
                {
                    var error = $"[{nameof(ScCoordWriter)}] Unknown command '{args[1]}'";
                    MyLog.Default.WriteLine(error);
                    MyAPIGateway.Utilities.ShowMessage($"[{nameof(ScCoordWriter)}]", error);
                    MyAPIGateway.Utilities.ShowMessage($"[{nameof(ScCoordWriter)}]", Usage);
                } break;
            }
        }

        public void ReceivedPacket(ushort channelId, byte[] data, ulong steamSenderId, bool isSenderServer)
        {
            if (data != null && data.Length == 1)
            {
                Recording = data[0] == 1;
            }
        }

        private string GetFactionName(long playerId)
        {
            IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            return playerFaction != null ? playerFaction.Name : "Unowned";
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
    }
}