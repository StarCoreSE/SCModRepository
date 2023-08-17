using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace ArtilleryBlockExplosion
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "Type18_Artillery_Block", "Type21_Artillery_Block", "Type24_Artillery_Block", "Type77_Railgun_Block", "Type78_Railgun_Block", "Type79_Railgun_Block", "Reaver_Coilgun_Block", "Torp_Block")]
	public class ArtilleryBlockExplosion : MyGameLogicComponent
	{
		private IMyConveyorSorter artilleryBlock;
		private int triggerTick = 0;
		private const int COUNTDOWN_TICKS = 10 * 60; // (60 ticks per second)

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

			IMySlimBlock slimBlock = artilleryBlock.SlimBlock;
			if (slimBlock == null) return;

			if (!artilleryBlock.Enabled)
			{
				if (slimBlock.Integrity < slimBlock.MaxIntegrity)
				{
					triggerTick += 1;
					if (triggerTick >= COUNTDOWN_TICKS)
					{
						DoRepair();
						triggerTick = 0; // Restart the timer after repair
					}
					else if (triggerTick % 60 == 0) // Show notification every second
					{
						int remainingSeconds = (COUNTDOWN_TICKS - triggerTick) / 60;
						string name = artilleryBlock.CustomName;
						string message = string.Format("Artillery Block ({0}) repairs in {1} seconds", name, remainingSeconds);

						MyVisualScriptLogicProvider.ShowNotificationLocal(message, 1000, "Red");
					}
				}
				else
				{
					triggerTick = 0; // Reset countdown if at full integrity
				}
			}
			else
			{
				triggerTick = 0; // Reset countdown if enabled
			}
		}

		private void DoRepair()
		{
			if (artilleryBlock == null || artilleryBlock.CubeGrid.Physics == null) return;

			IMySlimBlock slimBlock = artilleryBlock.SlimBlock;
			if (slimBlock == null) return;


			float repairAmount = 20; // what the fuck is this number keen??



			slimBlock.IncreaseMountLevel(repairAmount, 0L, null, 0f, false, MyOwnershipShareModeEnum.Faction);

			string name = artilleryBlock.CustomName;
			string message = string.Format("Artillery Block ({0}) repaired", name, repairAmount);

			MyVisualScriptLogicProvider.ShowNotificationLocal(message, 1000, "Green");
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
