using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Jnick_SCModRepository.SCCoordinateOutput.Data.Scripts.ScCoordWriter
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CoordsNetworking : MySessionComponentBase
    {
        private ushort netID = 29400; // Define a unique network ID for message communication

        public static CoordsNetworking I;

        public Action StartGlobalWriter = null;
        public Action StopGlobalWriter = null;

        public override void LoadData()
        {
            I = this;

            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(netID, CommandHandler);
        }

        protected override void UnloadData()
        {
            I = null;

            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(netID, CommandHandler);
        }

        /// <summary>
        /// If true, send start. If false, send stop.
        /// </summary>
        /// <param name="isStart"></param>
        public void SendMessage(bool isStart)
        {
            CommandPacket packet = new CommandPacket();
            if (isStart)
            {
                packet.StartCommand = true;
            }
            else
            {
                packet.StopCommand = true;
            }

            // Serialize and send the command packet to the server
            var serializedPacket = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToServer(netID, serializedPacket);
        }



        // Define message handler for command handling
        private void CommandHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            if (!MyAPIGateway.Session.IsServer) return; // Only handle commands on the server

            CommandPacket packet = MyAPIGateway.Utilities.SerializeFromBinary<CommandPacket>(arg2);
            if (packet == null) return;

            if (packet.StartCommand)
            {
                StartGlobalWriter?.Invoke();
            }
            else if (packet.StopCommand)
            {
                StopGlobalWriter?.Invoke();
            }
        }

        // Define packet structure for command handling
        [ProtoContract]
        internal class CommandPacket
        {
            [ProtoMember(1)]
            public bool StartCommand;

            [ProtoMember(2)]
            public bool StopCommand;
        }
    }
}
