using System;
using System.Collections.Generic;
using VRageMath;
using static MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication.DefinitionDefs;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    internal partial class ModularDefinition
    {
        // You can declare functions in here, and they are shared between all other ModularDefinition files.

        // This is the important bit.
        internal PhysicalDefinition Modular_Heat => new PhysicalDefinition
        {
            // Unique name of the definition.
            Name = "Modular_Heat",

            OnInit = () => { S_FusionManager.I.HeatDefinition = this; },

            // Triggers whenever a new part is added to an assembly.
            OnPartAdd = null,

            // Triggers whenever a part is removed from an assembly.
            OnPartRemove = null,

            // Triggers whenever a part is destroyed, simultaneously with OnPartRemove
            OnPartDestroy = null,

            // The most important block in an assembly. Connection checking starts here.
            BaseBlock = null,

            // All SubtypeIds that can be part of this assembly.
            AllowedBlocks = new[]
            {
                "Heat_Heatsink",
                "Caster_CentralPipe_0",
                "Caster_CentralPipe_90",
                "Caster_CentralPipe_T",
            },

            // Allowed connection directions & whitelists, measured in blocks.
            // If an allowed SubtypeId is not included here, connections are allowed on all sides.
            // If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            AllowedConnections = new Dictionary<string, Dictionary<Vector3I, string[]>>
            {
                ["Heat_Heatsink"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Up] = Array.Empty<string>(),
                    [Vector3I.Down] = Array.Empty<string>(),
                    [Vector3I.Left] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                },
                ["Caster_CentralPipe_0"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },
                ["Caster_CentralPipe_90"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                },
                ["Caster_CentralPipe_T"] = new Dictionary<Vector3I, string[]>
                {
                    [Vector3I.Forward] = Array.Empty<string>(),
                    [Vector3I.Right] = Array.Empty<string>(),
                    [Vector3I.Backward] = Array.Empty<string>(),
                },
            },
        };
    }
}