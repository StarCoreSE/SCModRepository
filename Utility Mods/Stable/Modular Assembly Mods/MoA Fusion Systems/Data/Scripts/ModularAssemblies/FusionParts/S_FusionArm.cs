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
        private const float BlockPowerStorage = 4f;

        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public readonly bool IsValid;

        public float PowerGeneration { get; }
        public float PowerStorage { get; }

        public IMyCubeBlock[] Parts;

        public S_FusionArm(MyEntity newPart, string rootSubtype)
        {
            var stopHits = 0;
            var ignore = new HashSet<IMyCubeBlock>();
            IsValid = PerformScan(newPart, ref ignore, rootSubtype, ref stopHits);
            MyAPIGateway.Utilities.ShowNotification(stopHits + " | " + ignore.Count);

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
                        break;
                    case "Caster_Accelerator_0":
                        PowerStorage += BlockPowerStorage;
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
        /// <param name="prevScan">The block entity to ignore; nullable.</param>
        /// <param name="stopAtSubtype">Exits the loop at this subtype.</param>
        /// <returns></returns>
        private static bool PerformScan(MyEntity blockEntity, ref HashSet<IMyCubeBlock> parts, string stopAtSubtype,
            ref int stopHits)
        {
            DebugDraw.DebugDraw.AddGridPoint(((IMyCubeBlock)blockEntity).Position,
                ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);

            var connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, false);

            if (connectedBlocks.Length < 2)
                return false;

            MyAPIGateway.Utilities.ShowNotification(connectedBlocks.Length + "");

            foreach (var connectedBlock in connectedBlocks)
            {
                DebugDraw.DebugDraw.AddGridPoint(((IMyCubeBlock)connectedBlock).Position,
                    ((IMyCubeBlock)connectedBlock).CubeGrid, Color.Red, 2);

                var connectedSubtype = ((IMyCubeBlock)connectedBlock).BlockDefinition.SubtypeName;

                if (connectedSubtype == stopAtSubtype)
                    stopHits++;
                else if (parts.Add((IMyCubeBlock)connectedBlock))
                    PerformScan(connectedBlock, ref parts, stopAtSubtype, ref stopHits);
            }
            
            return stopHits >= 2;
        }
    }
}