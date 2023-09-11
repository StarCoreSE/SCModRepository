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

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            MyAPIGateway.Utilities.ShowNotification("Update triggered", 1000);
            CreateArmorLine(10);
        }

        private void CreateArmorLine(int length)
        {
            for (int i = 1; i <= length; i++)
            {
                AddBlock("LargeBlockArmorBlock", new Vector3I(0, i, 0));
            }
        }

        private void AddBlock(string subtypeName, Vector3I offset)
        {
            var grid = block.CubeGrid;
            var blockPos = block.Position;
            var nextBlockPos = blockPos + offset;

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

            MyAPIGateway.Utilities.ShowNotification($"{subtypeName} added at {nextBlockPos}", 1000);
        }
    }
}



