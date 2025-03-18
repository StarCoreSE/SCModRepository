using CameraInfoApi.Data.Scripts.CameraInfoApi;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace CameraInfoApi
{
    internal class ServerMain : MySessionComponentBase
    {
        public Dictionary<MyCubeGrid, MyTuple<MatrixD, float>> CameraInfos = new Dictionary<MyCubeGrid, MyTuple<MatrixD, float>>();

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(3621, HandleMessage);
            InitPbApi();
        }

        protected override void UnloadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(3621, HandleMessage);
        }

        private void HandleMessage(ushort handlerId, byte[] package, ulong senderSteamId, bool fromServer)
        {
            if (package == null || package.Length == 0)
                return;
            var message = MyAPIGateway.Utilities.SerializeFromBinary<CameraDataPacket>(package);
            if (message == null)
                return;

            var grid = MyAPIGateway.Entities.GetEntityById(message.GridId) as MyCubeGrid;
            if (grid == null)
                return;

            if (message.FieldOfView == -1)
            {
                CameraInfos.Remove(grid);
                return;
            }

            CameraInfos[grid] = message.Tuple;
        }

        private void InitPbApi()
        {
            var property = MyAPIGateway.TerminalControls.CreateProperty<Func<MyTuple<MatrixD, float>?>, IMyProgrammableBlock>("CameraInfoApi");
            property.Getter = b => () =>
            {
                MyTuple<MatrixD, float> info;
                if (!CameraInfos.TryGetValue((MyCubeGrid) b.CubeGrid, out info))
                    return null;
                return new MyTuple<MatrixD, float>?(info);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(property);

            MyAPIGateway.Entities.GetEntities(null, ent =>
            {
                var grid = ent as IMyCubeGrid;
                if (grid == null)
                    return false;
                
                // Workaround for scripts crashing when loading before the API is ready (i.e. on world load)
                foreach (var pb in grid.GetFatBlocks<IMyProgrammableBlock>())
                    if (!pb.IsRunning && pb.ProgramData.Contains("CameraInfoApi"))
                        pb.Recompile();
                return false;
            });
        }
    }
}
