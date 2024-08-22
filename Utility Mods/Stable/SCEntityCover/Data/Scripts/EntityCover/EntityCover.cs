using ProtoBuf.Meta;
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

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "EntityCover", "EntityCover2", "EntityCoverEveFreighter", "EntityCover3", "EntityCover4", "EntityCover4RED", "EntityCover4BLU", "EntityCoverFractal", "EntityCoverColor", "EntityCoverEVEDepot", "EntityCoverSatellite", "EntityCoverContainerL", "EntityCoverEveDerelict")]
    public class EntityCoverGamelogic : MyGameLogicComponent
    {
        // Core
        public IMyBatteryBlock entityBattery;
        public string modelName; // New property to store the model name
        public Vector3 modelDimensions; // New property to store the model dimensions

        public static Dictionary<string, ModelInfo> subtypeToModelMap = new Dictionary<string, ModelInfo>()
        {
            { "EntityCover", new ModelInfo("REMlikeblocker_purple.mwm", new Vector3(275, 275, 275)) },    //block subtype, block model filename, hitbox dimensions //in Large Blocks (???)
            { "EntityCoverFractal", new ModelInfo("REMlikeblocker_purple.mwm", new Vector3(275, 275, 275)) },    //block subtype, block model filename, hitbox dimensions //in Large Blocks (???)
            { "EntityCoverColor", new ModelInfo("null", new Vector3(275, 275, 275)) },    //block subtype, block model filename, hitbox dimensions //in Large Blocks (???)
            { "EntityCoverEVEDepot", new ModelInfo("null", new Vector3(150, 80, 200)) },    //block subtype, block model filename, hitbox dimensions //in Large Blocks (???)
            { "EntityCover2", new ModelInfo("REMlikeblocker2_5km_purple.mwm", new Vector3(1250, 1250, 1250)) },    
            { "EntityCoverEveFreighter", new ModelInfo("evefreighter.mwm", new Vector3(180, 60, 500)) },
            { "EntityCover3", new ModelInfo("REMlikeblockerLong25kX.mwm", new Vector3(1250, 275, 275)) },
            { "EntityCover4", new ModelInfo("REMlikeblocker1kmplate_purple.mwm", new Vector3(500, 500, 50)) },    //don't forget the entitycomponentdescriptor too dumbass
            { "EntityCover4BLU", new ModelInfo("REMlikeblocker1kmplate_blue.mwm", new Vector3(500, 500, 50)) },    //don't forget the entitycomponentdescriptor too dumbass
            { "EntityCover4RED", new ModelInfo("REMlikeblocker1kmplate_red.mwm", new Vector3(500, 500, 50)) },    //don't forget the entitycomponentdescriptor too dumbass
            { "EntityCoverSatellite", new ModelInfo("essarray.mwm", new Vector3(200, 55, 110)) },    //don't forget the entitycomponentdescriptor too dumbass
            { "EntityCoverContainerL", new ModelInfo("ContainerL.mwm", new Vector3(70, 70, 160)) },    //don't forget the entitycomponentdescriptor too dumbass
            { "EntityCoverEveDerelict", new ModelInfo("EntityCoverEveDerelict.mwm", new Vector3(200, 1250, 200)) },    //don't forget the entitycomponentdescriptor too dumbass
            // Add more entries for additional variants...
        };

        public static Vector3 GetModelDimensionsBySubtype(string subtype)
        {
            return subtypeToModelMap.ContainsKey(subtype) ? subtypeToModelMap[subtype].ModelDimensions : new Vector3(100, 100, 100);
        }


        // Create a method to get the model name based on subtype ID
        private ModelInfo GetModelInfoForSubtype(string subtypeId)
        {
            if (subtypeToModelMap.ContainsKey(subtypeId))
            {
                return subtypeToModelMap[subtypeId];
            }
            else
            {
                // Set default model info if the subtype doesn't match any predefined cases.
                return new ModelInfo("DefaultModel.mwm", new Vector3(100, 100, 100));
            }
        }


        public class ModelInfo
        {
            public string ModelName { get; }
            public Vector3 ModelDimensions { get; }

            public ModelInfo(string modelName, Vector3 modelDimensions)
            {
                ModelName = modelName;
                ModelDimensions = modelDimensions;
            }
        }


        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            entityBattery = Entity as IMyBatteryBlock;
            entityBattery.OnClose += EntityBattery_OnClose;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // Set the modelName and modelDimensions based on the subtype of the battery block
            var modelInfo = GetModelInfoForSubtype(entityBattery.BlockDefinition.SubtypeId);
            modelName = modelInfo.ModelName;
            modelDimensions = modelInfo.ModelDimensions;
        }

        private void EntityBattery_OnClose(IMyEntity obj)
        {
            Close();
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (entityBattery.CubeGrid.Physics == null || EntityCover.Instance == null) return;

            // Get the subtype ID of the battery block
            string subtypeId = entityBattery.BlockDefinition.SubtypeId;

            // Get the model dimensions based on the subtype ID
            Vector3 modelDimensions = GetModelInfoForSubtype(subtypeId).ModelDimensions; // Use the modelDimensions property

            // Check for the EntityCoverColor block
            if (subtypeId == "EntityCoverColor")
            {
                // Check the position of the block
                Vector3D blockPosition = entityBattery.GetPosition(); // Assuming this gets the position of the block

                // Check if the block's x position is above 0
                if (blockPosition.X > 0)
                {
                    // Set the modelName to the red variant
                    modelName = "REMlikeblocker_RED.mwm";
                }
                else
                {
                    // Set the modelName to the blue variant
                    modelName = "REMlikeblocker_BLU.mwm";
                }
            }
            // Check for the EntityCoverColor block
            if (subtypeId == "EntityCoverEVEDepot")
            {
                // Check the position of the block
                Vector3D blockPosition = entityBattery.GetPosition(); // Assuming this gets the position of the block

                // Check if the block's x position is above 0
                if (blockPosition.X > 0)
                {
                    // Set the modelName to the red variant
                    modelName = "evedepotobjectred.mwm";
                }
                else
                {
                    // Set the modelName to the blue variant
                    modelName = "evedepotobjectblu.mwm";
                }
            }

            EntityCover.Instance.AddCover((IMyTerminalBlock)entityBattery, modelName, modelDimensions); // Pass the modelDimensions parameter
        }




        public override void Close()
        {
            try 
            {
                // Get entityId from block
                long entityId = entityBattery.EntityId;

                EntityCover.RemoveCover(entityId, modelName);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"EntityCover {ex}");
            }
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

        public bool DoDamage(float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0,
            long realHitEntityId = 0, bool shouldDetonateAmmo = true, MyStringHash? extraInfo = null)
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

        public void AddCover(IMyTerminalBlock block, string modelName, Vector3 modelDimensions) // Add the modelName and modelDimensions parameters
        {
            var cGrid = block.CubeGrid as MyCubeGrid;
            cGrid.ConvertToStatic();
            cGrid.DisplayName = "#EntityCover";
            cGrid.DestructibleBlocks = false;

            var blockEnt = CreateBlocker(block.EntityId, block.WorldMatrix, modelName, modelDimensions); // Pass the modelDimensions parameter
            allCoverEnts.Add(blockEnt);

            //MyAPIGateway.Utilities.ShowMessage("", $"Added cover: {block.EntityId}");
        }


        public static void RemoveCover(long entityId, string modelName)
        {
            // Find by entityId
            var blockEnt = allCoverEnts.Find(x => x.attachedEntityId == entityId);
            MyCubeGrid coverGrid = MyAPIGateway.Entities.GetEntityById(entityId) as MyCubeGrid;
            if (coverGrid != null)
                coverGrid.DestructibleBlocks = false;

            if (blockEnt != null)
            {
                allCoverEnts.Remove(blockEnt);

                // Close the entity
                blockEnt.Close();
            }
        }



        private BlockerEnt CreateBlocker(long attachedEntityId, MatrixD initialMatrix, string modelName, Vector3 modelDimensions) // Modify the constructor
        {
            var ent = new BlockerEnt(attachedEntityId, modelName);

            string modelPath = ModContext.ModPath + "\\Models\\" + modelName;

            ent.Init(null, modelPath, null, null, null);
            ent.DefinitionId = new MyDefinitionId(MyObjectBuilderType.Invalid, "CustomEntity");
            ent.Save = false;
            ent.WorldMatrix = initialMatrix;

            MyEntities.Add(ent, true);

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

        private void HitCallback(IMyEntity entity, bool arg2)
        {
            MyCubeGrid cGrid = entity as MyCubeGrid;
            if (cGrid == null || cGrid.Physics == null)
            {
                //MyAPIGateway.Utilities.ShowNotification("HitCallback: Entity is not a cube grid or has no physics", 5000, MyFontEnum.Red);
                return;
            }

            if (!isBouncing)
            {
                isBouncing = true;

                // Find the closest blocker to the colliding grid
                BlockerEnt closestBlocker = GetClosestBlocker(cGrid.PositionComp.GetPosition());
                if (closestBlocker == null)
                {
                    //MyAPIGateway.Utilities.ShowNotification("HitCallback: No closest blocker found", 5000, MyFontEnum.Red);
                    return;
                }

                // Calculate the direction to push the colliding grid away from the blocker
                Vector3D pushDirection = cGrid.PositionComp.WorldAABB.Center - closestBlocker.PositionComp.GetPosition();
                pushDirection.Normalize();
                Vector3D impulse = pushDirection * 25; // Modify the multiplier as needed for desired force

                // Apply the impulse to the colliding grid
                cGrid.Physics.LinearVelocityUnsafe = impulse;
                cGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, impulse, cGrid.WorldMatrix.Translation, null);

                //MyAPIGateway.Utilities.ShowNotification($"HitCallback: Applying force to {cGrid.DisplayName}", 5000, MyFontEnum.Green);
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