
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;

namespace SENetworkAPI
{
	public enum TransferType { ServerToClient, ClientToServer, Both }
	public enum SyncType { Post, Fetch, Broadcast, None }

	[ProtoContract]
	internal class SyncData
	{
		[ProtoMember(1)]
		public long Id;
		[ProtoMember(2)]
		public long EntityId;
		[ProtoMember(3)]
		public byte[] Data;
		[ProtoMember(4)]
		public SyncType SyncType;
	}

	public abstract class NetSync
	{
		internal static Dictionary<MyEntity, List<NetSync>> PropertiesByEntity = new Dictionary<MyEntity, List<NetSync>>();
		internal static Dictionary<long, NetSync> PropertyById = new Dictionary<long, NetSync>();

		internal static object locker = new object();
		internal static long generatorId = 1;
		internal static long GeneratePropertyId()
		{
			return generatorId++;
		}

		/// <summary>
		/// The allowed network communication direction
		/// </summary>
		public TransferType TransferType { get; internal set; }

		/// <summary>
		/// The identity of this property
		/// </summary>
		public long Id { get; internal set; }

		/// <summary>
		/// Enables/Disables network traffic out when setting a value
		/// </summary>
		public bool SyncOnLoad { get; internal set; }

		/// <summary>
		/// Limits sync updates to within sync distance
		/// </summary>
		public bool LimitToSyncDistance { get; internal set; }

		/// <summary>
		/// the last recorded network traffic
		/// </summary>
		public long LastMessageTimestamp { get; internal set; }

		/// <summary>
		/// Request the lastest value from the server
		/// </summary>
		public abstract void Fetch();

		/// <summary>
		/// Triggers after recieving a fetch request from clients
		/// and allows you to modify this property before it is sent.
		/// </summary>
		public Action<ulong> BeforeFetchRequestResponse;

		internal abstract void Push(SyncType type, ulong sendTo);

		internal abstract void SetNetworkValue(byte[] data, ulong sender);
	}

	public class NetSync<T> : NetSync
	{
		/// <summary>
		/// Fires each time the value is changed
		/// Provides the old value and the new value
		/// </summary>
		public Action<T, T> ValueChanged;

		/// <summary>
		/// Fires only when the a network call is made
		/// Provides the old value and the new value
		/// also provides the steamId
		/// </summary>
		public Action<T, T, ulong> ValueChangedByNetwork;

		/// <summary>
		/// this property syncs across the network when changed
		/// </summary>
		public T Value
		{
			get { return _value; }
			set
			{
				SetValue(value, SyncType.Broadcast);
			}
		}

		private T _value;
		private MyEntity Entity;
		private string sessionName;

		/// <param name="entity">IMyEntity object this property is attached to</param>
		/// <param name="transferType"></param>
		/// <param name="startingValue">Sets an initial value</param>
		/// <param name="syncOnLoad">automatically syncs data to clients when the class initializes</param>
		/// <param name="limitToSyncDistance">marking this true only sends data to clients within sync distance</param>
		public NetSync(IMyEntity entity, TransferType transferType, T startingValue = default(T), bool syncOnLoad = true, bool limitToSyncDistance = true)
		{
			if (entity == null)
			{
				throw new Exception("[NetworkAPI] Attemped to create a NetSync property. MyEntity was null.");
			}

			Init(entity as MyEntity, transferType, startingValue, syncOnLoad, limitToSyncDistance);
		}

		/// <param name="entity">MyEntity object this property is attached to</param>
		/// <param name="transferType"></param>
		/// <param name="startingValue">Sets an initial value</param>
		/// <param name="syncOnLoad">automatically syncs data to clients when the class initializes</param>
		/// <param name="limitToSyncDistance">marking this true only sends data to clients within sync distance</param>
		public NetSync(MyEntity entity, TransferType transferType, T startingValue = default(T), bool syncOnLoad = true, bool limitToSyncDistance = true)
		{
			if (entity == null)
			{
				throw new Exception("[NetworkAPI] Attemped to create a NetSync property. MyEntity was null.");
			}

			Init(entity, transferType, startingValue, syncOnLoad, limitToSyncDistance);
		}

