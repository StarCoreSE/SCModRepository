using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SENetworkAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using static VRageMath.Base6Directions;
using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;

namespace RelativeTopSpeedGV
{
	[MySessionComponentDescriptor(MyUpdateOrder.Simulation, 999)]
	public class RelativeTopSpeed : MySessionComponentBase
	{
		private const ushort ComId = 16341;
		private const string ModName = "Relative Top Speed";
		private const string CommandKeyword = "/rts";

		public NetSync<Settings> cfg;
		public static event Action<Settings> SettingsChanged;

		ConcurrentDictionary<long, Vector3> AccelForces = new ConcurrentDictionary<long, Vector3>();

		private bool showHud = false;

		private byte waitInterval = 0;
		private List<MyCubeGrid> ActiveGrids = new List<MyCubeGrid>();
		private List<MyCubeGrid> PassiveGrids = new List<MyCubeGrid>();
		private List<MyCubeGrid> DisabledGrids = new List<MyCubeGrid>();

		private MyObjectBuilderType thrustTypeId = null;
		private MyObjectBuilderType cockpitTypeId = null;

		private NetworkAPI Network => NetworkAPI.Instance;

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			thrustTypeId = MyObjectBuilderType.ParseBackwardsCompatible("Thrust");
			cockpitTypeId = MyObjectBuilderType.ParseBackwardsCompatible("Cockpit");

			NetworkAPI.LogNetworkTraffic = false;

			if (!NetworkAPI.IsInitialized)
			{
				NetworkAPI.Init(ComId, ModName, CommandKeyword);
			}

			if (!RtsApiBackend.IsInitialized)
			{
				RtsApiBackend.Init(this);
			}

			cfg = new NetSync<Settings>(this, TransferType.ServerToClient, Settings.Load(), true, false);
			cfg.ValueChangedByNetwork += SettingChanged;
			Settings.Instance = cfg.Value;

			Network.RegisterChatCommand(string.Empty, Chat_Help);
			Network.RegisterChatCommand("help", Chat_Help);
			Network.RegisterChatCommand("hud", Chat_Hud);
			Network.RegisterChatCommand("config", Chat_Config);

			if (!MyAPIGateway.Multiplayer.IsServer)
			{
				Network.RegisterChatCommand("load", (args) => { Network.SendCommand("load"); });
			}
			else
			{
				Network.RegisterNetworkCommand("load", ServerCallback_Load);
				Network.RegisterChatCommand("load", (args) => { cfg.Value = Settings.Load(); });
			}

			MyLog.Default.Info("[RelativeTopSpeed] Starting.");
			MyAPIGateway.Entities.OnEntityAdd += AddGrid;
			MyAPIGateway.Entities.OnEntityRemove += RemoveGrid;
		}

		private void SettingChanged(Settings o, Settings n, ulong sender)
		{
			MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed = n.SpeedLimit;
			MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed = n.SpeedLimit;
			n.CalculateCurve();
			Settings.Instance = n;
			SettingsChanged?.Invoke(n);
		}

		protected override void UnloadData()
		{
			MyAPIGateway.Entities.OnEntityAdd -= AddGrid;
			MyAPIGateway.Entities.OnEntityRemove -= RemoveGrid;

			RtsApiBackend.Close();
		}

		private void AddGrid(IMyEntity ent)
		{
			MyCubeGrid grid = ent as MyCubeGrid;
			if (grid == null || grid.Physics == null)
				return;
            
			//Ignoring suspension wheels and debris
			if (grid.BlocksCount <= 2)
            {
                /*foreach (var block in grid.GetBlocks())
                {
                    if (block.BlockDefinition.Id.SubtypeName.Contains("RealWheel"))
                    {
                        return;
                    }
                }*/
				return;
            }

			RegisterOrUpdateGridStatus(grid, grid.IsStatic);
			grid.OnStaticChanged += RegisterOrUpdateGridStatus;
		}

		private void RemoveGrid(IMyEntity ent)
		{
			MyCubeGrid grid = ent as MyCubeGrid;
			if (grid == null || grid.Physics == null)
				return;

			grid.OnStaticChanged -= RegisterOrUpdateGridStatus;
			ActiveGrids.Remove(grid);
			PassiveGrids.Remove(grid);
			DisabledGrids.Remove(grid);
		}

        private bool IsMoving(IMyEntity ent)
        {
			var gridIsMoving = false;
			if (ent.Physics.LinearVelocity.LengthSquared() > 1.0f || ent.Physics.AngularVelocity.LengthSquared() > 0.01f)
			{
				gridIsMoving = true;
			}
            return gridIsMoving;
        }
		
