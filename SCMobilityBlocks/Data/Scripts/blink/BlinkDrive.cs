using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SENetworkAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace BlinkDrive
{

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, "BlinkDriveLarge")]
	public class LargeBlinkDrive : BlinkDrive
	{
	}

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, "BlinkDriveSmall")]
	public class SmallBlinkDrive : BlinkDrive
	{
	}

	public class BlinkDrive : MyGameLogicComponent
	{
		private const float MinumumPowerRequirements = 0.01f;
		private const int VisualLifeTime = 12;

		public NetSync<bool> BlinkNextFrame;
		public NetSync<bool> AvoidEntity;
		public NetSync<bool> AvoidGrid;
		public NetSync<bool> AvoidVoxel;
		public NetSync<bool> AvoidPlanet;
		public NetSync<float> CurrentPowerCapacity;

		public bool IsLargeGrid { get; private set; }
		public float BlinkDistance { get; private set; }
		public float MaxPowerConsumptionRate { get; private set; }
		public float MaxPowerCapacity { get; private set; }
		public float CooldownBetweenBlinks { get; private set; }
		public float BlinkCount { get; private set; } = 1;

		public bool IsPowered => ResourceSink.SuppliedRatioByType(Electricity) == 1;//ResourceSink.IsPowerAvailable(Electricity, MinumumPowerRequirements);
		public bool IsBlinkEffectActive => TimeTillVisualsEnd > 0;
		public bool IsBlinkOnCooldown => TimeTillNextBlink > 0;
		public bool IsFullCharge => CurrentPowerCapacity.Value >= 1f;
		public float BlinkCost => (float)(MaxPowerCapacity / BlinkCount) / MaxPowerCapacity;

		private float currentPowerConsumptionRate;
		private float timeTillNextBlink;
		private float TimeTillNextBlink
		{
			get { return timeTillNextBlink; }
			set
			{
				timeTillNextBlink = value;
				if (timeTillNextBlink < 0)
				{
					timeTillNextBlink = 0;
				}
			}
		}

		private static readonly MyDefinitionId Electricity = MyResourceDistributorComponent.ElectricityId;

		public IMyCubeGrid Grid;
		public IMyTerminalBlock ModBlock;
		public MyCubeBlock CubeBlock;

		MySoundPair BlinkSoundEffect = new MySoundPair("BlinkDrive");
		private MyEntity3DSoundEmitter BlinkSoundEmitter;
		private MyResourceSinkComponent ResourceSink = new MyResourceSinkComponent();

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			ModBlock = Entity as IMyTerminalBlock;
			Grid = ModBlock.CubeGrid;
			CubeBlock = (MyCubeBlock)ModBlock;
			IsLargeGrid = Grid.GridSizeEnum == MyCubeSize.Large;

			ResourceSink = new MyResourceSinkComponent();
			MyResourceSinkInfo resourceInfo = new MyResourceSinkInfo() {
				ResourceTypeId = Electricity
			};

			ResourceSink.Init(MyStringHash.GetOrCompute("Doors"), MaxPowerConsumptionRate, PowerConsumptionFunc, CubeBlock);

			ResourceSink.SetRequiredInputByType(Electricity, 0);
			ResourceSink.SetMaxRequiredInputByType(Electricity, MaxPowerConsumptionRate);
			ResourceSink.SetRequiredInputFuncByType(Electricity, PowerConsumptionFunc);
			ResourceSink.Update();


			if (CubeBlock.Components.Contains(typeof(MyResourceSinkComponent)))
			{
				CubeBlock.Components.Remove<MyResourceSinkComponent>();
			}
			CubeBlock.Components.Add(ResourceSink);


			if (IsLargeGrid)
			{
				BlinkDistance = Core.Config.Value.LargeGrid_BlinkDistance;
				MaxPowerConsumptionRate = Core.Config.Value.LargeGrid_MaxPowerConsumptionRate;
				BlinkCount = Core.Config.Value.LargeGrid_BlinkCountAtFullCharge;
				MaxPowerCapacity = Core.Config.Value.LargeGrid_MaxPowerCapacity;
				CooldownBetweenBlinks = Core.Config.Value.LargeGrid_CooldownBetweenBlinks;
			}
			else
			{
				BlinkDistance = Core.Config.Value.SmallGrid_BlinkDistance;
				MaxPowerConsumptionRate = Core.Config.Value.SmallGrid_PowerConsumptionRate;
				BlinkCount = Core.Config.Value.SmallGrid_BlinkCountAtFullCharge;
				MaxPowerCapacity = Core.Config.Value.SmallGrid_MaxPowerCapacity;
				CooldownBetweenBlinks = Core.Config.Value.SmallGrid_CooldownBetweenBlinks;
			}

			BlinkNextFrame = new NetSync<bool>(this, TransferType.Both, false);
			AvoidEntity = new NetSync<bool>(this, TransferType.Both, false);
			AvoidGrid = new NetSync<bool>(this, TransferType.Both, Core.Config.Value.AvoidGrids);
			AvoidPlanet = new NetSync<bool>(this, TransferType.Both, Core.Config.Value.AvoidPlanets);
			AvoidVoxel = new NetSync<bool>(this, TransferType.Both, Core.Config.Value.AvoidVoxels);
			CurrentPowerCapacity = new NetSync<float>(this, TransferType.ServerToClient, 0);

			BlinkSoundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity, false, 1f);
			ModBlock.AppendingCustomInfo += CustomInfo;
			ModBlock.RefreshCustomInfo();


			BlinkDriveDefinition.Load(this);
			NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;
		}

		public override void Close()
		{
			StopBlinkParticleEffect();
			base.Close();
		}

		public override bool IsSerialized()
		{
			BlinkDriveDefinition.Save(this);
			return base.IsSerialized();
		}

		public override void UpdateBeforeSimulation()
		{
			UpdatePowerRequirements();
			UpdateBlockEmissives();
			UpdateBlinkParticleEffect();
			UpdateBlockAnimation();
			UpdateBlinkSequence();
			ModBlock.RefreshCustomInfo();
		}

		#region Blink Logic
		private bool CanBlink()
		{
			if (Grid.Physics == null)
			{
				DisplayNotification("Jump Failed: Grid has no physics", 500, "Red");
				return false;
			}

			if (Grid.IsStatic)
			{
				DisplayNotification("Cannot jump static grids", 500, "Red");
				return false;
			}

			if (TimeTillNextBlink > 0)
			{
				DisplayNotification("Wait to jump again", 500, "Red");
				return false;
			}

			if (CurrentPowerCapacity.Value - BlinkCost < 0)
			{
				DisplayNotification($"{ModBlock.CustomName} Charging {(CurrentPowerCapacity.Value/BlinkCost).ToString("p0")}", 500, "Red");
				return false;
			}

			Vector3D distance = ModBlock.WorldMatrix.Forward * BlinkDistance;
			Vector3D location = Grid.WorldAABB.Center + distance;

			List<IMyCubeGrid> grids = MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Physical);
			double childrenMass = 0;
			foreach (IMyCubeGrid grid in grids)
			{
				if (grid == Grid)
					continue;

				if (grid.Physics != null)
				{
					childrenMass += ((MyCubeGrid)grid).GetCurrentMass();
				}

				if (grid.IsStatic)
				{
					DisplayNotification("Cannot jump static grids", 1000, "Red");
					return false;
				}
			}


			if (Core.Config.Value.LimitSubgridJumpingByMass && ((MyCubeGrid)Grid).GetCurrentMass() < childrenMass)
			{
				DisplayNotification($"Cannot jump, grid mass: {((MyCubeGrid)Grid).GetCurrentMass().ToString("n0")} must be greater than subgrid mass: {childrenMass.ToString("n0")}", 1000, "Red");
				return false;
			}

			MatrixD matrix = Grid.WorldMatrix;
			matrix.Translation = location;
			BoundingBoxD gridBounds = (Grid as MyCubeGrid).GetPhysicalGroupAABB();
			BoundingBoxD bounds = new BoundingBoxD(location - gridBounds.HalfExtents, location + gridBounds.HalfExtents);
			BoundingBoxD boundsLocalized = new BoundingBoxD(-gridBounds.HalfExtents, gridBounds.HalfExtents);

			List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInAABB(ref bounds);

			if (entities.Count > 0 && AvoidEntity.Value)
			{
				DisplayNotification($"Entity obstruction cannot jump", 1000, "Red");
				return false;
			}
			else
			{
				foreach (IMyEntity entity in entities)
				{
					if (entity is IMyCubeBlock && AvoidGrid.Value)
					{
						DisplayNotification($"Grid obstruction cannot jump", 1000, "Red");
						return false;
					}
					else if (entity is MyPlanet && AvoidPlanet.Value)
					{
						MyTuple<float, float> voxelcheck = (entity as MyVoxelBase).GetVoxelContentInBoundingBox_Fast(boundsLocalized, matrix);
						if (!float.IsNaN(voxelcheck.Item2) && voxelcheck.Item2 > 0.1f)
						{
							DisplayNotification($"Terrain obstruction cannot jump: {(voxelcheck.Item2 * 100).ToString("n2")}%", 1000, "Red");
							return false;
						}
					}
					else if (entity is IMyVoxelMap && AvoidVoxel.Value)
					{
						MyTuple<float, float> voxelcheck = (entity as MyVoxelBase).GetVoxelContentInBoundingBox_Fast(boundsLocalized, matrix);
						if (!float.IsNaN(voxelcheck.Item2) && voxelcheck.Item2 > 0.1f)
						{
							DisplayNotification($"Terrain obstruction cannot jump: {(voxelcheck.Item2 * 100).ToString("n2")}%", 1000, "Red");
							return false;
						}
					}
				}
			}

			return true;
		}

		private void JumpAction(IMyTerminalBlock block)
		{
			BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

			if (drive == null || !drive.CanBlink())
				return;

			drive.BlinkNextFrame.Value = true;
		}

		private void UpdateBlinkSequence()
		{
			// this is the delay between jumps
			TimeTillNextBlink--;

			if (BlinkNextFrame.Value)
			{
				DoJump();
			}
		}

		private void DoJump()
		{
			BlinkNextFrame.SetValue(false, SyncType.None);

			float value = CurrentPowerCapacity.Value - BlinkCost;
			if (value < 0)
			{
				CurrentPowerCapacity.Value = 0;
			}
			else
			{
				CurrentPowerCapacity.Value = value;
			}

			BlinkSoundEmitter?.StopSound(true, true);
			BlinkSoundEmitter?.PlaySingleSound(BlinkSoundEffect, true);

			MatrixD GridJumpMatrix = Grid.WorldMatrix;
			Vector3D distance = ModBlock.WorldMatrix.Forward * BlinkDistance;
			GridJumpMatrix.Translation += distance;

			(Grid as MyEntity).Teleport(GridJumpMatrix);


			StartBlinkParticleEffect();
			TimeTillNextBlink = CooldownBetweenBlinks;
		}

		private bool IsValidPlacement()
		{
			if (Core.Config.Value.DrivesPerGrid > 0 && Grid.Physics == null)
			{
				List<IMySlimBlock> slims = new List<IMySlimBlock>();
				Grid.GetBlocks(slims, (s) => { return s.FatBlock != null && s.FatBlock.GameLogic.GetAs<BlinkDrive>() != null; });

				if (slims.Count > Core.Config.Value.DrivesPerGrid)
				{
					MyAPIGateway.Utilities.ShowNotification($"Only {Core.Config.Value.DrivesPerGrid} Blink Drives per grid", 3000, "Red");
					Grid.RemoveBlock(ModBlock.SlimBlock, true);
					return false;
				}
			}

			return true;
		}

		#endregion

		#region Power Management

		private void UpdatePowerRequirements()
		{
			ResourceSink.Update();

			if (IsPowered && ModBlock.IsWorking && !IsFullCharge)
			{
				float value = CurrentPowerCapacity.Value + (float)((float)Tools.MWToMWh(currentPowerConsumptionRate) / (float)MaxPowerCapacity);

				if (value > 1)
				{
					CurrentPowerCapacity.SetValue(1);
				}
				else
				{
					CurrentPowerCapacity.SetValue(value);
				}
			}
		}

		private float PowerConsumptionFunc()
		{
			if (!IsPowered || IsFullCharge)
			{
				return (currentPowerConsumptionRate = 0);
			}

			if (ModBlock.IsWorking)
			{

				return currentPowerConsumptionRate = MaxPowerConsumptionRate; // THIS IS A STAND IN WHILE KEEN FIXES THEIR BROKEN CODE.

				//currentPowerConsumptionRate = FindConsumptionRate(MaxPowerConsumptionRate, MaxPowerConsumptionRate);

				//return currentPowerConsumptionRate;
			}

			return (currentPowerConsumptionRate = 0);
		}

		private float FindConsumptionRate(float rate, float offset)
		{
			offset *= 0.5f;

			if (ResourceSink.IsPowerAvailable(Electricity, rate))
			{
				if (rate >= MaxPowerConsumptionRate)
				{
					return MaxPowerConsumptionRate;
				}

				if (offset <= 0.5f)
				{
					return rate;
				}

				return FindConsumptionRate(rate + offset, offset);
			}
			else
			{
				if (rate <= MinumumPowerRequirements)
				{
					return 0;
				}

				return FindConsumptionRate(rate - offset, offset);
			}
		}

		#endregion

		#region Visual Effects

		private MyParticleEffect BlinkTrailEffect;
		private float TimeTillVisualsEnd;
		Vector3D BlinkTrailAdjustmentVector;

		public MyEntitySubpart AnimationSubpart;
		private const float HingePosX = 0f; // Hinge position on the X axis. 0 is center.
		private const float HingePosY = 0f; // Hinge position on the Y axis. 0 is center.
		private const float HingePosZ = 0f; // Hinge position on the Z axis. 0 is center.
		private float RotX = 0f; // Rotation on the X axis. 0 is no rotation.
		private float RotY = 0f; // Rotation on the Y axis. 0 is no rotation.
		private float RotZ = 0f; // Rotation on the Z axis. 0 is no rotation.

		private float idleRotation = 0.1f;
		private float MaxRotation = 0.75f;
		private int StartFrames = 180;

		private static Matrix _rotMatrixX;
		private static Matrix _rotMatrixY;
		private static Matrix _rotMatrixZ;

		private void UpdateBlockAnimation()
		{
			float acceleration = (MaxRotation / StartFrames);
			if (ModBlock.IsWorking && IsPowered)
			{
				if (IsFullCharge)
				{
					AdjustBlockAnimationSpeed(acceleration, idleRotation, ref RotZ);
				}
				else
				{
					AdjustBlockAnimationSpeed(acceleration, MaxRotation, ref RotZ);
				}
			}
			else
			{
				AdjustBlockAnimationSpeed(acceleration, 0, ref RotZ);
			}

			_rotMatrixX = Matrix.CreateRotationX(RotX);
			_rotMatrixY = Matrix.CreateRotationY(RotY);
			_rotMatrixZ = Matrix.CreateRotationZ(RotZ);

			//MyAPIGateway.Utilities.ShowNotification($"Accel: {acceleration.ToString("N10")}, RotZ: {RotZ.ToString("N2")} Func: {ModBlock.IsFunctional} Physics: {ModBlock.CubeGrid.Physics != null} Working: {ModBlock.IsWorking}", 1);

			if (AnimationSubpart == null)
			{
				ModBlock.TryGetSubpart("core", out AnimationSubpart);
			}

			if (!ModBlock.IsFunctional || ModBlock.CubeGrid.Physics == null) // Ignore damaged or build progress blocks.
			{
				RotZ = 0;
				AnimationSubpart = null;
				return;
			}

			// Checks if subpart is removed (i.e. when changing block color).
			if (AnimationSubpart.Closed.Equals(true))
				AnimationSubpart.Subparts.Clear();

			if (AnimationSubpart != null)
			{
				var hingePos = new Vector3(HingePosX, HingePosY, HingePosZ); // This defines the location of a new pivot point.
				var MatrixTransl1 = Matrix.CreateTranslation(-hingePos);
				var MatrixTransl2 = Matrix.CreateTranslation(hingePos);
				var rotMatrix = AnimationSubpart.PositionComp.LocalMatrix;
				rotMatrix *= (MatrixTransl1 * _rotMatrixX * _rotMatrixY * _rotMatrixZ * MatrixTransl2);
				AnimationSubpart.PositionComp.LocalMatrix = rotMatrix;
			}
		}

		private void AdjustBlockAnimationSpeed(float accel, float cap, ref float current)
		{
			if (current > cap)
			{
				current -= accel;

				if (current < cap)
				{
					current = cap;
				}
			}
			else if (current < cap)
			{
				current += accel;

				if (current > cap)
				{
					current = cap;
				}
			}
		}

		private void StartBlinkParticleEffect()
		{
			if (MyAPIGateway.Utilities.IsDedicated)
				return;
			try
			{
				BlinkTrailEffect?.Stop();

				MatrixD gridMatrix = Grid.WorldMatrix;
				Vector3D direction = ModBlock.WorldMatrix.Forward;

				float gridWidth;
				float gridDepthOffset = 0.5f;
				if (direction == gridMatrix.Forward || direction == gridMatrix.Backward)
				{
					gridDepthOffset *= Grid.LocalAABB.Depth;
					gridWidth = Grid.LocalAABB.Width > Grid.LocalAABB.Height ? Grid.LocalAABB.Width : Grid.LocalAABB.Height;

				}
				else if (direction == gridMatrix.Left || direction == gridMatrix.Right)
				{
					gridDepthOffset *= Grid.LocalAABB.Width;
					gridWidth = Grid.LocalAABB.Depth > Grid.LocalAABB.Height ? Grid.LocalAABB.Depth : Grid.LocalAABB.Height;
				}
				else
				{
					gridDepthOffset *= Grid.LocalAABB.Height;
					gridWidth = Grid.LocalAABB.Depth > Grid.LocalAABB.Width ? Grid.LocalAABB.Depth : Grid.LocalAABB.Width;
				}

				float scale = gridWidth * 2;
				float particleHalfLength = 2.565f;
				//
				BlinkTrailAdjustmentVector = new Vector3D(0, 0, ((particleHalfLength * scale) + gridDepthOffset + Grid.GridSize));

				Vector3D origin = Grid.WorldAABB.Center;
				MatrixD rotationMatrix = MatrixD.CreateFromYawPitchRoll(MathHelper.ToRadians(0), MathHelper.ToRadians(-90), MathHelper.ToRadians(0));
				rotationMatrix.Translation = BlinkTrailAdjustmentVector;

				MatrixD matrix = ModBlock.WorldMatrix;
				matrix.Translation = origin;
				matrix = rotationMatrix * matrix;

				MyParticlesManager.TryCreateParticleEffect("BlinkDriveTrail", ref matrix, ref origin, uint.MaxValue, out BlinkTrailEffect);

				BlinkTrailEffect.UserScale = 0.01f;
				BlinkTrailEffect.UserEmitterScale = scale;

				if (Grid.Physics != null)
					BlinkTrailEffect.Velocity = Grid.Physics.LinearVelocity;

				TimeTillVisualsEnd = VisualLifeTime;
			}
			catch (Exception e)
			{
				MyLog.Default.Error(e.ToString());
			}
		}

		private void StopBlinkParticleEffect()
		{
			if (!MyAPIGateway.Utilities.IsDedicated)
			{
				BlinkTrailEffect?.Stop();
			}
		}

		private void UpdateBlinkParticleEffect()
		{
			if (!IsBlinkEffectActive)
				return;

			TimeTillVisualsEnd--;
			if (TimeTillVisualsEnd <= 0)
			{
				StopBlinkParticleEffect();
				TimeTillVisualsEnd = 0;
			}
		}

		private bool wasPoweredLastFrame = false;
		private bool emissivesBlink = false;
		private float nextRatio = 0;
		private int AnimateBlinkFrame = 0;
		private void UpdateBlockEmissives()
		{
			float powerFillRatio = CurrentPowerCapacity.Value;

			// keeps the code from firing 60 frames per second
			if (wasPoweredLastFrame != IsPowered || powerFillRatio >= nextRatio || AnimateBlinkFrame >= 30)
			{

				if (!(wasPoweredLastFrame = IsPowered))
				{
					ModBlock.SetEmissiveParts("Emissive0", Color.Red, float.MaxValue);
					ModBlock.SetEmissiveParts("Emissive1", Color.Red, float.MaxValue);
					ModBlock.SetEmissiveParts("Emissive2", Color.Red, float.MaxValue);
					ModBlock.SetEmissiveParts("Emissive3", Color.Red, float.MaxValue);
				}
				else if (IsFullCharge)
				{
					ModBlock.SetEmissiveParts("Emissive0", Color.Green, float.MaxValue);
					ModBlock.SetEmissiveParts("Emissive1", Color.Green, float.MaxValue);
					ModBlock.SetEmissiveParts("Emissive2", Color.Green, float.MaxValue);
					ModBlock.SetEmissiveParts("Emissive3", Color.Green, float.MaxValue);
					nextRatio = 2; // keeps the code from firing 60 frames per second
				}
				else
				{

					if (powerFillRatio >= 0.75)
					{
						ModBlock.SetEmissiveParts("Emissive0", Color.Green, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive1", Color.Green, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive2", Color.Green, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive3", (emissivesBlink) ? Color.Black : Color.Yellow, float.MaxValue);
						nextRatio = 1;
					}
					else if (powerFillRatio >= 0.5)
					{
						ModBlock.SetEmissiveParts("Emissive0", Color.Green, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive1", Color.Green, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive2", (emissivesBlink) ? Color.Black : Color.Yellow, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive3", Color.Black, float.MaxValue);
						nextRatio = 0.75f;

					}
					else if (powerFillRatio >= 0.25)
					{
						ModBlock.SetEmissiveParts("Emissive0", Color.Green, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive1", (emissivesBlink) ? Color.Black : Color.Yellow, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive2", Color.Black, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive3", Color.Black, float.MaxValue);
						nextRatio = 0.5f;
					}
					else if (powerFillRatio > 0)
					{
						ModBlock.SetEmissiveParts("Emissive0", (emissivesBlink) ? Color.Black : Color.Yellow, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive1", Color.Black, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive2", Color.Black, float.MaxValue);
						ModBlock.SetEmissiveParts("Emissive3", Color.Black, float.MaxValue);
						nextRatio = 0.25f;
					}
				}
			}

			if (AnimateBlinkFrame >= 30)
			{
				AnimateBlinkFrame = 0;
				emissivesBlink = !emissivesBlink;
			}

			AnimateBlinkFrame++;
		}

		private void InitializeEmissives()
		{
			ModBlock.SetEmissiveParts("Emissive0", Color.Black, float.MaxValue);
			ModBlock.SetEmissiveParts("Emissive1", Color.Black, float.MaxValue);
			ModBlock.SetEmissiveParts("Emissive2", Color.Black, float.MaxValue);
			ModBlock.SetEmissiveParts("Emissive3", Color.Black, float.MaxValue);
		}

		#endregion

		#region Terminal Controls

		private static bool TerminalInitialized = false;
		private bool WaitFrame = true;
		public override void UpdateOnceBeforeFrame()
		{
			if (WaitFrame)
			{
				NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
				WaitFrame = false;
				return;
			}

			if (!IsValidPlacement())
				return;

			if (TerminalInitialized)
				return;


			if (!MyAPIGateway.Utilities.IsDedicated)
			{
				InitializeEmissives();
				CreateTerminalControls();
			}

			CreateTerminalActions();

			TerminalInitialized = true;
		}

		public void CreateTerminalControls()
		{
			IMyTerminalControlCheckbox checkbox;
			IMyTerminalControlButton button;

			// Avoid Entities

			checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>("BlinkDrive_AvoidEntities");
			checkbox.SupportsMultipleBlocks = false;
			checkbox.Title = MyStringId.GetOrCompute("Avoid Entities");
			checkbox.Tooltip = MyStringId.GetOrCompute("Does not perform a jump if there are entities at the destination.");

			checkbox.Visible = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };
			checkbox.Enabled = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };

			checkbox.Getter = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					return drive.AvoidEntity.Value;
				}

				return false;
			};
			checkbox.Setter = (block, value) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					drive.AvoidEntity.Value = value;
					drive.UpdateControls();
				}
			};
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(checkbox);

			// Avoid Grids

			checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>("BlinkDrive_AvoidGrids");
			checkbox.SupportsMultipleBlocks = false;
			checkbox.Title = MyStringId.GetOrCompute("Avoid Grids");
			checkbox.Tooltip = MyStringId.GetOrCompute("Does not perform a jump if there are grids at the destination.");

			checkbox.Visible = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };
			checkbox.Enabled = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive == null)
					return false;

				if (Core.Config.Value.AvoidGrids)
				{
					drive.AvoidGrid.SetValue(true);
				}

				return !drive.AvoidEntity.Value && !Core.Config.Value.AvoidGrids;
			};

			checkbox.Getter = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					return drive.AvoidGrid.Value;
				}

				return false;
			};
			checkbox.Setter = (block, value) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					drive.AvoidGrid.Value = value;
				}
			};
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(checkbox);

			// Avoid Astroids

			checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>("BlinkDrive_AvoidAsteroids");
			checkbox.SupportsMultipleBlocks = false;
			checkbox.Title = MyStringId.GetOrCompute("Avoid Asteroids");
			checkbox.Tooltip = MyStringId.GetOrCompute("Does not perform a jump if there are asteroids at the destination.");

			checkbox.Visible = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };
			checkbox.Enabled = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive == null)
					return false;

				if (Core.Config.Value.AvoidVoxels)
				{
					drive.AvoidVoxel.Value = true;
				}

				return !drive.AvoidEntity.Value && !Core.Config.Value.AvoidVoxels;
			};

			checkbox.Getter = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					return drive.AvoidVoxel.Value;
				}

				return false;
			};
			checkbox.Setter = (block, value) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					drive.AvoidVoxel.Value = value;
				}
			};
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(checkbox);

			// Avoid Planets

			checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyUpgradeModule>("BlinkDrive_AvoidPlanets");
			checkbox.SupportsMultipleBlocks = false;
			checkbox.Title = MyStringId.GetOrCompute("Avoid Planets");
			checkbox.Tooltip = MyStringId.GetOrCompute("Does not perform a jump if there is a planet near by.");

			checkbox.Visible = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };
			checkbox.Enabled = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive == null)
					return false;

				if (Core.Config.Value.AvoidPlanets)
				{
					drive.AvoidPlanet.Value = true;
				}

				return !drive.AvoidEntity.Value && !Core.Config.Value.AvoidPlanets;
			};

			checkbox.Getter = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					return drive.AvoidPlanet.Value;
				}

				return false;
			};
			checkbox.Setter = (block, value) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();

				if (drive != null)
				{
					drive.AvoidPlanet.Value = value;
				}
			};
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(checkbox);

			// Jump

			button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>("BlinkDrive_Jump");
			button.Visible = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };
			button.Enabled = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };
			button.SupportsMultipleBlocks = false;
			button.Title = MyStringId.GetOrCompute("Jump");
			button.Tooltip = MyStringId.GetOrCompute("Initiates jump sequence");
			button.Action = JumpAction;
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(button);
		}

		private void CreateTerminalActions()
		{
			IMyTerminalAction button;

			// Avoid Entities

			button = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDrive_AvoidEntities");
			button.Enabled = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };
			button.Name = new StringBuilder("Avoid Entities");

			button.Writer = (block, text) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				text.Append((drive.AvoidEntity.Value) ? "On" : "Off");

			};
			button.Action = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				drive.AvoidEntity.Value = !drive.AvoidEntity.Value;
				drive.UpdateControls();
			};

			MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(button);

			// Avoid Grids

			button = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDrive_AvoidGrids");
			button.Name = new StringBuilder("Avoid Grids");

			button.Enabled = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };

			button.Writer = (block, text) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				if (drive.AvoidEntity.Value || Core.Config.Value.AvoidGrids)
				{
					text.Append("On");
				}
				else
				{
					text.Append((drive.AvoidGrid.Value) ? "On" : "Off");
				}
			};
			button.Action = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				drive.AvoidGrid.Value = !drive.AvoidGrid.Value;
			};

			MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(button);

			// Avoid Astroids

			button = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDrive_AvoidAsteroids");
			button.Name = new StringBuilder("Avoid Astroids");

			button.Enabled = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };

			button.Writer = (block, text) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				if (drive.AvoidEntity.Value || Core.Config.Value.AvoidVoxels)
				{
					text.Append("On");
				}
				else
				{
					text.Append((drive.AvoidVoxel.Value) ? "On" : "Off");
				}
			};
			button.Action = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				drive.AvoidVoxel.Value = !drive.AvoidVoxel.Value;
			};

			MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(button);

			// Avoid Planet

			button = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDrive_AvoidPlanets");
			button.Name = new StringBuilder("Avoid Planets");

			button.Enabled = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };

			button.Writer = (block, text) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				if (drive.AvoidEntity.Value || Core.Config.Value.AvoidPlanets)
				{
					text.Append("On");
				}
				else
				{
					text.Append((drive.AvoidPlanet.Value) ? "On" : "Off");
				}
			};
			button.Action = (block) => {
				BlinkDrive drive = block.GameLogic.GetAs<BlinkDrive>();
				if (drive == null)
					return;

				drive.AvoidPlanet.Value = !drive.AvoidPlanet.Value;
			};

			MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(button);

			// Jump

			button = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("BlinkDrive_Jump");
			button.Enabled = (block) => { return block.GameLogic.GetAs<BlinkDrive>() != null; };

			button.Name = new StringBuilder("Jump");

			button.Writer = (block, text) => { text.Append("Jump"); };
			button.Action = JumpAction;

			MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(button);
		}

		private void UpdateControls()
		{
			List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
			MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);

			foreach (IMyTerminalControl control in controls)
			{
				control.UpdateVisual();
			}
		}

		#endregion

		private void CustomInfo(IMyTerminalBlock block, StringBuilder info)
		{
			info.Clear();
			info.AppendLine($"Type: Blink Drive");
			info.AppendLine($"Max Capacity: {MaxPowerCapacity.ToString("n2")} MWh");
			info.AppendLine($"Max Consumption Rate: {MaxPowerConsumptionRate.ToString("n2")} MW");
			info.AppendLine($"\nStatus");
			info.AppendLine($"Current Capacity: {(CurrentPowerCapacity.Value * MaxPowerCapacity).ToString("n2")} MWh");
			info.AppendLine($"Current Consumption Rate: {currentPowerConsumptionRate.ToString("n2")} MW");

			info.AppendLine($"Blink Distance: {BlinkDistance.ToString("n0")}m");
			info.AppendLine($"Energy Cost: {(BlinkCost * MaxPowerCapacity).ToString("n2")}MWh");
		}

		private void DisplayNotification(string text, int lifetime, string color)
		{

			if (MyAPIGateway.Utilities.IsDedicated)
			{
				MyLog.Default.Info($"[BlinkDrive] {ModBlock.EntityId} msg: {text}");
				return;
			}

			IMyEntity entity = MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity;

			if (entity is IMyTerminalBlock && ((IMyTerminalBlock)entity).CubeGrid.EntityId == Grid.EntityId)
			{
				MyAPIGateway.Utilities.ShowNotification(text, lifetime, color);
			}
		}
	}
}
