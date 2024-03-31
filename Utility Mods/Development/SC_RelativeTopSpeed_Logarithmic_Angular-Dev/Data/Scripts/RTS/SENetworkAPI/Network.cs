using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Utils;
using VRageMath;

namespace SENetworkAPI
{
	public enum NetworkTypes { Dedicated, Server, Client }

	public abstract class NetworkAPI
	{
		public static NetworkAPI Instance = null;
		public static bool IsInitialized => Instance != null;
		public static bool LogNetworkTraffic = false;
		public const int CompressionThreshold = 100000;

		/// <summary>
		/// Event triggers apon reciveing data over the network
		/// steamId, command, data
		/// </summary>
		public event Action<ulong, string, byte[], DateTime> OnCommandRecived;

		public readonly ushort ComId;
		public readonly string Keyword;
		public readonly string ModName;

		internal bool UsingTextCommands => Keyword != null;

		internal Dictionary<string, Action<ulong, string, byte[], DateTime>> NetworkCommands = new Dictionary<string, Action<ulong, string, byte[], DateTime>>();
		internal Dictionary<string, Action<string>> ChatCommands = new Dictionary<string, Action<string>>();

		/// <summary>
		/// Event driven client, server syncing API. 
		/// </summary>
		/// <param name="comId">The communication channel this mod will listen on</param>
		/// <param name="modName">The title use for displaying chat messages</param>
		/// <param name="keyward">The string identifying a chat command</param>
		public NetworkAPI(ushort comId, string modName, string keyword = null)
		{
			ComId = comId;
			ModName = (modName == null) ? string.Empty : modName;
			Keyword = (keyword != null) ? keyword.ToLower() : null;

			if (UsingTextCommands)
			{
				MyAPIGateway.Utilities.MessageEntered -= HandleChatInput;
				MyAPIGateway.Utilities.MessageEntered += HandleChatInput;
			}

			MyAPIGateway.Multiplayer.UnregisterMessageHandler(ComId, HandleIncomingPacket);
			MyAPIGateway.Multiplayer.RegisterMessageHandler(ComId, HandleIncomingPacket);

			MyLog.Default.Info($"[NetworkAPI] Initialized. Type: {GetType().Name} ComId: {ComId} Name: {ModName} Keyword: {Keyword}");
		}

		/// <summary>
		/// Invokes chat command events
		/// </summary>
		/// <param name="messageText">Chat message string</param>
		/// <param name="sendToOthers">should be shown normally in global chat</param>
		private void HandleChatInput(string messageText, ref bool sendToOthers)
		{
			string[] args = messageText.ToLower().Split(' ');
			if (args[0] != Keyword)
				return;
			sendToOthers = false;

			string arguments = messageText.Substring(Keyword.Length).Trim(' ');

			// Meh... this is kinda yucky
			if (args.Length == 1 && ChatCommands.ContainsKey(string.Empty))
			{
				ChatCommands[string.Empty]?.Invoke(string.Empty);
			}
			else if (args.Length > 1 && ChatCommands.ContainsKey(args[1]))
			{
				ChatCommands[args[1]]?.Invoke(arguments.Substring(args[1].Length).Trim(' '));
			}
			else
			{
				if (!MyAPIGateway.Utilities.IsDedicated)
				{
					MyAPIGateway.Utilities.ShowMessage(ModName, "Command not recognized.");
				}
			}
		}

