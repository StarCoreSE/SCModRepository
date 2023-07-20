using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace klime.EntityCover
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "EntityCover")]
    public class EntityCoverGamelogic : MyGameLogicComponent
    {
        //Core
        public IMyBatteryBlock entityBattery;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            //if (!MyAPIGateway.Session.IsServer) return;

            entityBattery = Entity as IMyBatteryBlock;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (entityBattery.CubeGrid.Physics == null || EntityCover.Instance == null) return;

            EntityCover.Instance.AddCover((IMyTerminalBlock)entityBattery);
        }

        public override void Close()
        {
            if (EntityCover.Instance == null) return;

            EntityCover.Instance.RemoveCover((IMyTerminalBlock)entityBattery);
        }
    }
}