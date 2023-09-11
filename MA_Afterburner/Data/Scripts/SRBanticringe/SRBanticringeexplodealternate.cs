using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace SRBanticringe
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "SC_SRB")]
	public class SRBexplodealternate : MyGameLogicComponent
	{
		private IMyConveyorSorter SRB;
		private int triggerTick = 0;
		private const double OFFSET_DISTANCE = 3;

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			if (!MyAPIGateway.Session.IsServer) return; // Only do explosions serverside
			SRB = Entity as IMyConveyorSorter;
			NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
		}

		public override void UpdateOnceBeforeFrame()
		{
			if (SRB == null || SRB.CubeGrid.Physics == null) return;
			SRB.EnabledChanged += SRBEnabledChanged;
			NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
		}

		public override void UpdateAfterSimulation()
		{
			if (triggerTick == 1)
			{
				DoExplosion();
			}

			triggerTick = 0;
		}

		private void DoExplosion()
		{
			if (SRB == null || SRB.CubeGrid.Physics == null || SRB.Enabled) return;
			double radius = 30;
			BoundingSphereD sphere = new BoundingSphereD(SRB.WorldMatrix.Translation + (SRB.WorldMatrix.Forward * OFFSET_DISTANCE), radius); // Apply offset, 10);
			MyExplosionInfo explosion = new MyExplosionInfo(0f, 10000f, sphere, MyExplosionTypeEnum.CUSTOM, true);

			MyExplosions.AddExplosion(ref explosion);
		}

		private void SRBEnabledChanged(IMyTerminalBlock obj)
		{
			if (obj.EntityId != SRB.EntityId) return;
			triggerTick += 1;
		}

        public override void Close()
        {
            if (SRB != null)
            {
                SRB.EnabledChanged -= SRBEnabledChanged;
            }
        }

    }
}
