using System;
using System.Collections.Generic;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    FusionParts
{
    /// <summary>
    ///     Represents a single 'arm' (loop) of fusion accelerators.
    /// </summary>
    internal struct S_FusionArm
    {
        private const float LengthEfficiencyModifier = 1 / 40f;
        private const float BlockPowerGeneration = 0.005f;
        private const float BlockPowerStorage = 2f;

        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public readonly bool IsValid;

        public float PowerGeneration { get; }
        public float PowerStorage { get; }

        public IMyCubeBlock[] Parts;

        public S_FusionArm(MyEntity newPart, string rootSubtype)
        {
            var parts = new List<IMyCubeBlock>();
            var stopHits = 0;
            var ignore = new List<MyEntity>();
            IsValid = PerformScan(newPart, ref ignore, rootSubtype, ref stopHits, ref parts);

            PowerGeneration = 0;
            PowerStorage = 0;

            if (!IsValid)
            {
                parts.Clear();
                Parts = Array.Empty<IMyCubeBlock>();
                return;
            }

            foreach (var part in parts)
                switch (part?.BlockDefinition.SubtypeName)
                {
                    case "Caster_Accelerator_90":
                        PowerGeneration += BlockPowerGeneration;
                        break;
                    case "Caster_Accelerator_0":
                        PowerStorage += BlockPowerStorage;
                        break;
                }

            Parts = parts.ToArray();
            parts.Clear();

            // Power capacities scale with length.
            PowerGeneration *= (float)Math.Pow(Parts.Length, LengthEfficiencyModifier);
            PowerStorage *= (float)Math.Pow(Parts.Length, LengthEfficiencyModifier);
        }


        /// <summary>
        ///     Performs a recursive scan for connected blocks in an arm loop.
        /// </summary>
        /// <param name="blockEntity">The block entity to check.</param>
        /// <param name="prevScan">The block entity to ignore; nullable.</param>
        /// <param name="stopAtSubtype">Exits the loop at this subtype.</param>
        /// <returns></returns>
        private static bool PerformScan(MyEntity blockEntity, ref List<MyEntity> prevScan, string stopAtSubtype,
            ref int stopHits,
            ref List<IMyCubeBlock> parts)
        {
            if (ModularAPI.IsDebug())
                DebugDraw.DebugDraw.AddGridPoint(((IMyCubeBlock)blockEntity).Position,
                    ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);
            parts.Add((IMyCubeBlock)blockEntity);

            var connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, false);

            if (connectedBlocks.Length < 2)
                return false;

            foreach (var connectedBlock in connectedBlocks)
            {
                var connectedSubtype = ((IMyCubeBlock)connectedBlock).BlockDefinition.SubtypeName;
                if (connectedSubtype == stopAtSubtype)
                    stopHits++;

                if (!prevScan.Contains(connectedBlock) && connectedSubtype != stopAtSubtype)
                {
                    prevScan.Add(blockEntity);
                    PerformScan(connectedBlock, ref prevScan, stopAtSubtype, ref stopHits, ref parts);
                }
            }

            return stopHits == 2;
        }
    }
}