		/// <summary>
		/// Unpacks commands and handles arguments
		/// </summary>
		/// <param name="msg">Data chunck recived from the network</param>
		private void HandleIncomingPacket(byte[] msg)
		{
			try
			{
				Command cmd = MyAPIGateway.Utilities.SerializeFromBinary<Command>(msg);

				if (LogNetworkTraffic)
				{
					MyLog.Default.Info($"[NetworkAPI] ----- TRANSMISSION RECIEVED -----");
					MyLog.Default.Info($"[NetworkAPI] Type: {((cmd.IsProperty) ? "Property" : $"Command ID: {cmd.CommandString}")}, {(cmd.IsCompressed ? "Compressed, " : "")}From: {cmd.SteamId} ");
				}

				if (cmd.IsCompressed)
				{
					cmd.Data = MyCompression.Decompress(cmd.Data);
					cmd.IsCompressed = false;
				}

				if (cmd.IsProperty)
				{
					NetSync<object>.RouteMessage(MyAPIGateway.Utilities.SerializeFromBinary<SyncData>(cmd.Data), cmd.SteamId, cmd.Timestamp);
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(cmd.Message))
					{

						if (!MyAPIGateway.Utilities.IsDedicated)
						{
							if (MyAPIGateway.Session != null)
							{
								MyAPIGateway.Utilities.ShowMessage(ModName, cmd.Message);
							}
						}

						if (MyAPIGateway.Multiplayer.IsServer)
						{
							SendCommand(null, cmd.Message);
						}
					}

					if (cmd.CommandString != null)
					{
						OnCommandRecived?.Invoke(cmd.SteamId, cmd.CommandString, cmd.Data, new DateTime(cmd.Timestamp));

						string command = cmd.CommandString.Split(' ')[0];

						if (NetworkCommands.ContainsKey(command))
						{
							NetworkCommands[command]?.Invoke(cmd.SteamId, cmd.CommandString, cmd.Data, new DateTime(cmd.Timestamp));
						}
					}
				}

				if (LogNetworkTraffic)
				{
					MyLog.Default.Info($"[NetworkAPI] ----- END -----");
				}

			}
			catch (Exception e)
			{
				MyLog.Default.Error($"[NetworkAPI] Failure in message processing:\n{e.ToString()}");
			}
		}

		/// <summary>
		/// Registers a callback that will fire when the command string is sent
		/// </summary>
		/// <param name="command">The command that triggers the callback</param>
		/// <param name="callback">The function that runs when a command is recived</param>
		public void RegisterNetworkCommand(string command, Action<ulong, string, byte[], DateTime> callback)
		{
			if (command == null)
			{
				throw new Exception($"[NetworkAPI] Cannot register a command using null. null is reserved for chat messages.");
			}

			command = command.ToLower();

			if (NetworkCommands.ContainsKey(command))
			{
				throw new Exception($"[NetworkAPI] Failed to add the network command callback '{command}'. A command with the same name was already added.");
			}

			NetworkCommands.Add(command, callback);
		}

		/// <summary>
		/// Unregisters a command
		/// </summary>
		/// <param name="command"></param>
		public void UnregisterNetworkCommand(string command)
		{
			if (NetworkCommands.ContainsKey(command))
			{
				NetworkCommands.Remove(command);
			}
		}

		/// <summary>
		/// will trigger when you type <keyword> <command>
		/// </summary>
		/// <param name="command">this is the text command that will be typed into chat</param>
		/// <param name="callback">this is the function that will be called when the keyword is typed</param>
		public void RegisterChatCommand(string command, Action<string> callback)
		{
			if (command == null)
			{
				command = string.Empty;
			}

			command = command.ToLower();

			if (ChatCommands.ContainsKey(command))
			{
				throw new Exception($"[NetworkAPI] Failed to add the network command callback '{command}'. A command with the same name was already added.");
			}

			ChatCommands.Add(command, callback);
		}

		/// <summary>
		/// Unregisters a chat command
		/// </summary>
		/// <param name="command">the chat command to unregister</param>
		public void UnregisterChatCommand(string command)
		{
			if (ChatCommands.ContainsKey(command))
			{
				ChatCommands.Remove(command);
			}
		}

		/// <summary>
		/// Sends a command packet across the network
		/// </summary>
		/// <param name="commandString">The command word and any arguments delimidated with spaces</param>
		/// <param name="message">Text to be writen in chat</param>
		/// <param name="data">A serialized object used to send game information</param>
		/// <param name="sent">The date timestamp this command was sent</param>
		/// <param name="steamId">A players steam id</param>
		/// <param name="isReliable">Makes sure the data gets to the target</param>
		public abstract void SendCommand(string commandString, string message = null, byte[] data = null, DateTime? sent = null, ulong steamId = ulong.MinValue, bool isReliable = true);