		private bool HasActivationBlock(MyCubeGrid grid)
		{
			bool subHasThrust = false;
			bool subHasCockpit = false;
			bool subHasActivationBlocks = false;

			List<IMyCubeGrid> subs = MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical);
			foreach (MyCubeGrid sub in subs)
			{
				if (sub.BlocksCounters.ContainsKey(thrustTypeId) && sub.BlocksCounters[thrustTypeId] > 0)
				{
					subHasThrust = true;
				}

				if (sub.BlocksCounters.ContainsKey(cockpitTypeId) && sub.BlocksCounters[cockpitTypeId] > 0)
				{
					subHasCockpit = true;
				}

				if (cfg.Value.IgnoreGridsWithoutThrust && !subHasThrust ||
					cfg.Value.IgnoreGridsWithoutCockpit && !subHasCockpit)
				{
					continue;
				}

				subHasActivationBlocks = true;
			}

			return subHasActivationBlocks;
		}

		private void RegisterOrUpdateGridStatus(MyCubeGrid grid, bool isStatic)
		{
			if (isStatic)
			{
				if (!DisabledGrids.Contains(grid))
				{
					DisabledGrids.Add(grid);
				}

				PassiveGrids.Remove(grid);
				ActiveGrids.Remove(grid);
			}
			else if (IsMoving(grid) &&
				!(cfg.Value.IgnoreGridsWithoutThrust && grid.BlocksCounters.ContainsKey(thrustTypeId) && grid.BlocksCounters[thrustTypeId] == 0) &&
				!(cfg.Value.IgnoreGridsWithoutCockpit && grid.BlocksCounters.ContainsKey(cockpitTypeId) && grid.BlocksCounters[cockpitTypeId] == 0))
			{
				if (!ActiveGrids.Contains(grid))
				{
					ActiveGrids.Add(grid);
				}

				PassiveGrids.Remove(grid);
				DisabledGrids.Remove(grid);
			}
			else
			{
				if (!PassiveGrids.Contains(grid))
				{
					PassiveGrids.Add(grid);
				}

				ActiveGrids.Remove(grid);
				DisabledGrids.Remove(grid);
			}
		}

		public override void Simulate()
		{
			// update active / passive grids every 3 seconds
			if (waitInterval == 0)
			{
				for (int i = 0; i < PassiveGrids.Count; i++)
				{
					MyCubeGrid grid = PassiveGrids[i];

					if (!HasActivationBlock(grid))
					{
						continue;
					}

					if (IsMoving(grid))
					{
						if (!ActiveGrids.Contains(grid))
						{
							ActiveGrids.Add(grid);
						}

						PassiveGrids.Remove(grid);
						i--;
					}
				}

				for (int i = 0; i < ActiveGrids.Count; i++)
				{
					MyCubeGrid grid = ActiveGrids[i];
					if (!IsMoving(grid) || !HasActivationBlock(grid))
					{
						if (!PassiveGrids.Contains(grid))
						{
							PassiveGrids.Add(grid);
						}

						ActiveGrids.Remove(grid);
						i--;
					}
				}

				foreach (long key in AccelForces.Keys)
				{
					try
					{
						Vector3 value;
						AccelForces.TryGetValue(key, out value);

						if (value == Vector3.Zero)
						{
							AccelForces.TryRemove(key, out value);
						}
					}
					catch { }
				}

				waitInterval = 60; // reset, was 180
			}

			for (int i = 0; i < ActiveGrids.Count; i++)
			{
				UpdateGrid(i);
			}

			if (!MyAPIGateway.Utilities.IsDedicated)
			{
				if (showHud)
				{
					IMyControllableEntity controlledEntity = MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity;
					if (controlledEntity != null && controlledEntity is IMyCubeBlock && (controlledEntity as IMyCubeBlock).CubeGrid.Physics != null)
					{
						IMyCubeGrid grid = (controlledEntity as IMyCubeBlock).CubeGrid;

						float mass = grid.Physics.Mass;
						float speed = grid.Physics.Speed;
						float cruiseSpeed = GetCruiseSpeed(mass, grid.GridSizeEnum == MyCubeSize.Large);

						float boost = GetBoost(grid)[3];
						float resistance = (grid.GridSizeEnum == MyCubeSize.Large) ? cfg.Value.LargeGrid_ResistanceMultiplier : cfg.Value.SmallGrid_ResistanceMultiplyer;

						MyAPIGateway.Utilities.ShowNotification($"Mass: {mass.ToString("n0")}   Cruise: {cruiseSpeed.ToString("n2")}   Max Boost: {(boost).ToString("n2")}", 1);
					}
				}

				if (Settings.Debug && IsAllowedSpecialOperations(MyAPIGateway.Session.LocalHumanPlayer.SteamUserId))
				{
					MyAPIGateway.Utilities.ShowNotification($"Grids - Active: {ActiveGrids.Count}  Passive: {PassiveGrids.Count}  Disabled: {DisabledGrids.Count}", 1);
				}
			}

			waitInterval--;
		}


		private void UpdateGrid(int index)
		{

			MyCubeGrid grid = ActiveGrids[index];

			float speed = Math.Abs(grid.Physics.Speed);
			bool isLargeGrid = grid.GridSizeEnum == MyCubeSize.Large;
			float minSpeed = (isLargeGrid) ? cfg.Value.LargeGrid_MinCruise : cfg.Value.SmallGrid_MinCruise;
			float mass = grid.Physics.Mass;
			float cruiseSpeed = GetCruiseSpeed(mass, isLargeGrid);
			float maxBoost = GetBoostSpeed(mass, isLargeGrid);

			if (speed > minSpeed)
			{
				if (cfg.Value.EnableBoosting)
				{
					if (speed >= cruiseSpeed)
					{
						float resistance = (isLargeGrid) ? cfg.Value.LargeGrid_ResistanceMultiplier : cfg.Value.SmallGrid_ResistanceMultiplyer;

						float resistantForce = resistance * mass * (1 - (cruiseSpeed / speed));

						Vector3 velocity = grid.Physics.LinearVelocity * -resistantForce;
						grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, velocity, grid.Physics.CenterOfMassWorld, null, maxBoost);
					}
				}
				else
				{
					Vector3 inverseAccelerationForce = Vector3.Zero;

					if (!MyUtils.IsZero(grid.Physics.LinearAcceleration))
					{
						Vector3 addjustedAccel = (grid.Physics.LinearAcceleration * 0.01666666666666666666f);

						if (Math.Abs(addjustedAccel.LengthSquared() - grid.Physics.LinearVelocity.LengthSquared()) < 1)
						{
							return;
						}

						Vector3 lastAccelForce = AccelForces.GetOrAdd(grid.EntityId, Vector3.Zero);

						Vector3 velocityAfterAccel = (grid.Physics.LinearVelocity + lastAccelForce + addjustedAccel);
						float speedAfterAccel = velocityAfterAccel.Length();

						if (speedAfterAccel > cruiseSpeed)
						{
							AccelForces.TryUpdate(grid.EntityId, velocityAfterAccel * (1 - (cruiseSpeed / speedAfterAccel)), lastAccelForce);
						}
						else
						{
							AccelForces.TryUpdate(grid.EntityId, Vector3.Zero, lastAccelForce);
						}
					}

					inverseAccelerationForce = -AccelForces.GetOrAdd(grid.EntityId, Vector3.Zero) * mass;

					if (!MyUtils.IsZero(inverseAccelerationForce))
					{
						grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, inverseAccelerationForce, grid.Physics.CenterOfMassWorld, null);
					}
				}
			}
			
			if (cfg.Value.EnableAngularLimits)
			{
				Vector3 ang = grid.Physics.AngularVelocity;
				
				if (ang.LengthSquared() > (cfg.Value.GlobalMinAngularSpeed * cfg.Value.GlobalMinAngularSpeed))
				{
					float maxAngular = cruiseSpeed * ((isLargeGrid) ? cfg.Value.LargeGrid_AngularMassMult : cfg.Value.SmallGrid_AngularMassMult);
					var angSpeedReduction = MathHelper.Lerp(1, isLargeGrid ? cfg.Value.LargeGrid_AngularCruiseMult : cfg.Value.SmallGrid_AngularCruiseMult, MathHelper.Clamp(speed / cruiseSpeed, 0, 1)); 
					float reducedAng = maxAngular * angSpeedReduction; // at 0 m/s, reduction is 1x, as speed increases, it approaches 0.25x (AngularCruseMult)
					Vector3 inverseAng = Vector3.Zero;
					if (ang.Length() > reducedAng)
					{
						ang = Vector3.Normalize(ang) * reducedAng;
						grid.Physics.SetSpeeds(grid.Physics.LinearVelocity, ang);
						//inverseAng = 0.5f * grid.Physics.Mass * grid.Physics.AngularAcceleration * (float)(grid.GetPhysicalGroupAABB().Extents.Length() / 2 * grid.GridSize);
						//grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, inverseAng, null, true, false);
					}
				}
			}
		}

		public float[] GetAcceleration(IMyCubeGrid grid)
		{
			float[] accels = GetAccelerationsByDirection(grid);

			float min = float.MaxValue;
			float average = 0;
			float max = 0;

			for (int i = 0; i < 6; i++)
			{
				average += accels[i];

				if (accels[i] < min)
				{
					min = accels[i];
				}
			}

			average /= 6;

			if (accels[0] > accels[1])
			{
				max += accels[0];
			}
			else
			{
				max += accels[1];
			}

			if (accels[2] > accels[3])
			{
				max += accels[2];
			}
			else
			{
				max += accels[3];
			}

			if (accels[4] > accels[5])
			{
				max += accels[4];
			}
			else
			{
				max += accels[5];
			}

			return new float[] { accels[1], min, average, max };

		}

		public float[] GetBoost(IMyCubeGrid grid)
		{
			float[] accels = GetAcceleration(grid);
			float resistance = (grid.GridSizeEnum == MyCubeSize.Large) ? cfg.Value.LargeGrid_ResistanceMultiplier : cfg.Value.SmallGrid_ResistanceMultiplyer;

			accels[0] /= resistance;
			accels[1] /= resistance;
			accels[2] /= resistance;
			accels[3] /= resistance;

			return accels;
		}

		public float[] GetAccelerationsByDirection(IMyCubeGrid grid)
		{
			if (grid == null || grid.Physics == null)
				return new float[6];

			float mass = ((MyCubeGrid)grid).GetCurrentMass();

			float[] accelerations = new float[6];

			foreach (IMySlimBlock slim in (grid as MyCubeGrid).CubeBlocks)
			{
				if (!(slim.FatBlock is IMyThrust))
					continue;

				IMyThrust thruster = slim.FatBlock as IMyThrust;

				Direction direction = GetDirection(thruster.GridThrustDirection);

				accelerations[(int)direction] += thruster.MaxThrust;
			}

			// convert from force to accleration (m = f/a)
			for (int i = 0; i < 6; i++)
			{
				accelerations[i] /= mass;
			}

			return accelerations;
		}

		public float GetCruiseSpeed(IMyCubeGrid grid)
		{
			if (grid != null && grid.Physics != null)
			{
				return GetCruiseSpeed(((MyCubeGrid)grid).GetCurrentMass(), grid.GridSizeEnum == MyCubeSize.Large);
			}

			return 0;
		}

		public float GetMaxSpeed(IMyCubeGrid grid)
		{
			float speed = GetCruiseSpeed(grid);

			if (cfg.Value.EnableBoosting)
			{
				speed += GetBoost(grid)[3];
			}

			if (speed > cfg.Value.SpeedLimit)
			{
				speed = cfg.Value.SpeedLimit;
			}

			return speed;
		}

		public float GetCruiseSpeed(float mass, bool isLargeGrid) => cfg.Value.GetCruiseSpeed(mass, isLargeGrid);

		public float GetBoostSpeed(float mass, bool isLargeGrid) => cfg.Value.GetBoostSpeed(mass, isLargeGrid);

		#region Communications

		private void Chat_Help(string arguments)
		{
			MyAPIGateway.Utilities.ShowMessage(Network.ModName, "Relative Top Speed\nHUD: displays ship stats when in cockpit\nCONFIG: Displays the current config\nLOAD: load world configuration");
		}

		private void Chat_Hud(string arguments)
		{
			showHud = !showHud;
			MyAPIGateway.Utilities.ShowMessage(ModName, $"Hud display is {(showHud ? "ON" : "OFF")}");
		}

		private void Chat_Config(string arguments)
		{
			if (!MyAPIGateway.Utilities.IsDedicated)
			{
				MyAPIGateway.Utilities.ShowMissionScreen("Relative Top Speed", "Configuration", null, cfg.Value.ToString());
			}
		}

		private void ServerCallback_Load(ulong steamId, string commandString, byte[] data, DateTime timestamp)
		{
			if (IsAllowedSpecialOperations(steamId))
			{
				cfg.Value = Settings.Load();
			}
			else
			{
				Network.SendCommand(null, "Load command requires Admin status.", steamId: steamId);
			}
		}

		public static bool IsAllowedSpecialOperations(ulong steamId)
		{
			if (MyAPIGateway.Multiplayer.IsServer)
				return true;
			return IsAllowedSpecialOperations(MyAPIGateway.Session.GetUserPromoteLevel(steamId));
		}

		public static bool IsAllowedSpecialOperations(MyPromoteLevel level)
		{
			return level == MyPromoteLevel.SpaceMaster || level == MyPromoteLevel.Admin || level == MyPromoteLevel.Owner;
		}

		#endregion
	}
}
