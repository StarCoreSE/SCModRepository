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
using VRageMath;

namespace klime.EntityCover
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "EntityCover", "EntityCover2", "EntityCoverEveFreighter")]
    public class EntityCoverGamelogic : MyGameLogicComponent
    {
        // Core
        public IMyBatteryBlock entityBattery;
        public string modelName; // New property to store the model name
        public Vector3 modelDimensions; // New property to store the model dimensions

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // if (!MyAPIGateway.Session.IsServer) return;

            entityBattery = Entity as IMyBatteryBlock;
            entityBattery.OnClose += EntityBattery_OnClose;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // Set the modelName and modelDimensions based on the subtype of the battery block
            if (entityBattery.BlockDefinition.SubtypeId == "EntityCover")
            {
                modelName = "REMlikeblocker2x_purple.mwm"; // Set the model name for the first variant
            }
            else if (entityBattery.BlockDefinition.SubtypeId == "EntityCover2")
            {
                modelName = "REMlikeblocker2x.mwm"; // Set the model name for the second variant
            }
            else if (entityBattery.BlockDefinition.SubtypeId == "EntityCoverEveFreighter")
            {
                modelName = "eveobstacle3.mwm"; // Set the model name for the second variant
            }
            else if (entityBattery.BlockDefinition.SubtypeId == "EntityCover3")
            {
                modelName = "REMlikeblockerLong2.5kX.mwm"; // Set the model name for the second variant
            }
            // Add more else-if blocks for additional variants...
            else
            {
                // Set default values if the subtype does not match any of the predefined cases.
                modelName = "DefaultModel.mwm";
                modelDimensions = new Vector3(100, 100, 100);
            }
        }

        private void EntityBattery_OnClose(IMyEntity obj)
        {
            Close();
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (entityBattery.CubeGrid.Physics == null || EntityCover.Instance == null) return;

            // Separate the logic based on the subtype ID
            if (entityBattery.BlockDefinition.SubtypeId == "EntityCover")
            {
                EntityCover.Instance.AddCover((IMyTerminalBlock)entityBattery, modelName); // Pass the modelName for the first variant
            }
            else if (entityBattery.BlockDefinition.SubtypeId == "EntityCover2")
            {
                EntityCover.Instance.AddCover((IMyTerminalBlock)entityBattery, modelName); // Pass the modelName for the second variant
            }
            else if (entityBattery.BlockDefinition.SubtypeId == "EntityCoverEveFreighter")
            {
                EntityCover.Instance.AddCover((IMyTerminalBlock)entityBattery, modelName); // Pass the modelName for the second variant
            }
            // Add more else-if blocks for additional variants...
            else
            {
                // Handle the logic for other subtypes, if needed.
            }
        }

        public override void Close()
        {
            // Get entityId from block
            long entityId = entityBattery.EntityId;

            EntityCover.Instance.RemoveCover(entityId, modelName);
        }
    }
}
