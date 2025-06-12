using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SENetworkAPI
{
	public class Server : NetworkAPI
	{
		/// <summary>
		/// Server class contains a few server only feature beond what is inharited from the NetworkAPI
		/// </summary>
		/// <param name="comId">Identifies the channel to pass information to and from this mod</param>
		/// <param name="keyword">identifies what chat entries should be captured and sent to the server</param>
		public Server(ushort comId, string modName, string keyword = null) : base(comId, modName, keyword)
		{
		}

		/// <summary>
		/// Sends a command packet to the client(s)
		/// </summary>
		/// <param name="commandString">The command to be executed</param>
		/// <param name="message">Text that will be displayed in client chat</param>
		/// <param name="data">A serialized object to be sent across the network</param>
		/// <param name="sent">The date timestamp this command was sent</param>
		/// <param name="steamId">The client reciving this packet (if 0 it sends to all clients)</param>
		/// <param name="isReliable">Enture delivery of the packet</param>
		public override void SendCommand(string commandString, string message = null, byte[] data = null, DateTime? sent = null, ulong steamId = ulong.MinValue, bool isReliable = true)
		{
			SendCommand(new Command() { SteamId = steamId, CommandString = commandString, Message = message, Data = data, Timestamp = (sent == null) ? DateTime.UtcNow.Ticks : sent.Value.Ticks }, steamId, isReliable);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandString">Sends a command packet to the client(s)</param>
		/// <param name="point">the center of the sync location</param>
		/// <param name="radius">the distance the message reaches (defaults to sync distance)</param>
		/// <param name="message">Text that will be displayed in client chat</param>
		/// <param name="data">A serialized object to be sent across the network</param>
		/// <param name="sent">The date timestamp this command was sent</param>
		/// <param name="steamId">The client reciving this packet (if 0 it sends to all clients)</param>
		/// <param name="isReliable">Enture delivery of the packet</param>
		public override void SendCommand(string commandString, Vector3D point, double radius = 0, string message = null, byte[] data = null, DateTime? sent = null, ulong steamId = ulong.MinValue, bool isReliable = true)
		{
			SendCommand(new Command() { SteamId = steamId, CommandString = commandString, Message = message, Data = data, Timestamp = (sent == null) ? DateTime.UtcNow.Ticks : sent.Value.Ticks }, point, radius, steamId, isReliable);
		}

		/// <summary>
		/// Sends a command packet to a list of clients
		/// </summary>
		/// <param name="steamIds"></param>
		/// <param name="commandString">The command to be executed</param>
		/// <param name="message">Text that will be displayed in client chat</param>
		/// <param name="data">A serialized object to be sent across the network</param>
		/// <param name="sent">The date timestamp this command was sent</param>
		/// <param name="isReliable">Enture delivery of the packet</param>
		public void SendCommandTo(ulong[] steamIds, string commandString, string message = null, byte[] data = null, DateTime? sent = null, bool isReliable = true)
		{
			foreach (ulong id in steamIds)
			{
				SendCommand(new Command() { SteamId = id, CommandString = commandString, Message = message, Data = data, Timestamp = (sent == null) ? DateTime.UtcNow.Ticks : sent.Value.Ticks }, id, isReliable);
			}
		}

		/// <summary>
		/// Sends a command packet to the client(s)
		/// </summary>
		/// <param name="cmd">The object to be sent to the client</param>
		/// <param name="steamId">The players steam id</param>
		/// <param name="isReliable">Make sure the data arrives</param>
		internal override void SendCommand(Command cmd, ulong steamId = ulong.MinValue, bool isReliable = true)
		{
			if (cmd.Data != null && cmd.Data.Length > CompressionThreshold)
			{
				cmd.Data = MyCompression.Compress(cmd.Data);
				cmd.IsCompressed = true;
			}

			if (!string.IsNullOrWhiteSpace(cmd.Message) && MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session != null)
			{
				MyAPIGateway.Utilities.ShowMessage(ModName, cmd.Message);
			}

			byte[] packet = MyAPIGateway.Utilities.SerializeToBinary(cmd);

			if (LogNetworkTraffic)
			{
				MyLog.Default.Info($"[NetworkAPI] TRANSMITTING Bytes: {packet.Length}  Command: {cmd.CommandString}  User: {steamId}");
			}

			if (steamId == ulong.MinValue)
			{
				MyAPIGateway.Multiplayer.SendMessageToOthers(ComId, packet, isReliable);
			}
			else
			{
				MyAPIGateway.Multiplayer.SendMessageTo(ComId, packet, steamId, isReliable);
			}
		}

		/// <summary>
		/// Sends a command packet to the client(s)
		/// </summary>
		/// <param name="cmd">The object to be sent to the client</param>
		/// <param name="point">the center of the sync location</param>
		/// <param name="radius">the distance the message reaches (defaults to sync distance)</param>
		/// <param name="steamId">The players steam id</param>
		/// <param name="isReliable">Make sure the data arrives</param>
		internal override void SendCommand(Command cmd, Vector3D point, double radius = 0, ulong steamId = ulong.MinValue, bool isReliable = true)
		{
			if (cmd.Data != null && cmd.Data.Length > CompressionThreshold)
			{
				cmd.Data = MyCompression.Compress(cmd.Data);
				cmd.IsCompressed = true;
			}

			if (radius == 0)
			{
				radius = MyAPIGateway.Session.SessionSettings.SyncDistance;
			}

			List<IMyPlayer> players = new List<IMyPlayer>();
			if (steamId == ulong.MinValue)
			{
				MyAPIGateway.Players.GetPlayers(players, (p) => (p.GetPosition() - point).LengthSquared() < (radius * radius) && p.SteamUserId != cmd.SteamId);
			}
			else
			{
				MyAPIGateway.Players.GetPlayers(players, p => p.SteamUserId == steamId);
			}

			if (!string.IsNullOrWhiteSpace(cmd.Message) && MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session != null)
			{
				MyAPIGateway.Utilities.ShowMessage(ModName, cmd.Message);
			}

			cmd.Timestamp = DateTime.UtcNow.Ticks;
			byte[] packet = MyAPIGateway.Utilities.SerializeToBinary(cmd);

			if (LogNetworkTraffic)
			{
				MyLog.Default.Info($"[NetworkAPI] _TRANSMITTING_ Bytes: {packet.Length}  Command: {cmd.CommandString}  To: {players.Count} Users within {radius}m");
			}

			foreach (IMyPlayer player in players)
			{
				MyAPIGateway.Multiplayer.SendMessageTo(ComId, packet, player.SteamUserId, isReliable);
			}
		}

		public override void Say(string message)
		{
			SendCommand(null, message);
		}
	}
}
