﻿using Scripts.ModularAssemblies.Communication;
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

namespace Scripts.ModularAssemblies.FusionParts
{
    /// <summary>
    /// Represents a single 'arm' (loop) of fusion accelerators.
    /// </summary>
    internal struct S_FusionArm
    {
        const float LengthEfficiencyModifier = 0.1f;
        const float BlockPowerGeneration = 0.005f;
        const float BlockPowerStorage = 1f;

        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public readonly bool IsValid;

        public float PowerGeneration { get; private set; }
        public float PowerStorage { get; private set; }

        public IMyCubeBlock[] Parts;

        public S_FusionArm(MyEntity newPart, string rootSubtype)
        {
            List<IMyCubeBlock> parts = new List<IMyCubeBlock>();
            int stopHits = 0;
            IsValid = PerformScan(newPart, null, rootSubtype, ref stopHits, ref parts);

            PowerGeneration = 0;
            PowerStorage = 0;

            if (!IsValid)
            {
                parts.Clear();
                Parts = new IMyCubeBlock[0];
                return;
            }

            foreach (var part in parts)
            {
                switch ((part as IMyCubeBlock)?.BlockDefinition.SubtypeName)
                {
                    case "Caster_Accelerator_90":
                        PowerGeneration += BlockPowerGeneration;
                        break;
                    case "Caster_Accelerator_0":
                        PowerStorage += BlockPowerStorage;
                        break;
                }
            }
            Parts = parts.ToArray();
            parts.Clear();

            // Power capacities scale with length.
            PowerGeneration *= Parts.Length * LengthEfficiencyModifier;
            PowerStorage *= Parts.Length * LengthEfficiencyModifier;
        }


        /// <summary>
        /// Performs a recursive scan for connected blocks in an arm loop.
        /// </summary>
        /// <param name="blockEntity">The block entity to check.</param>
        /// <param name="prevScan">The block entity to ignore; nullable.</param>
        /// <param name="stopAtSubtype">Exits the loop at this subtype.</param>
        /// <returns></returns>
        static bool PerformScan(MyEntity blockEntity, MyEntity prevScan, string stopAtSubtype, ref int stopHits, ref List<IMyCubeBlock> parts)
        {
            if (ModularAPI.IsDebug())
                DebugDraw.AddGridPoint(((IMyCubeBlock)blockEntity).Position, ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);
            parts.Add((IMyCubeBlock) blockEntity);

            MyEntity[] connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, false);

            foreach (var connectedBlock in connectedBlocks)
            {
                string connectedSubtype = ((IMyCubeBlock)connectedBlock).BlockDefinition.SubtypeName;
                if (connectedSubtype == stopAtSubtype)
                    stopHits++;

                if (connectedBlock != prevScan && connectedSubtype != stopAtSubtype)
                {
                    PerformScan(connectedBlock, blockEntity, stopAtSubtype, ref stopHits, ref parts);
                }
            }

            return stopHits == 2;
        }
    }
}