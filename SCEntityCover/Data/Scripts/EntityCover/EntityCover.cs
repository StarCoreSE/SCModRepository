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
    public class BlockerEnt: MyEntity, IMyDestroyableObject
    {
        public float Integrity => float.MaxValue;
        public bool UseDamageSystem => true;
        public long attachedEntityId;
        public string modelName;

        public BlockerEnt(long attachedEntityId, string modelName)
        {
            this.attachedEntityId = attachedEntityId;
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
        public List<BlockerEnt> allCoverEnts = new List<BlockerEnt>();

        public string modelName = "REMlikeblocker2xB.mwm";
        public Vector3 modelDimensions = new Vector3(275, 275, 275); //250, 400, 80

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Instance = this;
        }

        public void AddCover(IMyTerminalBlock block)
        {
            var cGrid = block.CubeGrid as MyCubeGrid;
            cGrid.ConvertToStatic();
            cGrid.DisplayName = "#EntityCover";
            cGrid.DestructibleBlocks = false;

            var blockEnt = CreateBlocker(block.EntityId, block.WorldMatrix, modelName);
            allCoverEnts.Add(blockEnt);

            //MyAPIGateway.Utilities.ShowMessage("", $"Added cover: {block.EntityId}");
        }

        public void RemoveCover(IMyTerminalBlock block)
        {
            var blockEnt = allCoverEnts.Find(x => x.attachedEntityId == block.EntityId);
            if (blockEnt == null) return;

            blockEnt.Close();
            allCoverEnts.Remove(blockEnt);

            //MyAPIGateway.Utilities.ShowMessage("", $"Removed cover: {block.EntityId}");
        }



        private BlockerEnt CreateBlocker(long attachedEntityId, MatrixD initialMatrix, string modelName)
        {
            var ent = new BlockerEnt(attachedEntityId, modelName);
            string modelPath = ModContext.ModPath + "\\Models\\" + modelName;

            ent.Init(null, modelPath, null, null, null);
            ent.DefinitionId = new MyDefinitionId(MyObjectBuilderType.Invalid, "CustomEntity");
            ent.Save = false;
            ent.Render.EnableColorMaskHsv = true;
            ent.Render.ColorMaskHsv = new Vector3(277, 87, 95);
            //ent.Render.MetalnessColorable = false;
            ent.WorldMatrix = initialMatrix;
            MyEntities.Add(ent, true);

            CreateBlockerPhysics(ent);
            return ent;
        }

        private void CreateBlockerPhysics(BlockerEnt ent)
        {
            PhysicsSettings settings = new PhysicsSettings();
            settings.RigidBodyFlags |= RigidBodyFlag.RBF_STATIC;
            settings.DetectorColliderCallback = HitCallback;
            settings.Entity = ent;
            settings.WorldMatrix = ent.WorldMatrix;
            //ent.Render.ColorMaskHsv = new Vector3(0, 0, 0);
            MyAPIGateway.Physics.CreateBoxPhysics(settings, modelDimensions, 0f);
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
                var forceDir = -1 * Vector3D.Normalize(cGrid.LinearVelocity + cGrid.Physics.AngularVelocity);
                var forceMag = cGrid.Mass * ((cGrid.Speed + (float)cGrid.Physics.AngularVelocity.Length()) * 1.65f);
                var force = forceDir * forceMag;
                
                // Reflect the force direction
                //problematic and untrustworthy
               // var reflection = Vector3D.Reflect(forceDir, cGrid.WorldMatrix.Up);
               // var reflectedForce = (reflection / forceDir) * forceMag;

                cGrid.Physics.ApplyImpulse(force, cGrid.Physics.CenterOfMassWorld);
                //cGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, reflectedForce, cGrid.Physics.CenterOfMassWorld, null);
                //MyAPIGateway.Utilities.ShowMessage("", $"Hit: {cGrid.EntityId}, Force: {forceMag}");
            }
        }





        protected override void UnloadData()
        {
            Instance = null;
        }
    }
}