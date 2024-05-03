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

            var fileName = $"{DateTime.Now:dd-MM-yyyy HHmm} , {Extension}";

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
            TrackedGrids.ForEach(grid =>
            {
                // TODO: maybe get player name if currently controlled by player?
                //var controllerDisplayName = grid.ControlSystem.CurrentShipController.ControllerInfo.ControllingIdentityId;

                var cockpit = (grid as MyCubeGrid)?.MainCockpit as IMyCubeBlock;
                
                Matrix worldMatrix = cockpit?.WorldMatrix ?? grid.WorldMatrix;
                Vector3D forwardDirection = worldMatrix.Forward;
                var position = grid.GetPosition();
                var rotation = Quaternion.CreateFromForwardUp(forwardDirection, grid.WorldMatrix.Up);

                var healthPercent = 1.0f;
                var owner = GetGridOwner(grid);
                var faction = GetFactionName(owner.IdentityId);
                Writer.WriteLine($"{grid.CustomName},{owner?.DisplayName ?? "Unowned"},{faction},{Math.Round(healthPercent, 2)},{position.X},{position.Y},{position.Z},{rotation.X},{rotation.Y},{rotation.Z},{rotation.W}");
            });
            const string frameSeparator = "[STOP]";
            Writer.WriteLine(frameSeparator);
            Writer.Flush();
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