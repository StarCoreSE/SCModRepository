using System;
using System.Collections.Generic;
using System.Linq;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using Sandbox.ModAPI;
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
        private const float LengthEfficiencyModifier = 0.13f;
        private const float BlockPowerGeneration = 0.01f;
        private const float BlockPowerStorage = 16f;

        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public readonly bool IsValid;

        public float PowerGeneration { get; }
        public float PowerStorage { get; }

        public IMyCubeBlock[] Parts;

        public S_FusionArm(IMyCubeBlock newPart, string rootSubtype)
        {
            var ignore = new HashSet<IMyCubeBlock>();
            IsValid = PerformScan(newPart, ref ignore, rootSubtype);

            PowerGeneration = 0;
            PowerStorage = 0;

            if (!IsValid)
            {
                ignore.Clear();
                Parts = Array.Empty<IMyCubeBlock>();
                return;
            }

            foreach (var part in ignore)
                switch (part?.BlockDefinition.SubtypeName)
                {
                    case "Caster_Accelerator_90":
                        PowerGeneration += BlockPowerGeneration;
                        PowerStorage += BlockPowerStorage * 0.05f;
                        break;
                    case "Caster_Accelerator_0":
                        PowerStorage += BlockPowerStorage;
                        PowerGeneration += BlockPowerGeneration * 0.05f;
                        break;
                }

            Parts = ignore.ToArray();
            ignore.Clear();

            // Power capacities scale with length.
            PowerGeneration *= (float)Math.Pow(Parts.Length, LengthEfficiencyModifier);
            PowerStorage *= (float)Math.Pow(Parts.Length, LengthEfficiencyModifier);
        }


        /// <summary>
        ///     Performs a recursive scan for connected blocks in an arm loop.
        /// </summary>
        /// <param name="blockEntity">The block entity to check.</param>
        /// <param name="parts">Blocks determined to be part of the arm.</param>
        /// <param name="stopAtSubtype">Exits the loop at this subtype.</param>
        /// <param name="stopHits">Internal variable.</param>
        /// <returns></returns>
        private static bool PerformScan(IMyCubeBlock blockEntity, ref HashSet<IMyCubeBlock> parts, string stopAtSubtype)
        {
            if (ModularAPI.IsDebug())
                DebugDraw.DebugDraw.AddGridPoint(blockEntity.Position,
                    blockEntity.CubeGrid, Color.Blue, 2);

            var connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, "Modular_Fusion", false);

            if (connectedBlocks.Length < 2)
                return false;

            foreach (var connectedBlock in connectedBlocks)
            {
                var connectedSubtype = connectedBlock.BlockDefinition.SubtypeName;
                bool valid = parts.Add(connectedBlock);

                if (connectedSubtype != stopAtSubtype && valid && !PerformScan(connectedBlock, ref parts, stopAtSubtype))
                    return false;
            }
            
            return true;
        }
    }
}