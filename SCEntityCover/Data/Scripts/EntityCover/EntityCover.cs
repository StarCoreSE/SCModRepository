using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


namespace klime.EntityCover
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "EntityCover", "EntityCover2", "EntityCoverEveFreighter", "EntityCover3")]
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
                modelName = "REMlikeblockerLong25kX.mwm"; // Set the model name for the second variant
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
            else if (entityBattery.BlockDefinition.SubtypeId == "EntityCover3")
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

            EntityCover.RemoveCover(entityId, modelName);
        }
    }

    public class BlockerEnt : MyEntity, IMyDestroyableObject
    {
        public float Integrity => float.MaxValue;
        public bool UseDamageSystem => true;
        public long attachedEntityId;
        public string modelName; // Add the modelName field
        private Vector3 modelDimensions;

        public Vector3 ModelDimensions // Add the property to get/set the model dimensions
        {
            get { return modelDimensions; }
            set { modelDimensions = value; }
        }

        public BlockerEnt(long attachedEntityId, string modelName) // Modify the constructor
        {
            this.attachedEntityId = attachedEntityId;
            this.modelName = modelName;
        }

        public long BatteryBlockId { get; private set; }

        public BlockerEnt(long attachedEntityId, long batteryBlockId, string modelName) // Modify the constructor
        {
            this.attachedEntityId = attachedEntityId;
            this.BatteryBlockId = batteryBlockId; // Store the battery ID
            this.modelName = modelName;
        }

        public bool DoDamage(float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null,
            long attackerId = 0, long realHitEntityId = 0, bool shouldDetonateAmmo = true)
        {
            return true;
        }

        public void OnDestroy()
        {

        }
    }


    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class EntityCover : MySessionComponentBase
    {
        public static EntityCover Instance;
        public static List<BlockerEnt> allCoverEnts = new List<BlockerEnt>();


        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Instance = this;
        }

        public void AddCover(IMyTerminalBlock block, string modelName) // Add the modelName parameter
        {
            var cGrid = block.CubeGrid as MyCubeGrid;
            cGrid.ConvertToStatic();
            cGrid.DisplayName = "#EntityCover";
            cGrid.DestructibleBlocks = false;

            var blockEnt = CreateBlocker(block.EntityId, block.WorldMatrix, modelName);
            allCoverEnts.Add(blockEnt);

            //MyAPIGateway.Utilities.ShowMessage("", $"Added cover: {block.EntityId}");
        }

        public static void RemoveCover(long entityId, string modelName)
        {
            // Find by entityId
            var blockEnt = allCoverEnts.Find(x => x.attachedEntityId == entityId);

            if (blockEnt != null)
            {
                allCoverEnts.Remove(blockEnt);

                // Close the entity
                blockEnt.Close();
            }
        }



        private BlockerEnt CreateBlocker(long attachedEntityId, MatrixD initialMatrix, string modelName)
        {
            var ent = new BlockerEnt(attachedEntityId, modelName);

            string modelPath = ModContext.ModPath + "\\Models\\" + modelName;

            ent.Init(null, modelPath, null, null, null);
            ent.DefinitionId = new MyDefinitionId(MyObjectBuilderType.Invalid, "CustomEntity");
            ent.Save = false;
            ent.WorldMatrix = initialMatrix;
            MyEntities.Add(ent, true);

            // Retrieve the model dimensions based on the modelName
            Vector3 modelDimensions = GetModelDimensions(modelName);

            CreateBlockerPhysics(ent, modelDimensions); // Pass the modelDimensions parameter
            return ent;
        }

        private void CreateBlockerPhysics(BlockerEnt ent, Vector3 modelDimensions)
        {
            PhysicsSettings settings = new PhysicsSettings();
            settings.RigidBodyFlags |= RigidBodyFlag.RBF_STATIC;
            settings.DetectorColliderCallback = HitCallback;
            settings.Entity = ent;
            settings.WorldMatrix = ent.WorldMatrix;
            MyAPIGateway.Physics.CreateBoxPhysics(settings, modelDimensions, 0f);
        }

        private static Vector3 GetModelDimensions(string modelName)
        {
            // Add cases for each modelName and set their respective model dimensions
            switch (modelName)
            {
                case "REMlikeblocker2x_purple.mwm":
                    return new Vector3(275, 275, 275);
                case "REMlikeblocker2x.mwm":
                    return new Vector3(275, 275, 275);
                case "eveobstacle3.mwm":
                    return new Vector3(180, 60, 500);
                case "REMlikeblockerLong25kX.mwm":
                    return new Vector3(1000, 275, 275);
                // Add more cases for additional modelNames and their respective model dimensions
                default:
                    return new Vector3(100, 100, 100); // Default model dimensions
            }
        }

        private bool isBouncing = false;
        private int delayTicks = 0;
        private const int DelayDuration = 5; // Adjust the delay duration (in ticks) as needed

        public override void UpdateAfterSimulation()
        {
            if (isBouncing)
            {
                delayTicks++;
                if (delayTicks >= DelayDuration)
                {
                    delayTicks = 0;
                    isBouncing = false;
                }
            }
        }

        private void HitCallback(IMyEntity entity, bool arg2)
        {
            MyCubeGrid cGrid = entity as MyCubeGrid;
            if (cGrid == null || cGrid.Physics == null) return;

            if (!isBouncing)
            {
                isBouncing = true;

                // The BlockerEnt currently being collided with
                BlockerEnt thisEnt = GetClosestBlocker(entity.PositionComp.GetPosition());

                if (thisEnt == null)
                {
                    isBouncing = false; // If thisEnt is null, exit the method
                    return;
                }

                // Get impact location in thisEnt's relative coordiates
                Vector3D relImpact = Vector3D.Rotate(cGrid.PositionComp.GetPosition() - thisEnt.PositionComp.GetPosition(), thisEnt.WorldMatrix);

                // Get the normal of the collision box on the impacted side
                Vector3D boxNormal = Vector3D.Rotate(GenIntNormal(relImpact / (Vector3D)GetModelDimensions(thisEnt.modelName)), -thisEnt.WorldMatrix);

                // Get the incident velocity direction
                Vector3D incidentVelocity = cGrid.LinearVelocity + cGrid.Physics.AngularVelocity;
                Vector3D incidentAngularVelocity = cGrid.Physics.AngularVelocity;


                // Calculate the reflection direction using the law of reflection
                Vector3D reflection = Vector3D.Reflect(incidentVelocity, boxNormal);

                // Apply the reflection as the outgoing velocity
                cGrid.Physics.LinearVelocity = (Vector3)reflection;


                // Reverse the angular velocity of the grid to simulate a bounce
                cGrid.Physics.AngularVelocity = -incidentAngularVelocity;


                //    // Move the grid back half of its max extent, clamped between 5 and 50
                //    cGrid.PositionComp.SetPosition(cGrid.PositionComp.GetPosition() - Vector3D.Normalize(boxNormal) * cGrid.GridSize);
                //
                //    // Increase the linear velocity by 80% because its too low to care about
                //    if (incidentVelocity.AbsMax() < 1)
                //    {
                //        cGrid.Physics.LinearVelocity *= 1.8f;
                //
                //    }
                //    else
                //    {
                //        cGrid.Physics.LinearVelocity *= 0.65f;
                //    }


                Vector3 deez = (cGrid.Max + Vector3.Abs(cGrid.Min));


                 if (incidentVelocity.AbsMax() < 1)
                 {
                     // Move the grid back half of its max extent, clamped between 5 and 50
                     cGrid.PositionComp.SetPosition(cGrid.PositionComp.GetPosition() - Vector3D.Normalize(boxNormal) * MathHelper.Clamp((int)deez.AbsMax(), 5, (int)deez.AbsMax() * 0.5));
                     
                     // Increase the linear velocity by 80% because its too low to care about
                     cGrid.Physics.LinearVelocity *= 1.8f;
               
                     MyAPIGateway.Utilities.ShowMessage("", $"Low Incident Velocity: {incidentVelocity}");
                 }
                 else
                 {
                     // Move the grid back half of its max extent, clamped between 5 and 50
                     cGrid.PositionComp.SetPosition(cGrid.PositionComp.GetPosition() - Vector3D.Normalize(boxNormal) * MathHelper.Clamp((int)deez.AbsMax(), 5, (int)deez.AbsMax() * 0.25));

                    //Decrease the linear velocity by 20% because its too high probably
                    cGrid.Physics.LinearVelocity *= 0.8f;
                     MyAPIGateway.Utilities.ShowMessage("", $"Normal Incident Velocity: {incidentVelocity}");
                 }

            }
        }


        private static BlockerEnt GetClosestBlocker(Vector3D pos)
        {
            // warranty void if used at all -aristeas

            if (allCoverEnts.Count == 0)
                return null;

            BlockerEnt closest = allCoverEnts[0];

            foreach (var blockerEnt in allCoverEnts)
                if (Vector3D.DistanceSquared(blockerEnt.PositionComp.GetPosition(), pos) < Vector3D.DistanceSquared(closest.PositionComp.GetPosition(), pos))
                    closest = blockerEnt;

            return closest;
        }

        private static Vector3D GenIntNormal(Vector3D reference)
        {
            // Returns a unit Vector3D with the longest component of reference Vector3D. Hate. Why isn't this a built-in method.

            Vector3D toReturn = Vector3D.Zero;

            double x = Math.Abs(reference.X);
            double y = Math.Abs(reference.Y);
            double z = Math.Abs(reference.Z);

            if (x > y && x > z)
                toReturn.X = reference.X/x;

            else if (y > x && y > z)
                toReturn.Y = reference.Y/y;

            else
                toReturn.Z = reference.Z/z;
            
            return toReturn;
        }

        protected override void UnloadData()
        {
            // Close any open file streams or release other resources here
            // For example, if you have any open file streams, close them as follows:
            // MyAPIGateway.Utilities.CloseLogFile();

            // Clear the list of cover entities and set them to null to release references
            allCoverEnts.Clear();
            allCoverEnts = null;

            // Set the instance to null to release the reference
            Instance = null;
        }

    }




}