using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Invalid.spawngencover
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "EntityCoverFractal")]
    public class spawngencover : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private const int branchFactor = 4;

        // New Variables to store adjustable settings
        private int initialDepth = 100;
        private int initialLength = 225;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;
            CreateFractal(block.Position, initialDepth, initialLength, new Vector3I(0, 1, 0));
        }

        // Public method to change settings dynamically
        public void SetFractalAttributes(int depth, int length)
        {
            initialDepth = depth;
            initialLength = length;
        }

        private int currentBranchCount = 0;
        private const int MaxBranches = 100;

        private Random rand = new Random();

        private void CreateFractal(Vector3I origin, int depth, int length, Vector3I direction)
        {
            if (depth == 0 || currentBranchCount >= MaxBranches)
            {
                return;
            }

            for (int i = 1; i <= length; i++)
            {
                // AddBlock("LargeBlockArmorBlock", origin + i * direction);
                //if (block.Physics != null)
                //{
                //    MyAPIGateway.Utilities.ShowNotification("AAAH I'M FRACTOOOOOLING I'M GONNA FRACTOOOOOOL");
                //}
            }

            // Place EntityCover block at the end of each completed "length"
            //AddEntityCoverBlock("EntityCoverFractal", origin + length * direction);     DO NOT DO THIS
            AddEntityCoverBlock("EntityCoverColor", origin + length * direction);


            Vector3I[] newDirections =
            {
                new Vector3I(1, 0, 0),
                new Vector3I(-1, 0, 0),
                new Vector3I(0, 1, 0),
                new Vector3I(0, -1, 0),
                new Vector3I(0, 0, 1),
                new Vector3I(0, 0, -1)
            };

            Vector3I newOrigin = origin + length * direction;

            // Calculate an adaptive branch factor based on the current depth and branches left.
            int adaptiveBranchFactor = Math.Min(branchFactor, (MaxBranches - currentBranchCount) / depth);

            for (int i = 0; i < adaptiveBranchFactor; i++)
            {
                if (currentBranchCount >= MaxBranches)
                {
                    break;
                }
                int randomIndex = rand.Next(newDirections.Length);
                Vector3I selectedDirection = newDirections[randomIndex];
                currentBranchCount++;
                CreateFractal(newOrigin, depth - 1, length, selectedDirection);
            }
        }

        private void AddBlock(string subtypeName, Vector3I position)
        {
            var grid = block.CubeGrid;

            var nextBlockBuilder = new MyObjectBuilder_BatteryBlock
            {
                SubtypeName = subtypeName,
                Min = position,
                BlockOrientation = new MyBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up),
                ColorMaskHSV = new SerializableVector3(0, -1, 0),
                Owner = block.OwnerId,
                EntityId = 0,
                ShareMode = MyOwnershipShareModeEnum.None
            };

            IMySlimBlock newBlock = grid.AddBlock(nextBlockBuilder, false);

            if (newBlock == null)
            {
               // MyAPIGateway.Utilities.ShowNotification($"Failed to add {subtypeName}", 1000);
                return;
            }
           // MyAPIGateway.Utilities.ShowNotification($"{subtypeName} added at {position}", 1000);
        }

        private void AddEntityCoverBlock(string subtypeName, Vector3I position)
        {
            var grid = block.CubeGrid;
            var nextBlockBuilder = new MyObjectBuilder_BatteryBlock
            {
                SubtypeName = subtypeName,
                Min = position,
                BlockOrientation = new MyBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up),
                ColorMaskHSV = new SerializableVector3(0, -1, 0),
                Owner = block.OwnerId,
                EntityId = 0,
                ShareMode = MyOwnershipShareModeEnum.None
            };

            IMySlimBlock newBlock = grid.AddBlock(nextBlockBuilder, false);
            if (newBlock == null)
            {
               // MyAPIGateway.Utilities.ShowNotification($"Failed to add {subtypeName}", 1000);
                return;
            }
            //MyAPIGateway.Utilities.ShowNotification($"{subtypeName} added at {position}", 1000);
        }

    }
}
