using System.Collections.Generic;
using VRageMath;
using static MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication.DefinitionDefs;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    partial class ModularDefinition
    {
        // You can declare functions in here, and they are shared between all other ModularDefinition files.

        // This is the important bit.
        private PhysicalDefinition Modular_Fusion => new PhysicalDefinition
        {
            // Unique name of the definition.
            Name = "Modular_Fusion",

            OnInit = () => { S_FusionManager.I.Definition = this; },

            // Triggers whenever a new part is added to an assembly.
            OnPartAdd = S_FusionManager.I.OnPartAdd,

            // Triggers whenever a part is removed from an assembly.
            OnPartRemove = S_FusionManager.I.OnPartRemove,

            // Triggers whenever a part is destroyed, simultaneously with OnPartRemove
            OnPartDestroy = (PhysicalAssemblyId, BlockEntity, IsBaseBlock) =>
            {
                // You can remove this function, and any others if need be.
            },

            // The most important block in an assembly. Connection checking starts here.
            BaseBlock = "Caster_Controller",

            // All SubtypeIds that can be part of this assembly.
            AllowedBlocks = new[]
            {
                "Caster_FocusLens",
                "Caster_Accelerator_0",
                "Caster_Accelerator_90",
                "Caster_CentralPipe_0",
                "Caster_CentralPipe_90",
                "Caster_CentralPipe_T",
                "Caster_Feeder",
                "Caster_Controller",
                "Caster_Reactor"
            },

            // Allowed connection directions & whitelists, measured in blocks.
            // If an allowed SubtypeId is not included here, connections are allowed on all sides.
            // If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            AllowedConnections = new Dictionary<string, Dictionary<Vector3I, string[]>>
            {
                {
                    // Note - Offsets line up with BuildInfo block orientation.
                    // Note - Offsets are measured from the center of the block; in this case, the Caster_FocusLens is a 3x3 that has connections on the back in a plus shape.
                    "Caster_FocusLens", new Dictionary<Vector3I, string[]>
                    {
                        {
                            new Vector3I(1, 0, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller"
                            }
                        },
                        {
                            new Vector3I(-1, 0, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller"
                            }
                        },
                        {
                            new Vector3I(0, 1, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller"
                            }
                        },
                        {
                            new Vector3I(0, -1, 2), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor",
                                "Caster_Controller"
                            }
                        }
                    }
                },
                {
                    "Caster_Reactor", new Dictionary<Vector3I, string[]>
                    {
                        {
                            new Vector3I(0, 2, 0), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_FocusLens",
                                "Caster_Controller",
                                "Caster_Reactor"
                            }
                        },
                        {
                            new Vector3I(0, -2, 0), new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_FocusLens",
                                "Caster_Controller",
                                "Caster_Reactor"
                            }
                        }
                    }
                },
                {
                    "Caster_Accelerator_0", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        }
                    }
                },
                {
                    "Caster_Accelerator_90", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        },
                        {
                            Vector3I.Right, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        }
                    }
                },
                {
                    "Caster_CentralPipe_0", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        }
                    }
                },
                {
                    "Caster_CentralPipe_90", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        },
                        {
                            Vector3I.Right, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        }
                    }
                },
                {
                    "Caster_CentralPipe_T", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        },
                        {
                            Vector3I.Right, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        }
                    }
                },
                {
                    "Caster_Feeder", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Forward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        },
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Controller",
                                "Caster_FocusLens",
                                "Caster_Reactor"
                            }
                        },

                        {
                            Vector3I.Up, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        },
                        {
                            Vector3I.Down, new[]
                            {
                                "Caster_Accelerator_0",
                                "Caster_Accelerator_90",
                                "Caster_Feeder"
                            }
                        }
                    }
                },
                {
                    "Caster_Controller", new Dictionary<Vector3I, string[]>
                    {
                        {
                            Vector3I.Backward, new[]
                            {
                                "Caster_CentralPipe_0",
                                "Caster_CentralPipe_90",
                                "Caster_CentralPipe_T",
                                "Caster_Feeder",
                                "Caster_Reactor"
                            }
                        }
                    }
                }
            }
        };
    }
}