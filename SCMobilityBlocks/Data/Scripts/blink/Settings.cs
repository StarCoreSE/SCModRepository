using System;
using System.IO;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Utils;

namespace BlinkDrive
{
	[ProtoContract]
	public class Settings
	{
		public const string Filename = "BlinkDriveConfig.cfg";

		[ProtoMember(1)]
		public int Version;

		[ProtoMember(2)]
		public float LargeGrid_MaxPowerCapacity;

		[ProtoMember(3)]
		public int LargeGrid_BlinkCountAtFullCharge;

		[ProtoMember(4)]
		public float LargeGrid_MaxPowerConsumptionRate;

		[ProtoMember(5)]
		public float LargeGrid_BlinkDistance;

		[XmlIgnore]
		public int LargeGrid_CooldownBetweenBlinks; // needs several frames between jumps to let physics settle 


		[ProtoMember(10)]
		public float SmallGrid_MaxPowerCapacity;

		[ProtoMember(11)]
		public int SmallGrid_BlinkCountAtFullCharge;

		[ProtoMember(12)]
		public float SmallGrid_PowerConsumptionRate;

		[ProtoMember(13)]
		public float SmallGrid_BlinkDistance;

		[XmlIgnore]
		public int SmallGrid_CooldownBetweenBlinks;

		[ProtoMember(20)]
		public bool AvoidEntity;

		[ProtoMember(21)]
		public bool AvoidGrids;

		[ProtoMember(22)]
		public bool AvoidVoxels;

		[ProtoMember(23)]
		public bool AvoidPlanets;

		[ProtoMember(24)]
		public int DrivesPerGrid;

		[ProtoMember(25)]
		public bool LimitSubgridJumpingByMass;

		public static Settings GetDefaults()
		{
			return new Settings {
				Version = 2,
				LargeGrid_MaxPowerCapacity = 30f, // MWh
				LargeGrid_BlinkCountAtFullCharge = 3,
				LargeGrid_MaxPowerConsumptionRate = 300f, // MW/s
				LargeGrid_BlinkDistance = 1000,
				LargeGrid_CooldownBetweenBlinks = 10, // frames

				SmallGrid_MaxPowerCapacity = 3f, // MWh
				SmallGrid_BlinkCountAtFullCharge = 3,
				SmallGrid_PowerConsumptionRate = 30f, // MW/s
				SmallGrid_BlinkDistance = 500,
				SmallGrid_CooldownBetweenBlinks = 10, // frames

				AvoidEntity = false,
				AvoidGrids = false,
				AvoidPlanets = false,
				AvoidVoxels = false,
				DrivesPerGrid = 0,
				LimitSubgridJumpingByMass = false,
			};
		}

		public static Settings Load()
		{
			Settings defaults = GetDefaults();
			Settings settings = defaults;
			try
			{
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage(Filename, typeof(Settings)))
				{
					MyLog.Default.Info("[BlinkDrive] Loading saved settings");
					TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(Filename, typeof(Settings));
					string text = reader.ReadToEnd();
					reader.Close();

					settings = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);

					if (settings.Version != defaults.Version)
					{
						MyLog.Default.Info($"[BlinkDrive] Old version updating config {settings.Version}->{GetDefaults().Version}");
						settings = GetDefaults();
						Save(settings);
					}
				}
				else
				{
					MyLog.Default.Info("[BlinkDrive] Config file not found. Loading default settings");
					Save(settings);
				}
			}
			catch (Exception e)
			{
				MyLog.Default.Info($"[BlinkDrive] Failed to load saved configuration. Loading defaults\n {e.ToString()}");
				Save(settings);
			}

			return settings;
		}

		public static void Save(Settings settings)
		{
			try
			{
				MyLog.Default.Info($"[BlinkDrive] Saving Settings");
				TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(Filename, typeof(Settings));
				writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
				writer.Close();
			}
			catch (Exception e)
			{
				MyLog.Default.Info($"[BlinkDrive] Failed to save settings\n{e.ToString()}");
			}
		}
	}
}
