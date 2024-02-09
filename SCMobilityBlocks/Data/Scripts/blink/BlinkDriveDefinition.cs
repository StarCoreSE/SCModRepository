using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;

namespace BlinkDrive
{
	[ProtoContract]
	public class BlinkDriveDefinition
	{
		private static readonly Guid StorageGuid = new Guid("B7AF750E-0077-4826-BD0E-A75BF36BA3E5");

		[ProtoMember(1)]
		public bool AvoidEntity;

		[ProtoMember(2)]
		public bool AvoidGrid;

		[ProtoMember(3)]
		public bool AvoidPlanet;

		[ProtoMember(4)]
		public bool AvoidVoxel;

		[ProtoMember(5)]
		public float CurrentPowerCapacity;

		public static BlinkDriveDefinition CreateDefinition(BlinkDrive d)
		{
			return new BlinkDriveDefinition() {
				AvoidEntity = d.AvoidEntity.Value,
				AvoidGrid = d.AvoidGrid.Value,
				AvoidPlanet = d.AvoidPlanet.Value,
				AvoidVoxel = d.AvoidVoxel.Value,
				CurrentPowerCapacity = d.CurrentPowerCapacity.Value,
			};
		}

		public static BlinkDriveDefinition CreateDefaultDefinition()
		{
			return new BlinkDriveDefinition() {
				AvoidEntity = false,
				AvoidGrid = Core.Config.Value.AvoidGrids,
				AvoidPlanet = Core.Config.Value.AvoidPlanets,
				AvoidVoxel = Core.Config.Value.AvoidVoxels,
				CurrentPowerCapacity = 0,
			};
		}

		public static void Save(BlinkDrive d)
		{
			MyModStorageComponentBase storage = GetStorage(d.Entity);

			string def = MyAPIGateway.Utilities.SerializeToXML(CreateDefinition(d));
			if (storage.ContainsKey(StorageGuid))
			{
				storage[StorageGuid] = def;
			}
			else
			{
				storage.Add(StorageGuid, def);
			}
		}

		public static void Load(BlinkDrive d)
		{
			MyModStorageComponentBase storage = GetStorage(d.Entity);

			BlinkDriveDefinition def;
			if (storage.ContainsKey(StorageGuid))
			{
				def = MyAPIGateway.Utilities.SerializeFromXML<BlinkDriveDefinition>(storage[StorageGuid]);
			}
			else
			{
				MyLog.Default.Info($"[BlinkDrive] No data saved for: {d.Entity.EntityId}. Loading Defaults");
				def = CreateDefaultDefinition();
			}

			d.AvoidEntity.Value = def.AvoidEntity;
			d.AvoidGrid.Value = def.AvoidGrid;
			d.AvoidPlanet.Value = def.AvoidPlanet;
			d.AvoidVoxel.Value = def.AvoidVoxel;

			if (def.CurrentPowerCapacity > 1)
				def.CurrentPowerCapacity = 1f;

			d.CurrentPowerCapacity.Value = def.CurrentPowerCapacity;
		}

		public static MyModStorageComponentBase GetStorage(IMyEntity entity)
		{
			return entity.Storage ?? (entity.Storage = new MyModStorageComponent());
		}
	}
}