		/// <param name="logic">MyGameLogicComponent object this property is attached to</param>
		/// <param name="transferType"></param>
		/// <param name="startingValue">Sets an initial value</param>
		/// <param name="syncOnLoad">automatically syncs data to clients when the class initializes</param>
		/// <param name="limitToSyncDistance">marking this true only sends data to clients within sync distance</param>
		public NetSync(MyGameLogicComponent logic, TransferType transferType, T startingValue = default(T), bool syncOnLoad = true, bool limitToSyncDistance = true)
		{
			if (logic?.Entity == null)
			{
				throw new Exception("[NetworkAPI] Attemped to create a NetSync property. MyGameLogicComponent was null.");
			}

			Init(logic.Entity as MyEntity, transferType, startingValue, syncOnLoad, limitToSyncDistance);
		}

		/// <param name="logic">MySessionComponentBase object this property is attached to</param>
		/// <param name="transferType"></param>
		/// <param name="startingValue">Sets an initial value</param>
		/// <param name="syncOnLoad">automatically syncs data to clients when the class initializes</param>
		/// <param name="limitToSyncDistance">marking this true only sends data to clients within sync distance</param>
		public NetSync(MySessionComponentBase logic, TransferType transferType, T startingValue = default(T), bool syncOnLoad = true, bool limitToSyncDistance = true)
		{
			if (logic == null)
			{
				throw new Exception("[NetworkAPI] Attemped to create a NetSync property. MySessionComponentBase was null.");
			}

			sessionName = logic.GetType().Name;
			Init(null, transferType, startingValue, syncOnLoad, limitToSyncDistance);
		}

		/// <summary>
		/// This funtion is called by the constructer
		/// </summary>
		/// <param name="transferType"></param>
		/// <param name="startingValue">Sets an initial value</param>
		/// <param name="syncOnLoad">automatically syncs data to clients when the class initializes</param>
		/// <param name="limitToSyncDistance">marking this true only sends data to clients within sync distance</param>
		private void Init(MyEntity entity, TransferType transferType, T startingValue = default(T), bool syncOnLoad = true, bool limitToSyncDistance = true)
		{
			TransferType = transferType;
			_value = startingValue;
			SyncOnLoad = syncOnLoad;
			LimitToSyncDistance = limitToSyncDistance;

			if (entity != null)
			{
				Entity = entity;
				Entity.OnClose += Entity_OnClose;

				if (PropertiesByEntity.ContainsKey(Entity))
				{
					PropertiesByEntity[Entity].Add(this);
					Id = PropertiesByEntity[Entity].Count - 1;
				}
				else
				{
					PropertiesByEntity.Add(Entity, new List<NetSync> { this });
					Id = 0;
				}
			}
			else
			{
				lock (locker)
				{
					Id = GeneratePropertyId();
					PropertyById.Add(Id, this);
				}
			}

			if (SyncOnLoad)
			{
				if (Entity != null)
				{
					Entity.AddedToScene += SyncOnAddedToScene;
				}
				else
				{
					Fetch();
				}
			}

			if (NetworkAPI.LogNetworkTraffic)
			{
				MyLog.Default.Info($"[NetworkAPI] Property Created: {Descriptor()}, Transfer: {transferType}, SyncOnLoad: {SyncOnLoad}");
			}
		}

		private void SyncOnAddedToScene(MyEntity e) 
		{
			if (Entity != e)
				return;

			Fetch();			
			Entity.AddedToScene -= SyncOnAddedToScene;
		}

		private void Entity_OnClose(MyEntity entity)
		{
			PropertyById.Remove(Id);
		}

		/// <summary>
		/// Allows you to change how syncing works when setting the value this way
		/// </summary>
		public void SetValue(T val, SyncType syncType = SyncType.None)
		{
			T oldval = _value;
			lock (_value)
			{
				_value = val;
			}

			SendValue(syncType);
			ValueChanged?.Invoke(oldval, val);

		}

