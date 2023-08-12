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
using VRageRender;

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
                    return new Vector3(1250, 275, 275);
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
                Vector3D blockerCenter = thisEnt.PositionComp.GetPosition();
                // Get impact location in thisEnt's relative coordiates
                Vector3D relImpact = Vector3D.Rotate(cGrid.PositionComp.GetPosition() - thisEnt.PositionComp.GetPosition(), thisEnt.WorldMatrix);

                // Get the normal of the collision box on the impacted side
                Vector3D boxNormal = Vector3D.Rotate(GenIntNormal(relImpact / (Vector3D)GetModelDimensions(thisEnt.modelName)), -thisEnt.WorldMatrix);

                // Get the incident velocity direction
                Vector3D incidentVelocity = cGrid.LinearVelocity + cGrid.Physics.AngularVelocity;
                Vector3D incidentAngularVelocity = cGrid.Physics.AngularVelocity;


                // Calculate the reflection direction using the law of reflection
                Vector3D reflection = Vector3D.Reflect(incidentVelocity, boxNormal);


                if (incidentVelocity.AbsMax() < 10)
                {
                    // Determine the size of the grid's bounding box
                    BoundingBoxD boundingBox = cGrid.PositionComp.WorldAABB;
                    Vector3D size = boundingBox.Max - boundingBox.Min;

                    // Determine the maximum side length and calculate the warp distance as half of it
                    double maxSideLength = Math.Max(size.X, Math.Max(size.Y, size.Z));
                    double warpDistance = maxSideLength / 2.0; // Dividing by 2 to warp by half the distance

                    // Get the blocker's center position
                    //BlockerEnt thisEnt = GetClosestBlocker(entity.PositionComp.GetPosition());


                    // Determine the direction from the blocker's center to the grid's position
                    Vector3D directionFromBlocker = Vector3D.Normalize(cGrid.PositionComp.GetPosition() - blockerCenter);

                    // Determine if the grid is inside or outside the blocker based on the dot product with boxNormal
                    double dotProduct = Vector3D.Dot(directionFromBlocker, boxNormal);

                    // Determine the push direction based on whether the grid is inside or outside the blocker
                    Vector3D pushDirection = dotProduct < 0 ? -Vector3D.Normalize(boxNormal) : Vector3D.Normalize(boxNormal); // Reversed logic here

                    // Apply the push effect by moving the grid in the correct direction
                    cGrid.PositionComp.SetPosition(cGrid.PositionComp.GetPosition() + pushDirection * warpDistance);


                    MyAPIGateway.Utilities.ShowMessage("", $"Low Incident Velocity: {incidentVelocity}");

                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("", $"Normal Incident Velocity: {incidentVelocity}");

                    // Determine the direction from the grid's current position to the blocker's center
                    Vector3D directionToBlocker = Vector3D.Normalize(blockerCenter - cGrid.PositionComp.GetPosition());

                    // Calculate the dot product of the reflection and the direction to the blocker
                    double dotProductWithReflection = Vector3D.Dot(reflection, directionToBlocker);

                    // If the dot product is positive, the reflection is pointing towards the blocker
                    if (dotProductWithReflection > 0)
                    {
                        // Determine the size of the grid's bounding box
                        BoundingBoxD boundingBox = cGrid.PositionComp.WorldAABB;
                        Vector3D size = boundingBox.Max - boundingBox.Min;

                        // Determine the maximum side length and calculate the warp distance as half of it
                        double maxSideLength = Math.Max(size.X, Math.Max(size.Y, size.Z));
                        double warpDistance = maxSideLength / 2.0;

                        // Use the negated reflection as the push direction to push the grid away from the blocker
                        Vector3D pushDirection = -Vector3D.Normalize(reflection);

                        // Apply the push effect by moving the grid in the correct direction
                        cGrid.PositionComp.SetPosition(cGrid.PositionComp.GetPosition() + pushDirection * warpDistance);
                    }
                    else
                    {
                        cGrid.Physics.LinearVelocity = Vector3D.Multiply(reflection, 1);
                        // Reverse the angular velocity of the grid to simulate a bounce
                       if (incidentVelocity.AbsMax() > 50)
                        {
                            cGrid.Physics.AngularVelocity = -incidentAngularVelocity;
                        }
                       else
                        {
                            cGrid.Physics.AngularVelocity = Vector3D.Multiply(incidentAngularVelocity, 1.1);
                        }

                    }


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