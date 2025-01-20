using System;
using System.Collections.Generic;
using System.Linq;
using Epstein_Fusion_DS.Communication;
using Epstein_Fusion_DS.HudHelpers;
using VRage.Game.ModAPI;
using VRageMath;

namespace Epstein_Fusion_DS.
    FusionParts
{
    /// <summary>
    ///     Represents a single 'arm' (loop) of fusion accelerators.
    /// </summary>
    internal struct SFusionArm
    {
        private const float LengthEfficiencyModifier = 0f;
        private const float BlockPowerGeneration = 0.023f;
        private const float BlockPowerStorage = 32f;
        private const float SharedPropertyModifier = 0.05f;

        private static ModularDefinitionApi ModularApi => Epstein_Fusion_DS.ModularDefinition.ModularApi;

        public readonly bool IsValid;

        public float PowerGeneration { get; }
        public float PowerStorage { get; }

        public IMyCubeBlock[] Parts;

        public SFusionArm(IMyCubeBlock newPart, string rootSubtype)
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
                        PowerStorage += BlockPowerStorage * SharedPropertyModifier;
                        break;
                    case "Caster_Accelerator_0":
                        PowerStorage += BlockPowerStorage;
                        PowerGeneration += BlockPowerGeneration * SharedPropertyModifier;
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
            if (ModularApi.IsDebug())
                DebugDraw.AddGridPoint(blockEntity.Position,
                    blockEntity.CubeGrid, Color.Blue, 2);

            var connectedBlocks = ModularApi.GetConnectedBlocks(blockEntity, "Modular_Fusion", false);

            if (connectedBlocks.Length < 2)
                return false;

            foreach (var connectedBlock in connectedBlocks)
            {
                var connectedSubtype = connectedBlock.BlockDefinition.SubtypeName;
                var valid = parts.Add(connectedBlock);

                if (connectedSubtype != stopAtSubtype && valid &&
                    !PerformScan(connectedBlock, ref parts, stopAtSubtype))
                    return false;
            }

            return true;
        }
    }
}