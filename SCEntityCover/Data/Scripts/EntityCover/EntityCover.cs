using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
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
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace klime.EntityCover
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "EntityCover1", "EntityCover2", "EntityCoverEveFreighter", "EntityCover3")]
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
            if (entityBattery.BlockDefinition.SubtypeId == "EntityCover1")
            {
                modelName = "REMlikeblocker2x_purple.mwm"; // Set the model name for the first variant
            }
            else if (entityBattery.BlockDefinition.SubtypeId == "EntityCover2")
            {
                modelName = "REMlikeblocker2_5km_purple.mwm"; // Set the model name for the second variant
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
            if (entityBattery.BlockDefinition.SubtypeId == "EntityCover1")
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
            //ent.AddToGamePruningStructure();
            //ent.StaticForPruningStructure = true;
            // Retrieve the model dimensions based on the modelName
            Vector3 modelDimensions = GetModelDimensions(modelName);

            CreateBlockerPhysics(ent, modelDimensions); // Pass the modelDimensions parameter
            return ent;
        }

        private void CreateBlockerPhysics(BlockerEnt ent, Vector3 modelDimensions)
        {
            PhysicsSettings settings = new PhysicsSettings();
            settings.RigidBodyFlags |= RigidBodyFlag.RBF_STATIC;
            settings.CollisionLayer |= CollisionLayers.NoVoxelCollisionLayer;
            settings.IsPhantom = true;
            //settings.RigidBodyFlags |= RigidBodyFlag.RBF_DOUBLED_KINEMATIC;
            settings.DetectorColliderCallback = HitCallback;
            settings.Entity = ent;
            settings.WorldMatrix = ent.WorldMatrix;
            settings.WorldMatrix.GetOrientation();
            //settings.Entity.Flags |= EntityFlags.IsGamePrunningStructureObject;
            MyAPIGateway.Physics.CreateBoxPhysics(settings, modelDimensions, 1f);
            
        }

        private static Vector3 GetModelDimensions(string modelName)
        {
            // Add cases for each modelName and set their respective model dimensions
            switch (modelName)
            {
                case "REMlikeblocker2_5km_purple.mwm":
                    return new Vector3(1250, 1250, 1250);
                case "REMlikeblocker2x.mwm":
                    return new Vector3(250, 250, 250);
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
           // DrawLines(); //this is for debugging, remove the // in front of it to enable it
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
        private static BoundingBoxD GetLocalBoundingBox(BlockerEnt thisEnt)
        {
            // Assuming the bounding box is already in local coordinates or can be obtained as such
            return thisEnt.PositionComp.LocalAABB;
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
                MatrixD blockerOrientation = thisEnt.PositionComp.GetOrientation();

                // Determine the direction from the blocker's center to the grid's position
                Vector3D directionFromBlockerToGrid = Vector3D.Normalize(cGrid.PositionComp.GetPosition() - blockerCenter);

                // Half-extents of the blocker (275 in this case), this will need to somehow sample what the blocker is, and then get the half-extents from that.
                double halfExtents = 275;

                // Calculate the simulated "boxCenter" that places the "center" beneath the grid
                Vector3D simulatedBoxCenter = blockerCenter + directionFromBlockerToGrid * halfExtents;

                // Continue with the rest of the code, using "simulatedBoxCenter" instead of "blockerCenter"
                // Get the incident velocity direction
                double incidentSpeed = cGrid.Physics.Speed;
                Vector3D incidentVelocity = cGrid.LinearVelocity;
                Vector3D incidentVelocityB = cGrid.LinearVelocity;
                Vector3D incidentVelocityC = cGrid.LinearVelocity + cGrid.Physics.AngularVelocity;
                Vector3D incidentAngularVelocity = cGrid.Physics.AngularVelocity;

                // Calculate the relative impact point
                Vector3D relImpact = Vector3D.TransformNormal(cGrid.PositionComp.GetPosition() - thisEnt.PositionComp.GetPosition(), MatrixD.Invert(thisEnt.WorldMatrix));

                // Calculate the direction from the relative impact point to the local center
                Vector3D directionToCenter = Vector3D.Normalize(thisEnt.PositionComp.GetPosition() - relImpact);

                BoundingBoxD boundingBox = cGrid.PositionComp.WorldAABB;
                Vector3D size = boundingBox.Max - boundingBox.Min;

                // Determine the maximum side length and calculate the warp distance as half of it
                double maxSideLength = Math.Max(size.X, Math.Max(size.Y, size.Z));

                // Move the relative impact point 50 meters closer to the local center
                relImpact += directionToCenter * maxSideLength;
                // Determine the closest face normal in local coordinates
                Vector3D localNormal = DetermineClosestFaceNormal(relImpact, thisEnt);

                // Transform the local normal into world coordinates
                Vector3D worldNormal = Vector3D.TransformNormal(localNormal, thisEnt.WorldMatrix);

                // Reflect the incident velocity based on the world normal
                Vector3D reflection = Vector3D.Reflect(incidentVelocity, worldNormal);

                AddLine(cGrid.PositionComp.GetPosition(), incidentVelocity, Color.Red);
                AddLine(cGrid.PositionComp.GetPosition(), reflection, Color.Green);
                AddLine(cGrid.PositionComp.GetPosition(), worldNormal, Color.Blue);

                bool basic = false; //this just uses the basic reflection, no fancy stuff

                if (!basic) // if not using basic then use the fancy stuff
                {

                    // Determine the direction from the blocker's center to the grid's position.
                    // This is the only reliable information we have about the blocker's orientation.
                    Vector3D directionFromBlocker = Vector3D.Normalize(cGrid.PositionComp.GetPosition() - blockerCenter);
                    // Check if the grid is inside or outside the blocker
                    Vector3D boxNormal = CalculateHitFaceNormal(relImpact, blockerCenter, cGrid.PositionComp.GetPosition(), thisEnt.WorldMatrix);
                    double dotProduct = Vector3D.Dot(directionFromBlocker, worldNormal);
                    // Determine the direction from the blocker's center to the grid's position
                    Vector3D directionFromBlockerComplex = Vector3D.Normalize(cGrid.PositionComp.GetPosition() - simulatedBoxCenter);

                    bool MoreCalcs = true;

                    if (incidentVelocityC.AbsMax() < 10) // TODO: get rid of this, this shouldn't be nessasary if the rest of the code works.
                    {


                        // Determine the push direction based on whether the grid is inside or outside the blocker
                        Vector3D pushDirection = dotProduct < 0 ? -Vector3D.Normalize(boxNormal) : Vector3D.Normalize(boxNormal); // Reversed logic here

                        // Apply the push effect by moving the grid in the correct direction
                        cGrid.PositionComp.SetPosition(cGrid.PositionComp.GetPosition() + pushDirection * maxSideLength);


                        //MyAPIGateway.Utilities.ShowMessage("", $"Low Incident Velocity: {incidentVelocity}");
                        AddLine(cGrid.PositionComp.GetPosition(), incidentVelocityC, Color.Red);
                        AddLine(cGrid.PositionComp.GetPosition(), reflection, Color.Green);
                        AddLine(cGrid.PositionComp.GetPosition(), boxNormal, Color.Blue);
                    }
                    else if (MoreCalcs)
                    {
                        // Determine the direction from the grid's current position to the blocker's center
                        Vector3D directionToBlocker = Vector3D.Normalize(blockerCenter - cGrid.PositionComp.GetPosition());

                        // Calculate the dot product of the reflection and the direction to the blocker
                        double dotProductWithReflection = Vector3D.Dot(reflection, directionToBlocker);

                        // If the dot product is positive, the reflection is pointing towards the blocker
                        if (dotProductWithReflection > 0)
                        {
                           
                            directionFromBlocker = directionFromBlockerComplex;
                            // Project the grid's velocity onto the direction from the blocker
                            double velocityTowardsBlocker = Vector3D.Dot(incidentVelocity, directionFromBlocker);

                            // Calculate the velocity component to subtract
                            Vector3D velocityComponentToSubtract = directionFromBlocker * velocityTowardsBlocker;

                            // Subtract the velocity component from the grid's velocity
                            cGrid.Physics.LinearVelocity -= (velocityComponentToSubtract);

                            // Project the grid's angular velocity onto the direction from the blocker
                            double angularVelocityTowardsBlocker = Vector3D.Dot(incidentAngularVelocity, directionFromBlocker);

                            // Determine whether the rotation is towards or away from the blocker
                            if (angularVelocityTowardsBlocker > 0)
                            {
                                // Rotation is towards the blocker; invert the angular velocity
                                cGrid.Physics.AngularVelocity = -Vector3D.Multiply(incidentAngularVelocity, 0.65);
                            }
                            else
                            {
                                // Rotation is away from the blocker; add 50% to the angular velocity
                                cGrid.Physics.AngularVelocity = Vector3D.Multiply(incidentAngularVelocity, 0.65);
                            }

                            // Optionally, add additional push away from the blocker's centerspace 

                            var tempVel = cGrid.Physics.LinearVelocity;

                            tempVel += directionFromBlocker * (incidentSpeed + 1);
                            
                            cGrid.Physics.LinearVelocity = Vector3D.Multiply(tempVel, 0.75) * 1.1;


                        }
                        else
                        {
                            //MyAPIGateway.Utilities.ShowMessage("", $"Normal Incident Product: {dotProductWithReflection}");
                            AddLine(cGrid.PositionComp.GetPosition(), incidentVelocityC, Color.Red);
                            AddLine(cGrid.PositionComp.GetPosition(), reflection, Color.Green);
                            AddLine(cGrid.PositionComp.GetPosition(), boxNormal, Color.Blue);
                            //cGrid.Physics.AngularVelocity = -Vector3D.Multiply(incidentAngularVelocity, 0.95);


                            cGrid.Physics.LinearVelocity = Vector3D.Multiply(reflection, 0.75) * 1.1;

                        }


                    }
                }
            else
                {
                    // Determine the direction from the blocker's center to the grid's position
                    Vector3D directionFromBlocker = Vector3D.Normalize(cGrid.PositionComp.GetPosition() - simulatedBoxCenter);

                    // Project the grid's velocity onto the direction from the blocker
                    double velocityTowardsBlocker = Vector3D.Dot(incidentVelocity, directionFromBlocker);

                    // Calculate the velocity component to subtract
                    Vector3D velocityComponentToSubtract = directionFromBlocker * velocityTowardsBlocker;

                    // Subtract the velocity component from the grid's velocity
                    cGrid.Physics.LinearVelocity -= (velocityComponentToSubtract + 1);

                    // Project the grid's angular velocity onto the direction from the blocker
                    double angularVelocityTowardsBlocker = Vector3D.Dot(incidentAngularVelocity, directionFromBlocker);

                    // Determine whether the rotation is towards or away from the blocker
                    if (angularVelocityTowardsBlocker > 0)
                    {
                        // Rotation is towards the blocker; invert the angular velocity
                        cGrid.Physics.AngularVelocity = -Vector3D.Multiply(incidentAngularVelocity, 0.65);
                    }
                    else
                    {
                        // Rotation is away from the blocker; add 50% to the angular velocity
                        cGrid.Physics.AngularVelocity = Vector3D.Multiply(incidentAngularVelocity, 0.65);
                    }

                    // Optionally, add additional push away from the blocker's center
                    cGrid.Physics.LinearVelocity += directionFromBlocker * (incidentSpeed + 1);

                }
                AddLine(cGrid.PositionComp.GetPosition(), blockerCenter, Color.MediumPurple);
                AddLine(cGrid.PositionComp.GetPosition(), relImpact, Color.Teal);

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

        private static IEnumerable<Vector3D> GetFaceNormals()
        {
            yield return new Vector3D(1, 0, 0);  // Right face
            yield return new Vector3D(-1, 0, 0); // Left face
            yield return new Vector3D(0, 1, 0);  // Top face
            yield return new Vector3D(0, -1, 0); // Bottom face
            yield return new Vector3D(0, 0, 1);  // Front face
            yield return new Vector3D(0, 0, -1); // Back face
        }
        private static double CalculateDistanceToFace(Vector3D relImpact, Vector3D faceNormal, BoundingBoxD localBoundingBox)
        {
            // Determine the corresponding face plane
            double d = faceNormal.X * localBoundingBox.Max.X + faceNormal.Y * localBoundingBox.Max.Y + faceNormal.Z * localBoundingBox.Max.Z;

            // Calculate the distance from the point to the plane
            double distance = Math.Abs(Vector3D.Dot(faceNormal, relImpact) - d);

            return distance;
        }
        private static Vector3D DetermineClosestFaceNormal(Vector3D relImpact, BlockerEnt thisEnt)
        {
            BoundingBoxD localBoundingBox = GetLocalBoundingBox(thisEnt);
            Vector3D closestNormal = Vector3D.Zero;
            double minDistance = double.MaxValue;

            foreach (var localNormal in GetFaceNormals())
            {
                double distance = CalculateDistanceToFace(relImpact, localNormal, localBoundingBox);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNormal = localNormal;
                }
            }

            return closestNormal;
        }
        private static Vector3D CalculateHitFaceNormal(Vector3D relImpact, Vector3D blockerCenter, Vector3D collisionPoint, MatrixD worldMatrix)
        {
            Vector3D normalizedImpact = new Vector3D(
                Math.Abs(relImpact.X),
                Math.Abs(relImpact.Y),
                Math.Abs(relImpact.Z)
            );

            Vector3D normal;

            if (normalizedImpact.X > normalizedImpact.Y && normalizedImpact.X > normalizedImpact.Z)
                normal = new Vector3D(Math.Sign(relImpact.X), 0, 0);
            else if (normalizedImpact.Y > normalizedImpact.X && normalizedImpact.Y > normalizedImpact.Z)
                normal = new Vector3D(0, Math.Sign(relImpact.Y), 0);
            else
                normal = new Vector3D(0, 0, Math.Sign(relImpact.Z));

            Vector3D worldNormal = Vector3D.TransformNormal(normal, worldMatrix);

            // Determine the direction from the blocker's center to the collision point
            Vector3D directionFromBlocker = Vector3D.Normalize(collisionPoint - blockerCenter);

            // Verify the normal's direction by checking the dot product with the direction from the blocker
            double dotProduct = Vector3D.Dot(directionFromBlocker, worldNormal);
            if (dotProduct < 0)
            {
                // Reverse the normal if it's pointing in the wrong direction
                worldNormal = -worldNormal;
            }

            return worldNormal;
        }


        struct LineInfo
        {
            public Vector3D Origin;
            public Vector3D Direction;
            public Color Color;
            public DateTime Timestamp;

            public LineInfo(Vector3D origin, Vector3D direction, Color color)
            {
                Origin = origin;
                Direction = direction;
                Color = color;
                Timestamp = DateTime.Now;
            }
        }
        private List<LineInfo> linesToDraw = new List<LineInfo>();

        private void AddLine(Vector3D origin, Vector3D direction, Color color)
        {
            linesToDraw.Add(new LineInfo(origin, direction, color));
        }

        private void DrawLines()
        { //debuging kino
            float length = 10f;
            float thickness = 0.5f;

            linesToDraw.RemoveAll(line => (DateTime.Now - line.Timestamp).TotalSeconds > 5);

            foreach (var line in linesToDraw)
            {
                Vector4 colorVector = new Vector4(line.Color.R / 255.0f, line.Color.G / 255.0f, line.Color.B / 255.0f, line.Color.A / 255.0f);
                Vector3D endPoint = line.Origin + line.Direction * length;
                MySimpleObjectDraw.DrawLine(line.Origin, endPoint, MyStringId.GetOrCompute("Square"), ref colorVector, thickness);
            }
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