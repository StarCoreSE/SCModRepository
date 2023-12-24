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

namespace Invalid.MetalFoam
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Decoy), false, "LargeDecoy_MetalFoam")]
    public class MetalFoamGenerator : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private const int sphereRadius = 3; // 3 blocks in radius

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;

            // Register to damage handler to detect when block is destroyed
            block.SlimBlock.ComponentStack.IsFunctionalChanged += OnBlockDamaged;
        }

        //private void OnBlockDestroyed(IMyEntity obj)
        //{
        //    GenerateArmorSphere(block.Position, sphereRadius);
        //}

        // Handle damage event
        private void OnBlockDamaged()
        {
            // Check if block is destroyed
            if (!block.SlimBlock.IsDestroyed)
            {
                GenerateArmorSphere(block.Position, sphereRadius);

                // Optionally, unregister the damage handler here if the block won't regenerate
                block.SlimBlock.ComponentStack.IsFunctionalChanged -= OnBlockDamaged;
            }
        }

        private void GenerateArmorSphere(Vector3I center, int radius)
        {
            var grid = block.CubeGrid;
            Vector3I pos;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        pos = new Vector3I(x, y, z) + center;
                        double distance = Vector3D.Distance(new Vector3D(pos), new Vector3D(center));
                        if (distance <= radius)
                        {
                            AddArmorBlock("LargeBlockArmorBlock", pos);
                        }
                    }
                }
            }
        }


        private void AddArmorBlock(string subtypeName, Vector3I position)
        {
            var grid = block.CubeGrid; // Get the grid the block is part of

            // Define the block to be added
            var armorBlockBuilder = new MyObjectBuilder_CubeBlock
            {
                SubtypeName = subtypeName, // "LargeBlockArmorBlock" for large grid light armor block
                Min = position, // Position where the block will be placed
                ColorMaskHSV = new SerializableVector3(0, -1, 0), // Default color, change as needed
            };

            // Set the block orientation (facing up by default here)
            armorBlockBuilder.BlockOrientation = new MyBlockOrientation(
                Base6Directions.Direction.Forward,
                Base6Directions.Direction.Up);

            // Create the block on the grid
            grid.AddBlock(armorBlockBuilder, false);

            // Optionally, check for success and perform actions or notifications
        }


        public override void Close()
        {
            base.Close();
            if (block != null)
            {
                // Unregister the damage handler
                block.SlimBlock.ComponentStack.IsFunctionalChanged -= OnBlockDamaged;
            }
        }

    }
}