		/// <summary>
		/// Sets the data received over the network
		/// </summary>
		internal override void SetNetworkValue(byte[] data, ulong sender)
		{
			try
			{
				T oldval = _value;
				lock (_value)
				{
					_value = MyAPIGateway.Utilities.SerializeFromBinary<T>(data);

					if (NetworkAPI.LogNetworkTraffic)
					{
						MyLog.Default.Info($"[NetworkAPI] {Descriptor()} New value: {oldval} --- Old value: {_value}");
					}
				}

				if (MyAPIGateway.Multiplayer.IsServer)
				{
					SendValue();
				}

				ValueChanged?.Invoke(oldval, _value);
				ValueChangedByNetwork?.Invoke(oldval, _value, sender);
			}
			catch (Exception e)
			{
				MyLog.Default.Error($"[NetworkAPI] Failed to deserialize network property data\n{e}");
			}
		}

		/// <summary>
		/// sends the value across the network
		/// </summary>
		private void SendValue(SyncType syncType = SyncType.Broadcast, ulong sendTo = ulong.MinValue)
		{
			try
			{
				if (!NetworkAPI.IsInitialized)
				{
					MyLog.Default.Error($"[NetworkAPI] _ERROR_ The NetworkAPI has not been initialized. Use NetworkAPI.Init() to initialize it.");
					return;
				}

				if (syncType == SyncType.None)
				{
					if (NetworkAPI.LogNetworkTraffic)
					{
						MyLog.Default.Info($"[NetworkAPI] _INTERNAL_ {Descriptor()} Wont send value: {Value}");
					}

					return;
				}

				if (syncType != SyncType.Fetch && 
					(TransferType == TransferType.ServerToClient && !MyAPIGateway.Multiplayer.IsServer) ||
					(TransferType == TransferType.ClientToServer && MyAPIGateway.Multiplayer.IsServer))
				{
					if (NetworkAPI.LogNetworkTraffic)
					{
						MyLog.Default.Info($"[NetworkAPI] {Descriptor()} Bad send direction transfer type is {TransferType}");
					}

					return;
				}

				if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
					MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.PRIVATE)
				{
					if (NetworkAPI.LogNetworkTraffic)
					{
						MyLog.Default.Info($"[NetworkAPI] _OFFLINE_ {Descriptor()} Wont send value: {Value}");
					}

					return;
				}

				if (Value == null)
				{
					if (NetworkAPI.LogNetworkTraffic)
					{
						MyLog.Default.Error($"[NetworkAPI] _ERROR_ {Descriptor()} Value is null. Cannot transmit null value.");
					}

					return;
				}

				SyncData data = new SyncData() {
					Id = Id,
					EntityId = (Entity != null) ? Entity.EntityId : 0,
					Data = MyAPIGateway.Utilities.SerializeToBinary(_value),
					SyncType = syncType
				};

				ulong id = ulong.MinValue;
				if (MyAPIGateway.Session?.LocalHumanPlayer != null)
				{
					id = MyAPIGateway.Session.LocalHumanPlayer.SteamUserId;
				}

				if (id == sendTo && id != ulong.MinValue)
				{
					MyLog.Default.Error($"[NetworkAPI] _ERROR_ {Descriptor()} The sender id is the same as the recievers id. data will not be sent.");
				}

				if (NetworkAPI.LogNetworkTraffic)
				{
					MyLog.Default.Info($"[NetworkAPI] _TRANSMITTING_ {Descriptor()} - Id:{data.Id}, EId:{data.EntityId}, {data.SyncType}, {((data.SyncType == SyncType.Fetch) ? "" : $"Val:{_value}")}");
				}

				if (LimitToSyncDistance && Entity != null)
				{
					NetworkAPI.Instance.SendCommand(new Command() { IsProperty = true, Data = MyAPIGateway.Utilities.SerializeToBinary(data), SteamId = id }, Entity.PositionComp.GetPosition(), steamId: sendTo);
				}
				else
				{
					NetworkAPI.Instance.SendCommand(new Command() { IsProperty = true, Data = MyAPIGateway.Utilities.SerializeToBinary(data), SteamId = id }, steamId: sendTo);
				}
			}
			catch (Exception e)
			{
				MyLog.Default.Error($"[NetworkAPI] _ERROR_ SendValue(): Problem syncing value: {e}");
			}
		}

