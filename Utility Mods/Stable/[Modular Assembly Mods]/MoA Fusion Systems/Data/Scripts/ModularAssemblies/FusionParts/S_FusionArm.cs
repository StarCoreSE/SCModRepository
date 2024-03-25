using Scripts.ModularAssemblies.Communication;
using Scripts.ModularAssemblies.Debug;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace SCModRepository.Utility_Mods.Stable._Modular_Assembly_Mods_.MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    /// <summary>
    /// Represents a single 'arm' (loop) of fusion accelerators.
    /// </summary>
    internal class S_FusionArm : List<MyEntity>
    {
        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public List<IMyCubeBlock> StraightParts = new List<IMyCubeBlock>();
        public List<IMyCubeBlock> CornerParts = new List<IMyCubeBlock>();
        IMyCubeBlock BaseBlock = null;

        public S_FusionArm() { }

        public void AddPart(MyEntity part)
        {
            switch ((part as IMyCubeBlock)?.BlockDefinition.SubtypeName)
            {
                case "Caster_Accelerator_0":
                    StraightParts.Add((IMyCubeBlock) part);
                    break;
                case "Caster_Accelerator_90":
                    CornerParts.Add((IMyCubeBlock) part);
                    break;
            }

            if (part is IMyCubeBlock)
                Add(part);
        }

        int StopHits = 0;
        public bool PerformScan(MyEntity blockEntity, MyEntity prevScan, string StopAt)
        {
            if (ModularAPI.IsDebug())
                DebugDraw.AddGridPoint(((IMyCubeBlock)blockEntity).Position, ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);
            AddPart(blockEntity);

            MyEntity[] connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, false);

            foreach (var connectedBlock in connectedBlocks)
            {
                string connectedSubtype = ((IMyCubeBlock)connectedBlock).BlockDefinition.SubtypeName;
                if (connectedSubtype == StopAt)
                    StopHits++;

                if (connectedBlock != prevScan && connectedSubtype != StopAt)
                {
                    PerformScan(connectedBlock, blockEntity, StopAt);
                }
            }

            return StopHits == 2;
        }
    }
}
