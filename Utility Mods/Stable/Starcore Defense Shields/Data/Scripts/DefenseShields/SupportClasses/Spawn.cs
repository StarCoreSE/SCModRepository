using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Debris;
using VRage.Game.Components;
using VRage.Utils;

namespace DefenseShields.Support
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

    internal class Spawn
    {
        private static readonly SerializableBlockOrientation EntityOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);

        private static readonly MyObjectBuilder_CubeGrid CubeGridBuilder = new MyObjectBuilder_CubeGrid()
        {
            EntityId = 0,
            GridSizeEnum = MyCubeSize.Large,
            IsStatic = true,
            Skeleton = new List<BoneInfo>(),
            LinearVelocity = Vector3.Zero,
            AngularVelocity = Vector3.Zero,
            ConveyorLines = new List<MyObjectBuilder_ConveyorLine>(),
            BlockGroups = new List<MyObjectBuilder_BlockGroup>(),
            Handbrake = false,
            XMirroxPlane = null,
            YMirroxPlane = null,
            ZMirroxPlane = null,
            PersistentFlags = MyPersistentEntityFlags2.InScene,
            Name = "ArtificialCubeGrid",
            DisplayName = "FieldEffect",
            CreatePhysics = false,
            DestructibleBlocks = true,
            PositionAndOrientation = new MyPositionAndOrientation(Vector3D.Zero, Vector3D.Forward, Vector3D.Up),

            CubeBlocks = new List<MyObjectBuilder_CubeBlock>()
                {
                    new MyObjectBuilder_CubeBlock()
                    {
                        EntityId = 0,
                        BlockOrientation = EntityOrientation,
                        SubtypeName = string.Empty,
                        Name = string.Empty,
                        Min = Vector3I.Zero,
                        Owner = 0,
                        ShareMode = MyOwnershipShareModeEnum.None,
                        DeformationRatio = 0,
                    }
                }
        };

        private static readonly MyObjectBuilder_Meteor TestBuilder = new MyObjectBuilder_Meteor()
        {
            EntityId = 0,
            LinearVelocity = Vector3.Zero,
            AngularVelocity = Vector3.Zero,
            PersistentFlags = MyPersistentEntityFlags2.InScene,
            Name = "GravityMissile",
            PositionAndOrientation = new MyPositionAndOrientation(Vector3D.Zero, Vector3D.Forward, Vector3D.Up)
        };

        public static MyEntity EmptyEntity(string displayName, string model, MyEntity parent, bool parented = false)
        {
            try
            {
                var myParent = parented ? parent : null;
                var ent = new MyEntity { NeedsWorldMatrix = true, Physics = !parented ? new MyHandToolBase.MyBlockingBody(new MyHandToolBase(), parent) { IsPhantom = true } : null };
                ent.Init(new StringBuilder(displayName), model, myParent, null, null);
                ent.Name = $"{parent.EntityId}";
                ent.DefinitionId = new MyDefinitionId(MyObjectBuilderType.Invalid, MyStringHash.GetOrCompute("DefenseShield"));
                ent.Hierarchy.ChildId = parent.EntityId;
                MyEntities.Add(ent);
                return ent;
            }
            catch (Exception ex) { Log.Line($"Exception in EmptyEntity: {ex}"); return null; }
        }

        public static MyEntity SpawnBlock(string subtypeId, string name, bool isVisible = false, bool hasPhysics = false, bool isStatic = false, bool toSave = false, bool destructible = false, long ownerId = 0)
        {
            try
            {
                CubeGridBuilder.Name = name;
                CubeGridBuilder.CubeBlocks[0].SubtypeName = subtypeId;
                CubeGridBuilder.CreatePhysics = hasPhysics;
                CubeGridBuilder.IsStatic = isStatic;
                CubeGridBuilder.DestructibleBlocks = destructible;
                var ent = (MyEntity)MyAPIGateway.Entities.CreateFromObjectBuilder(CubeGridBuilder);

                ent.Flags &= ~EntityFlags.Save;
                ent.Render.Visible = isVisible;
                MyAPIGateway.Entities.AddEntity(ent);

                return ent;
            }
            catch (Exception ex)
            {
                Log.Line("Exception in Spawn");
                Log.Line($"{ex}");
                return null;
            }
        }

    }
}