		/// <summary>
		/// Sends a command packet across the network
		/// </summary>
		/// <param name="commandString">The command word and any arguments delimidated with spaces</param>
		/// <param name="point"></param>
		/// <param name="radius"></param>
		/// <param name="message">Text to be writen in chat</param>
		/// <param name="data">A serialized object used to send game information</param>
		/// <param name="sent">The date timestamp this command was sent</param>
		/// <param name="steamId">A players steam id</param>
		/// <param name="isReliable">Makes sure the data gets to the target</param>
		public abstract void SendCommand(string commandString, Vector3D point, double radius = 0, string message = null, byte[] data = null, DateTime? sent = null, ulong steamId = ulong.MinValue, bool isReliable = true);

		/// <summary>
		/// Sends a command packet to the server / client
		/// </summary>
		/// <param name="cmd">The object to be sent across the network</param>
		/// <param name="steamId">the id of the user this is being sent to. 0 sends it to all users in range</param>
		/// <param name="isReliable">make sure the packet reaches its destination</param>
		internal abstract void SendCommand(Command cmd, ulong steamId = ulong.MinValue, bool isReliable = true);


		/// <summary>
		/// Sends a command packet to the server / client if in range
		/// </summary>
		/// <param name="cmd">The object to be sent across the network</param>
		/// <param name="point">the center of the sending sphere</param>
		/// <param name="range">the radius of the sending sphere</param>
		/// <param name="steamId">the id of the user this is being sent to. 0 sends it to all users in range</param>
		/// <param name="isReliable">make sure the packet reaches its destination</param>
		internal abstract void SendCommand(Command cmd, Vector3D point, double range = 0, ulong steamId = ulong.MinValue, bool isReliable = true);

		/// <summary>
		/// Posts text into the ingame chat.
		/// </summary>
		/// <param name="message"></param>
		public abstract void Say(string message);

		/// <summary>
		/// Unregisters listeners
		/// </summary>
		[ObsoleteAttribute("This property is obsolete. Close is no longer required", false)]
		public void Close()
		{
			MyLog.Default.Info($"[NetworkAPI] Unregistering communication stream: {ComId}");
			if (UsingTextCommands)
			{
				MyAPIGateway.Utilities.MessageEntered -= HandleChatInput;
			}

			MyAPIGateway.Multiplayer.UnregisterMessageHandler(ComId, HandleIncomingPacket);

		}

		/// <summary>
		/// Calls Instance.Close()
		/// </summary>
		[ObsoleteAttribute("This property is obsolete. Dispose is no longer required", false)]
		public static void Dispose()
		{
			if (IsInitialized)
			{
				Instance.Close();
			}

			Instance = null;
		}

		/// <summary>
		/// Initializes the default instance of the NetworkAPI
		/// </summary>
		public static void Init(ushort comId, string modName, string keyword = null)
		{
			if (IsInitialized)
				return;

			if (!MyAPIGateway.Multiplayer.IsServer)
			{
				Instance = new Client(comId, modName, keyword);
			}
			else
			{
				Instance = new Server(comId, modName, keyword);
			}
		}

		/// <summary>
		/// Gets the diffrence between now and a given timestamp in milliseconds
		/// </summary>
		/// <returns></returns>
		public static float GetDeltaMilliseconds(long timestamp)
		{
			return (DateTime.UtcNow.Ticks - timestamp) / TimeSpan.TicksPerMillisecond;
		}

		/// <summary>
		/// Gets the diffrence between now and a given timestamp in frames (60 fps)
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>

		private static double frames = 1000d / 60d;
		public static int GetDeltaFrames(long timestamp)
		{
			return (int)Math.Ceiling(GetDeltaMilliseconds(timestamp) / frames);
		}
	}
}
