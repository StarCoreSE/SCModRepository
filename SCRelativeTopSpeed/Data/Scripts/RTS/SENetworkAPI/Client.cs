using Sandbox.ModAPI;
using System;
using VRage;
using VRage.Utils;
using VRageMath;

namespace SENetworkAPI
{
	public class Client : NetworkAPI
	{

		/// <summary>
		/// Handles communication with the server
		/// </summary>
		/// <param name="comId">Identifies the channel to pass information to and from this mod</param>
		/// <param name="keyword">identifies what chat entries should be captured and sent to the server</param>
		public Client(ushort comId, string modName, string keyword = null) : base(comId, modName, keyword)
		{
		}

		/// <summary>
		/// Sends a command packet to the server
		/// </summary>
		/// <param name="commandString">The command to be executed</param>
		/// <param name="message">Text that will be displayed in client chat</param>
		/// <param name="data">A serialized object to be sent across the network</param>
		/// <param name="sent">The date timestamp this command was sent</param>
		/// <param name="steamId">The client reciving this packet (if 0 it sends to all clients)</param>
		/// <param name="isReliable">Enture delivery of the packet</param>
		public override void SendCommand(string commandString, string message = null, byte[] data = null, DateTime? sent = null, ulong steamId = ulong.MinValue, bool isReliable = true)
		{
			if (MyAPIGateway.Session?.Player != null)
			{
				SendCommand(new Command() { CommandString = commandString, Message = message, Data = data, Timestamp = (sent == null) ? DateTime.UtcNow.Ticks : sent.Value.Ticks, SteamId = MyAPIGateway.Session.Player.SteamUserId }, MyAPIGateway.Session.Player.SteamUserId, isReliable);
			}
			else
			{
				MyLog.Default.Warning($"[NetworkAPI] ComID: {ComId} | Failed to send command. Session does not exist.");
			}
		}

		/// <summary>
		/// Sends a command packet to the server
		/// </summary>
		/// <param name="cmd">The object to be sent to the client</param>
		/// <param name="steamId">The users steam ID</param>
		/// <param name="isReliable">Makes sure the message is recieved by the server</param>
		internal override void SendCommand(Command cmd, ulong steamId = ulong.MinValue, bool isReliable = true)
		{
			if (cmd.Data != null && cmd.Data.Length > CompressionThreshold)
			{
				cmd.Data = MyCompression.Compress(cmd.Data);
				cmd.IsCompressed = true;
			}

			cmd.Timestamp = DateTime.UtcNow.Ticks;
			byte[] packet = MyAPIGateway.Utilities.SerializeToBinary(cmd);

			if (LogNetworkTraffic)
			{
				MyLog.Default.Info($"[NetworkAPI] TRANSMITTING Bytes: {packet.Length}  Command: {cmd.CommandString}  User: {steamId}");
			}

			MyAPIGateway.Multiplayer.SendMessageToServer(ComId, packet, isReliable);
		}

		/// <summary>
		/// Sends a command packet to the server
		/// </summary>
		/// <param name="commandString">The command to be executed</param>
		/// <param name="point">Client side send to server this is not used</param>
		/// <param name="radius">Client side send to server this is not used</param>
		/// <param name="message">Text that will be displayed in client chat</param>
		/// <param name="data">A serialized object to be sent across the network</param>
		/// <param name="sent">The date timestamp this command was sent</param>
		/// <param name="steamId">The client reciving this packet (if 0 it sends to all clients)</param>
		/// <param name="isReliable">Enture delivery of the packet</param>
		public override void SendCommand(string commandString, Vector3D point, double radius = 0, string message = null, byte[] data = null, DateTime? sent = null, ulong steamId = 0, bool isReliable = true)
		{
			SendCommand(commandString, message, data, sent, steamId, isReliable);
		}

		/// <summary>
		/// Sends a command packet to the server
		/// </summary>
		/// <param name="cmd">The object to be sent to the client</param>
		/// <param name="point">Client side send to server this is not used</param>
		/// <param name="radius">Client side send to server this is not used</param>
		/// <param name="steamId">The users steam ID</param>
		/// <param name="isReliable">Makes sure the message is recieved by the server</param>
		internal override void SendCommand(Command cmd, Vector3D point, double radius = 0, ulong steamId = 0, bool isReliable = true)
		{
			SendCommand(cmd, steamId, isReliable);
		}

		public override void Say(string message) 
		{
			SendCommand(null, message);
		}
	}
}
