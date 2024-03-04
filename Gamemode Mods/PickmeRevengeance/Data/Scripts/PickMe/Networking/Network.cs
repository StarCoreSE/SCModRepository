using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace PickMe.Networking
{
    /// <summary>
    /// Simple network communication example.
    /// 
    /// Always send to server as clients can't send to eachother directly.
    /// Then decide in the packet if it should be relayed to everyone else (except sender and server of course).
    /// 
    /// Security note:
    ///  SenderId is not reliable and can be altered by sender to claim they're someone else (like an admin).
    ///  If you need senderId to be secure, a more complicated process is required involving sending
    ///   every player a unique random ID and they sending that ID would confirm their identity.
    /// </summary>
    public class Network
    {
        public readonly ushort ChannelId;

        private List<IMyPlayer> tempPlayers = null;

        /// <summary>
        /// <paramref name="channelId"/> must be unique from all other mods that also use network packets.
        /// </summary>
        public Network(ushort channelId)
        {
            ChannelId = channelId;
        }

        /// <summary>
        /// Register packet monitoring, not necessary if you don't want the local machine to handle incomming packets.
        /// </summary>
        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ChannelId, ReceivedPacket);
        }

        /// <summary>
        /// This must be called on world unload if you called <see cref="Register"/>.
        /// </summary>
        public void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ChannelId, ReceivedPacket);
        }

        private void ReceivedPacket(ushort handler, byte[] raw, ulong id, bool isFromServer) // executed when a packet is received on this machine
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<StatePacket>(raw);
                HandlePacket(packet, raw);
            }
            catch (Exception e)
            {
                // Handle packet receive errors however you prefer, this is with logging. Remove try-catch to allow it to crash the game.
                // If another mod uses the same channel as your mod, this will throw errors being unable to deserialize their stuff.
                // In that case, one of you must change the channelId and NOT ignoring the error as it can noticeably impact performance.

                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
            }
        }

        private void HandlePacket(StatePacket packet, byte[] rawData = null)
        {
            var relay = packet.Received();
            if (relay)
            {
                rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);
                RelayToClients(packet, rawData);
            }
        }

        /// <summary>
        /// Send a packet to the server.
        /// Works from clients and server.
        /// </summary>
        public void SendToServer(StatePacket packet)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                HandlePacket(packet);
                return;
            }

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToServer(ChannelId, bytes);
        }

        /// <summary>
        /// Send a packet to a specific player.
        /// Only works server side.
        /// </summary>
        public void SendToPlayer(PacketBase packet, ulong steamId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(ChannelId, bytes, steamId);
        }

        /// <summary>
        /// Sends packet (or supplied bytes) to all players except server player and supplied packet's sender.
        /// Only works server side.
        /// </summary>
        public void RelayToClients(StatePacket packet, byte[] rawData = null)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            //packet.Text = Session.Instance.stateControl.Check(packet);//this executes the server match update

            if (tempPlayers == null)
                tempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);
            else
                tempPlayers.Clear();

            MyAPIGateway.Players.GetPlayers(tempPlayers);

            foreach (var p in tempPlayers)
            {
                if (p.IsBot)
                    continue;

                if (p.SteamUserId == MyAPIGateway.Multiplayer.ServerId)
                    continue;

                if(rawData == null)
                    rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

                MyAPIGateway.Multiplayer.SendMessageTo(ChannelId, rawData, p.SteamUserId);
            }

            tempPlayers.Clear();
        }
    }
}