		/// <summary>
		/// Receives and redirects all property traffic
		/// </summary>
		/// <param name="pack">this hold the path to the property and the data to sync</param>
		internal static void RouteMessage(SyncData pack, ulong sender, long timestamp)
		{
			if (pack == null)
			{
				MyLog.Default.Error($"[NetworkAPI] Property data is null");
				return;
			}

			if (NetworkAPI.LogNetworkTraffic)
			{
				MyLog.Default.Info($"[NetworkAPI] Id:{pack.Id}, EId:{pack.EntityId}, {pack.SyncType}");
			}

			NetSync property;
			if (pack.EntityId == 0)
			{
				if (!PropertyById.ContainsKey(pack.Id))
				{
					MyLog.Default.Info($"[NetworkAPI] id not registered in dictionary 'PropertyById'");
					return;
				}

				property = PropertyById[pack.Id];
			}
			else
			{
				MyEntity entity = (MyEntity)MyAPIGateway.Entities.GetEntityById(pack.EntityId);

				if (entity == null)
				{
					MyLog.Default.Info($"[NetworkAPI] Failed to get entity by id");
					return;
				}

				if (!PropertiesByEntity.ContainsKey(entity))
				{
					MyLog.Default.Info($"[NetworkAPI] Entity not registered in dictionary 'PropertiesByEntity'");
					return;
				}

				List<NetSync> properties = PropertiesByEntity[entity];

				if (pack.Id >= properties.Count)
				{
					MyLog.Default.Info($"[NetworkAPI] property index out of range");
					return;
				}

				property = properties[(int)pack.Id];
			}

			property.LastMessageTimestamp = timestamp;
			if (pack.SyncType == SyncType.Fetch)
			{
				property.BeforeFetchRequestResponse?.Invoke(sender);
				property.Push(SyncType.Post, sender);
			}
			else
			{
				property.SetNetworkValue(pack.Data, sender);
			}
		}

		/// <summary>
		/// Request the lastest value from the server
		/// Servers are not allowed to fetch from clients
		/// </summary>
		public override void Fetch()
		{
			if (!MyAPIGateway.Multiplayer.IsServer)
			{
				SendValue(SyncType.Fetch);
			}
		}

		/// <summary>
		/// Send data now
		/// </summary>
		public void Push()
		{
			SendValue();
		}

		/// <summary>
		/// Send data to single user
		/// </summary>
		public void Push(ulong sendTo)
		{
			SendValue(SyncType.Post, sendTo);
		}

		/// <summary>
		/// Send data across the network now
		/// </summary>
		internal override void Push(SyncType type, ulong sendTo = ulong.MinValue)
		{
			SendValue(type, sendTo);
		}

		/// <summary>
		/// Identifier for logging readability
		/// </summary>
		internal string Descriptor()
		{
			if (Entity != null)
			{
				if (Entity is MyCubeBlock)
				{
					return $"<{(Entity as MyCubeBlock).CubeGrid.DisplayName}_{((Entity.DefinitionId?.SubtypeId == null) ? Entity.GetType().Name.ToString() : Entity.DefinitionId?.SubtypeId.ToString())}.{Entity.EntityId}_{typeof(T).Name}.{Id}>";
				}

				return $"<{((Entity.DefinitionId?.SubtypeId == null) ? Entity.GetType().Name.ToString() : Entity.DefinitionId?.SubtypeId.ToString())}.{Entity.EntityId}_{typeof(T).Name}.{Id}>";
			}

			return $"<{sessionName}_{typeof(T).Name}.{Id}>";
		}
	}
}
