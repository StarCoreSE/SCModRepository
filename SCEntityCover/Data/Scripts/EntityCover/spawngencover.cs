using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "LargeBlockSmallGenerator")]
    public class spawngencover : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private bool addGeneratorNext = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            MyAPIGateway.Utilities.ShowNotification("Update triggered", 1000);
            AddNextBlockInChain();
        }

        private void AddNextBlockInChain()
        {
            if (addGeneratorNext)
            {
                AddBlock("LargeBlockSmallGenerator");
            }
            else
            {
                AddBlock("LargeBlockArmorBlock");
            }

            addGeneratorNext = !addGeneratorNext;
        }

        private void AddBlock(string subtypeName)
        {
            var grid = block.CubeGrid;
            var blockPos = block.Position;
            var nextBlockPos = blockPos + new Vector3I(0, 1, 0); // Assuming "up" is along the Y-axis

            var nextBlockBuilder = new MyObjectBuilder_CubeBlock
            {
                SubtypeName = subtypeName,
                Min = nextBlockPos,
                BlockOrientation = new MyBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up),
                ColorMaskHSV = new SerializableVector3(0, -1, 0),
                Owner = block.OwnerId,
                EntityId = 0,
                ShareMode = MyOwnershipShareModeEnum.None
            };

            IMySlimBlock newBlock = grid.AddBlock(nextBlockBuilder, false);

            if (newBlock == null)
            {
                MyAPIGateway.Utilities.ShowNotification($"Failed to add {subtypeName}", 1000);
                return;
            }

            MyAPIGateway.Utilities.ShowNotification($"{subtypeName} added", 1000);
        }
    }
}
