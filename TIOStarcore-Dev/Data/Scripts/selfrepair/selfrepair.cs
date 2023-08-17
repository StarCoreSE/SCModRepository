using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace ArtilleryBlockExplosion
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "Type18_Artillery_Block")]
	public class ArtilleryBlockExplosion : MyGameLogicComponent
	{
		private IMyConveyorSorter artilleryBlock;
		private int triggerTick = 0;
		private const int COUNTDOWN_SECONDS = 10 * 60; // 10 minutes in game time

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			if (!MyAPIGateway.Session.IsServer) return; // Only do explosions serverside
			artilleryBlock = Entity as IMyConveyorSorter;
			NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
		}

		public override void UpdateOnceBeforeFrame()
		{
			if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null) return;
			artilleryBlock.EnabledChanged += ArtilleryBlockEnabledChanged;
			NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
		}

		public override void UpdateAfterSimulation()
		{
			if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null) return;

			if (!artilleryBlock.Enabled)
			{
				triggerTick += 1;
				if (triggerTick >= COUNTDOWN_SECONDS)
				{
					DoExplosion();
					triggerTick = 0; // Restart the timer after explosion
				}
				else if (triggerTick % 60 == 0) // Show notification every second
				{
					int remainingSeconds = COUNTDOWN_SECONDS - triggerTick;
					int minutes = remainingSeconds / 60;
					int seconds = remainingSeconds % 60;
					string name = artilleryBlock.CustomName;
					string message = string.Format("Artillery Block repairs in {0} seconds", seconds);

					MyVisualScriptLogicProvider.ShowNotificationLocal(message, 1000, "Red");
				}
			}
			else
			{
				triggerTick = 0; // Reset countdown if enabled
			}
		}

		private void DoExplosion()
		{
			if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null) return;
			double radius = 30;
			BoundingSphereD sphere = new BoundingSphereD(artilleryBlock.WorldMatrix.Translation, radius);
			MyExplosionInfo explosion = new MyExplosionInfo(0f, -10000f, sphere, MyExplosionTypeEnum.CUSTOM, true);

			MyExplosions.AddExplosion(ref explosion);
		}

		private void ArtilleryBlockEnabledChanged(IMyTerminalBlock obj)
		{
			if (obj.EntityId != artilleryBlock.EntityId) return;
			if (artilleryBlock.Enabled)
			{
				triggerTick = 0; // Reset countdown if enabled
			}
		}

		public override void Close()
		{
			if (artilleryBlock != null)
			{
				artilleryBlock.EnabledChanged -= ArtilleryBlockEnabledChanged;
			}
		}
	}
}
